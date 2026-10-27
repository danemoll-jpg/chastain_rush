"""Round 76 (C): steep faces in the built ground beside the Kennesaw race's and the summit leap's graded lines (report only), from a regular
grid of the built terrain (ChastainRush.GroundGrid.Run -gridRects "kennesaw:-1340,-6760,-560,-5120,2"). Round 75's scan compared points of
neighbouring offset lines by their index along each line, which drift apart round the bends: this one compares grid neighbours 2 m apart.
A face: a rise of over FACE_M between two neighbouring cells 2 m apart (over 50 degrees), within NEAR_M of a line, not within LIP_M of a jump's
lip (the kickers and lips are meant to be steep) and not on the graded bench itself; faces within 20 m are one spot.
Usage: python Tools/mapgen/bank_faces.py <kennesaw.csv> [out.txt]"""
import csv, json, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FACE_M, NEAR_M, LIP_M = 2.4, 60.0, 60.0


def main():
    rows = list(csv.DictReader(open(sys.argv[1])))
    xs = sorted({float(r["x"]) for r in rows}); zs = sorted({float(r["z"]) for r in rows})
    ix = {x: i for i, x in enumerate(xs)}; iz = {z: i for i, z in enumerate(zs)}
    H = np.full((len(zs), len(xs)), np.nan)
    for r in rows:
        try: H[iz[float(r["z"])], ix[float(r["x"])]] = float(r["terrain"])
        except ValueError: pass
    lines, lips = [], []
    for f in ("Data/tracks/kennesaw_mountain.json", "Data/tracks/kennesaw_summit_leap.json"):
        d = json.load(open(os.path.join(ROOT, f)))
        for g in d["grades"]:
            p = np.array(g["points"]).reshape(-1, 2); hv = np.array(g.get("halves") or [g["half"]] * len(p)); lines.append((p, hv))
        for j in (d.get("jumps") or [d.get("jump")]):
            if j: lips.append(tuple(j["lip"][:2]))
    X, Z = np.meshgrid(np.array(xs), np.array(zs))
    near = np.full(X.shape, np.inf); onbench = np.zeros(X.shape, bool)
    for p, hv in lines:
        for k in range(0, len(p), 2):
            d = np.hypot(X - p[k, 0], Z - p[k, 1]); near = np.minimum(near, d); onbench |= d < hv[k] - 0.5
    lipd = np.full(X.shape, np.inf)
    for a, b in lips: lipd = np.minimum(lipd, np.hypot(X - a, Z - b))
    dx = np.abs(np.diff(H, axis=1)); dz = np.abs(np.diff(H, axis=0))
    face = np.zeros(X.shape); face[:, :-1] = np.fmax(face[:, :-1], np.nan_to_num(dx)); face[:-1, :] = np.fmax(face[:-1, :], np.nan_to_num(dz))
    hit = (face > FACE_M) & (near < NEAR_M) & (lipd > LIP_M) & ~onbench
    k = np.argwhere(hit); vals = sorted(((face[a, b], X[a, b], Z[a, b], near[a, b]) for a, b in k), reverse=True)
    spots = []
    for v in vals:
        if any(math.hypot(v[1] - s[1], v[2] - s[2]) < 20 for s in spots): continue
        spots.append(v)
    out = [f"{len(spots)} spots (faces over {FACE_M} m in 2 m within {NEAR_M:.0f} m of the lines, off the benches, {LIP_M:.0f} m from the lips); {int(hit.sum())} cells"]
    out += [f"{s[1]:8.1f} {s[2]:8.1f}  {s[0]:4.1f} m in 2 m, {s[3]:4.1f} m from a line" for s in spots]
    txt = "\n".join(out); print(txt)
    if len(sys.argv) > 2: open(sys.argv[2], "w").write(txt + "\n")


if __name__ == "__main__":
    main()
