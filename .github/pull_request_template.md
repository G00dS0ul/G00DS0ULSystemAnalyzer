## Summary

<!-- What does this PR do, in 1-3 sentences? Why is it needed? -->

Closes #(issue_no)

## Type of change

<!-- Check all that apply. -->

- [ ] Feature (new panel, service, endpoint, or capability)
- [ ] Bug fix (non-breaking change that fixes an issue)
- [ ] Performance / memory (engine, scanning, benchmarks)
- [ ] CI / build / infrastructure
- [ ] Documentation
- [ ] Refactor (no behavioural change)
- [ ] Breaking change

## Area

- [ ] Backend — `/backend` (ASP.NET Core 10, C#, SignalR)
- [ ] Frontend — `/frontend/gs_analyzer_ui` (Flutter / Dart / Riverpod)
- [ ] Benchmarks — `gs-benchmark`
- [ ] Cross-cutting / both ends

## What changed

<!-- Bullet the concrete changes: files, services, widgets, endpoints, providers. -->

-

## How to test

<!-- Exact steps a reviewer runs to verify. Backend on http://localhost:5200,
     frontend via `flutter run -d windows`, hot reload, etc. -->

1.

## Screenshots / recordings

<!-- REQUIRED for any UI change (before/after). Delete this section if not applicable. -->

## Merge gate checklist

<!-- Every PR to `main` is checked against CONTRIBUTING.md. Leave items unchecked
     if they don't yet pass so reviewers can see what's outstanding. Delete a line
     only if it is genuinely not applicable to this PR. -->

- [ ] Keeps the architecture boundary: Flutter → SignalR/REST → C# backend → OS APIs (no direct OS calls from Dart)
- [ ] New backend service has ≥1 xUnit test (happy path + 1 edge case)
- [ ] Any file-deletion code uses a temp directory only — never real paths
- [ ] New Flutter widget has ≥1 widget test covering the empty/loading state
- [ ] No hardcoded colors — all colors reference `HudTheme` constants; headers use `HudLabel` (ALL_CAPS)
- [ ] Ran `dotnet test` (backend) and `flutter analyze && flutter test` (frontend) locally — all green
- [ ] Cross-platform features verified on Windows (Linux validation noted in the issue)
- [ ] PR is focused — one feature/fix; branch named `feature/…` or `fix/…`
- [ ] Read CONTRIBUTING.md and claimed the issue before starting

## Related issues / notes

<!-- Links, follow-ups, or anything reviewers should know. -->
