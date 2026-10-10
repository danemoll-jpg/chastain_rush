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
        public float HalfWidth(float s) => halfWidth;
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
