"""Round 76 D (Dan, 2026-10-27: "every round we would have some clean up so it didn't take up so much space on the computer"): the clean-up
at the end of every round, after its final build. Prints the disk use per folder before and after, and one line for the round's status.

Deletes: Builds/ChastainRushDev (the profiling build; made again only when a round profiles), local/'s check output of rounds older than the
last three (round<N>, r<N>, r<N>perf, determinism, _to_delete; a file a rule or a script names by path is kept, see KEEP), Temp/, Unity's
Library/BuildHistory (old build reports; Unity keeps writing new ones), then git gc.
Keeps: the player build, raw/, Assets/Generated, Data/, Library's import and player-data caches (deleting them only makes the next build
slower), and everything else in local/ (reference_photos, owner_*.json, debug_reports, race_records, bump_log, loose files).
Usage: python Tools/cleanup_round.py [--dry-run]"""
import os
import re
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FOLDERS = ["Builds", "Assets/Generated", "local", "raw", ".git", "Data/screenshots", "Library", "Temp"]
SCRATCH = re.compile(r"(round\d+[a-z0-9]*|r\d+[a-z0-9]*|determinism|_to_delete)$")


def size_mb(p):
    total = 0
    for dp, _, fns in os.walk(p):
        for fn in fns:
            try: total += os.path.getsize(os.path.join(dp, fn))
            except OSError: pass
    return total / 1e6


def named_paths():
    """local/... paths that CLAUDE.md, TODO.md or a script names: kept."""
    pat = re.compile(r"local/[A-Za-z0-9_]+/[A-Za-z0-9_./-]+")
    out = set()
    for base in ("CLAUDE.md", "TODO.md", "Tools", "Assets/Scripts", "Assets/Editor"):
        p = os.path.join(ROOT, base)
        files = [p] if os.path.isfile(p) else [os.path.join(dp, f) for dp, _, fs in os.walk(p) for f in fs if f.endswith((".py", ".cs", ".md"))]
        for f in files:
            try: text = open(f, encoding="utf-8", errors="ignore").read()
            except OSError: continue
            out.update(m.rstrip(".,;)`/") for m in pat.findall(text))
    return out


def main():
    dry = "--dry-run" in sys.argv
    before = {f: size_mb(os.path.join(ROOT, f)) for f in FOLDERS}
    local = os.path.join(ROOT, "local")
    nums = sorted({int(m.group(1)) for d in os.listdir(local) for m in [re.match(r"round(\d+)", d)] if m and os.path.isdir(os.path.join(local, d))})
    last3 = set(nums[-3:])
    keep = named_paths()
    gone = []
    for d in sorted(os.listdir(local)):
        p = os.path.join(local, d)
        if not os.path.isdir(p) or not SCRATCH.match(d): continue
        m = re.match(r"round(\d+)", d)
        if m and int(m.group(1)) in last3: continue
        for dp, dns, fns in os.walk(p, topdown=False):
            for fn in fns:
                rel = os.path.relpath(os.path.join(dp, fn), ROOT).replace("\\", "/")
                if rel in keep or any(rel.startswith(k + "/") for k in keep): continue
                if not dry: os.remove(os.path.join(dp, fn))
            if not dry:
                for dn in dns:
                    q = os.path.join(dp, dn)
                    if os.path.isdir(q) and not os.listdir(q): os.rmdir(q)
        if not dry and not os.listdir(p): os.rmdir(p)
        gone.append(d)
    for rel in ("Builds/ChastainRushDev", "Temp", "Library/BuildHistory"):
        p = os.path.join(ROOT, rel)
        if os.path.isdir(p):
            gone.append(rel)
            if not dry: shutil.rmtree(p, ignore_errors=True)
    if not dry: subprocess.run(["git", "-C", ROOT, "gc", "-q"])
    after = {f: size_mb(os.path.join(ROOT, f)) for f in FOLDERS}
    for f in FOLDERS:
        print(f"{f:20s} {before[f] / 1000:6.2f} GB -> {after[f] / 1000:6.2f} GB")
    print(f"cleared: {', '.join(gone) or 'nothing'}")
    print(f"Disk use {sum(before.values()) / 1000:.1f} GB -> {sum(after.values()) / 1000:.1f} GB (these folders){' (dry run)' if dry else ''}")


if __name__ == "__main__":
    main()
