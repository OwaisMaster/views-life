---
name: update-docs
description: Update ViewsLife documentation after a code or architecture change. Use when APIs, auth behavior, tenant behavior, security posture, infrastructure, roadmap state, or implementation progress changes.
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
---

Update ViewsLife documentation for: $ARGUMENTS

Follow this workflow exactly:

## 1. Read code changes first
- Inspect the changed files before editing docs.
- Use `git diff` and direct file reads to determine what actually changed.
- Only document behavior that exists or decisions that were actually made.

## 2. Update the right docs
Prefer updating existing docs rather than creating duplicates.

Check for the closest matching files such as:
- `/docs/`
- phase progress documents
- technical specification documents
- development plan documents
- `/docs/adr/` for significant architecture decisions

If filenames differ from expectations, update the closest existing equivalent instead of inventing redundant files.

## 3. What must be documented when relevant
Update documentation when changes affect:
- architecture
- auth flows
- tenant model behavior
- security controls
- public or internal API contracts
- frontend/backend integration behavior
- environment or deployment assumptions
- AI enrichment behavior
- search behavior
- feature completion status
- roadmap sequencing

## 4. Documentation standards
- Be precise and factual.
- Do not overstate what is complete.
- Do not describe placeholders as production-ready.
- Preserve separation between current state, known gaps, and planned next steps.
- Keep wording clear enough for future contributors.
- Keep terminology consistent with the repo’s existing docs.

## 5. If an ADR is warranted
Create or update an ADR when:
- there is a meaningful architectural tradeoff
- a new cross-cutting rule is introduced
- a deployment or infrastructure decision changes
- a security-sensitive design decision is made

Include:
- decision
- context
- alternatives considered
- tradeoffs

## 6. Output requirements
At the end, provide:
- files updated
- what changed in the docs
- any doc gaps you intentionally left unchanged
- whether an ADR was created or recommended

## 7. Constraints
- Do not create busywork documentation.
- Do not duplicate the same content in multiple files unless the repo already expects it.
- Do not rewrite large docs unnecessarily when a small targeted update is enough.