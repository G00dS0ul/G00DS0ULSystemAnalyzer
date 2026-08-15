# Contributing to GS System Analyzer

Thanks for your interest in contributing. This is an active open-source project licensed under **Apache-2.0**. Contributions are welcome from developers with experience in Flutter/Dart and C#/ASP.NET Core.

Please read this document fully before opening a pull request.

## Table of Contents

- [Where We Are Right Now](#where-we-are-right-now)
- [What We're Building](#what-were-building)
- [Before You Start](#before-you-start)
- [Setting Up](#setting-up)
- [Engineering Rules](#engineering-rules)
- [Continuous Integration](#continuous-integration)
- [Pull Request Process](#pull-request-process)
- [Compensation & IP](#compensation--ip)
- [Code of Conduct](#code-of-conduct)
- [Questions & Community](#questions--community)

---

## Where We Are Right Now

> 🚧 **Stage: Pre-Beta, feature freeze approaching.**
> The engine and every primary telemetry panel are shipped and running. What is left for v2.0 is network and disk I/O, fine-grained cache invalidation, and alerting polish.
>
> **Public Beta — Sept 2026** · **Official release — Oct/Nov 2026**
>
> Active focus right now: `NET_IO`, `DISK_IO`, Memory Cache TTL & Invalidation, RAM Pressure Alerts, Watcher Event Log, and the Multi-Drive review follow-ups.

**Roadmap at a glance:**

- **Beta (now → Sept 2026)** — Network + disk I/O panels, fine-grained cache invalidation, RAM pressure alerting, watcher event log, incremental scan streaming.
- **Release 1.0 (Oct–Dec 2026)** — Multi-drive support, advanced GPU thermals, dark/light theme, unified single-pass scan engine, scheduled/automated nukes.
- **Release 2.0 (Q1 2027)** — Predictive analytics, behavioural baselines, macOS native layer.

> ⚠️ Feature-level status is tracked in **GitHub Issues**. An open issue means the feature is not done, regardless of what any board says.

---

## What We're Building

GS System Analyzer is a cross-platform desktop application for real-time system telemetry, disk intelligence, and high-performance file management. It combines CPU/RAM/thermal monitoring, visual disk analysis, and a bulk file operations engine — all in a single Cyber-HUD interface built with Flutter and a C# backend.

The repo is a **monorepo** with two top-level projects — a C# backend and a Flutter desktop client:

```text
core/
├── backend/                          ASP.NET Core 10 backend (C#, SignalR)
│   ├── BackgroundWorkers/            DriveMonitorService, ScheduledScanWorker
│   ├── Controllers/                  REST endpoints (Storage, Nuke, Telemetry, Thermal,
│   │                                 Drives, Settings, Schedules, Startup, Audit,
│   │                                 ScanDiff, TempFiles, TelemetryHistory)
│   ├── Engine/                       DiskScannerEngine, CpuSamplerEngine,
│   │                                 RamMonitoringEngine, ThermalMonitoringEngine,
│   │                                 FileTypeScanner, AgeHeatmapEngine
│   ├── Hubs/                         SignalR hubs (TelemetryHub → /hubs/telemetry)
│   ├── Interfaces/                   Service contracts (ISensorProvider, etc.)
│   ├── Models/                       Backend data models
│   ├── Services/                     Sensor, scan, nuke, telemetry, OEM services
│   ├── Program.cs                    App startup / DI registration
│   ├── GSSystemAnalyzer.csproj       Main backend project
│   ├── GSSystemAnalyzer.slnx         Solution file
│   ├── GSSystemAnalyzer.Tests/       xUnit backend tests
│   └── GSSystemAnalyzer.Benchmarks/  BenchmarkDotNet suites
│
└── frontend/
    └── gs_analyzer_ui/               Flutter app (Dart, Riverpod)
        ├── lib/
        │   ├── models/               Immutable data structures (StorageNode, DriveStats)
        │   ├── providers/            Riverpod state notifiers / providers
        │   ├── screen/               Main layout canvases (AnalyzerDashboard)
        │   ├── services/             External comms (ApiService, TelemetryService)
        │   ├── utils/                Global keys, HudTheme, NukeProtocol
        │   ├── widgets/              Modular UI components (Nodes, HUDs, Headers)
        │   └── main.dart             App entry point + ProviderScope wrapper
        ├── test/                     Flutter widget + unit tests
        ├── integration_test/         End-to-end integration tests
        ├── android/ ios/ linux/ macos/ web/ windows/   Platform runners
        └── pubspec.yaml
```

- **Backend** lives at `/backend` → ASP.NET Core 10, C#, SignalR. Main project: `GSSystemAnalyzer.csproj`. Backend unit tests live in `/backend/GSSystemAnalyzer.Tests`.
- **Frontend** lives at `/frontend/gs_analyzer_ui` → Flutter + Dart with Riverpod. Widget/unit tests live in `/frontend/gs_analyzer_ui/test`.
- **Benchmarks** also live in a companion repo, `GS-SystemAnalyzer/gs-benchmark`, which holds the threshold validator and the storage/memory/CPU/thermal suites used by the performance CI gate.

---

## Before You Start

1. **Check open issues first.** If you want to work on something, comment on the issue to claim it before writing code. This avoids duplicate work.
2. **Use the templates.** Bugs, features, docs, and performance issues each have a template under `.github/ISSUE_TEMPLATE/`. PRs have type-specific templates under `.github/PULL_REQUEST_TEMPLATE/` (feature, bugfix, performance, docs, ci_infra). Fill them in — incomplete reports get sent back.
3. **No unsolicited rewrites.** Do not refactor code outside the scope of your assigned issue. If you see something worth improving, open a separate issue for it.
4. **Read the architecture rule.** Flutter never calls OS APIs directly. All OS-level work goes through the C# backend and arrives at the Flutter client via SignalR or REST. Do not break this boundary.

---

## Setting Up

**Prerequisites**

- Flutter SDK (stable channel)
- .NET 10.0 SDK
- Git

### 1) Fork & clone

```bash
git clone https://github.com/<your-username>/core.git
cd core
```

> 🛑 **Clone into a path with no non-ASCII characters.**
> Emoji or other non-ASCII characters anywhere in the path (including your Windows profile folder name) can break tooling that still resolves paths through the legacy ANSI code page. If your user profile contains such characters, clone to something like `C:\dev\` instead of your home directory.

### 2) Start the backend (Terminal 1)

```bash
cd backend
dotnet restore
dotnet run            # serves on http://localhost:5200
```

- Run your terminal **as Administrator** — thermal/sensor reads require elevated privileges. Without it, thermal and several telemetry panels may show empty or ghost data.
- On corporate/vPro machines with HVCI or Memory Integrity enabled, the low-level sensor driver is blocked. The sensor stack falls back to WMI automatically; reduced thermal data on such machines is expected.
- Alternatively, open `backend/GSSystemAnalyzer.slnx` in Visual Studio / Rider and run from there.

### 3) Start the frontend (Terminal 2)

```bash
cd frontend/gs_analyzer_ui
flutter pub get
flutter run -d windows      # or: -d macos / -d linux
```

- Before running, confirm `ApiService` (in `lib/services/`) points at `http://localhost:5200`.
- The Flutter client is a reactive shell — it shows no real data unless the backend is running, because all OS-level telemetry arrives via SignalR/REST.

### 4) Iterate with hot reload

With `flutter run` active, edit any Dart file and:

- Press **`r`** for **hot reload**: pushes UI changes in ~1s and preserves current app state.
- Press **`R`** for **hot restart**: full state rebuild for provider or app-entry changes.
- Most IDEs (VS Code, Android Studio) hot-reload automatically on save.

---

## Engineering Rules

These are not suggestions. Every pull request is checked against them.

### 1) Architecture

- Flutter → SignalR/REST → C# backend → OS APIs. This is the only permitted data flow.
- Flutter widgets receive data via Riverpod providers connected to SignalR streams.
- No direct OS calls from Dart code.

### 2) UI — HudTheme

All colors, typography, and decorations must reference `HudTheme` constants from `lib/utils/hud_theme.dart`.

```dart
// ✅ Correct
Text('58.6 GB', style: HudTheme.valueStyle)

// ❌ Wrong — never hardcode colors
Text('58.6 GB', style: TextStyle(color: Colors.white))
```

- All panel headers use **ALL_CAPS with underscores** via the `HudLabel` widget.
- Icon colors: folders → `accentAmber`, files → `accentGreen`, delete → `accentRed`, navigation → `accentCyan`.
- `HudTheme` is currently a `static const` class. Do not add new hardcoded colors as a workaround — if a token is missing, add it to `HudTheme`.

### 3) Settings keys — the three-place rule

Any new configurable value must land in **all three** places in the same pull request:

1. the `AppSettings` schema (backend model + `toJson`/`fromJson`),
2. the validation rules (type + accepted range), and
3. the Settings panel UI.

A key that exists in only one or two of these is a bug, not a partial feature. PRs that add a key to fewer than three places will be sent back.

### 4) No duplicated helpers

Before writing a formatter, check whether one exists. Byte/rate formatting has been duplicated repeatedly; extend the shared helper instead of adding another copy.

### 5) Logging

- No raw `print()` in Dart. Use `debugPrint`/logger gated on `AdvancedSettings.enableDebugLogs`.
- Hardware and OEM probes must fail **once**, quietly, and cache the negative result. Re-probing a permanently unavailable sensor on every poll cycle floods logs and hides real failures.

### 6) Testing

- Every new backend service must include at least **one xUnit test** covering the happy path and **one edge case**.
- File deletion tests must use a **temp directory only** — never real paths.
- Every new Flutter widget must include at least **one widget test** covering the empty/loading state.
- Anything that walks the filesystem needs a test case with a **non-ASCII path**.
- Tests live in `/backend/GSSystemAnalyzer.Tests` (C#, xUnit) and `/frontend/gs_analyzer_ui/test` (Flutter). Integration tests live in `/frontend/gs_analyzer_ui/integration_test`.

Run the full suite before opening a PR:

```bash
# Backend (from /backend)
dotnet test

# Frontend (from /frontend/gs_analyzer_ui)
flutter analyze
flutter test
flutter test integration_test
```

```csharp
// ✅ Always use temp directories for file tests
var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
Directory.CreateDirectory(tempRoot);
```

### 7) Repo hygiene

- **Never commit scan caches, build output, or generated artifacts.** The repository already carries a large history from a committed scan cache; do not make it worse.
- End every file with a trailing newline.
- Keep diffs free of unrelated whitespace churn.

---

## Continuous Integration

Five workflows run in `.github/workflows/`:

| Workflow | What it does |
| --- | --- |
| `dart.yml` | `flutter analyze` · Flutter tests |
| `dotnet-desktop.yml` | Backend build + `dotnet test` |
| `benchmark.yml` | BenchmarkDotNet suites against `thresholds.json` |
| `benchmark-report.yml` | Publishes the benchmark comparison |
| `leak-perf-guard.yml` | Leak & performance gates |

**Leak & Performance Guard — three gates:**

1. **Soak test** — 1,000 iterations; heap growth must stay under 5 MB and thread count under 10.
2. **Benchmark regression** — fails if any benchmark exceeds 150% of its recorded baseline.
3. **Flutter leak tracking** — `leak_tracker_flutter_testing` must report no leaked widgets.

A red gate blocks merge. Do not disable a gate to get a PR through — if a gate is wrong, note it in the PR and open an issue against the gate.

---

## Pull Request Process

1. Fork the repo and create a branch from `main`.
2. Branch naming: `feature/short-description` or `fix/short-description`.
3. Pick the matching PR template from `.github/PULL_REQUEST_TEMPLATE/`.
4. Write or update tests for any code you add.
5. Run existing tests before submitting — PRs that break passing tests will not be reviewed.
6. Keep PRs focused. One feature or fix per PR. Large PRs will be asked to be split.
7. Write a clear PR description: what you changed, why, and how to test it.
8. **Backend contract changes and their frontend callers must land together.** A backend-only PR that changes a route or payload shape and leaves Flutter `ApiService` on the old contract will not be merged on its own.
9. Not all contributions will be accepted. Decisions are based on product direction and engineering priorities.

**Merge gate — every PR to `main` must satisfy:**

1. New backend service → at least 1 xUnit test (happy path + 1 edge case).
2. File deletion code → tests use temp directories only, never real paths.
3. New Flutter widget → at least 1 widget test covering empty/loading state.
4. Cross-platform feature → tested on at least Windows before merge; Linux validation tracked in the issue.
5. No hardcoded colors → all colors reference `HudTheme` constants.
6. No new settings key unless schema, validation, and UI all ship together.
7. No raw `print()`; logging gated on `enableDebugLogs`.
8. All three CI gates green.

---

## Compensation & IP

This project is **open source** under **Apache-2.0** and currently **bootstrapped with no revenue**.

- Contributing is **voluntary**.
- There is **no payment** at this stage.
- All contributions are licensed under Apache-2.0, the same license as the rest of the project.
- You retain the right to showcase your contributions in your portfolio and professional profiles, provided no confidential internal information (outside the public repo) is disclosed.
- Third-party notices are recorded in `NOTICE`. The project bundles LibreHardwareMonitorLib under MPL-2.0; if you add a dependency, add its notice there.

---

## Code of Conduct

The full text lives in `CODE_OF_CONDUCT.md`. In short:

- Be direct and professional in reviews and discussions.
- Criticism of code is not criticism of the person.
- No dismissiveness toward contributors at any experience level.
- The project lead has final say on technical direction.

---

## Questions & Community

- **GitHub Discussions** — open a discussion or comment on the relevant issue. Do not DM for questions that belong in public; public discussions help everyone.
- **Discord** — join the contributor community at [https://discord.gg/FA8WsVXMx](https://discord.gg/FA8WsVXMx) for onboarding, dev chat, and release pings.

---

*Contributions welcome.*
