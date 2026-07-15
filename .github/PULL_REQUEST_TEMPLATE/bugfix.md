## Bug fix

Closes #

### The bug
<!-- What was happening? Steps to reproduce. -->

### Root cause
<!-- Why it happened. -->

### The fix
<!-- What you changed and why it resolves the root cause. -->

### Regression coverage
<!-- The test that fails WITHOUT your fix and passes WITH it. -->

### Checklist
- [ ] Added/updated a test that fails without this fix
- [ ] `dotnet test` + `flutter analyze && flutter test` pass locally
- [ ] Change is scoped to the issue — no unrelated edits
- [ ] File-deletion code (if any) uses temp directories only, never real paths
- [ ] Colors use `HudTheme` constants (if UI touched)
- [ ] Verified on Windows; Linux status noted in the issue
- [ ] Branch named `fix/…`
