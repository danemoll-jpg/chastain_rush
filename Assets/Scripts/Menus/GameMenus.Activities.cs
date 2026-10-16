using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 36 (Part B): Racer's Free Roam activities in the menu (Racer's pause menu > ACTIVITIES: the activity selected, left / right to the next,
    // its three medal targets and the personal best, Start for a timed attempt); the speed traps need no start (drive through, either way). Here
    // also "Go there" (Free Roam only: to the site's approach, facing it), so a site is never a long drive away. The RECORDS page lists every
    // site's best and medal (GameMenus.Records.cs). Free Roam only, never in a race.
    public sealed partial class GameMenus
    {
        static ArcadeActivities Acts => ArcadeActivities.Instance;
        int ActivityRows(int n)
        {
            if (!Acts || Acts.Sites.Length == 0) return n;
            Row(n++, "activities", "ACTIVITIES", () => Navigate("activities"));
            return n;
        }
        int activitySite = -1;
        void RenderActivities()
        {
            var a = Acts; var sites = a ? a.Sites : new ActivitySite[0];
            if (sites.Length == 0) { Clear("ACTIVITIES", "No activities here."); Row(0, "back", "Back", BackPage); return; }
            if (activitySite < 0 || activitySite >= sites.Length) activitySite = Mathf.Max(0, System.Array.IndexOf(sites, a.Selected ? a.Selected : sites[0]));
            var s = sites[activitySite]; string kind = s.kind == ActivitySite.Kind.Jump ? "Jump" : s.kind == ActivitySite.Kind.Speed ? "Speed trap" : "Smash run";
            var pb = a.PersonalBest(s);
            float dist = car ? Vector3.ProjectOnPlane(s.transform.position - car.Body.position, Vector3.up).magnitude : 0f;
            string how = s.kind == ActivitySite.Kind.Speed ? "Drive through it, either way, as fast as you can." : s.kind == ActivitySite.Kind.Jump ? "Land a jump off its launch: the distance counts. Any landing scores; Start gives a timed attempt." : $"Smash its {s.PropCount} marked props in {s.Seconds:0} s.";
            Clear("ACTIVITIES", $"{s.title} · {kind} · {DisplayUnits.Distance(dist)} away\n{how}\nYour best: {(pb != null ? ArcadeActivities.Measurement(s, pb.value) + " · " + MedalUi.Name(pb.medal) : "none yet")}");
            var medals = LaterGroup("Activity medals", content, false, 40); medals.SetSiblingIndex(details.transform.GetSiblingIndex() + 1);
            MedalUi.TargetsRow(medals, details.font, a.TargetStrings(s), 22, 30, pb != null ? pb.medal : 0, Color.white, "MEDALS");
            int n = 0;
            Step(n++, "activity", $"{s.title}   ·   {activitySite + 1} / {sites.Length}", d => { activitySite = (activitySite + d + sites.Length) % sites.Length; Show(); });
            bool free = !race || race.State == RaceController.Stage.Free;
            if (s.kind != ActivitySite.Kind.Speed) { Row(n, "activity-start", "START ATTEMPT", () => { a.Select(s); a.BeginAttempt(); CloseMenu(); }); buttons[n++].interactable = free; }
            Row(n, "activity-go", "Go there", () => { if (GoToSite(s)) CloseMenu(); }); buttons[n++].interactable = free;
            Row(n++, "back", "Back", BackPage);
        }
        // to the site's approach, facing it (a jump: 60 m back along its launch; a speed trap: 80 m back; a smash run: beside it)
        bool GoToSite(ActivitySite s)
        {
            var respawn = car ? car.GetComponent<VehicleRespawn>() : null; if (!respawn) return false;
            var f = Vector3.ProjectOnPlane(s.forward, Vector3.up).normalized; if (f.sqrMagnitude < .5f) f = Vector3.forward;
            float back = s.kind == ActivitySite.Kind.Jump ? 60f : s.kind == ActivitySite.Kind.Speed ? 80f : s.radius * .6f;
            foreach (float k in new[] { 1f, .7f, .45f, .25f })
            {
                var p = s.transform.position - f * back * k; var q = RaceRoute.Surface(p.x, p.z);
                if (Physics.OverlapBox(q + Vector3.up * 1.2f, new Vector3(1.2f, .6f, 2.4f), Quaternion.LookRotation(f), ~0, QueryTriggerInteraction.Ignore).All(c => c is TerrainCollider || c.attachedRigidbody == car.Body))
                { respawn.PlaceAt(q + Vector3.up * .3f, Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg); if (race) race.NotifyTeleported(); return true; }
            }
            return false;
        }
    }
}
