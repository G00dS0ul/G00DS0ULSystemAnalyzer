# 🦅 GS System Analyzer

[Flutter](https://img.shields.io/badge/Frontend-Flutter-02569B?style=for-the-badge&logo=flutter&logoColor=white)

Flutter

[Riverpod](https://img.shields.io/badge/State-Riverpod-000000?style=for-the-badge&logo=dart&logoColor=white)

Riverpod

[[ASP.NET](http://ASP.NET) Core](https://img.shields.io/badge/Backend-ASP.NET_Core_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)

[ASP.NET](http://ASP.NET) Core

[SignalR](https://img.shields.io/badge/WebSockets-SignalR-0078D4?style=for-the-badge&logo=microsoft&logoColor=white)

SignalR

[Status](https://img.shields.io/badge/Status-Pre--Beta_(v2.0)-FF8C00?style=for-the-badge)

Status

[License](https://img.shields.io/badge/License-Apache_2.0-blue?style=for-the-badge)

License

[![Discord](https://img.shields.io/badge/Community-Discord-5865F2?style=for-the-badge&logo=discord&logoColor=white)]([https://discord.gg/FA8WsVXMx](https://discord.gg/FA8WsVXMx))

A high-performance, cross-platform system telemetry and disk management engine. Built with a reactive Flutter UI and powered by a multithreaded C# backend, GS System Analyzer provides real-time OS-level insights and execution protocols wrapped in a custom **"Cyber-HUD"** aesthetic.

> Think **Task Manager + TreeSize + HWiNFO**, fused into one keyboard-fast desktop cockpit — but open source, scriptable, and built around a strict frontend/backend boundary.
> 

---

## 📍 Project Status — Where We Are Right Now

**Current stage: Pre-Beta, v2.0 feature freeze approaching (August 2026).**

The engine is stable and every primary telemetry panel is live, including the full-screen thermal module, the process explorer, the settings panel, and the complete disk intelligence suite. What remains for v2.0 is network and disk I/O throughput, fine-grained cache invalidation, and alerting polish.

| Milestone | Target | State |
| --- | --- | --- |
| **Public Beta** | Sept 2026 | 🟡 In progress — feature freeze approaching |
| **Official Release** | Oct/Nov 2026 | ⏳ Planned |
| **Release 2.0** | Q1 2027 | 🔭 Future |

Feature-level status is tracked in GitHub Issues. Where any board or document disagrees with an open issue, **the issue is correct**.

---

## 🧭 Overview

GS System Analyzer is split into two cleanly decoupled halves that talk over REST + SignalR:

- **The Command Center (Frontend)** — a Flutter application using `Riverpod` for state management. It renders the Telemetry HUD, reactive directory trees, and all system-operation UX. Flutter **never** touches OS APIs directly.
- **The Engine Room (Backend)** — an [ASP.NET](http://ASP.NET) Core 10 (C#) backend that handles all heavy OS-level I/O, memory caching, multithreaded directory walking, and hardware sensor reads, then streams results to the UI.

This boundary is the single most important architectural rule in the project: **all OS-level work happens in C#, and data reaches Flutter only via SignalR streams or REST.**

```
┌─────────────────────┬──────────────────────────────────────────────┐
│  SIDEBAR            │  [CPU_LOAD]   [MEM_ALLOCATION]   [NET_IO]    │
│                     ├──────────────────────────────────────────────┤
│  DASHBOARD          │  [ACTIVE_PROCESS_TREE  (wide)]  [THERMAL]    │
│  PROCESS EXPLORER   │                                              │
│  CPU METRICS        │                                              │
│  MEMORY             │                                              │
│  STORAGE            │                                              │
│  STARTUP            │                                              │
│  NETWORK            │                                              │
│  THERMAL            │  ← dedicated full-screen thermal module      │
│  TELEMETRY HISTORY  │                                              │
│  SETTINGS           │                                              │
│  HELP               │                                              │
└─────────────────────┴──────────────────────────────────────────────┘
```

---

## 🔥 Key Features

### 1. The Nuke Protocol (Bulk Obliteration) — ✅ Shipped

A weapons-grade deletion system. Instead of N+1 API calls, the frontend bundles targeted nodes into a single JSON payload. The multithreaded C# backend obliterates massive directory structures and clears memory caches simultaneously. Every nuke is gated behind a mandatory **Dry Run** preview plus a non-dismissible confirmation, streams per-file progress, and can be sent to the recycle bin instead of deleted permanently.

### 2. Live Radar (Reactive File System Monitoring) — ✅ Shipped

`FileSystemWatcher` wired directly into a **SignalR WebSocket Hub**. Any external create/modify/delete on the target drive is instantly pushed to the Flutter UI, triggering a targeted Riverpod invalidation — zero manual refresh.

### 3. Parallel Disk Scanning Engine — ✅ Shipped

`Parallel.ForEachAsync` + `ConcurrentDictionary` aggressively map storage, calculating deep directory sizes across thousands of subfolders concurrently and caching results to cut CPU load on repeat reads. Live progress streams to the UI; scans are cancellable mid-flight with no dangling threads. Scans can also run on a background schedule.

### 4. `CPU_LOAD` Panel — ✅ Shipped

Real-time average CPU % with per-tick delta, live frequency, process/thread/handle counts, L1–L3 cache info, and grouped per-core bar charts (CORE 0–3, 4–7, 8–15…).

### 5. `MEM_ALLOCATION` Panel (RAM Scanner) — ✅ Shipped

Live total/used/cached/swap RAM plus a full per-process breakdown, with a kill-process action streamed back over SignalR.

### 6. `ACTIVE_PROCESS_TREE` (Process Explorer) — ✅ Shipped

Per-process CPU% and RAM% in one sortable table, with kill/pause actions.

### 7. Disk Intelligence Suite — ✅ Shipped

**Duplicate File Detector** (size pre-filter → partial hash → full SHA-256), **Large File Hunter** (top-N space hogs), **Temp Folder Cleaner**, **Deep File Type Analytics**, an **Extension Breakdown Table**, a **File Age Heatmap**, **Scan Result Diffing**, and **Permission Auditing** — all feeding into the Nuke Protocol with Dry Run safety.

### 8. `THERMAL_SENSORS` Panel + Thermal Module — ✅ Shipped

Real-time CPU package temps, board/chipset temps, fan RPM, and power draw, with a dedicated full-screen thermal module (live view + history). Sensors are read through a tiered provider stack — LibreHardwareMonitor, then WMI thermal zones, then vendor OEM WMI, then ACPI — with graceful `N/A` fallback when a tier is unavailable. *(GPU and extended sensors are deferred to v2.1.)*

### 9. Telemetry History & Reporting — ✅ Shipped

Rolling per-metric history with a selectable time range, plus scan report export to JSON, CSV, or self-contained HTML.

### 10. Settings / Config Panel — ✅ Shipped

Scan depth, exclusions, alert thresholds, poll intervals, cache TTL, and appearance — persisted to disk and read by every backend service.

> ⚠️ **Admin note:** Full thermal/fan/power data on Windows requires running the backend **as Administrator** (EC/MSR/RAPL access). Without elevation, CPU temps may still appear, but fan RPM, board sensors, and power can read `N/A`. On machines with HVCI / Memory Integrity enabled, the low-level driver is blocked entirely and the stack falls back to WMI — expected behaviour, not a bug.
> 

---

## 📂 Repo Layout

```
core/
├── backend/                       ASP.NET Core 10 backend (C#, SignalR)
│   ├── BackgroundWorkers/         Drive monitor, scheduled scan worker
│   ├── Controllers/               REST endpoints
│   ├── Engine/                    Scan, CPU, RAM, thermal, NETIO, file-type engines
│   ├── Hubs/                      SignalR hubs (TelemetryHub)
│   ├── Services/                  Sensor, nuke, telemetry, OEM services
│   ├── GSSystemAnalyzer.Tests/    xUnit backend tests
│   └── GSSystemAnalyzer.Benchmarks/
└── frontend/
    └── gs_analyzer_ui/            Flutter app (Dart, Riverpod)
        ├── lib/                   models · providers · screen · services · utils · widgets
        ├── test/                  Flutter widget + unit tests
        └── integration_test/      End-to-end tests
```

---

## 📊 Feature Status

| Feature | Target | Status |
| --- | --- | --- |
| Nuke Protocol + Dry Run + deletion progress stream | Beta | ✅ Shipped |
| Undo / recycle-bin deletion mode | Beta | ✅ Shipped |
| Live Radar (FileSystemWatcher + SignalR) | Beta | ✅ Shipped |
| Parallel disk scanning + progress stream + cancellation | Beta | ✅ Shipped |
| Background scheduled scans | Beta | ✅ Shipped |
| `CPU_LOAD` panel | Beta | ✅ Shipped |
| `MEM_ALLOCATION` panel | Beta | ✅ Shipped |
| `ACTIVE_PROCESS_TREE` / Process Explorer | Beta | ✅ Shipped |
| `THERMAL_SENSORS` panel + full-screen thermal module | Beta | ✅ Shipped |
| Duplicate File Detector | Beta | ✅ Shipped |
| Large File Hunter | Beta | ✅ Shipped |
| Temp Folder Cleaner | Beta | ✅ Shipped |
| Deep file type analytics + extension breakdown | Beta | ✅ Shipped |
| File age heatmap | Beta | ✅ Shipped |
| Scan result diffing | Beta | ✅ Shipped |
| Permission audit | Beta | ✅ Shipped |
| Startup program manager | Beta | ✅ Shipped |
| Historical telemetry charts | Beta | ✅ Shipped |
| Scan report export (JSON / CSV / HTML) | Beta | ✅ Shipped |
| Disk space threshold alerts | Beta | ✅ Shipped |
| Settings / Config panel | Beta | ✅ Shipped |
| `NET_IO` panel | Beta | ✅ Shipped |
| `DISK_IO` throughput panel | Beta | ⏳ Planned |
| Memory cache TTL & targeted invalidation | Beta | ✅ Shipped |
| RAM pressure alerts | Beta | ⏳ Planned |
| Watcher event log | Beta | ⏳ Planned |
| Incremental directory streaming to the UI | Beta | ⏳ Planned |
| Multi-drive support | Beta | ✅ Shipped |
| Unified single-pass scan engine | Beta | 🛠️ In progress |
| Bulk-deletion performance rework | Beta | 🛠️ In progress |
| Advanced thermals (GPU core/hotspot/VRAM) | Beta | ⏳ Planned |
| Dark / light theme toggle | v1.0.0 | ⏳ Planned |
| CPU history line-chart view | v1.0.0 | ⏳ Planned |
| Scheduled / automated nukes | v1.0.0 | ⏳ Planned |
| Predictive analytics, behavioural baselines, macOS layer | v3.0 | 🔭 Future |

---

## 🛠️ Installation & Setup

### Prerequisites

- [Flutter SDK](https://flutter.dev/docs/get-started/install) (stable channel)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Git

> 🛑 Clone into a path containing **no non-ASCII characters**. Emoji or other non-ASCII characters anywhere in the path — including your Windows profile folder name — break tooling that resolves paths through the legacy ANSI code page.
> 

### Running the Backend (C# Engine)

1. `cd backend`
2. Restore NuGet packages: `dotnet restore`
3. Launch the API + SignalR Hub: `dotnet run`

> 💡 On Windows, run your terminal **as Administrator** for full thermal/fan/power readings.
> 

The server initializes the Disk Scanner Engine and awaits WebSocket connections (default base URL `http://localhost:5200`).

### Running the Frontend (Flutter UI)

1. `cd frontend/gs_analyzer_ui`
2. Fetch dependencies: `flutter pub get`
3. Ensure `ApiService` in `lib/services/` points at `http://localhost:5200`.
4. Launch: `flutter run -d windows` *(or `-d linux` / `-d macos`)*

---

## 🗺️ Roadmap

### 🔜 Public Beta — Sept 2026

Feature-complete. Focus: `NET_IO`, `DISK_IO`, fine-grained cache invalidation, RAM pressure alerting, watcher event log, and full platform-matrix validation (Windows 10/11 + Ubuntu 22.04).

### 🎯 Release — Oct/Nov 2026 (Official Release)

Stable release of the full telemetry + disk-intelligence suite described above.

### 🔭 Release 2 — Q1 2027

- Predictive analytics and behavioural baselines
- Cross-platform expansion (macOS native layer)

---

## 💬 Community

Join the **GS System Analyzer** Discord — the hub for contributors and developers (issue triage, design discussion, build help, and release pings):

👉 [**discord.gg/FA8WsVXMx**](https://discord.gg/FA8WsVXMx)

A dedicated user-support space will open alongside the public beta.

---

## 🤝 Contributing

Contributions are welcome from developers with Flutter/Dart, C#/[ASP.NET](http://ASP.NET) Core, or C++ experience. Please read [**CONTRIBUTING.md**](https://CONTRIBUTING.md) in full before opening a pull request — it covers the architecture rule, the HudTheme UI contract, the settings three-place rule, and the CI gates every PR is checked against.

Issue and pull request templates live under `.github/`. Start from the one that matches your change.

---

## 📄 License

Licensed under the **Apache License 2.0** — see [LICENSE](LICENSE). Third-party notices are recorded in [NOTICE](NOTICE); the project bundles LibreHardwareMonitorLib under the MPL-2.0.

---

*Contributions welcome.*