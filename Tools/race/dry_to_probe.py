"""Round 88: a dry run's <out>.surf.tsv (MapBuilder.R83Dry.cs: the planned ground and deck tops under each line) as R88Probe's tsv, so
Tools/race/probe_ride.py can ride the planned surfaces (decks as BridgeDecks, the ground as Terrain; no bridge structure faces).

Usage: python Tools/race/dry_to_probe.py <dry.surf.tsv> <out.tsv>
"""
import sys

src, dst = sys.argv[1], sys.argv[2]
with open(src, encoding="utf-8") as f, open(dst, "w", encoding="utf-8") as o:
    next(f)
    o.write("line\tk\tx\tz\thits\n")
    for row in f:
        c = row.rstrip("\n").split("\t")
        hits = [(float(c[4]), "Terrain")]
        if len(c) > 5 and c[5]:
            hits += [(float(d.split("/")[0]), "BridgeDecks") for d in c[5].split("|")]
        hits.sort(reverse=True)
        o.write(f"{c[0]}\t{c[1]}\t{c[2]}\t{c[3]}\t" + "|".join(f"{y:.3f}/1.00/{n}" for y, n in hits) + "\n")
