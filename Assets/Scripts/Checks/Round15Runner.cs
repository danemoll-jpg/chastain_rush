using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 15 checks in the exe (ChastainRush.exe -exeRound15 <dir> [-r15Only water|race|shortcuts]); muted like every check
    // (CheckRun), its files in <dir>.
    //   water      item 2e: the Street Classic and the Needle 600 driven at full throttle into the neighborhood pond, straight
    //              across and out the far side: how deep each sat, the speed going in and across, that both got out, and that
    //              a reset in the water puts the vehicle on dry ground
    //   sounds     items 3 and 4: every kind of breakable breaks with its sound (counted, not heard: muted), the drop-in
    //              folders (run with -soundsRoot <test dir> holding goose_honk_test.wav and wood_test.wav; never Dan's folder)
    //   shortcuts  item 10: each vehicle as an AI driver sent down each official shortcut once (RoadDriver, forced to take it)
    //   race       item 13: a 3-lap race with three AI drivers and a fast scripted player taking both shortcuts
    public sealed partial class Round15Runner : MonoBehaviour
    {
        public string outputDir;
        readonly StringBuilder report = new StringBuilder();
        int fails, checks;
        ArcadeVehicle car; VehicleSwitch sw;
        readonly StringBuilder trace = new StringBuilder("vehicle,t,x,z,y,speed,wheels,immersion,tilt,touching\n");

        void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}"); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            string only = null;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-r15Only") only = args[i + 1];
            report.AppendLine($"Round 15 check, {DateTime.Now:yyyy-MM-dd HH:mm}, the exe, {Screen.width} x {Screen.height}. Sound muted throughout (AudioListener.volume {AudioListener.volume:0}).");
            yield return new WaitForSecondsRealtime(4f);
            car = FindAnyObjectByType<ArcadeVehicle>(); sw = car.GetComponent<VehicleSwitch>();
            if (only == null || only == "water") yield return Water();
            if (only == null || only == "sounds") yield return Sounds();
            yield return PartB(only);
            report.AppendLine();
            report.AppendLine(fails == 0 ? $"ALL {checks} CHECKS PASSED" : $"{fails} OF {checks} CHECKS FAILED");
            File.WriteAllText(Path.Combine(outputDir, "round15_check.txt"), report.ToString());
            if (trace.Length > 60) File.WriteAllText(Path.Combine(outputDir, "water_exit_trace.csv"), trace.ToString());
            Debug.Log("[Round15]\n" + report);
            Application.Quit(fails == 0 ? 0 : 1);
        }

        IEnumerator PartB(string only) { yield break; }   // Part B (AI drivers) comes in its own commit

        // ---------------------------------------------------------------- water (item 2e)

        IEnumerator Water()
        {
            report.AppendLine();
            report.AppendLine("=== Water: the neighborhood pond, driven straight across at full throttle");
            var pond = FindObjectsByType<ShallowWater>().Where(w => w.outline != null && w.outline.Length > 2)
                .OrderBy(w => Vector2.Distance(Centroid(w.outline), new Vector2(2819f, 858f))).FirstOrDefault();
            if (pond == null) { Check("the pond is registered as water", false); yield break; }
            var c = Centroid(pond.outline);
            // the line across: the chord through the middle whose banks (25 m beyond each shore) are clearest and flattest
            Vector2 bestA = default, bestB = default; float bestScore = float.MaxValue; Vector2 bestDir = default;
            for (int k = 0; k < 24; k++)
            {
                float a = k * Mathf.PI / 12f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                if (!Edge(pond, c, -d, out var A) || !Edge(pond, c, d, out var B)) continue;
                float score = 0f; var hitsAt = new List<string>();
                // tree trunks are colliders only near the car (TreeTrunks): put them round each bank before looking
                foreach (var bank in new[] { A, B })
                {
                    if (TreeTrunks.Instance) TreeTrunks.Instance.Refresh(new Vector3(bank.x, 0f, bank.y));
                    Physics.SyncTransforms();
                    for (float t = 0f; t <= 30f; t += 2f)
                    {
                        var p = bank == A ? A - d * t : B + d * t; var g = RaceRoute.Ground(p.x, p.y);
                        foreach (var hc in Physics.OverlapBox(g + Vector3.up * 1.2f, new Vector3(1.6f, 0.8f, 1.6f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                            if (!(hc is TerrainCollider) && hc.attachedRigidbody == null) { score += 100f; hitsAt.Add($"{hc.name}@({p.x:0},{p.y:0})"); }
                    }
                }
                for (float t = -28f; t <= 28f; t += 2f)
                {
                    if (t > -3f && t < 3f) continue;
                    var p = t < 0 ? A + d * t : B + d * t;
                    var g = RaceRoute.Ground(p.x, p.y);
                    var g2 = RaceRoute.Ground(p.x + d.x * 2f, p.y + d.y * 2f);
                    score += Mathf.Abs(g2.y - g.y) * 10f;
                }
                score += Mathf.Abs((B - A).magnitude - 45f) * 0.2f;
                report.AppendLine($"    line {k * 15} deg: ({A.x:0},{A.y:0}) to ({B.x:0},{B.y:0}), score {score:0}{(hitsAt.Count > 0 ? ", blocked by " + string.Join(" ", hitsAt.Distinct().Take(4)) : "")}");
                if (score < bestScore) { bestScore = score; bestA = A; bestB = B; bestDir = d; }
            }
            report.AppendLine($"  pond surface {pond.surfaceY:0.00} m; line from ({bestA.x:0},{bestA.y:0}) to ({bestB.x:0},{bestB.y:0}), {(bestB - bestA).magnitude:0} m of water");
            foreach (var id in new[] { "original", "moto" })
            {
                sw.Select(id); yield return new WaitForSecondsRealtime(0.5f);
                yield return DriveAcross(id, pond, bestA, bestB, bestDir);
            }
            sw.Select("original");
        }

        // ---------------------------------------------------------------- sounds (items 3 and 4)

        IEnumerator Sounds()
        {
            report.AppendLine();
            report.AppendLine("=== Break sounds and the drop-in folders (counted, not heard: the check is muted)");
            var d = SoundDropIns.Instance;
            float wait = 0f; while (d != null && !d.Loaded && wait < 10f) { wait += Time.unscaledDeltaTime; yield return null; }
            if (d == null) { Check("sound drop-ins loaded", false, "no SoundDropIns"); yield break; }
            report.AppendLine($"  drop-in folder: {SoundDropIns.Root}");
            Check("drop-in folders made with a READ ME", File.Exists(Path.Combine(SoundDropIns.Root, "Geese", "READ ME.txt")) && File.Exists(Path.Combine(SoundDropIns.Root, "Breaks", "READ ME.txt")));
            int woods = d.Breaks.TryGetValue("wood", out var wl) ? wl.Count : 0;
            report.AppendLine($"  read: {d.Honks.Count} goose honk(s) ({string.Join(", ", d.Honks.Select(c => c.name))}), wood {woods}");
            var flock = FindAnyObjectByType<GooseFlock>();
            Check("a dropped-in honk reaches the geese", flock != null && flock.honkClips.Any(c => c && c.name.StartsWith("goose_honk")) && flock.VoiceCount > 0, flock ? $"{flock.honkClips.Length} clip(s), {flock.VoiceCount} voices" : "no flock");
            // one breakable of every kind, broken by the car going past at 12 m/s (Hit, as its trigger calls it)
            var respawn = car.GetComponent<VehicleRespawn>();
            foreach (BreakSounds.Kind kind in Enum.GetValues(typeof(BreakSounds.Kind)))
            {
                var chunk = FindObjectsByType<FenceChunk>().FirstOrDefault(c => c.sound == kind && c.panels.Length > 0);
                if (chunk == null) { Check($"break sound: {kind}", false, "no breakable of this kind in the scene"); continue; }
                var at = chunk.transform.TransformPoint(chunk.panels[0].centre);
                var g = RaceRoute.Ground(at.x + 4f, at.z);
                respawn.PlaceAt(g + Vector3.up * 0.3f, 0f);
                yield return new WaitForSecondsRealtime(0.4f);
                int before = BreakSounds.Played;
                car.Body.linearVelocity = car.transform.forward * 12f;
                chunk.Hit(0, car);
                yield return null;
                var voices = FindObjectsByType<AudioSource>().Where(a => a.isPlaying && a.name.StartsWith("Break voice")).Select(a => a.clip ? a.clip.name : "?").ToList();
                Check($"break sound: {kind} ({chunk.name})", BreakSounds.Played == before + 1 && voices.Count > 0, $"playing: {string.Join(", ", voices)}");
                if (kind == BreakSounds.Kind.Hydrant)
                {
                    yield return null;
                    var rush = FindObjectsByType<AudioSource>().FirstOrDefault(a => a.isPlaying && a.loop && a.clip && (a.clip.name == "Hydrant rush" || a.clip.name.StartsWith("water")));
                    Check("hydrant: the rush of water plays while it runs", rush != null, rush ? rush.clip.name : "none");
                }
            }
            FenceChunk.RestoreAll();
        }

        static Vector2 Centroid(Vector2[] r) { var s = Vector2.zero; foreach (var p in r) s += p; return s / r.Length; }

        // where the ray from c along d leaves the outline
        static bool Edge(ShallowWater w, Vector2 c, Vector2 d, out Vector2 at)
        {
            at = c; bool inside = true;
            for (float t = 0f; t < 200f; t += 0.5f)
            {
                var p = c + d * t;
                bool now = w.Contains(new Vector3(p.x, 0f, p.y));
                if (inside && !now) { at = p; return t > 5f; }
                inside = now;
            }
            return false;
        }

        IEnumerator DriveAcross(string id, ShallowWater pond, Vector2 A, Vector2 B, Vector2 d)
        {
            var cfg = car.GetComponent<VehicleConfiguration>(); string name = cfg.Profile.Name;
            var respawn = car.GetComponent<VehicleRespawn>();
            var start = A - d * 30f; var g = RaceRoute.Ground(start.x, start.y);
            respawn.PlaceAt(g + Vector3.up * 0.3f, Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg);
            yield return new WaitForSecondsRealtime(1.0f);
            car.enabled = false;
            float time = 0f, vIn = -1f, vMin = float.MaxValue, vSum = 0f, wetTime = 0f, maxImm = 0f, maxDepth = 0f, maxBodyIn = 0f, height = VehicleHeight();
            float bodyBottom = BodyBottom();
            bool shot = false, gotOut = false; Vector3 midPos = default; float outAt = 0f; int splashIn = 0;
            var fb = car.GetComponent<WaterFeedback>(); int entry0 = fb ? fb.EntrySounds : 0;
            while (time < 40f)
            {
                yield return new WaitForFixedUpdate();
                float dt = Time.fixedDeltaTime; time += dt;
                var pos = car.Body.position; var q = new Vector2(pos.x, pos.z);
                // steer along the line: heading error plus a cross-track term (as the shortcut check steers)
                var dir3 = new Vector3(d.x, 0f, d.y); var fwd3 = Vector3.ProjectOnPlane(car.transform.forward, Vector3.up).normalized;
                float cte = Vector3.Dot(pos - new Vector3(A.x, pos.y, A.y), Vector3.Cross(Vector3.up, dir3));   // + = right of the line
                float head = Vector3.SignedAngle(fwd3, dir3, Vector3.up);
                float corr = -Mathf.Atan2(1.6f * cte, Mathf.Max(car.ForwardSpeed, 1f) + 2f) * Mathf.Rad2Deg;
                float steer = Mathf.Clamp((head + corr) / 18f, -1f, 1f);
                car.Simulate(1f, 0f, steer, dt);
                float v = car.Body.linearVelocity.magnitude;
                if (car.WaterImmersion > 0f)
                {
                    if (vIn < 0f) vIn = v;
                    wetTime += dt; vSum += v * dt; vMin = Mathf.Min(vMin, v); maxImm = Mathf.Max(maxImm, car.WaterImmersion);
                    var ground = RaceRoute.Ground(pos.x, pos.z).y;
                    maxDepth = Mathf.Max(maxDepth, pond.Surface - ground);
                    maxBodyIn = Mathf.Max(maxBodyIn, pond.Surface - (ground + bodyBottom));
                    if (!shot && Vector2.Dot(q - A, d) > (B - A).magnitude * 0.5f) { shot = true; midPos = pos; }
                }
                if (Vector2.Dot(q - B, d) > -4f && (trace.Length < 20000) && ((int)(time / dt)) % 10 == 0)
                {
                    var hitNames = Physics.OverlapBox(car.GetComponent<BoxCollider>().bounds.center, car.GetComponent<BoxCollider>().size * 0.5f + Vector3.one * 0.15f, car.transform.rotation, ~0, QueryTriggerInteraction.Ignore)
                        .Where(cc => !(cc is TerrainCollider) && cc.attachedRigidbody != car.Body).Select(cc => cc.name);
                    trace.AppendLine($"{id},{time:0.00},{pos.x:0.00},{pos.z:0.00},{pos.y:0.00},{car.Body.linearVelocity.magnitude:0.0},{car.GroundedWheels},{car.WaterImmersion:0.00},{Vector3.Angle(car.transform.up, Vector3.up):0},{string.Join(" ", hitNames)}");
                }
                if (Vector2.Dot(q - B, d) > 10f && car.WaterImmersion <= 0f) { gotOut = true; outAt = time; break; }
            }
            car.enabled = true;
            splashIn = fb ? fb.EntrySounds - entry0 : 0;
            if (shot)   // pictures: parked in the middle of the pond, from the side and as the player sees it
            {
                var mp = new Vector3(midPos.x, RaceRoute.Ground(midPos.x, midPos.z).y + 0.8f, midPos.z); var mr = Quaternion.LookRotation(new Vector3(d.x, 0f, d.y));
                car.Body.position = mp; car.Body.rotation = mr; car.transform.SetPositionAndRotation(mp, mr); car.Body.linearVelocity = Vector3.zero;   // not PlaceAt: that would make it the safe spot
                yield return new WaitForSecondsRealtime(2.0f);
                Shoot($"r15_water_{id}_side", car.Body.position, d);
                yield return CaptureScreen($"r15_water_{id}_chase");
                yield return new WaitForSecondsRealtime(0.5f);
            }
            float mean = wetTime > 0f ? vSum / wetTime : 0f;
            report.AppendLine($"  {name}: water {maxDepth:0.00} m deep at the deepest point it drove (wheel bottoms on the bed); the water reached {Mathf.Max(0f, maxBodyIn):0.00} m up the body (body bottom {bodyBottom:0.00} m over the ground, vehicle {height:0.00} m tall)");
            report.AppendLine($"     speed going in {vIn:0.0} m/s, across: lowest {(vMin < float.MaxValue ? vMin : 0f):0.0}, mean {mean:0.0} m/s; full immersion {maxImm * 100f:0}%; {wetTime:0.0} s in the water; splash sounds on entry {splashIn}");
            Check($"{name} drove in and out of the pond", gotOut, gotOut ? $"out on the far bank after {outAt:0.0} s" : $"still in or stuck after 40 s at ({car.Body.position.x:0},{car.Body.position.z:0})");
            Check($"{name} sat part-way in, not under", maxDepth > 0.3f && maxDepth < 0.75f && maxBodyIn < height * 0.6f, $"depth {maxDepth:0.00} m, top of the vehicle {height:0.00} m");
            Check($"{name} slowed in the water", vIn > 0f && mean < vIn * 0.85f, $"in {vIn:0.0}, mean across {mean:0.0} m/s");
            // a reset in the middle of the pond lands on dry ground
            var mid = (A + B) * 0.5f; var mg = RaceRoute.Ground(mid.x, mid.y);
            car.Body.position = new Vector3(mid.x, mg.y + 0.7f, mid.y); car.Body.linearVelocity = Vector3.zero; car.transform.position = car.Body.position;
            yield return new WaitForSecondsRealtime(1.5f);
            Vector3 before = car.Body.position;
            if (CarRecovery.Find(respawn, out var rp, out var ry)) respawn.PlaceAt(rp, ry); else respawn.PlaceAt(respawn.SafePosition, respawn.SafeYaw);
            yield return new WaitForSecondsRealtime(0.5f);
            bool dry = !ShallowWater.InWater(car.Body.position);
            Check($"{name}: a reset in the pond puts it on dry ground", dry, $"from ({before.x:0},{before.z:0}) to ({car.Body.position.x:0},{car.Body.position.z:0})");
        }

        // the game as it is shown (the HUD included), at the end of a frame
        IEnumerator CaptureScreen(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }

        float VehicleHeight()
        {
            var rs = car.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is LineRenderer)).ToArray();
            if (rs.Length == 0) return 1.4f;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var ground = RaceRoute.Ground(car.Body.position.x, car.Body.position.z).y;
            return b.max.y - ground;
        }

        float BodyBottom()
        {
            var box = car.GetComponent<BoxCollider>();
            var ground = RaceRoute.Ground(car.Body.position.x, car.Body.position.z).y;
            return box.bounds.min.y - ground;
        }

        void Shoot(string name, Vector3 at, Vector2 d)
        {
            var side = new Vector3(d.y, 0f, -d.x);
            var go = new GameObject("Round15 camera"); var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 40f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 1500f;
            go.transform.position = at + side * 9f + Vector3.up * 1.6f; go.transform.LookAt(at + Vector3.up * 0.4f);
            int w = 1920, h = 1080; var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex); rt.Release(); Destroy(go);
        }
    }
}
