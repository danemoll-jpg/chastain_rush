"""Round 62 (C): per -exeShots spot drive (names starting bug001 / bug002 / i575): the sharpest jolt and longest air within 60 m of Dan's spot, and the speed there. Usage: python Tools/mapgen/jolt_spots.py <dir>"""
import glob, os, sys, math
SPOTS = {"bug001": (617.23, 9.33), "bug002": (512.91, 18.66), "i575": (512.91, 18.66)}
for f in sorted(glob.glob(os.path.join(sys.argv[1], "*_trace.tsv"))):
    nm = os.path.basename(f)[:-10]; sp = next((v for k, v in SPOTS.items() if nm.startswith(k)), None)
    rows = []
    for ln in open(f).read().splitlines()[1:]:
        c = ln.split("\t")
        try: rows.append([float(v) for v in c[1:7]] + [int(c[7])])
        except (ValueError, IndexError): pass
    j = air = mair = 0.0; spd = []
    for i in range(1, len(rows)):
        t, x, y, z, vy, s, wh = rows[i]
        if t < .6 or (sp and math.dist((x, z), sp) > 60): air = 0.0; continue
        j = max(j, abs(vy - rows[i - 1][4])); air = air + .02 if wh == 0 else 0.0; mair = max(mair, air); spd.append(s)
    print(f"{nm}\tjolt {j:.2f} m/s\tair {mair:.2f} s\tspeed {min(spd) if spd else 0:.1f}-{max(spd) if spd else 0:.1f} m/s")
