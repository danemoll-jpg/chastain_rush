"""Round 82: steps in the built surface along the probe's lines (python Tools/mapgen/r82_probe_steps.py <probe.tsv>).

The probe (ChastainRush.R82Probe.Run) records the top surface every metre; a deck overhead shows as a jump of several metres up and back (ignored here:
over 3 m up, then back down within 40 m). Prints every change over 0.25 m in 1 m that is not one.
"""
import collections
import csv
import sys


def main():
    rows = list(csv.DictReader(open(sys.argv[1], encoding="utf-8"), delimiter="\t"))
    by = collections.defaultdict(list)
    for r in rows:
        by[r["line"]].append(r)
    for name, rs in by.items():
        ys = [float(r["top"]) if r["top"] != "NaN" else None for r in rs]
        # the surface driven on: drop overhead decks (a rise over 3 m that comes back within 40 samples)
        drive = list(ys)
        i = 1
        while i < len(drive):
            if drive[i] is not None and drive[i - 1] is not None and drive[i] - drive[i - 1] > 3:
                j = i
                while j < min(len(drive), i + 40) and drive[j] is not None and drive[j] - drive[i - 1] > 2:
                    j += 1
                if j < min(len(drive), i + 40):
                    for k in range(i, j):
                        drive[k] = None
                    i = j
                    continue
            i += 1
        out = []
        last = None
        for r, y in zip(rs, drive):
            if y is None:
                continue
            if last is not None and abs(y - last[0]) > .25 and abs(int(r["s"]) - int(last[1])) <= 2:
                out.append(f"  s {r['s']:>6} ({r['x']}, {r['z']}) {y - last[0]:+.2f} m onto {r['what']}")
            last = (y, r["s"])
        print(f"{name}: {len(out)} steps over 0.25 m")
        for o in out[:30]:
            print(o)


if __name__ == "__main__":
    main()
