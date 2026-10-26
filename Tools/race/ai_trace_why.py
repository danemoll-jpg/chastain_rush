"""Round 70 (B): what held the AI back, from a check's r70_trace_<course>.tsv (Round20Runner.R70.cs).
Per limiter (RoadDriver.Why): the time it was the binding limit, and the time lost against driving at the car's top target there
(seconds at speed v over a stretch that could have been driven at the free target). Per 100 m of the lap: mean speed, the main limiter."""
import sys, collections, csv
def main(path, piece=100.0):
    rows = list(csv.DictReader(open(path, encoding="utf-8"), delimiter="\t"))
    by = collections.defaultdict(list)
    for r in rows: by[r["ai"]].append(r)
    for ai, rs in by.items():
        tw = collections.Counter(); slow = collections.Counter(); piece_why = collections.defaultdict(collections.Counter); piece_v = collections.defaultdict(list)
        for a, b in zip(rs, rs[1:]):
            dt = float(b["t"]) - float(a["t"])
            if dt <= 0 or dt > .5: continue
            w = a["why"]; v = float(a["speed"]); tg = float(a["target"])
            key = w + (" " + a["of"] if w in ("zone", "zone ahead", "obstacle") else "")
            tw[key] += dt
            if v < 15: slow[key] += dt
            k = int(float(a["s"]) // piece); piece_why[k][key] += dt; piece_v[k].append(v)
        tot = sum(tw.values())
        print(f"\n{ai}: {tot:.0f} s traced")
        for k, t in tw.most_common(14): print(f"   {t:6.1f} s ({t / tot * 100:4.1f} %)  slow(<15 m/s) {slow[k]:5.1f} s  {k}")
        print("   per piece: " + "  ".join(f"{k * piece:.0f}:{sum(piece_v[k]) / len(piece_v[k]):.0f} {piece_why[k].most_common(1)[0][0][:14]}" for k in sorted(piece_v)))
if __name__ == "__main__": main(sys.argv[1], float(sys.argv[2]) if len(sys.argv) > 2 else 100.0)
