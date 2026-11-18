# Roadmap

The first goal is to replace the applications spreadsheet with a working tracker on the home server within 2–3 days. After that, each phase starts only when the previous one is used daily: the job search itself matters more than the tool.

| Phase | Essence | Duration | When to start |
| --- | --- | --- | --- |
| 0 | Manual tracker | 2–3 days | right away |
| 0.5 | Metrics, Weekly Review, master profile, PWA | ~1 week | once 3–4 weeks of applications have accumulated |
| 1 | AI via MCP | 2–3 days | after Phase 0, can be earlier than 0.5 |
| 2 | Background automation | 1–2 weeks | when manual entry becomes the bottleneck |
| 3 | Based on usage results | — | as the situation demands |

## Phase 0 — manual tracker (2–3 days)

Tasks are GitHub Issues in the [Phase 0](https://github.com/Karabaev/karajob/milestone/1) milestone. Each item carries its issue number.

**Result:** vacancies are added with the "+ Vacancy" button and moved along the board, and an application is recorded with a track and a CV version. All transitions are written to the journal. The app runs on the home server and opens from a phone via WireGuard.

### Day 1 — skeleton and data

- [x] #1 GitHub repository: a solution of the projects `Karajob.Domain`, `Karajob.Application`, `Karajob.Infrastructure`, `Karajob.Web`, `Karajob.Tests`; `.gitignore` with `.env`, `*.db`, `data/`.
- [x] #2 Entities: User, Company (slug, domains, aliases, UserCompanyNote), Project, Track, CvDocument + CvVersion, Vacancy + VacancyMatch, JobApplication, ApplicationEvent (bound to the vacancy). Time is UTC `DateTime`, money is integers, slugs and domains are lowercase.
- [x] #3 Stage transition rules in Domain (to Applied only with a track and a CV version, to Closed only with an outcome) + unit tests for them.
- [x] #4 EF Core + SQLite, the first migration, unique indexes (slug, domain, normalized vacancy URL), a `UserId` filter. A single user is created by the seed.
- [x] #5 Migrations at startup with a database backup; a `SqliteInMemory` mode with seed data for debugging.
- [x] #6, #7 Dockerfile + `docker-compose.yml` with a `/data` volume; GitHub Actions with `dotnet test`.

### Day 2 — reference data and the card

- [x] #8 Blazor (Interactive Server) + MudBlazor: a layout with a menu and a "+ Vacancy" button in the header.
- [x] #9 Companies: a list, a form (slug, name, website, domains, description, an icon from the favicon), company projects.
- [x] #10 Tracks: a form from a template (role, locations, industries, format, language, positioning, primary CV, status). Create 3 tracks:
  1. Unity Developer, PC, remote, Russian
  2. Unreal Developer, PC, remote, Russian
  3. Unity Developer, PC, remote or relocation to the EU, English
- [x] #32 Reference data for roles, languages and locations; tracks and vacancies link to them. Matching by format and location is not here, it is the Phase 2 prefilter.
- [x] #11 CV: PDF/DOCX upload, a list of versions, download. `IBrowserFile.OpenReadStream` has a default limit of ~500 KB — set `maxAllowedSize` explicitly.
- [x] #12 Vacancy card: a form (company with autocomplete and on-the-fly creation, role, link, location, format, matching tracks, notes), an event timeline.
- [x] #13 "+ Vacancy": a dialog with a single URL field (+ a note) → a card in Inbox, deduplication by URL. The company and role can be filled in later. Inbox is a separate section in the side menu: a list of cards with the buttons "→ To the board" (Targeted + tracks) and "Dismiss" (with a reason → Dismissed).

### Day 3 — board, application and rollout

- [x] #14 A kanban on `MudDropContainer`: Targeted → Preparing → Applied → Interviewing → Closed (Closed is collapsed; Inbox is not a column but a menu section, see #13). A filter by track.
- [x] #14 Every move writes an ApplicationEvent; an invalid transition is rolled back with a message.
- [x] #15 Dragging to Applied opens an "I applied" dialog: track, CV version, channel, date, cover letter text (pasted by hand). Dragging to Closed opens a dialog with the outcome.
- [ ] #16 Rollout to the home server: a fixed IP, a check from a phone via WireGuard, a daily copy of `/data` to another device. **Deferred:** daily database snapshots and the rollout instructions are ready (#40), the rollout itself and the off-server copy come later. The image is published to GHCR, and the server is deployed from it without the sources (#66).
- [x] ~~#17 Move the current applications from the spreadsheet into the tracker (by hand if there are fewer than 30; otherwise a simple CSV import).~~ No longer relevant.

### Not in Phase 0

Metrics and Weekly Review, check-ins and track decisions, master profile, profile selection screen, PWA and HTTPS, export/import, CV adaptation, any AI, calendar, sending emails. If you feel like adding something from this list along the way, write it into the backlog and move on.

### Done when

- [ ] The spreadsheet is no longer maintained: all current vacancies and applications are in the tracker.
- [ ] A vacancy can be added from a phone outside the home in a couple of clicks.
- [x] Every application is bound to a track and a CV version, and the journal has all transitions.
- [ ] Restarting the container does not lose data, and an off-server backup exists.

### Where you can get stuck

| Trap | How to avoid it |
| --- | --- |
| Render modes in Blazor (.NET 8+): the page rendered but the buttons do not react | The template `dotnet new blazor --interactivity Server --all-interactive` |
| `DbContext` in Blazor Server lives for the whole session and hits concurrent-request conflicts | `IDbContextFactory` and a short-lived context per operation |
| Drag-and-drop in MudBlazor turns out to be inconvenient | "→ next column" buttons on the card; on a phone they are more convenient anyway |
| A SQLite file in a container without a volume — the data disappears on rebuild | The connection string points to `/data`; verify by rebuilding on Day 1 |

## Phase 0.5 — when data has accumulated (~1 week)

Epic: #18. Tasks are in the [Phase 0.5](https://github.com/Karabaev/karajob/milestone/2) milestone.

**Result:** a Weekly Review is held once a week: for each active track the metrics are visible with "k of n" and an interval, and a check-in and a decision are recorded. Each family member has their own profile. A vacancy can be added from a phone via "Share" (Android) or a quick command (iPhone). The database can be exported to JSON and loaded back.

Order: block A → block B; blocks C, D, E, G are independent after #41.

### A. Profiles and settings

- [x] #41 Profile selection for the family: a selection screen → a cookie, a switcher in the header, `ICurrentUser` on the circuit, the `DbContext` factory is scoped.
- [x] #42 `SearchGoal`: a weekly applications goal, a deadline, a desired income; metric thresholds (market, conversion, minimum sample, maturation) — separate for each profile. A "Settings" screen.
- [x] #60 Platform reference data (PC, Consoles, Mobile, VR, Web): a track, a vacancy and a search goal link to several platforms.
- [x] #58 Currency reference data (by ISO code): the currency of a vacancy's salary and of the desired income is chosen from the reference data.
- [x] #59 Engine reference data: a company project has one engine, while a track, a vacancy and a search goal link to several.

### B. Metrics, decisions and Weekly Review

- [x] #43 Metrics calculation in Application: market, "k of n" conversion over matured applications + the Wilson interval, activity against the goal, a "not enough data" status.
- [x] #44 Track dashboard: criteria, "k of n" and the interval next to the percentage.
- [x] #45 Check-ins (motivation, confidence) and decisions (Continue / Expand / Disable) with a metrics snapshot; Expand starts a new period; a hint based on the decision tree.
- [x] #38 The user's time zone for displaying and entering dates (needed for week boundaries).
- [x] #46 Weekly Review screen: all active tracks, a check-in, decisions, stale applications → Ghosted, actual against the weekly goal.

### C. Master profile

- [x] #47 Master profile: Markdown with fixed sections, versions, preview, restore.

- [x] #98 Master profile access moved from the side menu to the profile dropdown (above "All profiles").
- [x] #100 Settings access moved from the side menu to the profile dropdown (after "Master profile").
- [x] #92 Links in the master profile Preview and the version dialog (`.markdown-body a`) use the theme primary color with an underline and a darker hover state.

### D. Access from a phone

- [x] #48 A personal profile token (generated, regenerated and revoked on demand in Profile Settings → API token; stored hashed, shown once) and `POST /api/inbox` with `Authorization: Bearer` for an iPhone Shortcuts quick command; Profile Settings got a vertical menu (General, API token). Instruction: `docs/iphone-shortcuts.md`.
- [ ] #49 HTTPS on a reverse proxy, PWA and Web Share Target → Quick Add; responsive layout of Inbox and Quick Add. Done in code: manifest, icons, service worker, `/quick-add` share target, responsive Inbox and Quick Add, deploy notes in `docs/deploy.md`. Open: the manual Android check (install PWA, Share → Karajob from the browser and LinkedIn).

### E. Data transfer

- [ ] #50 Export/import of the whole database to JSON (import only into an empty database).

### F. Localization

- [x] #62 English UI: resx localization (en by default, ru kept), reference data seeds and documentation in English.

### G. Application history

- [x] #75 Application history: every application (Applied, Interviewing, Closed) with outcome, track, CV version, channel, dates and cover letter, filters by stage/outcome and search by company or role; a closed vacancy can be reopened to Applied or Interviewing (the outcome is cleared, the application data is kept).


### G. Vacancy title

- [x] #73 Vacancy title: filled from the page behind the link (og:title / `<title>`, a plain GET without AI) when a vacancy is added, editable with a "fetch from link" button, shown on Board cards and in Inbox.
- [x] #80 Title field in the New vacancy dialog: fetched automatically when the link is entered, editable before Add; a hand-typed title is never overwritten, an empty one falls back to reading the page.
- [x] #89 Open link button next to the URL field on the vacancy card: opens the URL currently in the field in a new tab, shown only for an absolute http(s) URL.

### Company locations

- [x] #104 Company locations: a company has one or several locations from the `Location` reference data, edited on the company card and shown as chips in the companies list.

- [x] #101 Quick company creation on the vacancy page shows a toast with a link (new tab) to the company page, to fill in the rest of the card.

### CV preview

- [x] #96 CV PDF preview: a "Preview" button on PDF versions opens a dialog with the file in an `<iframe>` (the version file endpoint serves PDFs inline with `?inline=true`) and an "Open in new tab" link for phone browsers; DOCX versions keep only "Download".

### CV tags

- [x] #95 CV tags as a per-user entity: comma-separated input with autocomplete (a new tag is created on the fly), a tag directory page (`/tags`) with delete (detaches the tag from CVs) and a free hex color, colored chips in the CV list; existing tags are carried over by the `AddCvTags` migration.

### H. Branding

- [x] #82 Logo and favicon: transparent logo and icon, the favicon, the logo in the app bar instead of the text.

### I. Catalogs

- [x] #106 Time zone on a location: an optional IANA time zone, shown in the Locations table and edited in the location dialog (searchable, clearable); Serbia is seeded as `Europe/Belgrade`.
- [x] #112 Terminology: "hypothesis" is renamed to "Track" in code, UI (en/ru), the `/tracks` route, resx, tests and docs; the schema is renamed by the `RenameHypothesisToTrack` migration with data preserved.

- [x] #107 Track details in the To board dialog: language, platforms and work format shown as a secondary line under each track name.
- [x] #99 Profile settings page: name, color and time zone of the current profile are edited on `/profile-settings`, reached from the profile dropdown; the edit action is gone from "All profiles", and the avatar and time zone update without a reload.
- [x] #93 Open link icon button on the board card: opens the vacancy URL in a new tab for any stage, shown only for an absolute http(s) URL; the check is shared with the vacancy page via `VacancyText.OpenableUrl`.

### Not in Phase 0.5

Any AI and MCP, building the master profile from a CV with AI, CV adaptation and generation, the Contact entity and "Activity" goals for contacts and CV adaptations, job boards and a scanner, calendar, public access and OAuth. If you feel like adding something from this list along the way, write it into the backlog and move on.

### Done when

- [ ] The first Weekly Review has been held on real applications, and a decision with a metrics snapshot is recorded for a track.
- [ ] For every active track the dashboard shows "k of n" and the interval, and "not enough data" until the minimum sample is reached.
- [ ] The second family member has their own profile, and profile data is not mixed.
- [ ] A vacancy is added via "Share → Karajob" on Android and a quick command on iPhone.
- [ ] Export → import into an empty database restores all data.

## Phase 1 — AI via MCP

Epic: #19.

An MCP endpoint, the skills `karajob-ingest`, `karajob-apply`, `karajob-weekly-review` (based on the career-ops modes), a bookmarklet. Meetings into the calendar go through the Google Calendar connector in Claude itself. All AI work happens from Claude Desktop/Code on subscription limits.

## Phase 2 — background automation

Epic: #20.

`ILlmClient` + a worker on `claude -p`, parsing a link right from the UI, CV adaptation with a button. A scanner based on career-ops (importing companies and prompts, a sidecar if needed) plus hh.ru, with a prefilter and scoring by tracks. The Contact entity, native meeting sync with Google Calendar.

## Phase 3 — based on usage results

Epic: #21.

Assisted form filling in the browser (the `apply` mode from career-ops, stopping before Submit), a remote MCP connector with OAuth, interview preparation, rejection analysis.
