# Dead Code & Hygiene Guardrails (always-on)

Applies to ALL agents and human edits. These rules are ENFORCED by the build — a violation fails the gate, CI, and release packaging.

## Enforced rules (hard errors — do not violate)

These 17 analyzer rules are promoted from the suppression baseline and must never be violated:

- **S1481** — unused local variables
- **S4487** — unread private fields
- **RCS1213** — unused/duplicated private members
- **S1854** — dead stores / useless assignments
- **S108** — empty blocks (add a comment or merge)
- **S2486** — empty catch blocks (handle OR comment why ignored)
- **S3168** — `async void` (use `async Task`; fire-and-forget with `_ =`)
- **S927** — parameter names must match the interface/override
- **S1699** — no virtual calls in constructors
- **S112** — no `throw new Exception()` (use a specific exception type)
- **S3963** — static constructors must not be pure field init
- **S1118** — non-static classes with only static members $\rightarrow$ `static class`
- **S127** — no counter updates in the loop body
- **S6667** — logging in catch must pass the caught exception
- **S5445** — temp files: use `Path.GetRandomFileName()`, never `GetTempFileName()`
- **S4136** — overloads must be adjacent
- **S8969** — remove redundant null-forgiving operators

## The NoWarn baseline is SHRINK-ONLY

`Directory.Build.props` `<NoWarn>` is a debt ledger, not a safety valve. Rules only leave it (removed = enforced). Adding a rule back requires a `docs/decisions.md` entry with reason.

## Before you claim "done"

1. Delete dead code as you write features — do not leave unused locals/fields/catches behind (the gate will reject them).
2. Empty catches need a comment explaining why the exception is safe to ignore (e.g. harmless timeout in ARP probe).
3. Logging in a catch: pass the exception object.
4. Run the master gate (`scripts\Test-MasterGate.ps1`) — it runs build + static analysis + `dotnet format --verify-no-changes`.
5. Journal non-trivial changes in `.jules/*.md`.
