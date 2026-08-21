---
name: superpowers-tdd
description: Test-driven development workflows enforcing regression test creation and non-flaky assertions.
---

# superpowers-tdd

## Guidelines
1. **Red**: Write a failing test that reproduces the bug or asserts the required behavior.
2. **Green**: Implement the minimal code change to pass the test.
3. **Refactor**: Clean up implementation without altering external contracts or behavior.
4. **Zero Flakiness**: Ensure tests do not rely on hardcoded thread sleeps; use deterministic async await or task completion sources.
