using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 15 (item 6): Racer's RaceRoad, copied (a closed centreline: At, Project, ProjectNear, Relative,
    // the 16 m grid). Trimmed: Racer's own map parts (forest trail widths, the commercial highway blend and traffic lanes,
    // the connector-ramp bypass, geometry stations) are gone; HalfWidth is the lane's half width here. Built from the race
    // route's lap (RaceRoute.Path) by RaceController when AI drivers race.
    public sealed class RaceRoad : MonoBehaviour
    {
        public Vector3[] points;
        public bool openHighway;   // Racer: an open path (not closed); here the shortcuts' paths are open
        public float halfWidth = 4f;
        // Chastain Rush, Round 62: stretches with a half width of their own (RouteJson.ai_widths: a footbridge, an underpass, a mall's aisle)
        public Vector3[] halfZones;   // (s0, s1, half) each
        public float HalfWidth(float s)
        {
            if (halfZones != null)
                foreach (var z in halfZones) if (Mathf.Repeat(s - z.x, Length) <= z.y - z.x) return Mathf.Min(halfWidth, z.z);
            return halfWidth;
        }
        float[] distance;
        public float Length { get; private set; }

        public void Initialize()
        {
            if (distance != null && distance.Length == points.Length + 1 && Length > 0)
                return;
            distance = new float[points.Length + 1];
            for (int i = 0; i < points.Length-(openHighway?1:0); i++)
                distance[i + 1] = distance[i] + Vector3.Distance(points[i], points[(i + 1) % points.Length]);
            Length = distance[points.Length-(openHighway?1:0)];
            cellStart = null; // 0.82: the Project grid is rebuilt with the stations
        }

        public Vector3 At(float s, out Vector3 forward)
        {
            Initialize();
            s = openHighway?Mathf.Clamp(s,0,Length-.001f):Mathf.Repeat(s, Length);
            int lo = 0, hi = points.Length-(openHighway?1:0);
            while (lo + 1 < hi)
            {
                int mid = (lo + hi) / 2;
                if (distance[mid] <= s)
                    lo = mid;
                else
                    hi = mid;
            }

            var a = points[lo];
            var b = points[(lo + 1) % points.Length];
            forward = (b - a).normalized;
            return Vector3.Lerp(a, b, (s - distance[lo]) / (distance[lo + 1] - distance[lo]));
        }

        public float Project(Vector3 p, out float lateral)
        {
            Initialize();
            if (cellStart == null) BuildGrid();
            // 0.82 Part C: the nearest segment from a grid of the segments (XZ cells), searched outward ring by ring and
            // stopped once no unsearched cell can hold a closer one (3D distance >= horizontal distance >= ring * Cell).
            // Same distances and the same tie-break (lowest segment index) as the full scan this replaces, so the same
            // result; it used to test every segment on every call (each traffic car and AI calls it several times a step).
            float best = float.MaxValue, s = 0; int bestI = int.MaxValue;
            int cx = Mathf.FloorToInt((p.x - gridX) / Cell), cz = Mathf.FloorToInt((p.z - gridZ) / Cell);
            int rings = Mathf.Max(Mathf.Max(Mathf.Abs(cx), Mathf.Abs(cx - gridW + 1)), Mathf.Max(Mathf.Abs(cz), Mathf.Abs(cz - gridH + 1)));
            for (int k = FirstRing(cx, cz); k <= rings; k++)
            {
                Ring(cx, cz, k);
                foreach (int cell in ring)
                {
                        for (int j = cellStart[cell]; j < cellStart[cell + 1]; j++)
                        {
                            int i = cellItems[j];
                            var a = points[i];
                            var v = points[(i + 1) % points.Length] - a;
                            float t = Mathf.Clamp01(Vector3.Dot(p - a, v) / v.sqrMagnitude);
                            float d = (p - a - v * t).sqrMagnitude;
                            if (d < best || (d == best && i < bestI))
                            {
                                best = d; bestI = i;
                                s = distance[i] + t * (distance[i + 1] - distance[i]);
                            }
                        }
                }
                float bound = k * Cell; if (best < bound * bound) break;
            }

            lateral = Mathf.Sqrt(best);
            return s;
        }
        const float Cell = 16;
        // rings closer than the grid hold no cells; a ring lists only its cells inside the grid
        int FirstRing(int cx, int cz) => Mathf.Max(0, Mathf.Max(Mathf.Max(-cx, cx - gridW + 1), Mathf.Max(-cz, cz - gridH + 1)));
        readonly System.Collections.Generic.List<int> ring = new();
        void Ring(int cx, int cz, int k)
        {
            ring.Clear(); int x0 = Mathf.Max(cx - k, 0), x1 = Mathf.Min(cx + k, gridW - 1), z0 = Mathf.Max(cz - k, 0), z1 = Mathf.Min(cz + k, gridH - 1);
            for (int x = x0; x <= x1; x++)
            {
                if (x == cx - k || x == cx + k) { for (int z = z0; z <= z1; z++) ring.Add(x * gridH + z); }
                else { if (cz - k >= 0 && cz - k < gridH) ring.Add(x * gridH + cz - k); if (k > 0 && cz + k >= 0 && cz + k < gridH) ring.Add(x * gridH + cz + k); }
            }
        }
        int[] cellStart, cellItems; int gridW, gridH; float gridX, gridZ;
        void BuildGrid()
        {
            int n = points.Length - (openHighway ? 1 : 0);
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            foreach (var q in points) { minX = Mathf.Min(minX, q.x); minZ = Mathf.Min(minZ, q.z); maxX = Mathf.Max(maxX, q.x); maxZ = Mathf.Max(maxZ, q.z); }
            gridX = minX; gridZ = minZ; gridW = Mathf.Max(1, Mathf.FloorToInt((maxX - minX) / Cell) + 1); gridH = Mathf.Max(1, Mathf.FloorToInt((maxZ - minZ) / Cell) + 1);
            var count = new int[gridW * gridH + 1];
            void Cells(int i, System.Action<int> use)
            {
                var a = points[i]; var b = points[(i + 1) % points.Length];
                int x0 = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - gridX) / Cell), x1 = Mathf.FloorToInt((Mathf.Max(a.x, b.x) - gridX) / Cell);
                int z0 = Mathf.FloorToInt((Mathf.Min(a.z, b.z) - gridZ) / Cell), z1 = Mathf.FloorToInt((Mathf.Max(a.z, b.z) - gridZ) / Cell);
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(gridW - 1, x1); x++) for (int z = Mathf.Max(0, z0); z <= Mathf.Min(gridH - 1, z1); z++) use(x * gridH + z);
            }
            for (int i = 0; i < n; i++) Cells(i, c => count[c + 1]++);
            for (int c = 0; c < gridW * gridH; c++) count[c + 1] += count[c];
            cellStart = (int[])count.Clone(); cellItems = new int[count[gridW * gridH]];
            var fill = (int[])count.Clone();
            for (int i = 0; i < n; i++) Cells(i, c => cellItems[fill[c]++] = i);
        }

        public float Relative(float s, float origin) => Mathf.Repeat(s - origin, Length);

        // ---------------------------------------------------------------- Chastain Rush, Round 70 (B, Dan: "I think the ai should be not so slavish
        // to the actual roads"): the racing line. A lateral offset from the centerline every LineStep m, within the room on either side (room():
        // the lane's half width less the car's, and the open shoulder, lot or grass found beside it), relaxed toward the straightest line through the
        // corridor (each point pulled to the middle of its neighbors, coarse to fine): it uses the whole road, cuts across the inside of a bend to
        // its apex and runs out wide after it. Its curvature is what the AI plans its speed from (the centerline's polyline corners were
        // phantom bends). Closed lines only.
        public const float LineStep = 2f;
        float[] lineOff, lineCurv; float lineStep;
        public bool HasLine => lineOff != null;
        public float LineOffset(float s) => Sample(lineOff, s);
        public float LineCurvature(float s) => Sample(lineCurv, s);
        float Sample(float[] a, float s)
        {
            if (a == null) return 0f; int n = a.Length; float f = Mathf.Repeat(s, Length) / lineStep; int i = Mathf.FloorToInt(f); float t = f - i;
            return Mathf.Lerp(a[((i % n) + n) % n], a[(i + 1) % n], t);
        }
        public string LineInfo { get; private set; } = "";
        public void BuildLine(System.Func<Vector3, Vector3, float, int, float> room)
        {
            Initialize(); if (openHighway || Length < 50f) return;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int n = Mathf.Max(16, Mathf.RoundToInt(Length / LineStep)); lineStep = Length / n;
            var c = new Vector3[n]; var r = new Vector3[n]; var lo = new float[n]; var hi = new float[n];
            for (int i = 0; i < n; i++)
            {
                c[i] = At(i * lineStep, out var f); var fl = new Vector3(f.x, 0f, f.z); if (fl.sqrMagnitude < 1e-6f) fl = Vector3.forward;
                r[i] = Vector3.Cross(Vector3.up, fl.normalized).normalized;
                lo[i] = -room(c[i], r[i], i * lineStep, -1); hi[i] = room(c[i], r[i], i * lineStep, 1);
            }
            // the room eased along the line (each point's room the least within 6 m either side: no line darting into a one-sample gap)
            float[] Erode(float[] a, bool max) { var o = new float[n]; for (int i = 0; i < n; i++) { float v = a[i]; for (int k = -3; k <= 3; k++) { float w = a[((i + k) % n + n) % n]; v = max ? Mathf.Max(v, w) : Mathf.Min(v, w); } o[i] = v; } return o; }
            lo = Erode(lo, true); hi = Erode(hi, false);
            var off = new float[n];
            // coarse to fine: the relaxation on every 8th, 4th, 2nd, then every point
            foreach (int stride in new[] { 8, 4, 2, 1 })
            {
                int m = n / stride; if (m < 8) continue;
                for (int it = 0; it < (stride == 1 ? 300 : 500); it++)
                    for (int k = 0; k < m; k++)
                    {
                        int i = k * stride, ip = ((k - 1 + m) % m) * stride, inx = ((k + 1) % m) * stride;
                        var a = c[ip] + r[ip] * off[ip]; var b = c[inx] + r[inx] * off[inx]; var mid = (a + b) * .5f;
                        float want = Vector3.Dot(new Vector3(mid.x - c[i].x, 0f, mid.z - c[i].z), r[i]);
                        off[i] = Mathf.Clamp(off[i] + .7f * (want - off[i]), lo[i], hi[i]);
                    }
                if (stride > 1)   // the next level's points from this one's
                    for (int k = 0; k < m; k++)
                    {
                        int i0 = k * stride, i1 = ((k + 1) % m) * stride;
                        for (int j = 1; j < stride && i0 + j < n; j++) off[i0 + j] = Mathf.Clamp(Mathf.Lerp(off[i0], off[i1], j / (float)stride), lo[i0 + j], hi[i0 + j]);
                    }
            }
            // the curvature of the line (a circle through the points 6 m either side), averaged over 5 points
            var p = new Vector3[n]; for (int i = 0; i < n; i++) { p[i] = c[i] + r[i] * off[i]; p[i].y = 0f; }
            var kap = new float[n]; int kk = Mathf.Max(1, Mathf.RoundToInt(6f / lineStep));
            for (int i = 0; i < n; i++)
            {
                Vector3 A = p[((i - kk) % n + n) % n], B = p[i], C = p[(i + kk) % n];
                float ab = (B - A).magnitude, bc = (C - B).magnitude, ca = (A - C).magnitude, area2 = Mathf.Abs((B.x - A.x) * (C.z - A.z) - (B.z - A.z) * (C.x - A.x));
                kap[i] = ab * bc * ca > 1e-4f ? 2f * area2 / (ab * bc * ca) : 0f;
            }
            lineCurv = new float[n]; for (int i = 0; i < n; i++) { float sum = 0f; for (int k = -2; k <= 2; k++) sum += kap[((i + k) % n + n) % n]; lineCurv[i] = sum / 5f; }
            lineOff = off;
            float wide = 0f; int over = 0; for (int i = 0; i < n; i++) { wide = Mathf.Max(wide, Mathf.Max(hi[i], -lo[i])); if (Mathf.Abs(off[i]) > halfWidth) over++; }
            LineInfo = $"racing line: {n} points, room up to {wide:0.0} m, {over * lineStep:0} m of it past the lane's half width, {watch.ElapsedMilliseconds} ms";
        }
        public float ProjectNear(Vector3 p,float previous,float window,out float lateral)
        {
            // 0.82 Part C: the same grid search as Project (only segments whose projected station is within the window of
            // the previous one count, as before); same distances and tie-break as the old full scan, so the same result.
            Initialize(); if(cellStart==null)BuildGrid(); float best=float.MaxValue,result=previous; int bestI=int.MaxValue;
            int cx=Mathf.FloorToInt((p.x-gridX)/Cell),cz=Mathf.FloorToInt((p.z-gridZ)/Cell);
            int rings=Mathf.Max(Mathf.Max(Mathf.Abs(cx),Mathf.Abs(cx-gridW+1)),Mathf.Max(Mathf.Abs(cz),Mathf.Abs(cz-gridH+1)));
            for(int k=FirstRing(cx,cz);k<=rings;k++)
            {
                Ring(cx,cz,k);
                foreach(int cell in ring)
                {
                        for(int j=cellStart[cell];j<cellStart[cell+1];j++)
                        {
                            int i=cellItems[j];var a=points[i];var v=points[(i+1)%points.Length]-a;
                            float t=Mathf.Clamp01(Vector3.Dot(p-a,v)/Mathf.Max(.001f,v.sqrMagnitude));
                            float s=distance[i]+t*(distance[i+1]-distance[i]);
                            float delta=Mathf.Repeat(s-previous+Length*.5f,Length)-Length*.5f;
                            if(Mathf.Abs(delta)>window)continue;
                            float d=(p-a-v*t).sqrMagnitude;
                            if(d<best||(d==best&&i<bestI)){best=d;bestI=i;result=s;}
                        }
                }
                float bound=k*Cell;if(best<bound*bound)break;
            }
            lateral=Mathf.Sqrt(best);return result;
        }
    }
}
