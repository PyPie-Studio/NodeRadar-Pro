---
trigger: always_on
description: Mandatory UI/UX standards - Dark-Purple Glassmorphic theme tokens, 12-hour AM/PM formatting standard, strict English LTR layout, responsive DataGrids, and smooth live radar canvas animations.
alwaysApply: true
---
# UI/UX Standards

- **Dark-Purple Glassmorphic Palette**:
  - Base Dark: `#0D0B14`
  - Card & Surface: `#1A1625` / `#231E34` with acrylic semi-transparency
  - Primary Accent: `#6B21A8` (Deep Purple) / `#8B5CF6` (Vibrant Violet)
  - Highlight & Neon: `#A855F7` (Bright Purple) / `#10B981` (Sentinel Green)
  - Warning / Intrusion Alert: `#EF4444` (Crimson Red) / `#F59E0B` (Amber Warning)
- **12-Hour AM/PM Time Format Standard**:
  - **ALWAYS** enforce 12-hour AM/PM formatting across all DataGrids, uptime charts, telemetry cards, tooltips, and diagnostic logs.
  - **XAML Binding Standard:** `StringFormat='{}{0:yyyy-MM-dd  hh:mm:ss tt}'` or `StringFormat='{}{0:hh:mm:ss tt}'`
  - **C# String Standard:** `dateTime.ToString("yyyy-MM-dd hh:mm:ss tt")`
  - **NEVER** use 24-hour military time (`HH:mm` or `HH:mm:ss`).
- **Strict English LTR Layout**:
  - NodeRadar Pro is strictly 100% English and Left-To-Right (`FlowDirection="LeftToRight"`).
  - All status labels, grid headers, export formats, error toasts, and dialogs MUST remain in clean technical English.
- **Visual Feedback for Active Sweeps**:
  - During subnet sweeps, display clear animated radar pulses (`RadarCanvas.cs`), progress percentages, and active device counters.
  - Dynamic status pills: `.status-online` (Emerald), `.status-offline` (Slate), `.status-alert` (Red), `.status-probing` (Purple).
- **Responsive Layout & High-DPI Scaling**:
  - Support seamless scaling across 100%, 125%, 150%, and 200% Windows display scaling without clipped text or broken glass borders.
