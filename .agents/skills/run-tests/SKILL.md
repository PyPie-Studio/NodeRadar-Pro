---
name: run-tests
description: Standardized test execution, filtering, logging, and failure diagnostics for NodeRadar Pro.
---

# run-tests

## Commands
```powershell
# Run all tests in solution
dotnet test NodeRadarPro.slnx -c Release --nologo

# Run specific test class
dotnet test --filter "FullyQualifiedName~ArpResolverTests"
```

## Diagnostics
- Use `--verbosity normal` or `--logger "console;verbosity=detailed"` on test failures.
- Check cancellation tokens and socket timeouts if tests hang.
