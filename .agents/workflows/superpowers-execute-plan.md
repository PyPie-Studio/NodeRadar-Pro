---
description: Executes an approved plan in small steps with verification after each step. Writes execution artifacts to disk. Stops on failures. Finishes with review + summary.
---

# Superpowers Execute Plan

## Persist (mandatory)
You must write execution artifacts to disk under `artifacts/superpowers/`:
- Append execution notes to: `artifacts/superpowers/execution.md`
- Write the final summary to: `artifacts/superpowers/finish.md`

## Preconditions
1) The user must have approved a written plan at `artifacts/superpowers/plan.md`.
2) If `artifacts/superpowers/plan.md` does not exist, run `/superpowers-write-plan` first.

## Execution Rules
1) Implement ONE plan step at a time.
2) After each step, run verification commands (e.g. `dotnet build NodeRadarPro.slnx -c Release`).
3) If verification fails, switch to systematic debugging with `superpowers-debug`.
4) Record execution results in `artifacts/superpowers/execution.md`.

## Finish
1) Run a review pass (Blocker/Major/Minor/Nit).
2) Write final summary to `artifacts/superpowers/finish.md`.
