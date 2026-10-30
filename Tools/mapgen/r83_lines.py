"""Round 83: the lines the exe check drives (after the build; reads Assets/Generated/round82_crossings.txt through r82_lines.py).

  local/round83/r83_lines_a.json  the Express Lanes end to end (I-75's, I-575's) at 35 m/s, pictures at the three places of A, Round 82's places; the I-575
                                  Express flyover into the I-75 Express Lanes (A); the Big Shanty ramp
  local/round83/r83_lines_b.json  both roads through every crossing listed as changed or short (D: all of them, both ways), Skip Spann Connector and Frey Road
                                  among them (B), the ramp over I-75 by the Canton Road Connector (C)
Usage: python Tools/mapgen/r83_lines.py
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import r82_lines  # noqa: E402

ROOT = r82_lines.ROOT


def main():
    r82_lines.main()
    lines = json.load(open(os.path.join(ROOT, "local", "round82", "r82_lines.json"), encoding="utf-8"))["lines"]
    m = json.load(open(os.path.join(ROOT, "Data", "map.json"), encoding="utf-8"))
    R = {r["osm_id"]: r for r in m["roads"]}
    B = {b["osm_id"]: b for b in m["bridges"]}
    a, b = [], []
    for l in lines:
        if " over at " in l["name"] or " under at " in l["name"]:
            b.append(l)
        elif "end to end" in l["name"]:
            q = r82_lines.pts(l["line"])
            for nm, p in (("A_south", (527.0, -3383.0)), ("A_north", (513.0, -3332.0)), ("A_join", (845.0, -3832.0))):
                s, d = r82_lines.station(q, p)
                if d < 25:
                    l["pics"].append(round(s, 1))
                    l["picNames"].append(nm)
            a.append(l)
        elif "Big Shanty" in l["name"]:
            a.append(l)
    # A: the I-575 Express flyover into the I-75 Express Lanes (the I-575 Express road's last 300 m, its link, the flyover, the link down to the join,
    # 200 m of the lanes past it)
    q = r82_lines.pts(R[367181844]["points"])[-4:] + r82_lines.pts(R[624557373]["points"])[1:] + r82_lines.pts(B[451950526]["points"])[1:] \
        + r82_lines.pts(R[451950527]["points"])[1:] + r82_lines.pts(R[437647056]["points"])[1:4]
    q = r82_lines.resample(q, 4.0)
    st, nm = [], []
    for n, p in (("touchdown", (503.0, -3385.0)), ("beside", (760.0, -3770.0)), ("join", (850.0, -3832.0))):
        s, d = r82_lines.station(q, p)
        if d < 25:
            st.append(round(s, 1))
            nm.append(n)
    a.append({"name": "I-575 Express flyover into the I-75 Express Lanes", "both": True, "cap": 35, "line": [round(v, 2) for p in q for v in p], "pics": st, "picNames": nm})
    # B: Skip Spann Connector end to end, each carriageway (its bridge over I-75 and both approaches)
    for nm, ids, a0, a1 in (("Skip Spann Connector eastern carriageway end to end", (564146474, 564146488, 390749397, 564146480, 564146483, 564146479, 564146476, 390749398), (-791.0, 808.0), None),
                            ("Skip Spann Connector western carriageway end to end", (564146485, 390749399, 390749396, 564146481, 564146482, 390749401, 564146472), None, (-800.0, 790.0))):
        RB = dict(R); RB.update(B)
        q = ([a0] if a0 else []) + [p for i in ids for p in r82_lines.pts(RB[i]["points"])] + ([a1] if a1 else [])
        a.append({"name": nm, "both": True, "cap": 30, "line": [round(v, 2) for p in r82_lines.resample(q, 3.0) for v in p], "pics": [], "picNames": []})
    out = os.path.join(ROOT, "local", "round83")
    os.makedirs(out, exist_ok=True)
    json.dump({"lines": a}, open(os.path.join(out, "r83_lines_a.json"), "w", encoding="utf-8"))
    json.dump({"lines": b}, open(os.path.join(out, "r83_lines_b.json"), "w", encoding="utf-8"))
    print(f"r83_lines_a.json: {len(a)} lines; r83_lines_b.json: {len(b)} crossing lines ({2 * len(b)} runs)")


if __name__ == "__main__":
    main()
