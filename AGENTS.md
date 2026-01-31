# Agent Instructions

This project uses **bd** (beads) for issue tracking. Run `bd onboard` to get started.

## Quick Reference

```bash
bd ready              # Find available work
bd show <id>          # View issue details
bd update <id> --status in_progress  # Claim work
bd close <id>         # Complete work
bd sync               # Sync with git
```

## Creating Issues

**Every issue MUST include:**

1. **Context** - Background information explaining:
   - Why this work is needed
   - How it fits into the larger system
   - Any relevant technical constraints

2. **Acceptance Criteria** - Clear, testable requirements:
   - Use checkbox format: `- [ ] Criterion`
   - Be specific and measurable
   - Include edge cases where relevant

**Example:**
```bash
bd create --title="Add user authentication" --type=feature --priority=2 --description="## Context
The app currently has no auth. Users need to log in to access their WSL instances.
Parent feature: User Management (wslr-xyz)

## Acceptance Criteria
- [ ] Login form with username/password fields
- [ ] Password validation (min 8 chars)
- [ ] Error messages for invalid credentials
- [ ] Session persists across app restarts"
```

## After Closing a Task

**When you close a bd task**, immediately commit and push the changes:

```bash
bd close <id> --reason="description of what was done"
git add <changed-files>
git commit -m "feat: description of changes

Closes: <id>"
bd sync
git push
```

**Why?** This ensures:
- Work is preserved immediately (not lost if session ends unexpectedly)
- Other agents/sessions can see the latest changes
- Clear atomic commits tied to specific tasks

## Landing the Plane (Session Completion)

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

**MANDATORY WORKFLOW:**

1. **File issues for remaining work** - Create issues for anything that needs follow-up
2. **Run quality gates** (if code changed) - Tests, linters, builds
3. **Update issue status** - Close finished work, update in-progress items
4. **PUSH TO REMOTE** - This is MANDATORY:
   ```bash
   git pull --rebase
   bd sync
   git push
   git status  # MUST show "up to date with origin"
   ```
5. **Clean up** - Clear stashes, prune remote branches
6. **Verify** - All changes committed AND pushed
7. **Hand off** - Provide context for next session

**CRITICAL RULES:**
- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds

