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

- [ ] Applications with status, track, channel, compensation and next action
- [ ] Contacts (recruiters, referrers, hiring managers) linked to applications
- [ ] Activity timeline per application
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

1. **Core tracker API** — domain model, CRUD endpoints, validation, tests
2. **React UI** — list, filters, detail page, follow-up view
3. **Dashboard** — funnel and conversion per track
4. **AI job analysis** — structured analysis of pasted job descriptions
5. **AI reply drafts** — recruiter messages and emails
6. **MCP server** — manage the tracker from Claude
7. **Portfolio polish** — CI badge, Docker, screenshots, architecture diagram

## Getting started

Setup instructions will be added with the first working version.

## Privacy

The repository contains only fictional seed data. Real data stays in a local SQLite database that is excluded from version control, and API keys are read from environment variables or .NET user secrets.

## Author

**Serhii Hrytsenko** · Software Engineer · Barcelona, Spain

[LinkedIn](https://www.linkedin.com/in/shrytsenko) · [GitHub](https://github.com/shrytsenko)
