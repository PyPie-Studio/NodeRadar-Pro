# NodeRadar Pro Agent Skills Registry

This document outlines the specialized skills configured for the **NodeRadar Pro** network suite. These skills are auto-discovered from [`.agents/skills/`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/skills) and provide domain-specific knowledge and procedures for AI coding agents.

---

## 🛠 Active Skills List

| Skill Name | Target Scope | Key Description & Purpose |
| :--- | :--- | :--- |
| **`avalonia-desktop-architect`** | `UI/`, `Controls/` | Avalonia 12 UI desktop development, Dark-Purple theme tokens, SkiaSharp `RadarCanvas`, `UptimeChartControl`, 12-hour AM/PM formatting standard, and responsive layouts. |
| **`network-engine-architect`** | `Core/` | Low-level network discovery engine: Win32 `SendARP` API, ICMP multi-threading, TCP port sweeps, packet buffer pooling, and `NetworkAnalysis.md` heuristics. |
| **`litedb-cipher-architect`** | `Data/` | Embedded LiteDB v5 database management, AES-256 database password management via DPAPI, schema migrations, and indexing strategies. |
| **`superpowers-workflow`** | Entire Solution | Structured multi-phase AI development workflow (Brainstorm $\rightarrow$ Plan Gate $\rightarrow$ TDD/Execution $\rightarrow$ Review $\rightarrow$ Finish). |
| **`superpowers-brainstorm`** | Architecture / Features | Brainstorming phase generating goal, constraints, risks, options, and acceptance criteria. |
| **`superpowers-plan`** | Solution Planning | Generates step-by-step implementation plans with verification steps. |
| **`superpowers-tdd`** | Testing / Fixes | Test-driven development workflows enforcing regression testing. |
| **`superpowers-review`** | Code Review | Severity-tiered code review (Blocker, Major, Minor, Nit) before finalizing changes. |
| **`superpowers-finish`** | Release / Completion | Final review, documentation, and completion verification. |
| **`superpowers-debug`** | Network & UI Debugging | Systematic root-cause debugging for network timeouts, socket leaks, and UI rendering glitches. |
| **`superpowers-python-automation`** | Tooling / Automation | Python automation scripts and tools. |
| **`superpowers-rest-automation`** | External Services | REST API integrations (e.g. GitHub Releases API in `UpdateService.cs`). |
| **`jules-journals-auditor`** | `.jules/` | Audit and enforce findings from `.jules/sentinel.md` (Security), `.jules/bolt.md` (Perf), `.jules/palette.md` (UI), and `.jules/sweeper.md` (Clean Code). |
| **`run-tests`** | Test Suite | Standardized test execution, filtering, logging, and failure diagnostics. |
| **`assertion-quality`** | Test Suite | Rules and patterns for writing clean, non-flaky test assertions and exception verification. |
| **`humanizer`** | Documentation / PRs | Anti-AI writing style calibration (removes fluff, significance inflation, em dashes, and sycophancy). |
| **`ponytail`** | Any Coding Task | Laziest-solution discipline: stdlib/native first, shortest working diff, YAGNI. |
| **`graphify`** *(global)* | Knowledge Graph | AST knowledge graph queries for symbol hierarchies and relationships. |

---

## 📌 Skill File Locations

All workspace skills are auto-discovered from `.agents/skills/<name>/SKILL.md`:

| Skill | Path |
| :--- | :--- |
| `avalonia-desktop-architect` | `.agents/skills/avalonia-desktop-architect/SKILL.md` |
| `network-engine-architect` | `.agents/skills/network-engine-architect/SKILL.md` |
| `litedb-cipher-architect` | `.agents/skills/litedb-cipher-architect/SKILL.md` |
| `superpowers-workflow` | `.agents/skills/superpowers-workflow/SKILL.md` |
| `superpowers-brainstorm` | `.agents/skills/superpowers-brainstorm/SKILL.md` |
| `superpowers-plan` | `.agents/skills/superpowers-plan/SKILL.md` |
| `superpowers-tdd` | `.agents/skills/superpowers-tdd/SKILL.md` |
| `superpowers-review` | `.agents/skills/superpowers-review/SKILL.md` |
| `superpowers-finish` | `.agents/skills/superpowers-finish/SKILL.md` |
| `superpowers-debug` | `.agents/skills/superpowers-debug/SKILL.md` |
| `superpowers-python-automation` | `.agents/skills/superpowers-python-automation/SKILL.md` |
| `superpowers-rest-automation` | `.agents/skills/superpowers-rest-automation/SKILL.md` |
| `jules-journals-auditor` | `.agents/skills/jules-journals-auditor/SKILL.md` |
| `run-tests` | `.agents/skills/run-tests/SKILL.md` |
| `assertion-quality` | `.agents/skills/assertion-quality/SKILL.md` |
| `humanizer` | `.agents/skills/humanizer/SKILL.md` |
| `ponytail` | `.agents/skills/ponytail/SKILL.md` |

---

## 🔗 Related Configuration

- **Enforcement Rules:** [`.agents/rules/`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules) (UI/UX, Performance, Security, Dead Code, PR Review, Commit Conventions)
- **Agent Guide:** [`AGENTS.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/AGENTS.md) (Architecture, commands, patterns, gotchas)
- **MCP Servers:** [`opencode.json`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/opencode.json) and [`mcp.json`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/mcp.json)
