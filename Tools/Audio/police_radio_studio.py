"""Police Radio Studio: make Chastain Rush's police radio lines with your ElevenLabs voices.

Double-click "Police Radio Studio.bat" (or run: python Tools/Audio/police_radio_studio.py).

- Lines come from Tools/Audio/police-radio-lines.csv (id, text, when, map_names, speaker).
- Each line is saved as <id>.wav in SourceArt/Audio/PoliceRadio/ (kept in the project, built into the game)
  and copied to the game's sound drop-in folder
  %USERPROFILE%/AppData/LocalLow/Chastain Rush/Chastain Rush/Sounds/PoliceRadio/ (heard without a rebuild).
- Extra takes are saved as R05a, R05b ...; the game picks one at random.
- Two voices (Round 45): the csv's "speaker" column says who says each line: "cop" (the cars and dispatch), "pilot" (the
  helicopter, Air one) or "cop+pilot" (a piece both can use: the pilot's take is saved as <id>_air.wav).
- Your API key is kept only on this PC (%APPDATA%/ChastainRushRadio/settings.json) or read from ELEVENLABS_API_KEY.
  It is never written into the project.
- From the command line, no window: python Tools/Audio/police_radio_studio.py --make-missing
Uses only Python's standard library (tkinter, urllib, wave, winsound).
"""
import csv, json, os, queue, shutil, sys, threading, time, urllib.error, urllib.request, wave
import tkinter as tk
from tkinter import messagebox, ttk

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
LINES = os.path.join(HERE, "police-radio-lines.csv")
OUT = os.path.join(ROOT, "SourceArt", "Audio", "PoliceRadio")
DROPIN = os.path.join(os.path.expanduser("~"), "AppData", "LocalLow", "Chastain Rush", "Chastain Rush", "Sounds", "PoliceRadio")
CFG_DIR = os.path.join(os.environ.get("APPDATA", os.path.expanduser("~")), "ChastainRushRadio")
CFG = os.path.join(CFG_DIR, "settings.json")
API = "https://api.elevenlabs.io/v1"
RATE = 24000
VOICES = {"cop": "g8CcnC07pMG4qiREyAqr", "pilot": "TTipgIQvJszYvCktGfAa"}   # Dan's two ElevenLabs voices (Round 45; IDs are not secrets)
GROUPS = [("All", ""), ("Missing only", "?"), ("Chase lines (R)", "R"), ("Phrase pieces (P)", "P"), ("Units (U)", "U"),
          ("Streets (D)", "D"), ("Speed Patrol (S)", "S"), ("Hidden police (H)", "H")]

try:
    import winsound
except ImportError:
    winsound = None


def load_cfg():
    try:
        return json.load(open(CFG, encoding="utf-8"))
    except Exception:
        return {}


def save_cfg(c):
    os.makedirs(CFG_DIR, exist_ok=True)
    json.dump(c, open(CFG, "w", encoding="utf-8"), indent=1)


def call(path, key, body=None):
    req = urllib.request.Request(API + path, data=None if body is None else json.dumps(body).encode(),
                                 headers={"xi-api-key": key, "Content-Type": "application/json"},
                                 method="GET" if body is None else "POST")
    with urllib.request.urlopen(req, timeout=120) as r:
        return r.read()


def speakers(row):
    """(file stem, voice) for every clip a line needs: cop -> <id>; pilot -> <id>; cop+pilot -> <id> and <id>_air"""
    sp = (row.get("speaker") or "cop").strip().lower()
    out = []
    if "cop" in sp: out.append((row["id"], "cop"))
    if "pilot" in sp: out.append((row["id"] + ("_air" if "cop" in sp else ""), "pilot"))
    return out


def files_for(lid):
    if not os.path.isdir(OUT):
        return []
    lid = lid.upper()
    out = []
    for f in os.listdir(OUT):
        stem, ext = os.path.splitext(f)
        s = stem.upper()
        if ext.lower() in (".wav", ".mp3") and (s == lid or (len(s) == len(lid) + 1 and s.startswith(lid) and s[-1].isalpha())):
            out.append(f)
    return sorted(out)


def missing(row):
    return any(not files_for(stem) for stem, _ in speakers(row))


def read_lines(fields=None):
    with open(LINES, encoding="utf-8", newline="") as f:
        r = csv.DictReader(f)
        return r.fieldnames or fields, [dict(x) for x in r]


def make_clips(jobs, key, settings, report, stop=None):
    """jobs: (file stem, text, voice id). Writes OUT/<stem>.wav (16-bit mono) and a copy in the drop-in folder."""
    os.makedirs(OUT, exist_ok=True)
    made = failed = 0
    for n, (name, text, vid) in enumerate(jobs, 1):
        if stop is not None and stop.is_set(): break
        report("status", f"Making {name} ({n} of {len(jobs)}): {text}")
        body = {"text": text, "model_id": "eleven_multilingual_v2", "voice_settings": settings}
        try:
            pcm = call(f"/text-to-speech/{vid}?output_format=pcm_{RATE}", key, body)
            path = os.path.join(OUT, name + ".wav")
            old = os.path.join(OUT, name + ".mp3")
            if os.path.exists(old):
                os.replace(old, old + ".old")
            with wave.open(path, "wb") as w:
                w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE); w.writeframes(pcm)
            try:
                os.makedirs(DROPIN, exist_ok=True); shutil.copy2(path, os.path.join(DROPIN, name + ".wav"))
            except OSError:
                pass
            made += 1
        except urllib.error.HTTPError as e:
            failed += 1
            msg = e.read()[:200].decode("utf-8", "replace")
            report("status", f"FAILED {name}: {e.code} {msg}")
            if e.code in (401, 402, 429):
                report("error", f"Stopped: {e.code} (key, credits or rate limit). Finished files are kept.\n{msg}"); break
        except Exception as e:
            failed += 1; report("status", f"FAILED {name}: {e}")
        time.sleep(0.3)
    return made, failed


def make_missing_cli():
    """--make-missing: every clip not yet made, both voices, no window (the key from ELEVENLABS_API_KEY or settings.json; never printed)"""
    cfg = load_cfg()
    key = os.environ.get("ELEVENLABS_API_KEY", "") or cfg.get("key", "")
    if not key:
        print("No API key: set ELEVENLABS_API_KEY or save it once in the studio."); return 1
    vids = {who: (cfg.get("voice_" + who) or VOICES[who]).rsplit("  ", 1)[-1].strip() for who in VOICES}
    settings = {n: round(float(cfg.get(n, d)), 2) for n, d in [("stability", 0.45), ("similarity_boost", 0.8), ("style", 0.2)]}
    _, rows = read_lines()
    jobs = [(stem, r["text"], vids[who]) for r in rows for stem, who in speakers(r) if not files_for(stem)]
    print(f"{len(jobs)} clips to make")
    made, failed = make_clips(jobs, key, settings, lambda kind, msg: print(msg, flush=True))
    print(f"Done: {made} made, {failed} failed.")
    return 0 if failed == 0 else 2


class App:
    def __init__(self, root):
        self.root = root
        root.title("Chastain Rush - Police Radio Studio")
        root.geometry("1100x720")
        self.cfg = load_cfg()
        self.q = queue.Queue()
        self.stop = threading.Event()
        self.busy = False
        self.rows, self.fields = [], ["id", "text", "when", "map_names", "speaker"]
        self.read_lines()

        top = ttk.Frame(root, padding=8); top.pack(fill="x")
        ttk.Label(top, text="ElevenLabs API key").grid(row=0, column=0, sticky="w")
        self.key = tk.StringVar(value=os.environ.get("ELEVENLABS_API_KEY", "") or self.cfg.get("key", ""))
        ttk.Entry(top, textvariable=self.key, show="*", width=40).grid(row=0, column=1, sticky="w", padx=4)
        ttk.Button(top, text="Load my voices", command=self.load_voices).grid(row=0, column=2, padx=4)
        self.voice, self.voice_box = {}, {}
        for i, (who, label) in enumerate([("cop", "Cop"), ("pilot", "Helicopter pilot")]):
            ttk.Label(top, text=label).grid(row=i, column=3, sticky="w", padx=(12, 0))
            self.voice[who] = tk.StringVar(value=self.cfg.get("voice_" + who) or VOICES[who])
            self.voice_box[who] = ttk.Combobox(top, textvariable=self.voice[who], width=42, values=self.cfg.get("voices", []))
            self.voice_box[who].grid(row=i, column=4, sticky="w", padx=4)

        sl = ttk.Frame(root, padding=(8, 0)); sl.pack(fill="x")
        self.vs = {}
        for i, (name, default) in enumerate([("stability", 0.45), ("similarity_boost", 0.8), ("style", 0.2)]):
            ttk.Label(sl, text=name.replace("_", " ")).grid(row=0, column=i * 2, sticky="w")
            v = tk.DoubleVar(value=self.cfg.get(name, default)); self.vs[name] = v
            tk.Scale(sl, variable=v, from_=0, to=1, resolution=0.05, orient="horizontal", length=160).grid(row=0, column=i * 2 + 1, padx=(4, 16))
        ttk.Label(sl, text="Show").grid(row=0, column=6, sticky="w")
        self.group = tk.StringVar(value=GROUPS[0][0])
        cb = ttk.Combobox(sl, textvariable=self.group, values=[g[0] for g in GROUPS], state="readonly", width=18)
        cb.grid(row=0, column=7, padx=4); cb.bind("<<ComboboxSelected>>", lambda e: self.refresh())

        mid = ttk.Frame(root, padding=8); mid.pack(fill="both", expand=True)
        cols = ("id", "text", "when", "speaker", "files")
        self.tree = ttk.Treeview(mid, columns=cols, show="headings", selectmode="extended")
        for c, w in zip(cols, (70, 400, 300, 80, 200)):
            self.tree.heading(c, text=c.capitalize()); self.tree.column(c, width=w, anchor="w")
        sb = ttk.Scrollbar(mid, orient="vertical", command=self.tree.yview); self.tree.configure(yscrollcommand=sb.set)
        self.tree.pack(side="left", fill="both", expand=True); sb.pack(side="right", fill="y")
        self.tree.bind("<<TreeviewSelect>>", self.on_select)
        self.tree.bind("<Double-1>", lambda e: self.play())

        ed = ttk.Frame(root, padding=(8, 0)); ed.pack(fill="x")
        ttk.Label(ed, text="Text of the selected line (fix spelling for pronunciation, then Save text)").pack(anchor="w")
        row = ttk.Frame(ed); row.pack(fill="x")
        self.text = tk.StringVar()
        ttk.Entry(row, textvariable=self.text).pack(side="left", fill="x", expand=True)
        ttk.Button(row, text="Save text", command=self.save_text).pack(side="left", padx=4)

        bt = ttk.Frame(root, padding=8); bt.pack(fill="x")
        for label, fn in [("Make selected (replace)", lambda: self.generate(False)), ("Add a take to selected", self.add_take),
                          ("Make all missing", self.generate_missing), ("Play selected", self.play), ("Stop", self.stop_all),
                          ("Open folder", lambda: os.startfile(OUT) if os.path.isdir(OUT) else None)]:
            ttk.Button(bt, text=label, command=fn).pack(side="left", padx=3)
        self.status = tk.StringVar(value=f"{len(self.rows)} lines. Files go to {OUT} and the game's drop-in folder.")
        ttk.Label(root, textvariable=self.status, padding=(8, 0, 8, 8)).pack(fill="x")
        self.refresh()
        root.after(100, self.pump)

    # data
    def read_lines(self):
        self.fields, self.rows = read_lines(self.fields)

    def write_lines(self):
        with open(LINES, "w", encoding="utf-8", newline="") as f:
            w = csv.DictWriter(f, fieldnames=self.fields); w.writeheader(); w.writerows(self.rows)

    def refresh(self):
        sel = set(self.tree.selection())
        self.tree.delete(*self.tree.get_children())
        g = dict(GROUPS)[self.group.get()]
        made = 0
        for r in self.rows:
            fl = [f for stem, _ in speakers(r) for f in files_for(stem)]
            done = not missing(r); made += done
            if g == "?" and done: continue
            if g not in ("", "?") and not r["id"].upper().startswith(g): continue
            self.tree.insert("", "end", iid=r["id"], values=(r["id"], r["text"], r.get("when", ""), r.get("speaker") or "cop", ", ".join(fl) or "missing"))
        for s in sel:
            if self.tree.exists(s): self.tree.selection_add(s)
        self.status.set(f"{made} of {len(self.rows)} lines made.")

    def selected(self):
        ids = list(self.tree.selection())
        return [r for r in self.rows if r["id"] in ids]

    def on_select(self, _):
        s = self.selected()
        self.text.set(s[0]["text"] if len(s) == 1 else "")

    def save_text(self):
        s = self.selected()
        if len(s) != 1: return
        s[0]["text"] = self.text.get().strip()
        self.write_lines(); self.refresh()

    # voices
    def keyval(self):
        k = self.key.get().strip()
        if not k:
            messagebox.showinfo("API key", "Paste your ElevenLabs API key first (ElevenLabs > Profile > API keys).")
        return k

    def voice_id(self, who):
        v = self.voice[who].get().strip()
        return v.rsplit("  ", 1)[-1].strip() if "  " in v else v

    def load_voices(self):
        k = self.keyval()
        if not k: return
        try:
            vs = json.loads(call("/voices", k))["voices"]
        except Exception as e:
            messagebox.showerror("Voices", f"Could not load voices: {e}"); return
        names = [f'{v["name"]}  {v["voice_id"]}' for v in vs]
        for b in self.voice_box.values(): b["values"] = names
        self.cfg["voices"] = names; self.remember()
        self.status.set(f"{len(names)} voices loaded. Pick the Cop and Helicopter pilot voices.")

    def remember(self):
        self.cfg.update(key=self.key.get().strip() if not os.environ.get("ELEVENLABS_API_KEY") else self.cfg.get("key", ""),
                        voice_cop=self.voice["cop"].get(), voice_pilot=self.voice["pilot"].get(),
                        **{n: v.get() for n, v in self.vs.items()})
        save_cfg(self.cfg)

    # making clips
    def generate(self, takes_only, rows=None, only_missing=False):
        rows = rows if rows is not None else self.selected()
        if not rows or self.busy: return
        k = self.keyval()
        if not k: return
        vids = {who: self.voice_id(who) for who in VOICES}
        if not all(vids.values()):
            messagebox.showinfo("Voice", "Fill in both voices (Load my voices) or paste their voice IDs."); return
        self.remember()
        jobs = []
        for r in rows:
            for stem, who in speakers(r):
                have = files_for(stem)
                if only_missing and have: continue
                if takes_only:
                    letter = "abcdefghij"[max(0, len(have) - 1)] if have else ""
                    jobs.append((stem + letter, r["text"], vids[who]))
                else:
                    jobs.append((stem, r["text"], vids[who]))
        settings = {n: round(v.get(), 2) for n, v in self.vs.items()}
        self.busy = True; self.stop.clear()
        threading.Thread(target=self.worker, args=(jobs, k, settings), daemon=True).start()

    def add_take(self):
        self.generate(True)

    def generate_missing(self):
        self.generate(False, [r for r in self.rows if missing(r)], only_missing=True)

    def worker(self, jobs, key, settings):
        made, failed = make_clips(jobs, key, settings, lambda kind, msg: self.q.put((kind, msg)), self.stop)
        self.q.put(("done", f"Done: {made} made, {failed} failed."))

    def pump(self):
        try:
            while True:
                kind, msg = self.q.get_nowait()
                if kind == "error": messagebox.showerror("ElevenLabs", msg)
                if kind == "done": self.busy = False; self.refresh()
                self.status.set(msg)
        except queue.Empty:
            pass
        self.root.after(150, self.pump)

    def play(self):
        s = self.selected()
        if not s: return
        fl = [f for stem, _ in speakers(s[0]) for f in files_for(stem)]
        if not fl:
            self.status.set(f'{s[0]["id"]} has no file yet.'); return
        path = os.path.join(OUT, fl[-1])
        if winsound and path.lower().endswith(".wav"):
            winsound.PlaySound(path, winsound.SND_FILENAME | winsound.SND_ASYNC)
        else:
            os.startfile(path)
        self.status.set(f"Playing {fl[-1]}")

    def stop_all(self):
        self.stop.set()
        if winsound: winsound.PlaySound(None, 0)


if __name__ == "__main__":
    if "--make-missing" in sys.argv:
        sys.exit(make_missing_cli())
    root = tk.Tk()
    App(root)
    root.mainloop()
