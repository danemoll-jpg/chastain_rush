using System.Collections.Generic;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 39 (Part D): Racer's hidden police (0.99 Part D) on this map. Hiding places beside the roads (found once from the road
    // network: a flat, clear verge, the patrol car parked nose to the road, lights off): on the long fast roads (posted 40 mph or more, the interstate
    // left out: Canton Road, New Chastain Road, Chastain Road, Bells Ferry Road, Blackwell Road ...) up to eight, and in the neighborhoods' streets up to
    // four, at least 170 m apart, twelve at most. Two to four are used at a time and one moves now and then while the player cannot see it. A hidden cop
    // sees the player within 70 m either way with a clear line of sight; on the road and more than 10 mph over its limit (PoliceRoads: OSM's or the
    // class's) it flashes "CLOCKED 58 in a 45", pulls out with lights and siren, and a Getaway chase starts where the player is (GetawayChase.
    // BeginEmbedded: heat 1, Settings' difficulty). Escaped: "You lost them"; busted: the vehicle set at the roadside. Free Roam carries on, nothing kept.
    // Never: in a race, in split screen, during a police mode or the Stunt event or a timed activity, in the first 60 s, within 3 minutes of the last
    // chase, with Settings > Gameplay > Hidden police Off; and never in a check run unless it asks (-hiddenPolice).
    public sealed class HiddenPolice : MonoBehaviour
    {
        public const float SeeRange = 70, OverBy = 10, QuietStart = 60, QuietAfter = 180, SpawnDistance = 230, DespawnDistance = 300, MoveEvery = 200;
        public static HiddenPolice Current { get; private set; }
        public sealed class Spot { public Vector3 pos, forward; public string road; public bool fast; public ArcadeVehicle prop; public float movedAt, heardAt = -999; }
        public readonly List<Spot> All = new(), Active = new();
        RoadNet net; float attachedAt, lastEnd = -999, nextTick, nextMove; public RoadNet Net => net;
        public static string Flash = ""; public static float FlashAt = -99; public static string Line = ""; public static float LineAt = -99;
        public float AttachedAt { get => attachedAt; set => attachedAt = value; } public float LastEnd { get => lastEnd; set => lastEnd = value; }
        public static bool CheckAllows;   // a check run that asked for it (-hiddenPolice)
        public bool ChaseRunning => GetawayChase.Current;
        public static void RadioLine(params string[] ids) { Line = PoliceRadio.TextOf(ids); LineAt = Time.unscaledTime; PoliceRadio.Speak(ids); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Current || !PoliceRoads.Instance) return;
            CheckAllows = System.Environment.GetCommandLineArgs().Contains("-hiddenPolice");
            new GameObject("Hidden police").AddComponent<HiddenPolice>();
        }
        void Awake() { Current = this; attachedAt = Time.time; nextMove = Time.time + MoveEvery; }
        void Start() { net = RoadNet.Shared; BuildSpots(); Pick(Mathf.Clamp(Random.Range(2, 5), 1, Mathf.Max(1, All.Count))); }
        void OnDestroy() { foreach (var s in All) DropProp(s); if (Current == this) Current = null; }
        public static void Ended(bool caught) { if (Current) { Current.lastEnd = Time.time; Current.ReplaceUsed(); } }
        public bool Enabled => GameSettings.Settings.hiddenPolice && (!CheckRun.Active || CheckAllows);
        public int Difficulty => Mathf.Clamp(GameSettings.Settings.hiddenPoliceDifficulty, 0, 2);

        // ---------- the hiding places ----------
        void BuildSpots()
        {
            var roads = PoliceRoads.Instance; var rng = new System.Random(390909); var found = new List<Spot>();
            for (int i = 0; i < net.Count; i += 3)
            {
                int r = net.Road[i]; string cls = roads.classes[r]; int limit = roads.limits[r];
                if (cls == "motorway" || cls == "motorway_link" || cls == "service" || cls.EndsWith("_link")) continue;
                bool fast = limit >= 40; bool street = !fast && (cls == "residential" || cls == "living_street" || cls == "unclassified");
                if (!fast && !street) continue;
                bool junction = false; foreach (int j in net.Near(net.P[i], 45)) if (net.Adj[j].Count >= 3) { junction = true; break; } if (junction) continue;
                var t = net.Tangent(i); var right = Vector3.Cross(Vector3.up, t).normalized;
                foreach (int side in new[] { -1, 1 })
                {
                    var pos = net.P[i] + right * side * (net.Half[i] + 4.2f);
                    if (!Physics.Raycast(pos + Vector3.up * 25, Vector3.down, out var hit, 60, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (hit.normal.y < .97f || hit.point.y < net.P[i].y - 1.6f || hit.point.y > net.P[i].y + 1.0f || !(hit.collider is TerrainCollider)) continue;
                    pos = hit.point; int n = net.Nearest(pos, out _, 60); if (n < 0 || net.Road[n] != r) continue;
                    if (PoliceKit.Underground(pos)) continue;
                    var forward = -right * side; var rot = Quaternion.LookRotation(forward);
                    bool blocked = false; foreach (var c in Physics.OverlapBox(pos + Vector3.up * 1.1f, new Vector3(1.2f, .8f, 2.6f), rot, ~0, QueryTriggerInteraction.Ignore)) if (!(c is TerrainCollider)) { blocked = true; break; }
                    if (blocked) continue;
                    found.Add(new Spot { pos = pos, forward = forward, road = net.RoadNames[r], fast = fast });
                }
            }
            foreach (var (fast, want0) in new[] { (true, 8), (false, 4) })
            {
                int want = want0;
                foreach (var f in found.Where(x => x.fast == fast).OrderBy(_ => rng.Next())) { if (want <= 0) break; if (All.Any(s => (s.pos - f.pos).sqrMagnitude < 170 * 170)) continue; All.Add(f); want--; }
            }
            Debug.Log($"[Police] hidden police: {All.Count} hiding places of {found.Count} found ({All.Count(s => s.fast)} on the fast roads, {All.Count(s => !s.fast)} in the neighborhoods): " + string.Join(", ", All.Select(s => $"{s.road} ({s.pos.x:0}, {s.pos.z:0})")));
        }
        void Pick(int count) { Active.Clear(); foreach (var s in All.OrderBy(_ => Random.value).Take(count)) { Active.Add(s); s.movedAt = Time.time; } }
        void ReplaceUsed()
        {
            int want = Random.Range(2, 5); var me = PoliceKit.Player(1);
            while (Active.Count < want)
            {
                var free = All.Where(s => !Active.Contains(s) && (!me || Vector3.Distance(s.pos, me.Body.position) > 250)).OrderBy(_ => Random.value).FirstOrDefault(); if (free == null) break;
                Active.Add(free); free.movedAt = Time.time;
            }
        }

        // ---------- each frame ----------
        void Update()
        {
            var car = PoliceKit.Player(1); if (!car || All.Count == 0) return;
            if (!Enabled || SplitScreen.Active || !PoliceKit.FreeRoam) { foreach (var s in All) DropProp(s); return; }
            var me = car.Body.position;
            foreach (var s in Active) { float d = Vector3.Distance(me, s.pos); if (!s.prop && d < SpawnDistance) s.prop = MakeProp(car, s); else if (s.prop && d > DespawnDistance) DropProp(s); }
            foreach (var s in All) if (!Active.Contains(s) && s.prop) DropProp(s);
            if (Time.time >= nextMove) { nextMove = Time.time + MoveEvery * (.75f + .5f * Random.value); MoveOne(me); }
            if (Time.unscaledTime < nextTick) return; nextTick = Time.unscaledTime + .2f;
            if (!CanTrip()) return;
            float mph = PoliceKit.Mph(car.Body.linearVelocity.magnitude); int limit = PoliceKit.LimitAt(me, out bool onRoad);
            foreach (var s in Active)
            {
                if (!s.prop) continue; var toPlayer = me - s.prop.Body.position; toPlayer.y = 0; float d = toPlayer.magnitude;
                if (d < 35 && Time.time - s.heardAt > 150 && Random.value < .5f) { s.heardAt = Time.time; if (mph <= limit + OverBy) RadioLine("H01"); }
                if (!Sees(s, me, mph, limit, onRoad, car.transform, out _)) continue;
                Trip(s, Mathf.RoundToInt(mph), limit); return;
            }
        }
        static ArcadeVehicle MakeProp(ArcadeVehicle template, Spot s)
        {
            var car = PoliceKit.MakePatrol(template, "police", s.pos, s.forward, "HIDDEN COP"); car.Body.isKinematic = true;
            car.gameObject.AddComponent<PoliceLights>().Siren = false; return car;
        }
        public bool Sees(Spot s, Vector3 me, float mph, int limit, bool onRoad, Transform player, out string why)
        {
            why = "";
            if (!s.prop) { why = "no car parked yet (over " + SpawnDistance.ToString("F0") + " m)"; return false; }
            var toPlayer = me - s.prop.Body.position; toPlayer.y = 0; float d = toPlayer.magnitude;
            if (d > SeeRange) { why = "out of range"; return false; }
            if (!onRoad) { why = "off the road"; return false; }
            if (mph <= limit + OverBy) { why = "not fast enough"; return false; }
            if (!PoliceKit.LineOfSight(s.prop.Body.position + Vector3.up * 1.3f, me + Vector3.up, player)) { why = "view blocked"; return false; }
            return true;
        }
        public float QuietLeft => Mathf.Max(0, Mathf.Max(QuietStart - (Time.time - attachedAt), QuietAfter - (Time.time - lastEnd)));
        public string DebugLine
        {
            get
            {
                var car = PoliceKit.Player(1); if (!car || Active.Count == 0) return "Hidden police: none placed";
                var me = car.Body.position; float mph = PoliceKit.Mph(car.Body.linearVelocity.magnitude); int limit = PoliceKit.LimitAt(me, out bool onRoad);
                var s = Active.OrderBy(x => Vector3.Distance(x.prop ? x.prop.Body.position : x.pos, me)).First(); float d = Vector3.Distance(s.prop ? s.prop.Body.position : s.pos, me);
                return $"Hidden police: nearest {d:F0} m ({s.road}) · {mph:F0} mph vs {limit + OverBy:F0} ({limit}+{OverBy:F0}) · on road {(onRoad ? "yes" : "no")} · quiet {(ChaseRunning ? "chase on" : QuietLeft.ToString("F0") + " s")}{(Enabled ? "" : " · OFF")}";
            }
        }
        bool CanTrip()
        {
            if (Time.timeScale == 0 || !PoliceKit.FreeRoam || SplitScreen.Active || ChaseRunning || PoliceSession.Running) return false;
            if (StuntEvent.Current) return false;
            var acts = PoliceKit.Player(1).GetComponentInParent<ArcadeActivities>(); if (acts && acts.AttemptActive) return false;
            return CheckAllows && CheckRun.Active ? Time.time - lastEnd >= 20 : Time.time - attachedAt >= QuietStart && Time.time - lastEnd >= QuietAfter;
        }
        // checks: trip the nearest active spot now (as a pass over the limit would)
        public bool TripNearest() { var car = PoliceKit.Player(1); var s = Active.Where(x => x.prop).OrderBy(x => Vector3.Distance(x.pos, car.Body.position)).FirstOrDefault(); if (s == null) return false; Trip(s, 60, 45); return true; }
        void Trip(Spot s, int mph, int limit)
        {
            Flash = $"CLOCKED {mph} in a {limit}"; FlashAt = Time.unscaledTime;
            var at = s.prop.Body.position; var forward = s.prop.transform.forward; DropProp(s); Active.Remove(s);
            GetawayChase.BeginEmbedded(net, at, forward, Difficulty);
        }
        void MoveOne(Vector3 me)
        {
            var out1 = Active.Where(s => Vector3.Distance(s.pos, me) > 260 && !s.prop && Time.time - s.movedAt > 90).OrderBy(_ => Random.value).FirstOrDefault(); if (out1 == null) return;
            var target = All.Where(s => !Active.Contains(s) && Vector3.Distance(s.pos, me) > 260).OrderBy(_ => Random.value).FirstOrDefault(); if (target == null) return;
            Active.Remove(out1); Active.Add(target); target.movedAt = Time.time;
        }
        void DropProp(Spot s) { if (s.prop) { Destroy(s.prop.gameObject); s.prop = null; } }
    }
}
