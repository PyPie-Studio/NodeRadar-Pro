# Planning Discipline (Always-On)

## Plan Gate for Non-Trivial Work
If the task involves architectural changes, new protocol handlers, refactoring existing scanner loops, or multi-file edits:
1. **Brainstorm**: Identify goals, constraints, risks, and acceptance criteria.
2. **Plan**: Write a step-by-step implementation plan with concrete verification steps.
3. **Approval**: Obtain user approval before modifying code.

## Verification Gate
Every step must have a clear verification mechanism:
- Compiler check with 0 warnings (`TreatWarningsAsErrors=true`).
- Execution verification of network sweeps / UI rendering.
- Master quality gate run (`scripts/Test-MasterGate.ps1`).

## Minimal Working Diffs (Ponytail Discipline)
- Reach for .NET standard library and native APIs before introducing third-party dependencies.
- Make the shortest working diff that completely fulfills the requirement.
- Do not introduce speculative abstractions, unused interfaces, or dead config flags.
