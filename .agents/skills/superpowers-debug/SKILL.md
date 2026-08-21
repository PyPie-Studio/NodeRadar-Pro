---
name: superpowers-debug
description: Systematic root-cause debugging for network timeouts, socket leaks, thread deadlocks, and UI glitches.
---

# superpowers-debug

## Debugging Methodology
1. **Reproduce**: Establish a minimal reproducible scenario (single IP probe, port test, or mock frame).
2. **Isolate**: Inspect socket states, Wireshark captures, or Avalonia UI thread dispatchers.
3. **Hypothesize**: Formulate a falsifiable hypothesis.
4. **Fix & Verify**: Apply the minimal targeted fix and verify resolution.
