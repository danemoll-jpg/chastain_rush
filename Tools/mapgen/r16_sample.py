"""Round 77: the generated ground (Assets/Generated/terrain_full.r16, and terrain_piece3.r16 under stage 3's piece) as a sampler, in game
metres: heights the way the map build first sees them (before any of its levelling). Used by the Round 77 scans; also a command line:
python Tools/mapgen/r16_sample.py x z [x z ...]"""
import json, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
GEN = os.path.join(ROOT, "Assets", "Generated")


class Ground:
    def __init__(self):
        self.parts = []
        for nm in ("terrain_piece3", "terrain_full"):
            j = os.path.join(GEN, nm + ".json")
            if not os.path.exists(j): continue
            t = json.load(open(j)); a = np.memmap(os.path.join(GEN, nm + ".r16"), dtype="<u2", mode="r", shape=(t["grid_z"], t["grid_x"]))
            self.parts.append((t, a))

    def h(self, x, z):
        x = np.atleast_1d(np.asarray(x, float)); z = np.atleast_1d(np.asarray(z, float)); out = np.full(x.shape, np.nan)
        for t, a in self.parts:
            fx = (x - t["origin_x_m"]) / t["step_x_m"]; fz = (z - t["origin_z_m"]) / t["step_z_m"]
            ok = np.isnan(out) & (fx >= 0) & (fz >= 0) & (fx <= t["grid_x"] - 1) & (fz <= t["grid_z"] - 1)
            if not ok.any(): continue
            i0 = np.clip(np.floor(fx[ok]).astype(int), 0, t["grid_x"] - 2); k0 = np.clip(np.floor(fz[ok]).astype(int), 0, t["grid_z"] - 2)
            u = fx[ok] - i0; v = fz[ok] - k0
            q = lambda kk, ii: a[kk, ii].astype(float)
            v01 = (q(k0, i0) * (1 - u) + q(k0, i0 + 1) * u) * (1 - v) + (q(k0 + 1, i0) * (1 - u) + q(k0 + 1, i0 + 1) * u) * v
            out[ok] = t["min_height"] + v01 / 65535.0 * (t["max_height"] - t["min_height"])
        return out


if __name__ == "__main__":
    g = Ground(); v = [float(s) for s in sys.argv[1:]]
    for x, z in zip(v[0::2], v[1::2]): print(x, z, g.h(x, z)[0])
