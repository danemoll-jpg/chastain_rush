using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 72 (Dan, 2026-10-26: "Quarry stunt jumps"; 2026-10-25: "stunt races that have large ramps to jump you over evel knievel style"): the Viking
    // Quarry jumps as Free Roam events. Three steel kickers on the quarry's south rim (MapBuilder.QuarryJumps.cs, Tools/mapgen/make_quarry_jumps.py),
    // each with a start gantry at its run-up's head, a landing deck across the lake and bleachers.
    //   The start: stop in the gantry's box facing the kicker (any ground vehicle; not a flying one) and hold still a moment: a fanfare and a 3-2-1-GO
    //   on the gantry's lights. The speed check: the HUD shows the jump's lip speed window (Evel Knievel's: too slow and you are short, in the lake;
    //   too fast and you overshoot the landing deck) and your speed against it, green inside it.
    //   The jump (a real one: the kicker is a held steel deck, nothing aims the flight): measured from the lip to where it comes down: the distance,
    //   the height over the lip, the air time, and the landing: CLEAN (on the landing deck, on its wheels, upright), ROUGH (down off the deck, or
    //   bounced, or not upright), CRASH (wiped out) or SPLASH (in the lake). Clean and rough jumps go on the jump's Top 10 by distance
    //   (quarry-jumps-v1.json beside the race records; a check's own folder in a check). The crowd roars on a landing.
    //   After a splash or a crash the vehicle is put back at the gantry after 3 s. L3 / K in the next 12 s after a jump watches its replay (Round 57's
    //   viewer: TV cameras on the rims, the kicker and the landing deck).
    public sealed class QuarryJumps : MonoBehaviour
    {
        [Serializable] public class Spot { public float[] at; public float yaw; }
        [Serializable] public class Window { public float lo, hi; public float[] lands; }
        [Serializable] public class Jump
        {
            public string id, name; public float[] origin, dir, deck_s, deck_y, land_s, land_y, start; public float half, lip_s, lip_y, land_half, water_y, start_yaw, runout_to;
            public Window window; public Spot gantry; public Spot[] stands;
        }
        [Serializable] public class File_ { public Jump[] jumps; }
        [TextArea] public string json;   // Data/tracks/quarry_jumps.json as it was at the map build
        public static QuarryJumps Instance { get; private set; }
        public Jump[] Jumps { get; private set; } = new Jump[0];
        public Transform[] lights;       // per jump: the gantry's three lamps (red, red, green), lit by the countdown (the builder sets them)

        public enum Phase { Idle, Hold, Countdown, Run, Air, Result }
        public Phase State { get; private set; }
        public Jump Current { get; private set; }
        public string Result { get; private set; } = "";
        public float LastDistance, LastHeight, LastAir, LastLipSpeed; public string LastLanding = ""; public int LastRank;
        float t0, holdFor, airAt, apex, resultUntil, putBackAt = -1f, replayUntil;
        Vector3 lipPoint; bool overLip; ArcadeVehicle car;
        readonly List<ReplayData.Frame> frames = new List<ReplayData.Frame>(); float recStart;

        void Awake() { Instance = this; try { Jumps = JsonUtility.FromJson<File_>(json)?.jumps ?? new Jump[0]; } catch (Exception e) { Debug.LogWarning("[Round72] quarry jumps unreadable: " + e.Message); } }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public static Vector3 At(Jump j, float s, float lat = 0f) { var q = new Vector2(j.origin[0], j.origin[1]) + new Vector2(j.dir[0], j.dir[1]) * s + new Vector2(-j.dir[1], j.dir[0]) * lat; return new Vector3(q.x, 0f, q.y); }
        public static Vector3 Forward(Jump j) => new Vector3(j.dir[0], 0f, j.dir[1]);
        public static Vector3 StartPoint(Jump j) { var p = new Vector3(j.start[0], 0f, j.start[1]); p.y = GroundQuery.Height(p.x, p.z); return p; }
        float Along(Jump j, Vector3 p) => Vector3.Dot(new Vector3(p.x - j.origin[0], 0f, p.z - j.origin[1]), Forward(j));
        float Across(Jump j, Vector3 p) => Vector3.Dot(new Vector3(p.x - j.origin[0], 0f, p.z - j.origin[1]), new Vector3(-j.dir[1], 0f, j.dir[0]));
        float LipY(Jump j) => j.lip_y;
        static bool Allowed(ArcadeVehicle c)
        {
            if (!c || !PoliceKit.FreeRoam || SplitScreen.Active || PoliceSession.Running || StuntEvent.Current || TrailerMode.Active || ReplayViewer.Active) return false;
            var cfg = c.GetComponent<VehicleConfiguration>(); return cfg && !cfg.Profile.Flies;
        }

        void FixedUpdate()
        {
            car = PoliceKit.Player(1);
            if (!Allowed(car)) { if (State != Phase.Idle && State != Phase.Result) Abandon("left the event"); return; }
            var p = car.Body.position; var v = car.Body.linearVelocity; float dt = Time.fixedDeltaTime;
            if (State == Phase.Idle || State == Phase.Result)
            {
                if (putBackAt > 0f && Time.time >= putBackAt) { putBackAt = -1f; PutAtStart(Current); }
                // into a gantry's box, facing the kicker, still: hold, then the countdown
                Current = State == Phase.Result ? Current : null;
                foreach (var j in Jumps)
                {
                    var sp = StartPoint(j); var d = p - sp; d.y = 0f;
                    if (d.magnitude > 9f || Vector3.Dot(car.transform.forward, Forward(j)) < .85f || v.magnitude > 1.5f || car.GroundedWheels == 0) continue;
                    if (State == Phase.Result && Time.time < resultUntil - 6f) break;   // the result stands a moment first
                    Current = j; State = Phase.Hold; holdFor = 0f; break;
                }
                return;
            }
            var J = Current;
            if (State == Phase.Hold)
            {
                if ((p - StartPoint(J)).magnitude > 10f || v.magnitude > 1.5f) { State = Phase.Idle; Current = null; Light(J, 0); return; }
                holdFor += dt;
                if (holdFor > .8f) { State = Phase.Countdown; t0 = Time.time; Fanfare(); Debug.Log($"[Round72] {J.name}: countdown"); }
                return;
            }
            if (State == Phase.Countdown)
            {
                car.Body.linearVelocity = new Vector3(0f, Mathf.Min(0f, v.y), 0f);   // held on the line until GO
                float c = Time.time - t0; Light(J, c < 1f ? 1 : c < 2f ? 2 : 3);
                if (c >= 3f) { State = Phase.Run; Light(J, 3); overLip = false; frames.Clear(); recStart = Time.time; Debug.Log($"[Round72] {J.name}: GO"); }
                return;
            }
            Record();
            float s = Along(J, p);
            if (State == Phase.Run)
            {
                if (s < -20f || Mathf.Abs(Across(J, p)) > 40f || Time.time - t0 > 40f) { Abandon("off the run-up"); return; }
                if (s >= J.lip_s && !overLip)
                {
                    overLip = true; LastLipSpeed = v.magnitude; lipPoint = p; apex = p.y; airAt = Time.time; State = Phase.Air;
                    Debug.Log($"[Round72] {J.name}: off the lip at {LastLipSpeed:0.0} m/s (window {J.window.lo:0.0} to {J.window.hi:0.0})");
                }
                return;
            }
            if (State == Phase.Air)
            {
                apex = Mathf.Max(apex, p.y);
                float air = Time.time - airAt;
                bool inWater = car.WaterImmersion > .05f || p.y < J.water_y + .8f;
                bool down = air > .4f && (car.GroundedWheels > 0 || BodyDown(p));
                if (inWater || down || air > 20f) Land(J, p, air, inWater);
            }
        }

        // the body down on something (not the vehicle's own colliders): a landing on its side or roof, which turns no wheel
        static readonly RaycastHit[] downHits = new RaycastHit[8];
        bool BodyDown(Vector3 p)
        {
            int n = Physics.RaycastNonAlloc(p + Vector3.up * .5f, Vector3.down, downHits, 1.6f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (downHits[i].rigidbody != car.Body) return true;
            return false;
        }

        void Land(Jump J, Vector3 p, float air, bool water)
        {
            var cfg = car.GetComponent<VehicleConfiguration>();
            float s = Along(J, p), lat = Mathf.Abs(Across(J, p));
            bool onDeck = J.land_s != null && J.land_s.Length > 1 && s >= J.land_s[0] - 1f && s <= J.land_s[J.land_s.Length - 1] + 1f && lat <= J.land_half + .5f;
            LastDistance = Vector3.ProjectOnPlane(p - lipPoint, Vector3.up).magnitude; LastHeight = apex - J.lip_y; LastAir = air;
            string landing = water ? "SPLASH" : cfg && cfg.WipedOut ? "CRASH" : onDeck && car.transform.up.y > .75f ? "CLEAN" : "ROUGH";
            // a rough landing that wipes out in the next moment is a crash: looked at again shortly
            LastLanding = landing; State = Phase.Result; resultUntil = Time.time + 9f; replayUntil = Time.time + 12f; LastRank = 0;
            if (landing == "CLEAN" || landing == "ROUGH") StartCoroutine(Settle(J));
            else { putBackAt = Time.time + 3f; Finish(J); }
            Debug.Log($"[Round72] {J.name}: {landing}, {LastDistance:0.0} m, +{LastHeight:0.0} m over the lip, {LastAir:0.00} s in the air, lip {LastLipSpeed:0.0} m/s, landed {s - J.lip_s:0} m out ({(onDeck ? "on the deck" : "off it")})");
        }
        System.Collections.IEnumerator Settle(Jump J)
        {
            var cfg = car.GetComponent<VehicleConfiguration>();
            for (float t = 0f; t < 1.2f; t += Time.deltaTime) { if (cfg && cfg.WipedOut) { LastLanding = "CRASH"; putBackAt = Time.time + 2.5f; break; } yield return null; }
            Cheer(LastLanding == "CLEAN" ? 1f : .6f);
            Finish(J);
        }
        void Finish(Jump J)
        {
            if (LastLanding == "CLEAN" || LastLanding == "ROUGH") LastRank = Add(J, car, LastDistance, LastHeight, LastAir, LastLanding);
            Result = $"{J.name.ToUpper()}   {LastLanding}\n{DisplayUnits.Jump(LastDistance)}   +{DisplayUnits.Feet(Mathf.Max(0f, LastHeight)):0} ft over the lip   {LastAir:0.0} s in the air   off the lip at {DisplayUnits.Mph(LastLipSpeed):0} mph" +
                     (LastRank > 0 ? $"\nTOP 10  #{LastRank}" : LastLanding == "SPLASH" ? "\nShort: into the lake. Back to the gantry." : LastLanding == "CRASH" ? "\nCrashed. Back to the gantry." : "");
            Light(J, 0);
        }
        void Abandon(string why) { Debug.Log($"[Round72] {(Current != null ? Current.name : "")}: {why}"); if (Current != null) Light(Current, 0); State = Phase.Idle; Current = null; }

        public void PutAtStart(Jump j) { if (j == null || !car) return; PoliceKit.Put(car, StartPoint(j), Forward(j)); }

        // ---------------------------------------------------------------- the Top 10 (by distance; clean and rough landings)
        [Serializable] public class Entry { public string jump, vehicle, name, landing, date; public float distance, height, air; }
        [Serializable] class Board { public List<Entry> entries = new List<Entry>(); }
        Board board;
        public static string BoardPath => Path.Combine(Path.GetDirectoryName(RaceController.RecordsPath()), "quarry-jumps-v1.json");
        Board Load() { if (board != null) return board; try { board = System.IO.File.Exists(BoardPath) ? JsonUtility.FromJson<Board>(System.IO.File.ReadAllText(BoardPath)) ?? new Board() : new Board(); } catch (Exception) { board = new Board(); } return board; }
        public List<Entry> Top(string jump) => Load().entries.Where(e => e.jump == jump).OrderByDescending(e => e.distance).Take(10).ToList();
        int Add(Jump j, ArcadeVehicle c, float dist, float height, float air, string landing)
        {
            var b = Load(); var cfg = c.GetComponent<VehicleConfiguration>();
            var e = new Entry { jump = j.id, vehicle = cfg ? cfg.profileId : "original", name = PlayerNames.Player, landing = landing, date = DateTime.UtcNow.ToString("o"), distance = dist, height = height, air = air };
            b.entries.Add(e);
            var top = Top(j.id); int rank = top.IndexOf(e) + 1;
            b.entries = b.entries.Where(x => x.jump != j.id || top.Contains(x)).ToList();   // each jump keeps its ten
            try { Directory.CreateDirectory(Path.GetDirectoryName(BoardPath)); System.IO.File.WriteAllText(BoardPath, JsonUtility.ToJson(b, true)); } catch (Exception ex) { Debug.LogWarning("[Round72] the Top 10 could not be saved: " + ex.Message); }
            return rank;
        }

        // ---------------------------------------------------------------- the replay of the jump (Round 57's viewer)
        void Record()
        {
            if (!car || frames.Count > 6000) return;
            float t = Time.time - recStart; if (frames.Count > 0 && t - frames[frames.Count - 1].t < .05f) return;
            frames.Add(new ReplayData.Frame { t = t, p = car.Body.position, q = car.Body.rotation, speed = Vector3.Dot(car.Body.linearVelocity, car.transform.forward), steer = 0f, throttle = 1f, wheels = (byte)car.GroundedWheels });
        }
        void LateUpdate()
        {
            // a few seconds of the landing go on the recording too
            if (State == Phase.Result && Time.time < resultUntil - 6f) Record();
        }
        public bool CanWatch => State == Phase.Result && Time.time < replayUntil && frames.Count > 10 && !ReplayViewer.Active;
        public void Watch()
        {
            if (!CanWatch || Current == null) return;
            var cfg = car.GetComponent<VehicleConfiguration>(); var J = Current;
            var d = new ReplayData { kind = "jump", routeName = "Viking Quarry: " + J.name, recordName = J.name, laps = 1, t0 = 0f, t1 = frames[frames.Count - 1].t, focus = 0, date = DateTime.UtcNow.ToString("o") };
            var r = new ReplayData.Racer { name = PlayerNames.Player, vehicle = cfg ? cfg.profileId : "original", player = true, seat = 1 };
            r.track.frames.AddRange(frames); d.racers.Add(r);
            // TV cameras: by the kicker, on each rim beside the flight, at the landing deck, from the bleachers
            var tv = new List<Vector3>();
            foreach (var (s, lat, up) in new[] { (J.lip_s - 60f, 18f, 4f), (J.lip_s + 10f, -30f, 8f), (J.lip_s + (J.land_s[0] - J.lip_s) * .5f, 120f, 30f), (J.land_s[0] - 10f, 26f, 6f), (J.land_s[J.land_s.Length - 1] + 30f, -24f, 5f) })
            { var q = At(J, s, lat); float gy = GroundQuery.Height(q.x, q.z); q.y = Mathf.Max(float.IsNaN(gy) ? J.lip_y : gy + up, J.water_y + up); tv.Add(q); }
            ReplayViewer.ExtraTv = tv;
            ReplayViewer.Open(d, () => ReplayViewer.ExtraTv = null);
            replayUntil = 0f;
        }

        // ---------------------------------------------------------------- lights, fanfare, crowd
        void Light(Jump j, int stage)
        {
            // the race's own gate materials (the scene's): red for the two reds, green for GO, black for a lamp that is off
            int k = Array.IndexOf(Jumps, j); var race = RaceController.Instance; if (lights == null || k < 0 || 3 * k + 2 >= lights.Length || !race) return;
            for (int i = 0; i < 3; i++)
            {
                var l = lights[3 * k + i]; var rd = l ? l.GetComponent<Renderer>() : null; if (!rd) continue;
                bool on = stage == 3 ? i == 2 : i < stage && i < 2;
                rd.sharedMaterial = !on ? race.gateBlack : i == 2 ? race.nextMaterial : race.missedMaterial;
            }
        }
        void Start() { foreach (var j in Jumps) Light(j, 0); }
        AudioSource src; static AudioClip fanfare, roar;
        AudioSource Src() { if (!src) { src = gameObject.AddComponent<AudioSource>(); src.spatialBlend = 0f; src.playOnAwake = false; src.priority = 40; } return src; }
        void Fanfare()
        {
            if (!fanfare)
            {
                // a brass fanfare made here: G C E G (held), each note a few harmonics with a quick attack
                int rate = 22050; float[] notes = { 392f, 523.3f, 659.3f, 784f }; float[] lens = { .18f, .18f, .18f, .9f }; int n = (int)(rate * (lens.Sum() + .3f)); var d = new float[n]; int at = 0;
                for (int k = 0; k < notes.Length; k++)
                {
                    int m = (int)(lens[k] * rate);
                    for (int i = 0; i < m + rate / 5 && at + i < n; i++)
                    {
                        float t = i / (float)rate, env = Mathf.Clamp01(t * 40f) * (i < m ? 1f : Mathf.Exp(-(i - m) / (float)rate * 18f)) * (k == 3 ? 1f - .25f * t : 1f);
                        float w = 0f; for (int h = 1; h <= 5; h++) w += Mathf.Sin(2f * Mathf.PI * notes[k] * h * t * (1f + .002f * Mathf.Sin(2f * Mathf.PI * 5f * t))) / (h * h * .7f + .3f);
                        d[at + i] += w * env * .22f;
                    }
                    at += m;
                }
                fanfare = AudioClip.Create("Quarry fanfare", n, 1, rate, false); fanfare.SetData(d, 0);
            }
            Src().PlayOneShot(fanfare, SoundMix.Gain(SoundMix.Kind.Effects));
        }
        void Cheer(float loud)
        {
            if (!roar)
            {
                // a crowd's roar: noise shaped into voices' band, swelling and dying away
                int rate = 22050, n = rate * 3; var d = new float[n]; var rnd = new System.Random(72); float lp = 0f, lp2 = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)rate, x = (float)rnd.NextDouble() * 2f - 1f; lp += (x - lp) * .25f; lp2 += (lp - lp2) * .08f;
                    float env = Mathf.Clamp01(t * 3f) * Mathf.Exp(-Mathf.Max(0f, t - .8f) * 1.2f);
                    d[i] = (lp - lp2) * env * 1.6f * (1f + .25f * Mathf.Sin(2f * Mathf.PI * 3.1f * t));
                }
                roar = AudioClip.Create("Quarry crowd", n, 1, rate, false); roar.SetData(d, 0);
            }
            Src().PlayOneShot(roar, SoundMix.Gain(SoundMix.Kind.Effects) * loud);
        }

        // ---------------------------------------------------------------- the HUD
        static GUIStyle big, small;
        void Update()
        {
            if (CanWatch && WatchPressed()) Watch();
        }
        static bool WatchPressed()
        {
            var k = UnityEngine.InputSystem.Keyboard.current; var g = UnityEngine.InputSystem.Gamepad.current;
            return (k != null && k.kKey.wasPressedThisFrame) || (g != null && g.leftStickButton.wasPressedThisFrame && !GameMenus.Open);
        }
        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || GameMenus.Open || WorldMap.IsOpen || TrailerMode.HudHidden || ReplayViewer.Active || !car) return;
            if (big == null)
            {
                big = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; big.normal.textColor = Color.white;
                small = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter }; small.normal.textColor = Color.white;
            }
            float sc = Mathf.Max(1f, Screen.height / 800f); var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(sc, sc, 1f)); float W = Screen.width / sc;
            void Panel(Rect r) { var c = GUI.color; GUI.color = new Color(0, 0, 0, .55f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = c; }
            var J = Current;
            if (State == Phase.Idle)
            {
                foreach (var j in Jumps)
                {
                    float dd = Vector3.Distance(car.Body.position, StartPoint(j));
                    if (dd > 60f || !Allowed(car)) continue;
                    var r = new Rect(W / 2 - 360, 120, 720, 70); Panel(r);
                    GUI.Label(new Rect(r.x, r.y + 2, r.width, 36), "VIKING QUARRY: " + j.name.ToUpper(), big);
                    GUI.Label(new Rect(r.x, r.y + 38, r.width, 28), $"stop in the gantry facing the ramp to start   ·   lip speed {DisplayUnits.Mph(j.window.lo):0} to {DisplayUnits.Mph(j.window.hi):0} mph", small);
                    break;
                }
            }
            else if (State == Phase.Hold || State == Phase.Countdown)
            {
                float c = Time.time - t0; string txt = State == Phase.Hold ? "HOLD STILL" : c < 1f ? "3" : c < 2f ? "2" : "1";
                var r = new Rect(W / 2 - 300, 120, 600, 90); Panel(r);
                GUI.Label(new Rect(r.x, r.y + 2, r.width, 50), txt, big);
                GUI.Label(new Rect(r.x, r.y + 52, r.width, 30), $"{J.name}: hit the lip at {DisplayUnits.Mph(J.window.lo):0} to {DisplayUnits.Mph(J.window.hi):0} mph", small);
            }
            else if (State == Phase.Run || State == Phase.Air)
            {
                float v = State == Phase.Run ? car.Body.linearVelocity.magnitude : LastLipSpeed;
                bool inside = v >= J.window.lo && v <= J.window.hi;
                var r = new Rect(W / 2 - 300, 110, 600, 80); Panel(r);
                var st = new GUIStyle(big); st.normal.textColor = inside ? new Color(.3f, 1f, .4f) : v < J.window.lo ? new Color(1f, .8f, .3f) : new Color(1f, .35f, .3f);
                GUI.Label(new Rect(r.x, r.y + 2, r.width, 44), State == Phase.Air ? (Time.time - airAt < 1.2f ? (inside ? "GO!" : v < J.window.lo ? "TOO SLOW" : "TOO FAST") : $"{DisplayUnits.Feet(car.Body.position.y - J.water_y):0} ft over the lake") : $"{DisplayUnits.Mph(v):0} mph", st);
                GUI.Label(new Rect(r.x, r.y + 46, r.width, 28), $"the lip: {DisplayUnits.Mph(J.window.lo):0} to {DisplayUnits.Mph(J.window.hi):0} mph" + (State == Phase.Air ? $"   ·   off it at {DisplayUnits.Mph(LastLipSpeed):0}" : ""), small);
            }
            else if (State == Phase.Result && Time.time < resultUntil)
            {
                var top = Top(J.id); var r = new Rect(W / 2 - 380, 100, 760, 126 + 22 * Mathf.Min(5, top.Count)); Panel(r);
                var lines = Result.Split('\n');
                var st = new GUIStyle(big); st.normal.textColor = LastLanding == "CLEAN" ? new Color(.3f, 1f, .4f) : LastLanding == "ROUGH" ? new Color(1f, .85f, .3f) : new Color(1f, .4f, .35f);
                GUI.Label(new Rect(r.x, r.y + 4, r.width, 40), lines[0], st);
                for (int i = 1; i < lines.Length; i++) GUI.Label(new Rect(r.x, r.y + 18 + 26 * i, r.width, 26), lines[i], small);
                float y = r.y + 30 + 26 * lines.Length;
                for (int i = 0; i < Mathf.Min(5, top.Count); i++) { var e = top[i]; GUI.Label(new Rect(r.x, y, r.width, 22), $"{i + 1}. {e.name}  {VehicleProfile.Find(e.vehicle).Name}  {DisplayUnits.Jump(e.distance)}  {e.landing.ToLower()}", new GUIStyle(small) { fontSize = 16 }); y += 22; }
                if (CanWatch) GUI.Label(new Rect(r.x, r.yMax - 4, r.width, 26), "L3 / K: watch the jump again", new GUIStyle(small) { fontSize = 16 });
            }
            GUI.matrix = old;
        }
    }
}
