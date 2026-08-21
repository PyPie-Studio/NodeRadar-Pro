---
name: jules-journals-auditor
description: Audit and enforce findings from .jules/sentinel.md (Security), .jules/bolt.md (Perf), .jules/palette.md (UI), and .jules/sweeper.md (Clean Code).
---

# jules-journals-auditor

## Purpose
Enforces cross-session learning persistence:
1. Check `.jules/` journals before major refactors to avoid repeating known pitfalls.
2. Ensure every significant fix or feature appends a concise, dated lesson to the relevant `.jules/*.md` journal.
