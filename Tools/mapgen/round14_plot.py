"""Round 14: plot the exe speed traces written by ChastainRush.exe -exeRound14 (Checks/Round14Runner.cs).

python Tools/mapgen/round14_plot.py <before_dir> <after_dir> <out_dir>

One picture per run (straight, climb, lap): speed against distance for each vehicle, before (thin) and after (thick),
with a dot wherever the body's contacts took more than 0.5 m/s in one physics step. Report only."""
import csv, os, sys
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

before, after, out = sys.argv[1:4]
os.makedirs(out, exist_ok=True)
NAMES = {"moto": "Needle 600", "mower": "Turf Rocket", "atv": "Trail Four", "pebble": "Pebble Coupe", "original": "Street Classic"}
COLOURS = {"moto": "#c0392b", "mower": "#27ae60", "atv": "#2471a3", "pebble": "#8e44ad", "original": "#7f8c8d"}
TITLES = {"straight": "New Chastain Road straight, full throttle from rest",
          "climb": "Steepest 150 m of Chastain Glen Lane, full throttle from 8 m/s",
          "lap": "One lap of the Chastain Glen loop (scripted driver)"}

def load(path):
    if not os.path.exists(path): return None
    with open(path, newline="") as f: return list(csv.DictReader(f, delimiter="\t"))

for run in ("straight", "climb", "lap"):
    fig, axes = plt.subplots(len(NAMES), 1, figsize=(13, 2.3 * len(NAMES)), sharex=True)
    for ax, (vid, name) in zip(axes, NAMES.items()):
        for tag, d, lw, alpha in (("before", before, 1.0, 0.55), ("after", after, 2.0, 1.0)):
            rows = load(os.path.join(d, f"r14_{tag}_{vid}_{run}.tsv"))
            if not rows: continue
            s = [float(r["s"]) for r in rows]; v = [float(r["speed"]) for r in rows]
            ax.plot(s, v, color=COLOURS[vid], lw=lw, alpha=alpha, label=f"{tag}")
            hits = [(float(r["s"]), float(r["speed"])) for r in rows if float(r["body"]) < -0.5]
            if hits: ax.scatter([h[0] for h in hits], [h[1] for h in hits], s=18, color="black", zorder=5, marker="x" if tag == "before" else "o")
        ax.set_ylabel("m/s"); ax.set_title(name, loc="left", fontsize=10); ax.grid(alpha=0.3); ax.legend(loc="lower right", fontsize=8)
    axes[-1].set_xlabel("metres driven")
    fig.suptitle(TITLES[run] + "  (x / dot: a body contact took > 0.5 m/s in one 20 ms step)", fontsize=11)
    fig.tight_layout()
    fig.savefig(os.path.join(out, f"round14_{run}.png"), dpi=90)
    plt.close(fig)
    print("wrote", os.path.join(out, f"round14_{run}.png"))
