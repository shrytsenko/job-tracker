# Job Tracker

A personal job-search tracker with AI assistance, built with **ASP.NET Core**, **React** and **TypeScript**.

Track applications, recruiters and referrals in one place, see which follow-ups are due, and compare how different job tracks are converting. An AI assistant analyses job descriptions, flags restrictions like "US work authorization required", scores how well a role matches your profile, and drafts replies to recruiters.

> 🚧 **Work in progress.** I'm building this in public while running my own job search. See the roadmap below for what's done and what's next.

## Why I built it

During my own job hunt I ended up juggling LinkedIn messages, Djinni, recruiter emails, freelance platforms and referrals from former colleagues across several markets. Spreadsheets didn't answer the questions I actually had:

- Who haven't I replied to, and who hasn't replied to me?
- Is this role even open to someone based in Spain?
- Which track is working better for me: developer or test automation?

So I'm building the tool I needed, and using it as a showcase of how I approach software: a clean backend, tests at every level, CI from day one and practical AI integration.

## Features

- [x] Applications with status, track, channel, compensation and next action (API)
- [x] Contacts (recruiters, referrers, hiring managers) linked to applications (API)
- [x] Activity timeline per application (API)
- [ ] "Needs follow-up" view for stale applications and overdue actions
- [ ] Dashboard with a funnel and conversion rates per track
- [ ] AI job description analysis: stack, seniority, location restrictions, match score, red flags
- [ ] AI-drafted replies to recruiters (always editable, never sent automatically)
- [ ] MCP server so AI assistants like Claude can read and update the tracker

## Tech stack

| Layer | Technology |
| --- | --- |
| Backend | C#, ASP.NET Core Web API, Entity Framework Core |
| Database | SQLite |
| Frontend | React, TypeScript, Vite |
| AI | LLM provider behind a swappable `ILlmClient` interface |
| Testing | xUnit, `WebApplicationFactory` integration tests, Playwright end-to-end tests |
| CI/CD | GitHub Actions |
| Packaging | Docker, docker-compose |

## Roadmap

1. ✅ **Core tracker API** — domain model, CRUD endpoints, validation, tests
2. **React UI** — list, filters, detail page, follow-up view
3. **Dashboard** — funnel and conversion per track
4. **AI job analysis** — structured analysis of pasted job descriptions
5. **AI reply drafts** — recruiter messages and emails
6. **MCP server** — manage the tracker from Claude
7. **Portfolio polish** — CI badge, Docker, screenshots, architecture diagram

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
cd backend
dotnet run --project src/JobTracker.Api
```

The API starts on `http://localhost:5122` and opens **Swagger UI** at `http://localhost:5122/swagger`, where you can try every endpoint. The raw OpenAPI document is at `/openapi/v1.json`. The SQLite database (`jobtracker.db`) is created and migrated automatically on first start. [`JobTracker.Api.http`](backend/src/JobTracker.Api/JobTracker.Api.http) has ready-made requests for VS Code / Rider / Visual Studio.

### Running the tests

```bash
cd backend
dotnet test
```

## API overview

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/applications?status=&track=&channel=` | List applications, optional filters |
| GET | `/api/applications/{id}` | Application with contacts and timeline |
| POST | `/api/applications` | Create |
| PUT | `/api/applications/{id}` | Update (including status) |
| DELETE | `/api/applications/{id}` | Delete (with its timeline) |
| POST | `/api/applications/{id}/activities` | Add a timeline entry |
| PUT / DELETE | `/api/applications/{id}/contacts/{contactId}` | Link / unlink a contact |
| GET, POST | `/api/contacts` | List / create contacts |
| GET, PUT, DELETE | `/api/contacts/{id}` | Read / update / delete a contact |

Enums travel as strings (`"Applied"`, `"SDET"`). Errors follow [RFC 9457 ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457); validation errors list the offending fields under `errors`.

## Architecture

```
backend/
  src/JobTracker.Core            Domain entities, enums and rules. No dependencies.
  src/JobTracker.Infrastructure  EF Core DbContext, SQLite, migrations.
  src/JobTracker.Api             Minimal API endpoints, request/response contracts, error handling, OpenAPI.
  tests/JobTracker.Core.Tests    Unit tests for domain rules (xUnit).
  tests/JobTracker.Api.Tests     Integration tests through HTTP with WebApplicationFactory and in-memory SQLite.
```

Key decisions:

- **Minimal APIs with route groups** instead of controllers: less ceremony and the idiomatic style in modern .NET, while still grouped per resource.
- **Rules live on the entities.** For example, logging a real interaction moves `LastContactOn` forward, and moving to `Applied` stamps `AppliedOn`. The endpoints stay thin and the rules are unit-testable without a database.
- **No repository layer.** `DbContext` already is a unit of work over repositories. Another abstraction would add code without adding value at this size.
- **Enums stored as text** in SQLite, so the database file is readable and reordering enum members can't corrupt data.
- **Integration tests run the real migrations** against an in-memory SQLite database, so they test the same schema production uses.

## Privacy

The repository contains only fictional seed data. Real data stays in a local SQLite database that is excluded from version control, and API keys are read from environment variables or .NET user secrets.

## Author

**Serhii Hrytsenko** · Software Engineer · Barcelona, Spain

[LinkedIn](https://www.linkedin.com/in/shrytsenko) · [GitHub](https://github.com/shrytsenko)
