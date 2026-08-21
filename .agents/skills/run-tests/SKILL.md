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

## Diagnostics & Flaky Socket Prevention
- Use `--verbosity normal` or `--logger "console;verbosity=detailed"` on test failures.
- **Dynamic Port Binding**: Never hardcode listening ports (e.g. 8080) in test fixtures. Use `new TcpListener(IPAddress.Loopback, 0)` or dynamic fallback arrays to prevent conflicts with local services.
- **Mock Listener Disposal Timing**: Keep mock server socket connections open until after the client completes reading/writing to avoid TCP connection reset (RST) races.
- **Mock HTTP Handlers**: Test network services using `MockHttpMessageHandler` and dependency-injected `HttpClient` instances rather than reaching out to external networks.
- **Analyzer Compliance in Tests**: Ensure test methods contain assertions or `Record.Exception` (S2699), suppress Theory duplicate method warnings (S4144), and avoid unused empty classes (S2094).
