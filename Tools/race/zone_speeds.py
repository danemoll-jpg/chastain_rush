"""Round 70 (B): the player's speed through each of a route's AI zones (held speeds), from a race replay, against the zone's speed."""
import sys, json, os
sys.path.insert(0, os.path.dirname(__file__)); import replay_ai_table as R
def main(rp, route):
    h, rs = R.read(rp); pts, c = R.lap_line(h); j = json.load(open(route, encoding="utf-8"))
    zones = j.get("ai_zones_reverse" if h["direction"] == "reverse" and j.get("ai_zones_reverse") else "ai_zones", [])
    off = j.get("gate_s", 0.0)   # zone stations are the route's own (from the loop's first point)
    for x in rs:
        if not x["player"]: continue
        fr = [f for f in x["frames"] if f[0] >= 0]; prev = None; acc = {z["id"]: [] for z in zones}
        for f in fr:
            s, d = R.station(pts, c, f[1], f[3], prev); prev = s
            for z in zones:
                if z["s0"] <= s <= z["s1"] and d < 25: acc[z["id"]].append(f[4])
        for z in zones:
            v = acc[z["id"]]
            print(f"  {z['id']:<34} zone {z['speed']:5.1f} m/s   player mean {sum(v)/len(v) if v else 0:5.1f}  min {min(v) if v else 0:5.1f}")
if __name__ == "__main__": main(sys.argv[1], sys.argv[2])
