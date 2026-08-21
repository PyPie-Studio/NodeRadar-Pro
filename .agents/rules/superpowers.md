# Superpowers Rules (Always-On)

These rules apply to ALL work unless the user explicitly opts out.

## 1) Plan Gate for Non-Trivial Work
If the task is anything beyond a tiny change, do NOT edit code immediately.
You MUST:
1) Brainstorm briefly (goal, constraints, risks, acceptance criteria)
2) Write a step-by-step plan with verification steps
3) Ask the user to approve the plan
Only after approval may you implement.

### Execute-Plan Gate
After the user approves a plan, proceed through implementation sequentially, validating each step.

### What Counts as "Tiny"?
- Single-file change
- Obvious edit / typo fix
- Low risk
Even then: do a mini-plan (3–5 steps) and include verification.

## 2) Verification is Mandatory
After implementation, you MUST provide:
- Exact commands to verify (build/tests/lint/run)
- Results of executing those commands

## 3) Prefer TDD / Regression Tests
- If fixing a network bug: add a regression test when practical
- If adding protocol behavior: verify payload dissection against standard test vectors

## 4) Review Pass Required
Before final response, do a review pass and list issues by severity:
- Blocker / Major / Minor / Nit

## 5) Safety
- Never log secrets or DPAPI plaintexts
- Add timeouts and cancellation tokens for all network socket operations
- Fail safe (graceful non-admin fallback, no silent data corruption)

## Artifact Persistence
Any brainstorm, plan, review, or finish output should be written under:
`artifacts/superpowers/`
Confirm files exist on disk after writing.
