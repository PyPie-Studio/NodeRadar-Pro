# NodeRadar Pro Agent Superpowers & Capabilities Framework

This document outlines the enterprise-grade capabilities, verification pipelines, and automated workflows available to AI agents working on **NodeRadar Pro**. Detailed enforcement standards are maintained in [`.agents/rules/`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules).

---

## ⚡ Capabilities Overview

| Superpower | Enforcement Rule / Skill | Summary |
| :--- | :--- | :--- |
| 🛡 **Defensive Security** | [`security-guardrails.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/security-guardrails.md) | Windows DPAPI encryption, AES-256 LiteDB protection, path sanitization, constant-time comparisons, SHA-256 self-audit. |
| ⚡ **High-Performance Networking** | [`performance-guardrails.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/performance-guardrails.md) | Zero-allocation byte buffers (`ArrayPool`), `SemaphoreSlim` sweep concurrency throttling, socket disposal discipline. |
| 🖥 **Avalonia 12 UI & Graphics** | [`ui-ux-standards.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/ui-ux-standards.md) | Dark-Purple Glassmorphic palette, SkiaSharp animated radar sweep, 12-hour AM/PM formatting standard, strict English LTR layout. |
| 🧹 **Dead Code & Hygiene** | [`dead-code-hygiene.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/dead-code-hygiene.md) | 17 hard analyzer rules enforced as compiler errors via `Directory.Build.props`. |
| 🧪 **Multi-Tier Quality Gate** | [`pr-review-checklist.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/rules/pr-review-checklist.md) | Mandatory local master gate, Roslynator & SonarAnalyzer verification, `dotnet format` enforcement. |
| 📓 **Jules Journal Auditing** | Skill: [`jules-journals-auditor`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.agents/skills/jules-journals-auditor/SKILL.md) | Mandatory cross-session journaling in `.jules/` (bolt, palette, sentinel, sweeper). |
| 📦 **Automated Release Pipeline** | [`scripts/Build-ReleasePackage.ps1`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/scripts/Build-ReleasePackage.ps1) | Automated .NET 10 publish + Obfuscar IL protection + Inno Setup 6 compilation. |

---

## 🧪 Verification Pipeline Commands

```powershell
# 1. Solution Build in Release
dotnet build NodeRadarPro.slnx -c Release

# 2. Local CI Master Gate
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-MasterGate.ps1

# 3. Static Analysis Report
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Get-AnalyzerReport.ps1

# 4. Release Packaging & Obfuscation
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build-ReleasePackage.ps1
```

---

## 🔗 Quick Reference

- **Agent Guide:** [`AGENTS.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/AGENTS.md) — Architecture, commands, patterns, gotchas
- **Skills Registry:** [`SKILLS.md`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/SKILLS.md) — 14 workspace skills
- **Change Journals:** [`.jules/`](file:///c:/Users/tryku/Desktop/Coding/Projects/C%23/Cross-Platform/NodeRadar%20Pro/.jules) — bolt, palette, sentinel, sweeper
- **Knowledge Graph:** `graphify-out/` — AST graph representation
