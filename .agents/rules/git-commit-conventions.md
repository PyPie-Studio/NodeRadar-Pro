# Git Commit Conventions

## Format
```
<type>(<scope>): <subject>
```

## Allowed Types
- `feat`: A new user-facing feature or protocol engine (e.g. mDNS, SSDP, DHCP).
- `fix`: A bug fix (e.g. socket timeout, memory leak, UI glitch).
- `perf`: A code change that improves performance or reduces allocations.
- `refactor`: Code change that neither fixes a bug nor adds a feature.
- `sec`: Security enhancement (DPAPI, LiteDB encryption, path validation).
- `style`: Formatting, whitespace, or code style adjustments.
- `docs`: Documentation updates, agent guide, or roadmap edits.
- `test`: Adding or updating tests.
- `chore`: Tooling, build scripts, Inno Setup or package updates.

## Scopes
`core`, `ui`, `data`, `scanner`, `radar`, `updater`, `inno`, `scripts`, `rules`, `skills`
