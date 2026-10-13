"""Round 23 follow-ups (item 10): the proposal markers as the network is built. Reads Data/pipes/network.json (Tools/mapgen/
pipe_network.py) and writes Data/proposals/round23_network.json (MapBuilder.Proposals.cs, the debug menu's "Proposal markers",
the map's proposal layer; the labels: Tools/handmade/make_proposal_labels.py):

  - the parts of the network NOT built yet (stage over build_stage): their pipes, junctions, exits, manholes, glass stretches,
    as planned (removed as each stage is built: the map's pipe layer is then the real network)
  - the UNDERGROUND RACE, kept (Dan, 2026-10-13 01:03: "We can keep the markers for the race though so I can see what is
    proposed"), moved onto the network as built: its lap (the hub floor, up to the mid ledge, M1 back to J1 through J2 and the
    glass under the north Enclave pond, the hairpin in J1, M5 along the Enclave pond's glass to J3 and into the hub on its top
    ledge, down the ramps to the floor), its checkpoints, the gates G1 to G9 that would seal the other ways (on the openings
    where they would stand) and the three switch shortcuts S1 to S3 with their switches. Nothing of the race is built.

Usage: python Tools/mapgen/network_markers.py   (then Tools/handmade/make_proposal_labels.py, then Build Map)
"""
import json, math, os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
NET = json.load(open(os.path.join(ROOT, "Data/pipes/network.json")))
OUT = os.path.join(ROOT, "Data/proposals/round23_network.json")
ST = NET["build_stage"]
T = {t["id"]: t for t in NET["tubes"]}
C = {c["id"]: c for c in NET["chambers"]}


def xz(t, every=4.0):
    p = t["pts"]; pts = [(p[i], p[i + 2]) for i in range(0, len(p), 3)]
    out = [pts[0]]; acc = 0.0
    for i in range(1, len(pts)):
        acc += math.dist(pts[i], pts[i - 1])
        if acc >= every or i == len(pts) - 1: out.append(pts[i]); acc = 0.0
    return out


def end_point(tid, at_b):
    p = T[tid]["pts"]; i = len(p) - 3 if at_b else 0
    return (p[i], p[i + 2])


def mid_point(tid, frac=0.5):
    pts = xz(T[tid], 1.0); return pts[int(frac * (len(pts) - 1))]


def flat(pts): return [round(v, 2) for q in pts for v in q]


lines, marks = [], []
# ---- what is not built yet
for t in NET["tubes"]:
    if t["stage"] <= ST: continue
    main = t["main"]
    col = t["stripe"] if t["stripe"] else ([0.93, 0.93, 0.93] if t["d"] < 5 else [0.75, 0.75, 0.78])
    lines.append(dict(set="M" if main else "B", name=t["id"], points=flat(xz(t)), spacing=20.0 if main else 26.0, color=col, stage=t["stage"]))
for c in NET["chambers"]:
    if c["stage"] <= ST: continue
    kind = "hub" if c["kind"] == "hub" else "shaft" if c["kind"] == "shaft" else "junction"
    text = {"hub": f"Chasm {c['length']:.0f} x {c['width']:.0f} m", "shaft": f"Drop shaft, {c.get('drop', 0):.0f} m", "junction": f"Floor {c['floor']:.0f} m"}[kind]
    marks.append(dict(set="N", kind=kind, title=c["name"], text=f"{text} (stage {c['stage']})", x=c["x"], z=c["z"]))
    if c["kind"] == "hub":
        u = (math.sin(math.radians(c["yaw"])), math.cos(math.radians(c["yaw"]))); v = (u[1], -u[0]); hl, hw = c["length"] / 2, c["width"] / 2
        ring = [(c["x"] + u[0] * a + v[0] * b, c["z"] + u[1] * a + v[1] * b) for a, b in ((hl, hw), (hl, -hw), (-hl, -hw), (-hl, hw), (hl, hw))]
        lines.append(dict(set="H", name="The hub (outline)", points=flat(ring), spacing=12.0, color=[0.95, 0.25, 0.85], stage=c["stage"]))
for m in NET["manholes"]:
    if m["stage"] <= ST: continue
    marks.append(dict(set="N", kind="manhole", title=f"{m['id']} MANHOLE", text=f"{m['road'].replace(' Northeast', '').replace(' Northwest', '')} (stage {m['stage']})", x=m["x"], z=m["z"]))
for x in NET["exits"]:
    if x["stage"] <= ST: continue
    marks.append(dict(set="N", kind="outfall" if x.get("outfall") else "exit", title=x["name"], text=f"Pipe {x['tube']} (stage {x['stage']})", x=x["a"][0], z=x["a"][1]))
for t in NET["tubes"]:
    if t["stage"] <= ST: continue
    for g in t["glass"]:
        p = xz(t, 1.0); s = (g["s0"] + g["s1"]) * 0.5; q = p[min(len(p) - 1, int(s))]
        marks.append(dict(set="N", kind="aquarium", title=f"GLASS {t['id']} W{g['pond']}", text=f"{g['s1'] - g['s0']:.0f} m under the pond (stage {t['stage']})", x=q[0], z=q[1]))
    if t.get("flood"):
        p = xz(t, 1.0); q = p[min(len(p) - 1, int((t["flood"]["s0"] + t["flood"]["s1"]) * 0.5))]
        marks.append(dict(set="N", kind="flooded", title=f"FLOODED {t['id']}", text=f"Slow (stage {t['stage']})", x=q[0], z=q[1]))

# ---- the underground race (kept; on the network as built)
hub = C["HUB"]; hu = (math.sin(math.radians(hub["yaw"])), math.cos(math.radians(hub["yaw"]))); hv = (hu[1], -hu[0])
def H(a, b): return (hub["x"] + hu[0] * a + hv[0] * b, hub["z"] + hu[1] * a + hv[1] * b)
def rev(p): return list(reversed(p))
lap = [H(0, -10), H(20, -15)]                                    # the floor by the pool, toward the floor ramp's foot (north side)
lap += [H(33, -12), H(-17, -12)]                                 # up the ramp to the mid ledge
lap += rev(xz(T["M1b"], 6.0)) + rev(xz(T["M1a"], 6.0))           # M1 back west: J2, the glass under W1, into J1
jc = C["J1"]; lap += [(jc["x"], jc["z"])]                        # the hairpin in J1
lap += xz(T["M5a"], 6.0) + xz(T["M5b"], 6.0)                     # M5 south: the glass along W4, J3, into the hub's top ledge
lap += [H(-5, 30), H(45, 30), H(45, -20), H(30, -25), H(0, -10)]  # along the top ledge, down the mid-to-top ramp, round to the floor
lines.append(dict(set="R", name="Underground race (lap)", points=flat(lap), spacing=30.0, color=[0.85, 0.75, 1.0], stage=1))
def glass_mid(tid):
    t = T[tid]; g = t["glass"][0] if t["glass"] else None
    if not g: return mid_point(tid)
    p = xz(t, 1.0); return p[min(len(p) - 1, int((g["s0"] + g["s1"]) * 0.5))]
race = [
    ("start", "RACE START / FINISH", "Hub floor, by the pool", H(0, -10)),
    ("checkpoint", "CP1 GLASS UNDER W1", "M1, the north pond", glass_mid("M1a")),
    ("checkpoint", "CP2 J1 SOUTH FORK", "The hairpin in J1", (jc["x"], jc["z"])),
    ("checkpoint", "CP3 GLASS UNDER W4", "M5, the Enclave pond", glass_mid("M5a")),
    ("checkpoint", "CP4 J3 ENCLAVE JCT", "M5, J3", (C["J3"]["x"], C["J3"]["z"])),
    ("checkpoint", "CP5 HUB TOP LEDGE", "South side, M5's mouth", end_point("M5b", True)),
    ("gate", "GATE G1 BF RIGHT PIPE", "Seals the entrance (race)", end_point("BF-R", True)),
    ("gate", "GATE G3 B1 AT J2", "Seals the north maze", end_point("B1a", False)),
    ("gate", "GATE G4 B4 AT J2", "Seals the drop shaft's way", end_point("B4b", True)),
    ("gate", "GATE G5 B6 AT J3", "Seals the Enclave maze", end_point("B6", False)),
    ("gate", "GATE G6 B7 AT J3", "Seals the Enclave maze", end_point("B7a", False)),
    ("gate", "GATE G7 M3 AT HUB", "Seals M3 east", end_point("M3a", False)),
    ("gate", "GATE G8 B1 AT HUB", "Seals the north maze", end_point("B1c", True)),
    ("gate", "GATE G9 B9 AT HUB", "Seals the south-east maze", end_point("B9a", False)),
    ("gate", "SHORTCUT GATE S1", "Waterfall leap (hub)", H(-5, 31)),
    ("switch", "SWITCH S1", "Plate up M5's wall", mid_point("M5b", 0.4)),
    ("gate", "SHORTCUT GATE S2", "Across the pond's bend", mid_point("M5a", 0.38)),
    ("switch", "SWITCH S2", "Lever, side alcove", mid_point("M5a", 0.12)),
    ("gate", "SHORTCUT GATE S3", "High line over J2", mid_point("M1b", 0.15)),
    ("switch", "SWITCH S3", "Plate in J1's corner", (jc["x"] + 6, jc["z"] - 6)),
]
for kind, title, text, at in race: marks.append(dict(set="R", kind=kind, title=title, text=text, x=round(at[0], 2), z=round(at[1], 2)))
lines.append(dict(set="R", name="S1 Waterfall leap", points=flat([H(-5, 31), H(-5, 20), H(-2, 0), H(0, -10)]), spacing=12.0, color=[1.0, 0.72, 0.2], stage=1))
lines.append(dict(set="R", name="S2 Across the bend", points=flat([mid_point("M5a", 0.38), mid_point("M5a", 0.72)]), spacing=12.0, color=[1.0, 0.72, 0.2], stage=1))
lines.append(dict(set="R", name="S3 High line", points=flat([mid_point("M1b", 0.15), (C["J2"]["x"], C["J2"]["z"]), glass_mid("M1a")]), spacing=12.0, color=[1.0, 0.72, 0.2], stage=1))

starts = [
    dict(name="Main entrance (BF)", x=1413.47, z=1176.45, yaw=41.0),
    dict(name="Glen pond entrance (GP)", x=2905.39, z=804.6, yaw=91.0),
    dict(name="Above the hub", x=1840.0, z=1145.0, yaw=90.0),
    dict(name="Parkwood Drive manholes", x=1640.0, z=1279.0, yaw=90.0),
    dict(name="Enclave (W4 pond)", x=1660.0, z=960.0, yaw=270.0),
    dict(name="Under the bridges (J6)", x=2410.0, z=840.0, yaw=180.0),
    dict(name="Behind the shops (J8)", x=3240.0, z=1275.0, yaw=270.0),
    dict(name="Between the lakes (J9)", x=900.0, z=1240.0, yaw=270.0),
]
doc = dict(note=f"Round 23 follow-ups: the proposal markers (TEMPORARY): what of the network is not built yet (build_stage {ST}) and the "
                "underground race (kept; nothing of it is built). Made by Tools/mapgen/network_markers.py from Data/pipes/network.json.",
           lines=lines, marks=marks, starts=starts)
json.dump(doc, open(OUT, "w", encoding="utf-8"), indent=1)
print(f"wrote {OUT}: build_stage {ST}; {len(lines)} lines, {len(marks)} marks ({sum(1 for m in marks if m['set'] == 'R')} of the race)")
