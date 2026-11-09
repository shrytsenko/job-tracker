# Job Tracker — project brief for Claude Code

## Who this is for

I'm Serhii, a software engineer in Barcelona (12 years in IT: 4+ years of C# / .NET backend development, 5+ years of C# test automation). I was recently laid off and I'm job hunting across several markets (EU, UK, US remote, Spain, Ukraine) and two tracks: **.NET developer** (main) and **SDET / test automation** (backup).

This project has two goals, in this order:

1. **A tool I actually use every day** to track applications, recruiters, referrals and follow-ups.
2. **A portfolio project** on my public GitHub that shows employers how I build software: clean C# backend, React UI, tests at every level, CI, and a practical AI integration.

Because of goal 2, I must be able to explain every part of this code in an interview.

## How to work with me

- Build in **small phases** (below). Finish one phase, stop, and summarise what you did and why before starting the next.
- **Explain key decisions** briefly (architecture, libraries, patterns) so I learn and can defend them.
- Prefer **simple, idiomatic, boring** solutions over clever ones. No over-engineering.
- I review and **commit myself**. Propose commit messages in Conventional Commits style (`feat:`, `fix:`, `test:`, `chore:`), one logical change per commit.
- Ask me before adding a new dependency that isn't listed here.
- Keep the README up to date as features land.

## Tech stack

| Layer | Choice |
| --- | --- |
| Backend | ASP.NET Core Web API (current LTS .NET), C#, controllers or minimal APIs (pick one and stay consistent) |
| Data | Entity Framework Core + SQLite (file-based, zero setup) |
| Frontend | React + TypeScript, Vite |
| AI | LLM provider behind an `ILlmClient` interface (start with the Anthropic Messages API; check the current official .NET SDK, otherwise use a typed `HttpClient`) |
| Tests | xUnit for unit tests, `WebApplicationFactory` integration tests for the API, Playwright end-to-end tests for the UI |
| CI | GitHub Actions: build, test, lint on every push and PR |
| Packaging | Docker + docker-compose to run API and UI together |
| Later | MCP server in C# (official ModelContextProtocol C# SDK) |

## Domain model (starting point, refine as needed)

**Application**
- Company, Role title, Job URL
- Track: `Developer` | `SDET` | `Integration` | `Other`
- Channel: `LinkedIn` | `Djinni` | `Email` | `Referral` | `Platform` (Upwork, Shakers, Toptal…) | `CompanyWebsite` | `Other`
- Status: `Interested` → `Applied` → `RecruiterScreen` → `TechInterview` → `FinalInterview` → `Offer` → `Accepted`; plus `Rejected`, `Withdrawn`, `NoResponse`
- Location / work model: remote region (e.g. "Remote EU", "US only"), hybrid city
- Employment type: Permanent | Contract | Freelance project
- Compensation: amount + currency + period (hour / day / month / year) + net/gross
- Applied on, Last contact on
- Next action + due date
- Notes (markdown)
- Linked contacts
- Optional: the job description text and its AI analysis

**Contact**
- Name, Company, Role (recruiter, referrer, ex-colleague, hiring manager), LinkedIn URL, email
- Notes, last contact date

**Activity** (timeline per application)
- Date, type (message sent, reply received, interview, note), short text

**JobAnalysis** (output of the AI feature)
- Extracted stack / required skills, seniority level, location or work-authorization restrictions, employment type, salary if stated
- Match score (0–100) against my CV, strengths, gaps, red flags (e.g. "US work authorization required")
- Model name and timestamp

## Phases

### Phase 1 — Core tracker (backend)
- Solution structure: `src/JobTracker.Api`, `src/JobTracker.Core` (domain + application logic), `src/JobTracker.Infrastructure` (EF Core, LLM client), `tests/…`
- EF Core model + migrations for Application, Contact, Activity
- REST endpoints: CRUD for applications and contacts, add activity, filter applications by status / track / channel
- Validation, consistent error responses (ProblemDetails), OpenAPI / Swagger
- Unit tests for domain rules + integration tests for endpoints
- **Done when:** I can create, list, filter and update applications through Swagger, and `dotnet test` is green.

### Phase 2 — React UI
- Applications list with filters and search
- Application detail page with timeline, contacts and next action
- Create / edit forms
- "Needs follow-up" view: applications with no contact for N days (default 5) or an overdue next action
- **Done when:** I can run my whole job hunt from the UI without Swagger.

### Phase 3 — Dashboard
- Counts per status and per track
- Simple funnel per track: applied → recruiter screen → tech interview → offer, with conversion rates
- This supports my real decision rule: after 30 days, compare the Developer and SDET funnels and decide where to focus.
- **Done when:** the dashboard shows correct numbers for the seed data (covered by tests).

### Phase 4 — AI job analysis
- Paste a job description → backend calls the LLM through `ILlmClient` → returns a structured `JobAnalysis` (JSON schema, validated before saving)
- My CV summary is stored as a config/profile entry and passed as context for the match score
- Show the analysis on the application page, with red flags highlighted
- Handle failures gracefully (timeouts, invalid JSON, missing API key)
- Tests use a fake `ILlmClient`; no real API calls in CI
- **Done when:** pasting a real job post gives a sensible analysis and the "US only" type of restriction is reliably flagged.

### Phase 5 — AI reply drafts
- From an application, generate a draft reply to a recruiter (LinkedIn message or email) using the job, my CV summary and a tone option (short / detailed)
- The draft is editable text; nothing is sent automatically

### Phase 6 — MCP server
- A separate `src/JobTracker.Mcp` project exposing tools such as: `list_applications`, `add_application`, `update_status`, `log_activity`, `get_followups`
- Lets me manage the tracker from Claude: "add the wynwood QA contract", "who haven't I replied to in 5 days?"
- README section on how to connect it to Claude Desktop / Claude Code

### Phase 7 — Polish for the portfolio
- GitHub Actions CI with a status badge
- Docker compose for one-command startup
- README: what it does, screenshots or a GIF, architecture diagram, how to run, how tests are organised, what I'd do next
- Playwright e2e tests for the main flows (create application, change status, see it in follow-ups)

## Security and privacy rules (important)

- **This repository is public.** Never commit real personal data.
- The SQLite database file is **git-ignored**. Real data lives only on my machine.
- Seed data must be **fictional** (fake companies, fake recruiters, fake emails) but realistic enough for screenshots and tests.
- API keys come from **user secrets / environment variables** only. Add a `.env.example`, never a real key.
- Don't log job descriptions or personal data at info level.

## Definition of done for any change

- Builds with no warnings treated as errors in new code
- Relevant tests added and passing
- README / docs updated if behaviour changed
- A short summary for me: what changed, why, and how to try it
