"""Compare two crossing-check reports run by run (Round 89): better / same / worse.

usage: python Tools/race/compare_crossings.py <before.txt> <after.txt> [--out file] [--all]
A run's badness: not through > resets > wall hits > air over 0.3 s > landing over 6 m/s.
Worse = a run that was clean and is now faulted, or a faulted run whose air or landing rose by
more than 0.15 s / 1.5 m/s, or that gained resets or wall hits.
"""
import re, sys, math

RX = re.compile(r"^\s{2}(?P<name>.+?) \((?P<id>\d+)\) (?P<role>over|under) at \((?P<x>-?\d+), (?P<z>-?\d+)\).*?(?P<other>\(the other way\))?, \d+ m: "
                r"(?P<not>NOT )?through in (?P<t>[\d.]+) s, top \d+ m/s, longest in the air (?P<air>[\d.]+) s, hardest landing (?P<land>[\d.]+) m/s "
                r"\(at \((?P<lx>-?\d+), (?P<lz>-?\d+)\)\), resets (?P<res>\d+), wall hits (?P<wall>\d+)")


def load(path):
    runs = []
    text = open(path, encoding="utf-8", errors="replace").read()
    # a result can wrap in the file: join continuation lines (they start with no two-space indent)
    lines = []
    for ln in text.splitlines():
        if lines and ln and not ln.startswith(" ") and not ln.startswith("===") and not ln.startswith("FAIL") and not ln.startswith("PASS") \
                and not ln.startswith("Round") and lines[-1].startswith("  ") and "<- LOOK" not in lines[-1] and ": through" not in ln:
            lines[-1] += " " + ln
        else:
            lines.append(ln)
    for ln in lines:
        m = RX.match(ln)
        if not m:
            continue
        d = m.groupdict()
        runs.append(dict(name=d["name"], id=d["id"], role=d["role"], x=int(d["x"]), z=int(d["z"]), other=bool(d["other"]),
                         through=not d["not"], air=float(d["air"]), land=float(d["land"]), res=int(d["res"]), wall=int(d["wall"]),
                         at=(int(d["lx"]), int(d["lz"])), text=ln.strip()))
    return runs


def faulted(r):
    return (not r["through"]) or r["res"] > 0 or r["wall"] > 0 or r["air"] > 0.3 or r["land"] > 6.0


def score(r):
    return (0 if r["through"] else 100) + 10 * r["res"] + 5 * r["wall"] + max(0.0, r["air"] - 0.3) * 4 + max(0.0, r["land"] - 6.0) * 0.5


def match(a, bs, used):
    best, bd = None, 1e9
    for i, b in enumerate(bs):
        if i in used or b["id"] != a["id"] or b["role"] != a["role"] or b["other"] != a["other"]:
            continue
        d = math.hypot(b["x"] - a["x"], b["z"] - a["z"])
        if d < bd:
            best, bd = i, d
    return best if bd <= 40 else None


def verdict(a, b):
    fa, fb = faulted(a), faulted(b)
    if not fa and not fb:
        return "same (clean)"
    if fa and not fb:
        return "better (now clean)"
    if not fa and fb:
        return "WORSE (was clean)"
    if (b["through"] < a["through"]) or b["res"] > a["res"] or b["wall"] > a["wall"] or b["air"] > a["air"] + 0.15 or b["land"] > a["land"] + 1.5:
        if score(b) <= score(a) and b["through"] >= a["through"]:
            return "same (faulted, mixed)"
        return "WORSE"
    if score(b) < score(a) - 0.5:
        return "better (still faulted)"
    return "same (faulted)"


def brief(r):
    s = ("through" if r["through"] else "NOT through") + f", air {r['air']:.2f} s, landing {r['land']:.1f} m/s"
    if r["res"]:
        s += f", resets {r['res']}"
    if r["wall"]:
        s += f", walls {r['wall']}"
    return s


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    out = sys.argv[sys.argv.index("--out") + 1] if "--out" in sys.argv else None
    if out in args:
        args.remove(out)
    show_all = "--all" in sys.argv
    A, B = load(args[0]), load(args[1])
    used, rows, counts = set(), [], {}
    for a in A:
        j = match(a, B, used)
        if j is None:
            rows.append(("missing in after", a, None))
            continue
        used.add(j)
        rows.append((verdict(a, B[j]), a, B[j]))
    for j, b in enumerate(B):
        if j not in used:
            rows.append(("new in after", None, b))
    lines = [f"before: {args[0]} ({len(A)} runs); after: {args[1]} ({len(B)} runs)"]
    for v, a, b in rows:
        k = v.split(" (")[0]
        counts[k] = counts.get(k, 0) + 1
    lines.append("counts: " + ", ".join(f"{k} {n}" for k, n in sorted(counts.items())))
    order = ["WORSE", "better", "same (faulted", "missing", "new"]
    for key in order:
        for v, a, b in rows:
            if not v.startswith(key):
                continue
            if v.startswith("same (clean") and not show_all:
                continue
            r = a or b
            lines.append(f"{v:24s} {r['name']} ({r['id']}) {r['role']} at ({r['x']}, {r['z']}){' the other way' if r['other'] else ''}: "
                         f"{brief(a) if a else '-'}  ->  {brief(b) if b else '-'}")
    text = "\n".join(lines)
    print(text)
    if out:
        open(out, "w", encoding="utf-8").write(text + "\n")


if __name__ == "__main__":
    main()
