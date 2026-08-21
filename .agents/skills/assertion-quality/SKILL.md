---
name: assertion-quality
description: Rules and patterns for writing clean, descriptive, non-flaky test assertions and exception verification.
---

# assertion-quality

## Guidelines
- **Explicit Expectations**: Use `Assert.Equal(expected, actual)` rather than `Assert.True(...)`.
- **Exception Verification**: Use `await Assert.ThrowsAsync<SpecificException>(...)` rather than generic `Exception`.
- **Deterministic Clocks & Delays**: Avoid `Task.Delay(500)` in assertions; use deterministic events or task completion sources.
