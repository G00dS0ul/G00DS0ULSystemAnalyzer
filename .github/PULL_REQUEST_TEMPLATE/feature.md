## Feature

Closes #

### What this adds
<!-- The capability, panel, service, or endpoint. -->

### Design / approach
<!-- Key decisions: new providers, services, hubs, models. Why this shape? -->

### Area
- [ ] Backend — `/backend`
- [ ] Frontend — `/frontend/gs_analyzer_ui`
- [ ] Both

### How to test
1.

### Screenshots / recordings
<!-- REQUIRED if this touches the UI (before/after). -->

### Checklist
- [ ] Architecture boundary respected (Flutter → SignalR/REST → C# → OS; no direct OS calls from Dart)
- [ ] New backend service → ≥1 xUnit test (happy path + edge case)
- [ ] New Flutter widget → ≥1 widget test (empty/loading state)
- [ ] Colors use `HudTheme` constants; headers use `HudLabel` (ALL_CAPS)
- [ ] `dotnet test` + `flutter analyze && flutter test` pass locally
- [ ] Verified on Windows; Linux status noted in the issue
- [ ] Docs / PRD updated if behaviour changed
- [ ] One feature per PR; branch named `feature/…`
