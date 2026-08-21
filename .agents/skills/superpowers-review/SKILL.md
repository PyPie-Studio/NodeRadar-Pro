---
name: superpowers-review
description: Code review focused on correctness, performance, security, and severity-tiered findings.
---

# superpowers-review

## Review Tiers
- **Blocker**: Security vulnerability, data loss risk, infinite loops, compiler errors, socket/memory leaks.
- **Major**: Architectural violation, missing error handling, performance regression in sweep loops.
- **Minor**: Suboptimal code structure, missing comments on complex bitwise/packet operations.
- **Nit**: Formatting, naming consistency.
