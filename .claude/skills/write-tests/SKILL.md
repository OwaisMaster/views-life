---
name: write-tests
description: Write or update ViewsLife tests for a backend or frontend change, then run the relevant validation commands. Use when endpoints, auth, tenant scoping, UI flows, validation, or security-sensitive code changes.
disable-model-invocation: true
allowed-tools:
  - Read
  - Write
  - Edit
  - MultiEdit
  - Glob
  - Grep
  - Bash(git diff *)
  - Bash(git status *)
  - Bash(dotnet restore *)
  - Bash(dotnet build *)
  - Bash(dotnet test *)
  - Bash(npm run lint *)
  - Bash(npm run build *)
  - Bash(npm run test *)
---

Write or update tests for: $ARGUMENTS

Follow this workflow exactly:

## 1. Inspect scope first
- Read the changed files and identify the behavior that actually changed.
- Use `git diff` and related file reads before writing tests.
- Prefer small, high-value tests over broad speculative coverage.

## 2. Respect ViewsLife testing priorities
- Preserve deterministic tests.
- Avoid flaky timing-based or environment-dependent tests.
- Prefer backend integration tests for API/auth/tenant behavior.
- Prefer backend unit tests for isolated service or business-logic behavior.
- Prefer frontend tests for rendering states, auth flow handling, error handling, and user-visible behavior.
- Do not add brittle tests that assert internal implementation details unnecessarily.

## 3. Focus on project-specific risk areas
When relevant, explicitly test:
- tenant isolation
- auth cookie/session behavior
- claims-based authorization
- input validation and normalization
- generic error handling
- security hardening paths such as lockout or abuse prevention
- SSR-safe frontend auth behavior
- note visibility/privacy behavior

## 4. Match existing project patterns
- Reuse the existing test structure and naming patterns in the repo.
- Do not invent a parallel testing style if the repo already has a clear pattern.
- Keep test names descriptive and behavior-focused.

## 5. Run relevant validation
Use only the commands relevant to the touched area.

For backend changes, prefer:
- `dotnet restore`
- `dotnet build`
- `dotnet test`

For frontend changes, prefer:
- `npm run lint`
- `npm run build`
- `npm run test` if the project already supports it

Do not fail the task merely because a command does not exist in the repo yet. Note that clearly in the summary.

## 6. Output requirements
At the end, provide:
- files added or updated
- behaviors covered
- commands run
- pass/fail results
- remaining meaningful test gaps, if any

## 7. Constraints
- Do not weaken existing tests just to make the suite pass.
- Do not remove security-sensitive coverage without a strong reason.
- Do not claim full coverage.
- Keep diffs reviewable.