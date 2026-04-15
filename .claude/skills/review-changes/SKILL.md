---
name: review-changes
description: Review recent ViewsLife code changes for correctness, security, tenant isolation, architecture fit, and validation quality. Use before commits or PRs, especially for auth, notes, search, sharing, AI, and frontend/backend boundary changes.
allowed-tools:
  - Read
  - Glob
  - Grep
  - Bash(git diff *)
  - Bash(git status *)
  - Bash(dotnet build *)
  - Bash(dotnet test *)
  - Bash(npm run lint *)
  - Bash(npm run build *)
---

Review these changes: $ARGUMENTS

Follow this workflow exactly:

## 1. Inspect the real change set
- Start with `git status` and `git diff`.
- If arguments narrow the scope, focus there first.
- Read the touched files directly before forming conclusions.

## 2. Review against ViewsLife’s core invariants
Always check for:
- tenant isolation failures
- accidental cross-tenant reads or writes
- auth/session/cookie regressions
- claims misuse or missing authorization checks
- validation or normalization gaps
- raw error leakage to the UI
- insecure logging of sensitive information
- privacy regressions
- AI mutating original content instead of storing derived metadata
- frontend leakage of server-side or sensitive concerns
- docs or test coverage missing for risky changes

## 3. Review for architecture fit
Check whether the change:
- matches the modular monolith/BFF direction
- keeps controllers thin enough
- avoids unnecessary abstraction
- follows existing project structure and naming
- preserves a clean server/client boundary in the frontend
- keeps diffs reviewable and coherent

## 4. Validate when useful
Run only the relevant commands for the touched area:
- `dotnet build`
- `dotnet test`
- `npm run lint`
- `npm run build`

If a command is unavailable or not relevant, note that clearly.

## 5. Required output format
Return exactly these sections:

### Critical issues
List only issues that should block merge or commit.

### Medium issues
List important but non-blocking issues.

### Minor issues
List nits, cleanup items, or follow-up suggestions.

### Validation
List commands run and results.

### Overall verdict
Choose one:
- ready
- ready with fixes
- not ready

## 6. Constraints
- Do not make code changes unless explicitly asked.
- Do not invent issues without evidence from the diff or code.
- Be direct and specific.
- Reference exact files and behaviors when possible.