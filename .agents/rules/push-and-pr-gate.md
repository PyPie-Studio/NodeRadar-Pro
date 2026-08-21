# Push & Master Gate Policy

## Local Quality Gate Enforcement
Before pushing any commit to `master` or `main`:
1. Execute the master quality gate:
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-MasterGate.ps1
   ```
2. The gate script must exit 0 and print `== GATE PASSED ==`.
3. If the gate fails on compiler warnings, static analysis errors, or formatting violations, resolve all issues before pushing.
4. `.githooks/pre-push` is installed to automatically invoke the gate before push operations.
