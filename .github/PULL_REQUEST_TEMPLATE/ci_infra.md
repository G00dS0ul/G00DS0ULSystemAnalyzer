## CI / Build / Infrastructure

Closes #

### What changed
<!-- Workflow, action, build config, secret usage, matrix, etc. -->

### Why
<!-- The problem this solves. -->

### Blast radius
<!-- Which workflows/jobs are affected? Fork PRs? Secrets? Default-branch-only behaviour? -->

### Verification
<!-- Link to a run where this passed (or intentionally failed). -->

### Checklist
- [ ] On the correct branch (`workflow_run` workflows only take effect from `main`)
- [ ] No secret values committed; secrets referenced via `secrets.*`
- [ ] Linked a run showing the intended result
- [ ] Shell steps are robust (no silent `set -e` / `pipefail` foot-guns; globs handle no-match)
- [ ] Matrix/artifact names are consistent across producer and consumer workflows
