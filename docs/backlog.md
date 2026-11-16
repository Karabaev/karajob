# Backlog

Ideas that came up along the way but are not part of the current phase (see [roadmap.md](roadmap.md)). Format: one line — the idea, where it came from, which phase it belongs to.

- Profile deletion: decide what to do with its data (cascade all personal data, or transfer it), and a confirmation in the UI. From #41, as the situation demands.
- `OnValidatePrincipal` checks that the profile exists in the DB on every request with a cookie, static files included. If it becomes noticeable, check once every N minutes by `IssuedUtc`. From the #41 review, as the situation demands.
- Switching profile in one tab: in another open tab the circuit stays on the old profile until reload, and the header switcher returns 400 (the antiforgery token is bound to the old identity). Catch the antiforgery error on `/profiles/select` and redirect to `/profiles`. From the #41 review, as the situation demands.
- Extracting CV text on upload (PdfPig for PDF, OpenXML for DOCX) and storing it in `CvVersion` — needed for `get_cv_text` and AI scenarios. From #11, Phase 1.
- Cleaning up orphaned files in `/data/files` (a file left behind after a failed deletion, or in Development after the in-memory database restarts). From #11, as the situation demands.
- Display time zone for dates: in Interactive Server `ToLocalTime()` runs on the server (UTC in the container). Pick one zone (a setting or the browser offset) and a shared formatter. The same zone for entering dates: "today" by default in the application and event dialogs is currently computed in UTC. From the #11 and #14 reviews, Phase 0.5 — #38.
- REST `POST /api/inbox` (URL + note → `VacancyService.QuickAddAsync`) for the iPhone Shortcuts quick command from design.md "Quick Add to Inbox". Needed together with access from outside the home and some authorization of the endpoint. From #13, Phase 0.5 — #48.
- Loading a vacancy pulls `Matches`, `Events` and `Platforms` in one query (a Cartesian product), and the board loads platforms it does not show. If it becomes noticeable — `AsSplitQuery()` in `VacancyService` and `IgnoreAutoIncludes()` on the board. From the #60 review, as the situation demands.
- "Activity" goals for contacts and CV adaptations in `SearchGoal` and the metrics: in Phase 0.5 only applications are counted, because the Contact entity and CV adaptations come later. From the #18 decomposition, Phase 2.
