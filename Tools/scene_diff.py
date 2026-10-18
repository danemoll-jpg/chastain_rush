"""Round 42 (Part A): compare two scene dumps (ChastainRush.SceneDump.Run -dumpOut <file>).

    python Tools/scene_diff.py <a> <b> [--box x0,z0,x1,z1] [--list 20]

Lists every object and terrain block that differs (changed, only in a, only in b), split into those whose ground bounds
reach the box and those that do not. With no box: everything is "outside". Exit code 0 when the two are the same.
"""
import argparse
import sys


def load(path):
    out = {}
    with open(path, encoding="utf-8") as f:
        for line in f:
            parts = line.rstrip("\n").split("\t")
            if len(parts) != 4:
                continue
            kind, key, bounds, h = parts
            out[(kind, key)] = (tuple(float(v) for v in bounds.split(",")), h)
    return out


def reaches(b, box):
    if box is None:
        return False
    x0, z0, x1, z1 = b
    return not (x1 < box[0] or x0 > box[2] or z1 < box[1] or z0 > box[3])


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("a")
    ap.add_argument("b")
    ap.add_argument("--box")
    ap.add_argument("--list", type=int, default=20)
    args = ap.parse_args()
    box = tuple(float(v) for v in args.box.split(",")) if args.box else None
    a, b = load(args.a), load(args.b)
    groups = {"inside": [], "outside": []}
    for k in sorted(set(a) | set(b)):
        va, vb = a.get(k), b.get(k)
        if va and vb and va[1] == vb[1]:
            continue
        what = "changed" if va and vb else ("only in a" if va else "only in b")
        bounds = [v[0] for v in (va, vb) if v]
        side = "inside" if any(reaches(bb, box) for bb in bounds) else "outside"
        groups[side].append((k, what))
    print(f"a: {args.a} ({len(a)} lines)\nb: {args.b} ({len(b)} lines)" + (f"\nbox: {args.box}" if box else ""))
    for side in ("outside", "inside"):
        g = groups[side]
        objs = sum(1 for (k, _) in g if k[0] == "O")
        print(f"{side} the box: {len(g)} differences ({objs} objects, {len(g) - objs} terrain blocks)")
        for (k, what) in g[: args.list]:
            print(f"  {what}: {k[0]} {k[1]}")
        if len(g) > args.list:
            print(f"  ... {len(g) - args.list} more")
    return 0 if not groups["inside"] and not groups["outside"] else 1


if __name__ == "__main__":
    sys.exit(main())
