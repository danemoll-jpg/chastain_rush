# How this collaboration works — process notes

Not project-specific. These are the habits worth carrying into any new
project using this same Claude-plans / coding-agent-executes pattern.

## The core loop
1. You bring an idea, bug report, or piece of feedback to Claude in plain
   language — however casual or half-formed.
2. Claude scopes it: asks the one clarifying question that actually matters
   (not more), flags real design tensions before they become bugs, and writes
   the result into `TODO.md`/`CLAUDE.md` precisely enough that a coding agent
   can act on it without guessing.
3. Claude tells you explicitly what to relay to your coding agent — a short,
   clear instruction, not a paraphrase of the whole doc.
4. Your coding agent builds it and reports back (usually as updated
   `TODO.md`/`CLAUDE.md` files).
5. Claude reconciles: syncs the docs, and — this part matters — checks every
   previously-open item off individually against what actually shipped,
   rather than accepting whatever the report happens to lead with.

## Habits worth keeping
- **Multi-item batches get numbered, not run together in prose.** A coding
  agent (or a person) is far more likely to silently drop one item out of a
  paragraph than one item out of a numbered list.
- **Silent partial completion is the failure mode to watch for**, not
  misunderstanding. A coding agent can know about a requested item and still
  choose not to build it without saying so. Ask for explicit per-item status
  (done / not done / deferred, and why) rather than assuming a deploy means
  everything requested got done.
- **Resolve open questions, don't let them drift.** When a design decision is
  genuinely open, say so plainly, give a recommended default, and get an
  actual answer — then mark it resolved and move on. A question that sits
  "open" for several rounds is a sign it needs to just be asked directly.
- **Distinguish "deferred" from "decided against."** A backlog item kept
  alive indefinitely as "later, maybe" is different from one that's been
  honestly assessed and closed. Say which one it is.
- **Track verification status separately from "built."** "Preview-verified"
  and "confirmed on the real device/environment" are different claims — don't
  let the first stand in for the second, especially for anything environment-
  specific (mobile browsers, real hardware, real third-party services).
- **When something fails repeatedly, get real diagnostic data before trying
  again — don't just guess harder.** A bug that survives several plausible-
  looking fixes is a sign the diagnosis is wrong, not that the next fix needs
  to be cleverer. Add instrumentation, capture real before/after evidence,
  and let the data pick the next move.
- **Sometimes the fix is avoiding the trigger, not solving the root cause.**
  Especially for gnarly environment-specific bugs (this project's whole
  scroll-bug saga, eventually solved by restructuring the UI to never trigger
  it, not by fixing the underlying platform issue) — a workaround that
  actually holds is a legitimate resolution, not a consolation prize. Just be
  honest in the docs about what's actually fixed vs. routed around.
- **If deploys/pushes are a limited resource**, ask your coding agent to
  batch a full round's changes into one push rather than pushing
  incrementally per change — this is worth stating explicitly, since
  incremental pushes can quietly burn through a quota with no one round ever
  being "the" round that used it up.
- **Casual, offhand feedback is often the most useful kind.** Some of the
  best fixes in this project came from throwaway lines like "it isn't size,
  the buttons aren't lined up" — plain-language descriptions of what's wrong
  are worth taking literally before assuming something more complex is going
  on.
