---
name: avalonia-desktop-architect
description: Avalonia UI 12.0.2 desktop development, Dark-Purple theme tokens, SkiaSharp RadarCanvas, UptimeChartControl, 12-hour AM/PM formatting standard, and responsive layout rules.
---

# avalonia-desktop-architect

This skill provides design standards, XAML and C# UI rules, and formatting directives for **NodeRadar Pro** (Avalonia UI .NET 10).

## 🎨 Theme & Styling Architecture
- **Theme Definitions:** `DarkPurpleTheme.cs`, `ThemeTokens.cs`.
- **Palette Hierarchy:** Base dark `#0D0B14`, surface card `#1A1625`, deep purple `#6B21A8`, vibrant violet `#8B5CF6`, bright neon `#A855F7`.
- **Glassmorphism:** Use semi-transparent acrylic panels with subtle border glows, rounded corners (`CornerRadius="8"` or `"12"`), and smooth hover transitions.
- **Canvas Rendering:** `RadarCanvas.cs` renders radar sweep lines and blips using SkiaSharp. Cache `SKPaint` and `SKPath` instances to minimize GC pressure during sweeps.
- **Dynamic Charts:** `UptimeChartControl.cs` draws 24-hour uptime pulses. Keep point calculations optimized and limit invalidations.

## 🕒 12-Hour AM/PM Time Format Standard
- **ALWAYS** enforce 12-hour AM/PM time formatting for all date/time displays in DataGrids, tooltips, diagnostic logs, and export files.
- **XAML Binding Standard:**
  ```xaml
  StringFormat='{}{0:yyyy-MM-dd  hh:mm:ss tt}'
  ```
- **C# Format Standard:**
  ```csharp
  dateTime.ToString("yyyy-MM-dd hh:mm:ss tt")
  ```
- **NEVER** use 24-hour military time (`HH:mm` or `HH:mm:ss`).

## 🌐 Layout & Localization Scoping
- NodeRadar Pro is 100% English and Left-To-Right (`FlowDirection="LeftToRight"`).
- Keep labels, grid columns, telemetry titles, and error toasts in concise technical English.

## 🧵 Thread Dispatching Rules
- Never modify UI controls or bound `ObservableCollection<T>` directly from background threads.
- Always dispatch UI mutations via `Dispatcher.UIThread.Post(...)` or `Dispatcher.UIThread.InvokeAsync(...)`.
