# ViewsLife — Phase 1 Development Plan (MVP Execution)

## Objective
Deliver a working MVP in ~1 week with:
- Authentication (Apple + fallback)
- Apple Notes import (core functionality)
- Note storage and display
- AI enrichment (tags + summary)
- Basic search
- Multi-tenant foundation

---

## Guiding Principles
- Move fast, avoid overengineering
- Build vertically (end-to-end slices)
- Ship working features early
- Defer non-critical complexity
- Keep architecture extensible

---

# PHASE 1 — ORDERED EXECUTION PLAN

---

## Step 0 — Project Initialization (Day 0)

### Goals
Establish a clean, production-ready foundation.

### Tasks
- Create Git repository
- Set up folder structure:
  - `/frontend`
  - `/backend`
  - `/infra`
  - `/docs`
- Initialize:
  - React (Next.js + TypeScript)
  - .NET Web API (BFF)
- Configure environment variables strategy
- Create initial README

### Output
- Running frontend + backend locally
- Clean repo baseline

---

## Step 1 — Authentication System (Day 1)

### Goals
Enable users to sign in and establish tenant identity.

### Tasks
- Implement authentication system:
  - Apple Sign-In (required)
  - Email/password (fallback)
- Create user model
- Establish session handling (JWT or cookie-based)
- Implement role system (basic: user/admin)
- Create protected routes

### Output
- Users can sign in and access dashboard
- Each user has unique identity (tenant base)

---

## Step 2 — Database & Core Schema (Day 1)

### Goals
Enable persistent storage for notes and users.

### Tasks
- Set up PostgreSQL instance
- Configure ORM (Entity Framework Core)
- Create core tables:
  - Users
  - Notes
  - NoteMetadata (JSON or structured)
- Add indexes:
  - UserId
  - CreatedAt
- Implement migrations

### Output
- Database connected and operational
- Notes can be stored per user

---

## Step 3 — Notes Domain (Core CRUD) (Day 2)

### Goals
Enable storing and retrieving notes.

### Tasks
- Implement Notes API:
  - Create note
  - Get notes (by user)
  - Get single note
- Implement visibility model:
  - private
  - public
- Ensure tenant isolation (UserId filtering)

### Output
- Notes can be saved and retrieved per user

---

## Step 4 — Apple Notes Ingestion (Critical Path) (Day 2–3)

### Goals
Allow users to import notes from Apple Notes.

### Tasks
- Implement Apple authentication integration
- Build ingestion service:
  - Fetch notes
  - Normalize data (text, metadata)
- Map Apple Notes → internal model
- Store imported notes
- Track last sync timestamp

### Constraints
- Must work in MVP (even if limited)
- If blocked:
  - implement temporary manual import fallback

### Output
- User can import notes into system

---

## Step 5 — Frontend Note Display (Day 3)

### Goals
Allow users to view their notes.

### Tasks
- Build dashboard page:
  - list of notes
- Build note detail page:
  - full content
  - metadata display
- Implement basic UI:
  - clean layout
  - mobile responsive

### Output
- Notes visible in UI

---

## Step 6 — AI Enrichment Pipeline (Day 4)

### Goals
Automatically enhance notes with AI.

### Tasks
- Implement background worker (or async job system)
- Integrate AI provider
- Build enrichment pipeline:
  - generate summary
  - extract tags
- Store results in metadata

### Constraints
- Async only (no blocking UI)
- Must not alter original note content

### Output
- Notes enriched with AI metadata

---

## Step 7 — Basic Search (Day 4–5)

### Goals
Enable users to find notes quickly.

### Tasks
- Implement database full-text search
- Add API endpoint:
  - search by keyword
- Add frontend search bar

### Output
- Users can search notes

---

## Step 8 — Permissions & Sharing (Day 5)

### Goals
Enable basic note visibility control.

### Tasks
- Add note visibility toggle:
  - private
  - public
- Implement public note access endpoint
- Generate shareable links

### Output
- Notes can be shared externally

---

## Step 9 — Minimal AI Safety Layer (Day 5)

### Goals
Prevent accidental exposure of sensitive content.

### Tasks
- Implement AI-based detection:
  - PII
  - sensitive content
- Flag notes during import
- Default flagged notes to private

### Output
- Risky notes are automatically protected

---

## Step 10 — Deployment Setup (Day 6)

### Goals
Make MVP accessible online.

### Tasks
- Deploy frontend (Vercel)
- Deploy backend (Railway/Fly.io)
- Deploy database (Neon)
- Configure environment variables
- Test production environment

### Output
- Live MVP accessible via URL

---

## Step 11 — CI/CD + Basic Observability (Day 6–7)

### Goals
Ensure stability and maintainability.

### Tasks
- Set up GitHub Actions:
  - build
  - deploy
- Add logging
- Add error tracking
- Enable basic monitoring

### Output
- Automated deployments
- Visibility into errors

---

# MVP COMPLETION CHECKLIST

## Must Be Working
- [ ] User authentication
- [ ] Apple Notes import
- [ ] Notes stored per user
- [ ] Notes visible in UI
- [ ] AI summary + tags
- [ ] Search functionality
- [ ] Public/private sharing
- [ ] Deployed environment

---

# POST-MVP (NEXT PRIORITIES)

## Phase 2 (Immediately After)
- Comments
- Reactions
- Collections
- Moderation tools

## Phase 3
- Graph visualization
- AI chat (RAG)
- Advanced semantic search

---

# KEY RISKS (PHASE 1)

## 1. Apple Notes Integration
- Potential API limitations
- Mitigation:
  - fallback import method

## 2. AI Latency/Cost
- Mitigation:
  - async processing
  - batching

## 3. Overengineering
- Avoid:
  - microservices
  - complex infra

---

# DEVELOPMENT STRATEGY

## Work Pattern
- Build feature vertically:
  - backend → frontend → test → deploy
- Commit frequently:
  - after each working feature
- Merge to main only when stable

---

# DEFINITION OF DONE (PHASE 1)

A user can:
1. Sign in  
2. Import Apple Notes  
3. See notes in UI  
4. Search notes  
5. View AI summaries  
6. Share notes publicly  

---

# FINAL NOTE

This phase is about:
- Proving the concept
- Establishing architecture
- Delivering real user value quickly

Not about:
- perfection
- completeness
- advanced features

Ship fast. Iterate immediately.