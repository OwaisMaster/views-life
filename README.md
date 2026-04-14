# ViewsLife

ViewsLife is a multi-tenant, AI-enhanced, interactive note archive platform focused on importing Apple Notes into a searchable, explorable, and collaborative web experience.

## Repository Structure

- `frontend/` — Next.js + TypeScript web application
- `backend/` — ASP.NET Core Web API (BFF)
- `infra/` — infrastructure as code and deployment assets
- `docs/` — technical specifications, ADRs, and project documentation

## Local Development

### Frontend
Run from `frontend/`:

```bash
npm install
npm run dev

### Backend
Run from `backend/ViewsLife.Api`:

```bash
dotnet restore
dotnet run --launch-profile https

### Infra
docker compose -f docker-compose.postgres.yml up -d
docker compose -f docker-compose.postgres.yml down 
