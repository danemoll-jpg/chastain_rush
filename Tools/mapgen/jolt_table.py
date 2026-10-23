"""Round 62 (C): the jolt scan's table from the -exeShots traces: per drive the sharpest jolt (change of the body's vertical speed in one physics
step), the longest spell with no wheel down, the slowest speed; per road the worst; before (the Round 61 exe) against after (this build).
Usage: python Tools/mapgen/jolt_table.py <before dir> [<after dir>]"""
import json, os, sys, glob, math
shots = {s["name"]: s for s in json.load(open("local/round62/jolt_shots.json"))["shots"]}
def scan(d):
    out = {}
    for f in glob.glob(os.path.join(d, "*_trace.tsv")):
        nm = os.path.basename(f)[:-10]; rows = []
        for ln in open(f).read().splitlines()[1:]:
            c = ln.split("\t")
            try: rows.append((float(c[1]), float(c[2]), float(c[3]), float(c[4]), float(c[5]), float(c[6]), int(c[7])))
            except (ValueError, IndexError): pass
        if len(rows) < 10: continue
        j = 0.0; at = None; air = 0.0; mair = 0.0; slow = 99.0
        for i in range(1, len(rows)):
            t, x, y, z, vy, sp, wh = rows[i]
            if t < 0.6: continue
            jj = abs(vy - rows[i - 1][4])
            if jj > j: j, at = jj, (round(x), round(z))
            air = air + 0.02 if wh == 0 else 0.0; mair = max(mair, air); slow = min(slow, sp)
        out[nm] = (j, at, mair, slow, rows[-1][1:4])
    return out
b = scan(sys.argv[1]); a = scan(sys.argv[2]) if len(sys.argv) > 2 else {}
roads = {}
for nm, s in shots.items():
    r = roads.setdefault(s["road"], {"b": (0, None, 0, 99), "a": (0, None, 0, 99), "n": 0})
    r["n"] += 1
    for k, src in (("b", b), ("a", a)):
        if nm in src:
            j, at, mair, slow, _ = src[nm]; w = r[k]
            r[k] = (max(w[0], j), at if j > w[0] else w[1], max(w[2], mair), min(w[3], slow))
print("road\tdrives\tbefore: sharpest jolt m/s (at), longest air s, slowest m/s\tafter: the same")
for nm, r in sorted(roads.items(), key=lambda kv: -kv[1]["b"][0]):
    fb, fa = r["b"], r["a"]
    print(f"{nm}\t{r['n']}\t{fb[0]:.2f} {fb[1]}, {fb[2]:.2f} s, {fb[3]:.1f}\t" + (f"{fa[0]:.2f} {fa[1]}, {fa[2]:.2f} s, {fa[3]:.1f}" if a else "-"))
allb = [v[0] for v in b.values()]; alla = [v[0] for v in a.values()]
def over(vals, x): return sum(1 for v in vals if v > x)
print(f"ALL: {len(b)} drives before, {len(a)} after; drives with a jolt over 1.5 m/s: {over(allb, 1.5)} -> {over(alla, 1.5) if a else '-'}; over 3 m/s: {over(allb, 3.0)} -> {over(alla, 3.0) if a else '-'}; "
      f"with air over 0.15 s: {sum(1 for v in b.values() if v[2] > .15)} -> {sum(1 for v in a.values() if v[2] > .15) if a else '-'}")
