# Palette Journal: UI/UX & Glassmorphism Learnings

This journal records visual hierarchy, theme tokens, typography, and Avalonia UI layout refinements for **NodeRadar Pro**.

---

## 2026-08-17 — Dark-Purple Glassmorphic Palette Baseline
- **Problem**: Inconsistent hardcoded color brushes across different page views (`ScannerPage.cs`, `InventoryPage.cs`, `SettingsPage.cs`).
- **Decision**: Centralize all brushes into `DarkPurpleTheme.cs` and `ThemeTokens.cs`. Use `#0D0B14` for base background, `#1A1625` for card surfaces, `#6B21A8` for primary accents, and `#A855F7` for hover/active highlights.
- **Impact**: Unified visual presentation across all navigation tabs and overlays.

## 2026-08-17 — 12-Hour AM/PM Time Formatting Standard
- **Problem**: 24-hour time (`HH:mm`) was used in some diagnostic timestamps while 12-hour format was used in inventory tables.
- **Decision**: Mandate 12-hour AM/PM formatting standard (`yyyy-MM-dd hh:mm:ss tt`) across all controls, charts, logs, and export files.
- **Impact**: Predictable, user-friendly timestamp displays across the application.
