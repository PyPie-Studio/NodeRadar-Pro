---
description: Executes independent plan steps in parallel using isolated subagents.
---

# Superpowers Execute Plan Parallel

## Procedure
1. Identify independent steps in `artifacts/superpowers/plan.md`.
2. Launch isolated subagent runs for parallel steps using `spawn_subagent.py`.
3. Collect and verify subagent outputs.
4. Merge results and run master verification gate (`scripts/Test-MasterGate.ps1`).
