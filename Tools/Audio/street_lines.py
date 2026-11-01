"""Round 89 (B): the police radio's street lines for the whole map.

Lists every named street in Data/map.json that no D line of Tools/Audio/police-radio-lines.csv covers (as PoliceRadio.StreetId matches:
the full name, or the name without its Northeast / Northwest / Southeast / Southwest), in three groups:
  main     every named trunk / primary / secondary / tertiary road and its links
  highway  the motorways as people say them (I-75, I-575, the Express Lanes)
  local    residential / unclassified / living streets within 300 m of a race course, a police spawn point (any police road is one: the
           whole network), a hidden cop or a fast-travel spot
usage: python Tools/Audio/street_lines.py [--write main,highway[,local]] [--list <file>]
--write appends the chosen groups as new D lines (after the highest D id; existing ids never change), cop voice.
"""
import csv, json, math, os, re, sys, glob

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CSV = os.path.join(ROOT, "Tools", "Audio", "police-radio-lines.csv")
SUFFIX = (" Northeast", " Northwest", " Southeast", " Southwest")
MAIN = {"trunk", "trunk_link", "primary", "primary_link", "secondary", "secondary_link", "tertiary", "tertiary_link"}
LOCAL = {"residential", "unclassified", "living_street"}
# the motorways' OSM names as people say them
HIGHWAYS = {"Larry McDonald Memorial Highway": "I-75", "Phillip Landrum Memorial Highway": "I-575", "Northwest Corridor Express Lanes": "the Express Lanes"}


def base(n):
    for s in SUFFIX:
        if n.endswith(s):
            return n[: -len(s)]
    return n


def spoken(n):
    """the words for a street: its name without the quadrant (as the existing lines), abbreviations written out"""
    t = base(n).strip()
    t = re.sub(r"\bSt\b\.?", "Street", t)
    t = re.sub(r"\bRd\b\.?", "Road", t)
    t = re.sub(r"\bDr\b\.?", "Drive", t)
    t = re.sub(r"\bAve\b\.?", "Avenue", t)
    t = re.sub(r"\bPkwy\b\.?", "Parkway", t)
    t = re.sub(r"\bHwy\b\.?", "Highway", t)
    t = re.sub(r"^US (\d+)", r"U.S. \1", t)
    return t + "."


def load_csv():
    rows = list(csv.DictReader(open(CSV, encoding="utf-8")))
    covered = set()
    for r in rows:
        if r["id"].startswith("D"):
            for n in re.split(r"[;|]", r.get("map_names") or ""):
                if n.strip():
                    covered.add(n.strip().lower())
    return rows, covered


def existing_base(rows):
    """base name -> the D line already saying it ("Chastain Road Northwest" -> D002 says "Chastain Road")"""
    out = {}
    for r in rows:
        if r["id"].startswith("D"):
            for n in re.split(r"[;|]", r.get("map_names") or ""):
                if n.strip():
                    out.setdefault(base(n.strip()).lower(), r["id"])
    return out


def is_covered(name, covered):
    return name.lower() in covered or base(name).lower() in covered


def pts(flat):
    return [(flat[i], flat[i + 1]) for i in range(0, len(flat) - 1, 2)]


def anchors():
    """points a local street must be within 300 m of: race course lines, hidden cops, fast-travel spots"""
    A = []
    for f in glob.glob(os.path.join(ROOT, "Data", "routes", "*.json")):
        try:
            d = json.load(open(f, encoding="utf-8"))
        except Exception:
            continue
        if d.get("air"):
            continue
        for key in ("loop", "loop_rev", "lead_in"):
            v = d.get(key)
            if isinstance(v, list) and v and isinstance(v[0], (int, float)):
                A += pts(v)[::4]
    # fast travel (eggs.json destinations) and the hidden cruisers' spots
    try:
        e = json.load(open(os.path.join(ROOT, "Data", "explore", "eggs.json"), encoding="utf-8"))
        for p in e.get("destinations", []):
            if "x" in p and "z" in p:
                A.append((p["x"], p["z"]))
    except Exception:
        pass
    # the hidden cruisers' fixed spots (HiddenPolice.CantonSpots); the others it picks at run time along the roads, and the chase's cops spawn
    # wherever the player is, so those are not fixed places
    src = open(os.path.join(ROOT, "Assets", "Scripts", "Police", "HiddenPolice.cs"), encoding="utf-8").read()
    for m in re.finditer(r"\((-?\d+(?:\.\d+)?)f,\s*(-?\d+(?:\.\d+)?)f,\s*-?\d+(?:\.\d+)?f\)", src):
        A.append((float(m.group(1)), float(m.group(2))))
    return A


def main():
    m = json.load(open(os.path.join(ROOT, "Data", "map.json"), encoding="utf-8"))
    rows, covered = load_csv()
    groups = {"main": {}, "highway": {}, "local": {}}
    for r in m["roads"]:
        n = (r.get("name") or "").strip()
        if not n or r.get("tunnel"):
            continue
        c = r["class"]
        if n in HIGHWAYS or base(n) in HIGHWAYS:
            g = "highway"
        elif c in MAIN:
            g = "main"
        elif c in LOCAL:
            g = "local"
        elif c in ("motorway", "motorway_link"):
            g = "highway"
        else:
            continue
        if is_covered(n, covered):
            continue
        key = base(n)
        e = groups[g].setdefault(key, {"names": set(), "pts": []})
        e["names"].add(n)
        e["pts"] += pts(r["points"])[::3]
    # a street in two groups counts once, in the higher one
    for k in list(groups["local"]):
        if k in groups["main"] or k in groups["highway"]:
            del groups["local"][k]
    for k in list(groups["main"]):
        if k in groups["highway"]:
            del groups["main"][k]
    # a name whose base an existing line already says (the bare "Chastain Road" beside D002's "Chastain Road Northwest"): added to that line's
    # map_names, no new clip
    eb = existing_base(rows)
    extend = {}
    for g in groups:
        for k in list(groups[g]):
            if k.lower() in eb:
                extend.setdefault(eb[k.lower()], set()).update(groups[g][k]["names"] | {k})
                del groups[g][k]
    A = anchors()
    cell = {}
    for (x, z) in A:
        cell.setdefault((int(x // 300), int(z // 300)), []).append((x, z))

    def near(p):
        cx, cz = int(p[0] // 300), int(p[1] // 300)
        for i in (-1, 0, 1):
            for j in (-1, 0, 1):
                for q in cell.get((cx + i, cz + j), ()):
                    if (q[0] - p[0]) ** 2 + (q[1] - p[1]) ** 2 < 300 ** 2:
                        return True
        return False

    local_near = {k: v for k, v in groups["local"].items() if any(near(p) for p in v["pts"])}
    groups["local"] = local_near
    out = []
    for g in ("highway", "main", "local"):
        for k in sorted(groups[g]):
            text = HIGHWAYS.get(k, None)
            text = (text[0].upper() + text[1:] + ".") if text else spoken(k)
            out.append((g, k, text, sorted(groups[g][k]["names"])))
    chars = {g: sum(len(t) for gg, _, t, _ in out if gg == g) for g in groups}
    print(f"streets without a line: highway {len(groups['highway'])}, main {len(groups['main'])}, local near a course / cop / fast travel {len(groups['local'])}; "
          f"characters {chars}; names added to existing lines: {', '.join(f'{i} +{sorted(v)}' for i, v in sorted(extend.items()))}")
    if "--list" in sys.argv:
        with open(sys.argv[sys.argv.index("--list") + 1], "w", encoding="utf-8") as f:
            for g, k, t, names in out:
                f.write(f"{g}\t{t}\t{'; '.join(names)}\n")
    if "--write" in sys.argv:
        want = set(sys.argv[sys.argv.index("--write") + 1].split(","))
        last = max(int(r["id"][1:]) for r in rows if re.match(r"^D\d+$", r["id"]))
        add = [(g, k, t, names) for g, k, t, names in out if g in want]
        lines = open(CSV, encoding="utf-8").read().rstrip("\n").split("\n")
        # names added to existing lines (their text and clip unchanged)
        for i, l in enumerate(lines):
            rid = l.split(",", 1)[0]
            if rid in extend:
                r = next(csv.reader([l]))
                have = [n for n in re.split(r"[;|]", r[3]) if n.strip()]
                r[3] = ";".join(have + sorted(n for n in extend[rid] if n.lower() not in {h.lower() for h in have}))
                lines[i] = ",".join('"' + c.replace('"', '""') + '"' if ("," in c or '"' in c) else c for c in r)
        # new D lines go after the last D line (the file's order: R, P, U, S, H, D ...)
        idx = max(i for i, l in enumerate(lines) if re.match(r"^D\d+,", l))
        new = []
        for i, (g, k, t, names) in enumerate(add):
            mn = ";".join([k] + [n for n in names if n != k])
            q = lambda s: '"' + s.replace('"', '""') + '"' if ("," in s or '"' in s) else s
            new.append(f"D{last + 1 + i:03d},{q(t)},street name,{q(mn)},cop")
        lines[idx + 1: idx + 1] = new
        open(CSV, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
        print(f"wrote {len(new)} lines D{last + 1:03d} to D{last + len(new):03d}")


if __name__ == "__main__":
    main()
