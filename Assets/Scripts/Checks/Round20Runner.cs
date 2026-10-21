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
    // Round 20, checked in the exe (ChastainRush.exe -exeRound20 <dir> [-r20Only bj1,ramp,map,lap,fences,tramp,n5,n2,r1] [-r20Vehicles id,id]
    // -logFile <dir>\Player.log), muted like every check (CheckRun), its files in <dir>. One targeted check per piece:
    //   bj1   BUG-001: the Drifter Twin over the footbridge's near end at race speed, on Dan's line and 1 m either side; the
    //         slowest it goes there and what it touched
    //   lap   the whole Noonday Creek lap once on each trail vehicle: every dead stop (or near stop) and where
    //   ramp  BUG-002: every vehicle that comes through P1 off the outlet ramp over the pond: air, height, distance, on and on
    //   map   the map with the controller (a virtual gamepad), in free roam and in a race: open, zoom, pan, close
    //   fences, tramp, n5, n2, r1: the follow-ups (Round20Runner.Follow.cs)
    public sealed partial class Round20Runner : MonoBehaviour
    {
        public string outputDir;
        public string tag = "round20";   // Round 21: -exeRound21 runs this runner's Round 21 parts (Round20Runner.R21.cs), its report round21_check.txt
        readonly StringBuilder report = new StringBuilder();
        int fails, checks;
        ArcadeVehicle car; VehicleSwitch sw; VehicleRespawn respawn; RaceController race;
        static readonly string[] Small = { "atv", "moto", "scrambler", "drifter", "mower" };

        void Update() { var hud = DeveloperHud.Instance; if (hud && hud.MenuOpen) { hud.CloseMenu(); report.AppendLine("  (the debug menu was opened from outside; closed)"); } }

        void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}"); Flush(); }
        void Flush() { try { File.WriteAllText(Path.Combine(outputDir, tag + "_check.txt"), report.ToString()); } catch (Exception) { } }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            report.AppendLine($"{(tag == "round53" ? "Round 53" : tag == "round52" ? "Round 52" : tag == "round50" ? "Round 50" : tag == "round49" ? "Round 49" : tag == "round48" ? "Round 48" : tag == "round47" ? "Round 47" : tag == "round46" ? "Round 46" : tag == "round45" ? "Round 45" : tag == "round44" ? "Round 44" : tag == "round43" ? "Round 43" : tag == "round42" ? "Round 42" : tag == "round41" ? "Round 41" : tag == "round40" ? "Round 40" : tag == "round39" ? "Round 39" : tag == "round38" ? "Round 38" : tag == "round37" ? "Round 37" : tag == "round36" ? "Round 36" : tag == "round35" ? "Round 35" : tag == "round34" ? "Round 34" : tag == "round33" ? "Round 33" : tag == "round26" ? "Round 26" : tag == "round21" ? "Round 21" : "Round 20")} check, {DateTime.Now:yyyy-MM-dd HH:mm}, the exe, {Screen.width} x {Screen.height}, muted.");
            yield return new WaitForSecondsRealtime(4f);
            car = FindAnyObjectByType<ArcadeVehicle>();
            foreach (var v in FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None)) { var vi = v.GetComponent<VehicleInput>(); if (vi && vi.enabled) car = v; }   // Round 33: the player, not a traffic car (-traffic)
            sw = car.GetComponent<VehicleSwitch>(); respawn = car.GetComponent<VehicleRespawn>(); race = RaceController.Instance;
            var hud = DeveloperHud.Instance; if (hud) hud.Visible = false;
            string only = null; string[] ids = VehicleProfile.All.Select(p => p.Id).ToArray();
            var argv = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < argv.Length; i++) { if (argv[i] == "-r53Only" || argv[i] == "-r52Only" || argv[i] == "-r50Only" || argv[i] == "-r49Only" || argv[i] == "-r48Only" || argv[i] == "-r47Only" || argv[i] == "-r46Only" || argv[i] == "-r45Only" || argv[i] == "-r44Only" || argv[i] == "-r43Only" || argv[i] == "-r42Only" || argv[i] == "-r41Only" || argv[i] == "-r40Only" || argv[i] == "-r39Only" || argv[i] == "-r38Only" || argv[i] == "-r37Only" || argv[i] == "-r36Only" || argv[i] == "-r20Only" || argv[i] == "-r21Only" || argv[i] == "-r26Only" || argv[i] == "-r33Only" || argv[i] == "-r34Only" || argv[i] == "-r35Only") only = argv[i + 1]; if (argv[i] == "-r20Vehicles") ids = argv[i + 1].Split(','); }
            float minutes = 25f; for (int i = 0; i + 1 < argv.Length; i++) if (argv[i] == "-r20Minutes") float.TryParse(argv[i + 1], out minutes);
            StartCoroutine(Watchdog(minutes));   // a check run always ends (a stuck part must not leave the exe running)
            var parts = only == null ? null : only.Split(',');
            if (tag == "round21" && parts == null) parts = new[] { "r1", "six", "races", "revlap", "fwdlap", "revslow", "revrace" };   // Round 21's default parts
            if (tag == "round26" && parts == null) parts = new[] { "ramps", "express", "walls" };   // Round 26 (Round20Runner.R26.cs)
            if (tag == "round33" && parts == null) parts = new[] { "jump", "shortcuts", "hollow", "pics", "race" };   // Round 33 (Round20Runner.R33.cs)
            if (tag == "round34" && parts == null) parts = new[] { "lap", "bumps", "radio", "records" };   // Round 34 (Round20Runner.R34*.cs)
            bool Do(string p) => parts == null || parts.Contains(p);
            if (tag == "round35" && parts == null) parts = new[] { "line", "radio", "cpics", "grip", "fps" };   // Round 35 (Round20Runner.R35*.cs)
            if (tag == "round37" && parts == null) parts = new[] { "parkramps", "parkloop" };   // Round 37 (Round20Runner.R37*.cs)
            if (tag == "round39" && parts == null) parts = new[] { "walkers", "walkpics", "walkdodge", "walkfps" };   // Round 39 (Round20Runner.R39*.cs)
            if (tag == "round40" && parts == null) parts = new[] { "plume", "plumewhen", "plumepics", "corner", "clues", "crest", "ridgerace", "lines", "median", "cantoncop", "houses" };   // Round 40 (Round20Runner.R40*.cs)
            if (tag == "round43" && parts == null) parts = new[] { "plume", "ghost", "winner", "trailer" };   // Round 43 (Round20Runner.R43*.cs)
            if (tag == "round52" && parts == null) parts = new[] { "ahlap", "ahshut", "ahpics", "ahhouses" };   // Round 52 (Round20Runner.R52.cs); ahrace with -traffic Normal
            if (tag == "round53" && parts == null) parts = new[] { "sclean", "scheat", "sshort", "sflag", "sjunction", "ahshut", "spics" };   // Round 53 (Round20Runner.R53.cs); srace, srevrace with -traffic Normal
            if (tag == "round50" && parts == null) parts = new[] { "kclean", "kcheat", "kshort", "ktrail", "kpics" };   // Round 50 (Round20Runner.R50.cs); krace with -traffic Normal
            if (tag == "round49" && parts == null) parts = new[] { "lktrail", "lkintake", "lkkicker", "lkshut", "lkfwd", "lkmap", "lkpics" };   // Round 49 (Round20Runner.R49.cs); lkrace with -traffic Normal
            if (tag == "round48" && parts == null) parts = new[] { "eggpickup", "eggtier", "eggmap", "eggtravel", "eggwaypoint", "eggrace", "eggpics" };   // Round 48 (Round20Runner.R48.cs)
            if (tag == "round47" && parts == null) parts = new[] { "lkrevlap", "lkfwdlap", "lkrevshut", "lkrevpics", "drives" };   // Round 47 (Round20Runner.R47.cs); lkrevrace with -traffic Normal
            if (tag == "round46" && parts == null) parts = new[] { "boat", "boattimes", "boatrace", "drain", "pics" };   // Round 46 (Round20Runner.R46.cs)
            if (tag == "round45" && parts == null) parts = new[] { "voices" };   // Round 45 (Round20Runner.R45.cs)
            if (tag == "round44" && parts == null) parts = new[] { "times", "wjump", "drain", "lakesrace", "pics" };   // Round 44 (Round20Runner.R44.cs)
            if (tag == "round42" && parts == null) parts = new[] { "windows", "aisle", "plume" };   // Round 42 (Round20Runner.R42.cs)
            if (tag == "round41" && parts == null) parts = new[] { "crypt", "cryptcrash", "cryptpics", "ridgerace" };   // Round 41 (Round20Runner.R41*.cs)
            if (tag == "round38" && parts == null) parts = new[] { "bankhop", "bankthrough", "bankpics", "smash", "shoprow", "shopramp", "vault", "cpics", "cfps", "rivals", "splitai", "life", "lifepics", "lifefps" };   // Round 38 (Round20Runner.R38*.cs)
            if (tag == "round36" && parts == null) parts = new[] { "fps", "air", "tricks", "event", "glass", "inside", "rooframp", "pics" };   // Round 36 (Round20Runner.R36*.cs)
            if (Do("bj1")) yield return Guard(Bj1());
            if (Do("ramp")) yield return Guard(Ramp(ids));
            if (Do("map")) yield return Guard(MapCheck());
            if (Do("lap") && tag != "round34") yield return Guard(Lap(ids));
            // the follow-ups (Round20Runner.Follow.cs)
            if (Do("fences")) yield return Guard(Fences());
            if (Do("tramp")) yield return Guard(Tramp());
            if (Do("n5")) yield return Guard(N5(ids));
            if (Do("n2")) yield return Guard(N2(ids));
            if (Do("r1")) yield return Guard(R1(ids));
            // Round 21 (Round20Runner.R21.cs)
            if (Do("six")) yield return Guard(Six(ids));
            if (Do("races")) yield return Guard(Races());
            // Round 21 Part B (Round20Runner.R21B.cs)
            if (Do("revlap")) yield return Guard(LapJumps("reverse", ids));
            if (Do("fwdlap")) yield return Guard(LapJumps("forward", ids));
            if (Do("revslow")) yield return Guard(RevSlow());
            if (Do("revrace") && tag != "round40") yield return Guard(RevRace());
            if (Do("bj1diag")) yield return Guard(Bj1Diag());
            // the Round 21 follow-ups (Round20Runner.R21F.cs)
            if (Do("tube")) yield return Guard(Tube(ids));
            if (Do("ra")) yield return Guard(Ra(ids));
            if (Do("rc")) yield return Guard(Rc(ids));
            // Round 26 (Round20Runner.R26.cs)
            if (Do("ramps")) yield return Guard(Ramps());
            if (Do("express")) yield return Guard(Express());
            if (Do("walls")) yield return Guard(Walls());
            // Round 33 (Round20Runner.R33.cs): the Chastain Ridge race
            if (Do("jump")) yield return Guard(R33Jump(ids));
            if (Do("shortcuts")) yield return Guard(R33Shortcuts());
            if (Do("hollow")) yield return Guard(R33Hollow());
            if (Do("pics") && tag != "round36") yield return Guard(R33Pics());
            if (Do("race")) yield return Guard(R33Race());
            // Round 34 (Round20Runner.R34*.cs)
            if (tag == "round34")
            {
                if (Do("lap")) yield return Guard(R34Lap());
                if (Do("bumps")) yield return Guard(R34Bumps());
                if (Do("radio")) yield return Guard(R34Radio());   // Part B (Round20Runner.R34B.cs)
                if (Do("records")) yield return Guard(R34Records());   // Part C (Round20Runner.R34C.cs)
            }
            // Round 35 (Round20Runner.R35*.cs)
            if (tag == "round35")
            {
                if (Do("line")) yield return Guard(R35Line());
                if (Do("radio")) yield return Guard(R35Radio());   // Part B (Round20Runner.R35B.cs)
                if (Do("cpics")) yield return Guard(R35Pics());   // Part C (Round20Runner.R35C.cs)
                if (Do("grip")) yield return Guard(R35Grip());
                if (Do("fps")) yield return Guard(R35Fps());
            }
            // Round 36 (Round20Runner.R36*.cs): the stunt park, the free roam events, the hydrants
            if (tag == "round36")
            {
                if (Do("fps")) yield return Guard(R36Fps());
                if (Do("air")) yield return Guard(R36Air());
                if (Do("tricks")) yield return Guard(R36Tricks());
                if (Do("event")) yield return Guard(R36Event());
                if (Do("glass")) yield return Guard(R36Glass());
                if (Do("inside")) yield return Guard(R36Inside());
                if (Do("rooframp")) yield return Guard(R36RoofRamp());
                if (Do("pics")) yield return Guard(R36Pics());
                if (Do("measure")) yield return Guard(R36Measure());   // Part B (Round20Runner.R36B.cs)
                if (Do("acts")) yield return Guard(R36Acts());
                if (Do("mappic")) yield return Guard(R36MapPic());
                if (Do("hydrants")) yield return Guard(R36Hydrants());   // Parts C and D (Round20Runner.R36C.cs)
                if (Do("cfps")) yield return Guard(R36CantonFps());
                if (Do("rig")) yield return Guard(R36Rig());
                if (Do("geyser")) yield return Guard(R36Geyser());
            }
            // Round 50 (Round20Runner.R50.cs): the kicker line's carport and shrub beds, the lake trail at its real width
            if (tag == "round50")
            {
                if (Do("kclean")) yield return Guard(R50Clean());
                if (Do("kcheat")) yield return Guard(R50Cheat());
                if (Do("kshort")) yield return Guard(R50Short());
                if (Do("krace")) yield return Guard(R50Race());
                if (Do("ktrail")) yield return Guard(R50Trail(ids));
                if (Do("kpics")) yield return Guard(R50Pics());
            }
            // Round 49 (Round20Runner.R49.cs): the reverse Chastain Lakes shortcuts, the lake trail behind the pool, the map's pointer
            if (tag == "round49")
            {
                if (Do("lktrail")) yield return Guard(R49Trail(ids));
                if (Do("lkintake")) yield return Guard(R49Intake());
                if (Do("lkkicker")) yield return Guard(R49Kicker());
                if (Do("lkshut")) yield return Guard(R49Shut());
                if (Do("lkfwd")) yield return Guard(R49Fwd());
                if (Do("lkrace")) yield return Guard(R49Race());
                if (Do("lkmap")) yield return Guard(R49Map());
                if (Do("lkpics")) yield return Guard(R49Pics());
            }
            // Round 48 (Round20Runner.R48.cs): the goose egg hunt and exploration
            if (tag == "round48")
            {
                if (Do("eggpickup")) yield return Guard(R48Pickup());
                if (Do("eggtier")) yield return Guard(R48Tier());
                if (Do("eggmap")) yield return Guard(R48Map());
                if (Do("eggtravel")) yield return Guard(R48Travel());
                if (Do("eggwaypoint")) yield return Guard(R48Waypoint());
                if (Do("eggrace")) yield return Guard(R48Race());
                if (Do("eggpics")) yield return Guard(R48Pics());
            }
            // Round 53 (Round20Runner.R53.cs): Addison Heights' Lyndon road crew, the junction, the house
            if (tag == "round53")
            {
                if (Do("sclean")) yield return Guard(R53Clean());
                if (Do("scheat")) yield return Guard(R53Cheat());
                if (Do("sshort")) yield return Guard(R53Short());
                if (Do("sflag")) yield return Guard(R53Flag());
                if (Do("sjunction")) yield return Guard(R53Junction());
                if (Do("ahshut")) yield return Guard(R52Shut());
                if (Do("srace")) for (int k = 1; k <= 2; k++) yield return Guard(R53Race("forward", k));
                if (Do("srevrace")) yield return Guard(R53Race("reverse", 1));
                if (Do("spics")) yield return Guard(R53Pics());
            }
            // Round 52 (Round20Runner.R52.cs): the Addison Heights race, Dan's three houses
            if (tag == "round52")
            {
                if (Do("ahhouses")) yield return Guard(R52Houses());
                if (Do("ahshut")) yield return Guard(R52Shut());
                if (Do("ahpics")) yield return Guard(R52Pics());
                if (Do("ahrace")) foreach (var dr in new[] { "forward", "reverse" }) for (int k = 1; k <= 2; k++) yield return Guard(R52Race(dr, k));
                if (Do("ahlap")) yield return Guard(R52Laps(ids));
            }
            // Round 47 (Round20Runner.R47.cs): reverse Chastain Lakes, the drives on Chastain Road
            if (tag == "round47")
            {
                if (Do("lkrevlap")) yield return Guard(R47RevLaps());
                if (Do("lkfwdlap")) yield return Guard(R47FwdLap());
                if (Do("lkrevshut")) yield return Guard(R47Shut());
                if (Do("lkrevrace")) yield return Guard(R47RevRace());
                if (Do("lkrevpics")) yield return Guard(R47Pics());
                if (Do("drives")) yield return Guard(R47Drives());
            }
            // Round 46 (Round20Runner.R46.cs): the Chastain Lakes shortcuts redone
            if (tag == "round46")
            {
                if (Do("speed")) yield return Guard(R46Speed(ids));
                if (Do("boattrace")) yield return Guard(R46BoatTrace());
                if (Do("boat")) yield return Guard(R46Boat(ids));
                if (Do("boattimes")) yield return Guard(R46Times());
                if (Do("boatrace")) yield return Guard(R46Race());
                if (Do("drain")) yield return Guard(R46Drain());
                if (Do("pics")) yield return Guard(R46Pics());
            }
            // Round 45 (Round20Runner.R45.cs): the police voices
            if (tag == "round45" && Do("voices")) yield return Guard(R45Voices());
            // Round 44 (Round20Runner.R44.cs): the Chastain Lakes race
            if (tag == "round44")
            {
                if (Do("times")) yield return Guard(R44Times());
                if (Do("wjump")) yield return Guard(R44WJump(ids));
                if (Do("drain")) yield return Guard(R44Drain());
                if (Do("lakesrace")) yield return Guard(R44Race());
                if (Do("pics")) yield return Guard(R44Pics());
            }
            // Round 43 (Round20Runner.R43*.cs)
            if (tag == "round43")
            {
                if (Do("plume")) yield return Guard(R43Plume());       // A1
                if (Do("ghost")) yield return Guard(R43Ghost());       // B1 (Round20Runner.R43B.cs)
                if (Do("winner")) yield return Guard(R43Winner());     // C1
                if (Do("trailer")) yield return Guard(R43Trailer());   // D1
            }
            // Round 42 (Round20Runner.R42.cs)
            if (tag == "round42")
            {
                if (Do("windows")) yield return Guard(R42Windows());   // B1
                if (Do("aisle")) yield return Guard(R42Aisle());       // B2
                if (Do("plume")) yield return Guard(R42Plume());       // B3
            }
            // Round 41 (Round20Runner.R41*.cs)
            if (tag == "round41")
            {
                if (Do("crypt")) yield return Guard(R41Crypt(ids));          // Part A
                if (Do("cryptin")) yield return Guard(R41CryptIn(ids));
                if (Do("cryptcrash")) yield return Guard(R41CryptCrash());
                if (Do("cryptpics")) yield return Guard(R41CryptPics());
                if (Do("ridgerace")) yield return Guard(R40RevRace());
                if (Do("crest")) yield return Guard(R41Crest());              // Part B (Round20Runner.R41B.cs)
                if (Do("crestpics")) yield return Guard(R41CrestPics());
                if (Do("pipes")) yield return Guard(R41Pipes());              // Part C
            }
            // Round 40 (Round20Runner.R40*.cs)
            if (tag == "round40")
            {
                if (Do("plume")) yield return Guard(R40Plume(ids));          // Part A
                if (Do("plumewhen")) yield return Guard(R40PlumeWhen());
                if (Do("plumepics")) yield return Guard(R40PlumePics());
                if (Do("fwdleap")) yield return Guard(R33Jump(ids));   // the forward Ridge leap re-flown (Round 33's check)
                if (Do("corner")) yield return Guard(R40Corner());
                if (Do("clues")) yield return Guard(R40Clues());
                if (Do("crest")) yield return Guard(R40Crest());
                if (Do("ridgerace")) yield return Guard(R40RevRace());
                if (Do("lines")) yield return Guard(R40Lines());              // Part B (Round20Runner.R40B.cs)
                if (Do("median")) yield return Guard(R40Median());
                if (Do("cantoncop")) yield return Guard(R40CantonCop());
                if (Do("houses")) yield return Guard(R40Houses());            // Part C
            }
            // Round 39 (Round20Runner.R39*.cs)
            if (tag == "round39")
            {
                if (Do("walkers")) yield return Guard(R39Walkers());      // Part B
                if (Do("walkpics")) yield return Guard(R39WalkPics());
                if (Do("walkdodge")) yield return Guard(R39WalkDodge());
                if (Do("walkfps")) yield return Guard(R39WalkFps());
                if (Do("climb")) yield return Guard(R39Climb(ids));        // Part C (Round20Runner.R39C.cs)
                if (Do("fwdleap")) yield return Guard(R33Jump(ids));   // the forward Ridge leap re-flown (Round 33's check)
                if (Do("climbreset")) yield return Guard(R39ClimbReset());
                if (Do("ridgerev")) yield return Guard(R39RevRace());
                if (Do("revpics")) yield return Guard(R39RevPics());
                if (Do("getaway")) yield return Guard(R39Getaway());        // Part D (Round20Runner.R39D.cs)
                if (Do("escape")) yield return Guard(R39Escape());
                if (Do("patrol")) yield return Guard(R39Patrol());
                if (Do("hidden")) yield return Guard(R39Hidden());
                if (Do("chase2")) yield return Guard(R39Chase2());
                if (Do("radio")) yield return Guard(R39Radio());
                if (Do("policefps")) yield return Guard(R39PoliceFps());
            }
            // Round 38 (Round20Runner.R38*.cs)
            if (tag == "round38")
            {
                if (Do("bankhop")) yield return Guard(R38BankHop());
                if (Do("bankthrough")) yield return Guard(R38BankThrough());
                if (Do("bankpics")) yield return Guard(R38BankPics());
                if (Do("smash")) yield return Guard(R38Smash());          // Part B (Round20Runner.R38B.cs)
                if (Do("shoprow")) yield return Guard(R38ShopRow());      // Part C
                if (Do("shopramp")) yield return Guard(R38ShopRamp());
                if (Do("vault")) yield return Guard(R38Vault());
                if (Do("cpics")) yield return Guard(R38CPics());
                if (Do("cfps")) yield return Guard(R38CFps());
                if (Do("rivals")) yield return Guard(R38Rivals());        // Part D
                if (Do("splitai")) yield return Guard(R38SplitAi());
                if (Do("life")) yield return Guard(R38Life());            // Part E (Round20Runner.R38E.cs)
                if (Do("lifepics")) yield return Guard(R38LifePics());
                if (Do("lifefps")) yield return Guard(R38LifeFps());
            }
            // Round 37 (Round20Runner.R37.cs): the stunt park's ramps and the loop
            if (tag == "round37")
            {
                if (Do("parkramps")) yield return Guard(R37RampsPart());
                if (Do("parkloop")) yield return Guard(R37LoopPart());
                // Part B (Round20Runner.R37B.cs): split screen with two virtual controllers
                if (Do("splitmenu")) yield return Guard(R37SplitMenu());
                if (Do("splitrace")) yield return Guard(R37SplitRace(false));
                if (Do("splitai")) yield return Guard(R37SplitRace(true));
                if (Do("splitroam")) yield return Guard(R37SplitRoam());
                if (Do("splitpark")) yield return Guard(R37SplitPark());
                if (Do("splitevent")) yield return Guard(R37SplitEvent());
                if (Do("splitpics")) yield return Guard(R37SplitPics());
                if (Do("splitfps")) yield return Guard(R37SplitFps());
            }
            report.AppendLine(); report.AppendLine($"{checks - fails} of {checks} checks passed.");
            Flush();
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(fails == 0 ? 0 : 1);
        }

        IEnumerator Watchdog(float minutes)
        {
            yield return new WaitForSecondsRealtime(minutes * 60f);
            Check($"the check finished within {minutes:0} minutes", false, "stopped by the watchdog");
            Time.timeScale = 1f; AudioListener.pause = false; Flush();
            Application.Quit(2);
        }

        IEnumerator Guard(IEnumerator part)
        {
            while (true)
            {
                object cur;
                try { if (!part.MoveNext()) yield break; cur = part.Current; }
                catch (Exception e) { Check("no exception", false, e.ToString()); yield break; }
                yield return cur;
            }
        }

        // ---------------------------------------------------------------- 1. BUG-001, the footbridge's near end

        // what the vehicle's body touched (a tap on the car's own object)
        sealed class Touches : MonoBehaviour
        {
            public readonly List<string> seen = new List<string>();
            public bool on;
            void OnCollisionEnter(Collision c) => Note(c, "enter");
            void OnCollisionStay(Collision c) => Note(c, "stay");
            void Note(Collision c, string how)
            {
                if (!on || c.contactCount == 0) return;
                var ct = c.GetContact(0); var p = ct.point;
                if (c.impulse.magnitude < 50f && ct.normal.y > 0.6f) return;   // resting on the ground
                seen.Add($"{how} {c.collider.name} at ({p.x:0.00}, {p.y:0.00}, {p.z:0.00}) normal ({ct.normal.x:0.00}, {ct.normal.y:0.00}, {ct.normal.z:0.00}) impulse {c.impulse.magnitude:0}");
            }
        }

        IEnumerator Bj1()
        {
            report.AppendLine(); report.AppendLine("=== 1. BUG-001: the footbridge's near end (BJ1), the Drifter Twin at race speed");
            UseRoute("noonday_creek.json", "forward");
            var j = BigJump.All.First(b => b.id == "BJ1");
            var nearEnd = j.lip - j.dir * 16.5f;
            float sNear = AbsStation(nearEnd);
            var tap = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>();
            var vids = new[] { "drifter", "moto", "scrambler", "atv", "mower" };
            var bad = new List<string>();
            foreach (var id in vids)
                foreach (float off in new[] { 0f, 1f, -1f, 2f, -2f, 2.5f, -2.5f })
                {
                    yield return Vehicle(id);
                    var line = LapSlice(sNear - 150f, sNear + 12f).Select(p => p + Vector3.Cross(Vector3.up, j.dir).normalized * off * Mathf.Clamp01((AbsStation(p) - (sNear - 40f)) / 20f)).ToList();
                    Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                    var run = new Run(); float vIn = -1f, vMin = 99f; tap.seen.Clear(); tap.on = false;
                    var drive = StartCoroutine(DriveLine(line, 99f, 30f, run, null, null, 4f));
                    while (!run.reached && run.end == "" && run.time < 29.9f)
                    {
                        float a = Vector3.Dot(Flat(car.Body.position - nearEnd), j.dir);
                        tap.on = a > -6f && a < 4f;
                        if (a > -8f && vIn < 0f) vIn = Flat(car.Body.linearVelocity).magnitude;
                        if (a > -6f && a < 4f) vMin = Mathf.Min(vMin, Flat(car.Body.linearVelocity).magnitude);
                        yield return new WaitForFixedUpdate();
                    }
                    yield return drive; tap.on = false;
                    bool ok = run.reached && vMin > vIn * 0.8f;
                    if (!ok) bad.Add($"{Nm(id)} {off:+0.0;-0.0;0} m");
                    report.AppendLine($"  {Nm(id),-16} {off,4:+0.0;-0.0;0} m off the line: {vIn:0.0} m/s 8 m before the deck, slowest {vMin:0.0} m/s from 6 m before to 4 m on it, {(run.reached ? "on" : "STOPPED " + run.end)}{(tap.seen.Count > 0 ? "; touched: " + string.Join("; ", tap.seen.Distinct().Take(4)) : "")}");
                }
            Check("BUG-001: every trail vehicle onto the footbridge at race speed, on the line and up to 2.5 m either side (the tread is 4.5 m wide), without a catch (keeps 80% of its speed)", bad.Count == 0, bad.Count == 0 ? "" : "caught: " + string.Join(", ", bad));
        }

        // ---------------------------------------------------------------- the whole lap, looking for dead stops

        IEnumerator Lap(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== The Noonday Creek lap on each trail vehicle, flat out: every dead stop or sudden loss of speed on the ground");
            UseRoute("noonday_creek.json", "forward");
            float L = race.Route.LapLength;
            var small = Small.Where(ids.Contains).ToArray(); if (small.Length == 0) small = Small;
            int stops = 0;
            foreach (var id in small)
            {
                yield return Vehicle(id);
                var line = LapSlice(race.Route.GateS, race.Route.GateS + L + 20f);
                Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                var run = new Run(); var hist = new Queue<(float t, float v, Vector3 p)>(); var found = new List<string>(); float lastAt = -99f; float t = 0f;
                var tap = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>(); tap.seen.Clear(); tap.on = true;
                var drive = StartCoroutine(DriveLine(line, 99f, 240f, run, null, null, 4f, false));
                while (!run.reached && run.end == "" && run.time < 239.9f)
                {
                    yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                    float v = Flat(car.Body.linearVelocity).magnitude; var p = car.Body.position;
                    if (tap.seen.Count > 40) tap.seen.RemoveRange(0, tap.seen.Count - 40);
                    hist.Enqueue((t, v, p)); while (hist.Count > 0 && hist.Peek().t < t - 0.5f) hist.Dequeue();
                    float vMax = hist.Max(h => h.v);
                    bool inPipe = DrainPipe.Inside(p, out _) != null;
                    if (car.GroundedWheels > 0 && vMax > 10f && v < vMax * 0.45f && t - lastAt > 3f && !inPipe)
                    {
                        lastAt = t; var at = hist.First(h => h.v == vMax).p;
                        var hits = tap.seen.Where(x => !x.Contains("Terrain") || !x.Contains("normal (0.0")).Reverse().Take(3).ToList();
                        found.Add($"{vMax:0.0} to {v:0.0} m/s in 0.5 s at ({p.x:0.0}, {p.z:0.0}), lap station {race.Route.LapStationOf(p):0} m (from ({at.x:0.0}, {at.z:0.0})); last contacts: {(hits.Count > 0 ? string.Join("; ", hits) : "none")}");
                    }
                }
                yield return drive; tap.on = false;
                stops += found.Count;
                report.AppendLine($"  {Nm(id),-16} {(run.reached ? $"the lap in {run.time:0.0} s" : "DID NOT FINISH " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})")}, {found.Count} sudden stop(s){(found.Count > 0 ? ":" : "")}");
                foreach (var f in found) report.AppendLine("      " + f);
                if (!run.reached) stops++;
            }
            Check("the lap on each trail vehicle: no dead stop on the ground (none from over 10 m/s to under 45% of it within 0.5 s, pipes aside)", stops == 0, $"{stops} found (listed above)");
        }

        // ---------------------------------------------------------------- shared (as Round19Runner)

        void UseRoute(string file, string direction)
        {
            RaceController.RouteOverride = file; RaceController.DirectionOverride = direction; RaceController.LapsOverride = 0;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload();
        }

        IEnumerator Vehicle(string id) { if (sw) sw.Select(id); car.enabled = true; yield return new WaitForSecondsRealtime(0.4f); }
        static string Nm(string id) => VehicleProfile.Find(id).Name;

        void Place(Vector3 p, Vector3 fwd, float speed)
        {
            fwd.y = 0f; fwd.Normalize();
            respawn.PlaceAt(p + Vector3.up * 0.3f, Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg);
            car.Body.linearVelocity = fwd * speed;
            race.NotifyTeleported();
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }

        // a still picture from a camera placed by hand (the chase camera set aside for it)
        IEnumerator Still(string name, Vector3 at, Vector3 lookAt, float fov = 60f)
        {
            var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); bool was = chase && chase.enabled; if (chase) chase.enabled = false;
            float f0 = cam.fieldOfView; cam.fieldOfView = fov;
            cam.transform.position = at; cam.transform.LookAt(lookAt);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture(name);
            cam.fieldOfView = f0; if (chase) { chase.enabled = was; chase.Snap(); }
        }

        public sealed class Run
        {
            public bool reached; public float time, maxDev, maxAir, minSpeed = 99f, speedAt = -1f; public int minWheels = 99;
            public Vector3 firstLanding = new Vector3(float.NaN, 0f, 0f); public string end = ""; public float upAtLanding = -1f;
            public List<Vector3> trail = new List<Vector3>();
        }

        IEnumerator DriveLine(List<Vector3> line, float cap, float timeout, Run run, Vector3? markAt = null, Func<Run, bool> stop = null, float aimAhead = 5f, bool giveUp = true, float[] profile = null)
        {
            car.enabled = false;
            var cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Flat(line[i] - line[i - 1]).magnitude;
            float total = cum[cum.Length - 1], t = 0f, air = 0f, stopped = 0f;
            float markS = markAt.HasValue ? Project(line, cum, markAt.Value, out _) : -1f; bool marked = false;
            float lastS = 0f;
            while (t < timeout)
            {
                yield return new WaitForFixedUpdate();
                float dt = Time.fixedDeltaTime; t += dt;
                var p = car.Body.position; float s = ProjectNear(line, cum, p, lastS, out float dev); lastS = s;
                if (t > 0.6f) { run.maxDev = Mathf.Max(run.maxDev, dev); run.minWheels = Mathf.Min(run.minWheels, car.GroundedWheels); }
                if (!marked && markS >= 0f && s >= markS) { marked = true; run.speedAt = Flat(car.Body.linearVelocity).magnitude; }
                if (s >= total - 1.5f) { run.reached = true; break; }
                if (stop != null && stop(run)) break;
                var here = At(line, cum, s); var fl = Flat(At(line, cum, s + 0.5f) - here).normalized;
                var aim = At(line, cum, s + aimAhead + 0.12f * Mathf.Max(0f, car.ForwardSpeed));
                float cte = Vector3.Dot(Flat(p - here), Vector3.Cross(Vector3.up, fl));
                float head = Vector3.SignedAngle(Flat(car.transform.forward).normalized, Flat(aim - here).normalized, Vector3.up);
                float corr = -Mathf.Atan2(1.6f * cte, Mathf.Max(car.ForwardSpeed, 1f) + 2f) * Mathf.Rad2Deg;
                float steer = Mathf.Clamp((head + corr) / 18f, -1f, 1f);
                float want = cap;
                for (float d = 4f; d <= 40f; d += 4f)
                {
                    Vector3 a = At(line, cum, s + d - 4f), b = At(line, cum, s + d), c = At(line, cum, s + d + 4f);
                    float area = Mathf.Abs(Vector3.Cross(Flat(b - a), Flat(c - a)).y); if (area < 1e-3f) continue;
                    float r = Flat(b - a).magnitude * Flat(c - b).magnitude * Flat(c - a).magnitude / (2f * area);
                    want = Mathf.Min(want, Mathf.Sqrt(Mathf.Pow(Mathf.Sqrt(0.8f * car.maxGripAcceleration * r), 2f) + 2f * 0.8f * car.braking * Mathf.Max(0f, d - 6f)));
                }
                if (profile != null)
                    for (int i = 0; i + 1 < profile.Length; i += 2)
                    {
                        if (s <= profile[i]) want = Mathf.Min(want, Mathf.Sqrt(profile[i + 1] * profile[i + 1] + 2f * 0.8f * car.braking * (profile[i] - s)));
                        else if (i + 3 < profile.Length && s < profile[i + 2]) want = Mathf.Min(want, Mathf.Lerp(profile[i + 1], profile[i + 3], Mathf.InverseLerp(profile[i], profile[i + 2], s)));
                    }
                float v = car.ForwardSpeed;
                if (car.GroundedWheels == 0) steer = 0f;
                car.Simulate(v < want ? 1f : 0f, v > want + 1.5f ? 1f : 0f, steer, dt);
                if (car.GroundedWheels == 0) air += dt; else { if (air > 0.3f && float.IsNaN(run.firstLanding.x)) { run.firstLanding = p; run.upAtLanding = car.transform.up.y; } air = 0f; }
                run.maxAir = Mathf.Max(run.maxAir, air);
                if (t > 0.5f) run.minSpeed = Mathf.Min(run.minSpeed, Flat(car.Body.linearVelocity).magnitude);
                stopped = car.Body.linearVelocity.magnitude < 1f ? stopped + dt : 0f;
                if (stopped > 4f && giveUp) { run.end = "stopped"; break; }
                if (stopped > 8f) { run.end = "stopped"; break; }
                if (((int)(t / 0.1f)) != ((int)((t - dt) / 0.1f))) run.trail.Add(p);
            }
            run.time = t;
            car.enabled = true;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static float Project(List<Vector3> line, float[] cum, Vector3 p, out float dev) => ProjectNear(line, cum, p, -1f, out dev);
        // nearest point on the line; with 'near' >= 0 only within 60 m of it along the line (a whole lap passes close to itself)
        static float ProjectNear(List<Vector3> line, float[] cum, Vector3 p, float near, out float dev)
        {
            float best = float.MaxValue, bs = 0f;
            for (int i = 0; i + 1 < line.Count; i++)
            {
                if (near >= 0f && (cum[i + 1] < near - 60f || cum[i] > near + 60f)) continue;
                Vector3 a = Flat(line[i]), ab = Flat(line[i + 1] - line[i]), q = Flat(p);
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(q - a, ab) / ab.sqrMagnitude);
                float d = (a + ab * t - q).sqrMagnitude;
                if (d < best) { best = d; bs = cum[i] + t * (cum[i + 1] - cum[i]); }
            }
            dev = Mathf.Sqrt(best); return bs;
        }
        static Vector3 At(List<Vector3> line, float[] cum, float s)
        {
            s = Mathf.Clamp(s, 0f, cum[cum.Length - 1]);
            int i = 0; while (i + 2 < cum.Length && cum[i + 1] < s) i++;
            return Vector3.Lerp(line[i], line[i + 1], Mathf.InverseLerp(cum[i], cum[i + 1], s));
        }
        List<Vector3> LapSlice(float s0, float s1, float step = 2f) { var l = new List<Vector3>(); for (float s = s0; s <= s1; s += step) l.Add(race.Route.PointAt(s, out _)); return l; }
        float AbsStation(Vector3 p) => race.Route.LapStationOf(p) + race.Route.GateS;
    }
}
