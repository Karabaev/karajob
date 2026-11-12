---
name: task
description: Full dev pipeline for a GitHub issue - phase check, clarifying questions, plan in plan mode, TDD tests, orchestrated implementation by worker agents in worktrees (linear branch, merged into master via PR), ADR at the end. Use as /task <issue number> [description].
---

You are the orchestrator (strong model). Run the steps in order for the task in `$ARGUMENTS`.

## 0. Name the session and load the issue
The task id is a GitHub issue number in `Karabaev/karajob`: the first token of `$ARGUMENTS` if it looks like one (e.g. `12` or `#12`), otherwise ask for it (the user may answer "none"). Read it with `gh issue view <N> -R Karabaev/karajob --comments`. Rename the session with `set_session_title` (`mcp__ccd_session_mgmt__set_session_title`, load it via ToolSearch first) using `Karajob | #<N> | <short description>`, without an id `Karajob | <short description>`. Keep the description short, in the user's language. Update the title if the scope changes materially.

## 1. Scope check and clarify
Check the task against `docs/roadmap.md`: if it belongs to a later phase or to "Не входит в Фазу N", say so and propose putting it into `docs/backlog.md` instead of doing it. Read the relevant code first. Then ask (AskUserQuestion, batches of up to 4, as many batches as needed) only about non-obvious points the user probably forgot: behavior edge cases, layer ownership (Domain / Application / Infrastructure / Web), entity and migration changes, per-user vs shared data (`UserId` filter), UI (Blazor component, render mode, mobile layout). Skip trivia you can decide by convention. Always ask: "Run the work in a separate worktree or in the main checkout?" (the feature branch is always created).

## 2. Plan (Claude Code plan mode)
Call EnterPlanMode and write the plan in the plan file; the user reviews it and approves via ExitPlanMode. Do not write code, tests, branches or docs before approval. The plan must contain: approach, subtasks (files owned with no overlap between subtasks, interfaces/signatures, tests that must turn green), and the verification. EF Core migrations (`dotnet ef migrations add`, model snapshot) and `.csproj`/`.sln`/`Directory.*.props` changes are the orchestrator's job only: workers must not create migrations, because parallel snapshots conflict. If direction changes later, re-enter plan mode.

## 3. Branch
Branch name: `feature/<N>-<slug>` (without an issue: `feature/<slug>`), always from fresh `master` (`git fetch origin && git switch master && git pull --ff-only`).
- Main checkout: `git switch -c feature/<N>-<slug>` (working tree must be clean; otherwise ask).
- Worktree: `git worktree add .claude/worktrees/<slug> -b feature/<N>-<slug> master` (`.claude/worktrees/` is gitignored) and work from there.
Workers branch from it and are integrated back into it.

## 4. Tests first (TDD)
Spawn `test-writer` with the approved spec. Run `dotnet test` and confirm the new tests are red **for the right reason** (assertion failure or `NotImplementedException`, not a compile error or broken setup) and every previously green test is still green. Commit the tests.

## 5. Delegate
- Task touching 1-2 files or with no independent subtasks: do it yourself.
- Otherwise spawn `worker` agents in parallel, one per plan subtask (`isolation: worktree`, so each gets its own worktree). Make sure the orchestrator's checkout is on the feature branch first so worker worktrees start from it; report each worker's returned branch name for integration.

## 6. Integrate (rebase only, no merge commits)
The project keeps a linear history. For each worker branch: rebase it onto the feature branch (`git rebase <feature>` in the worker's worktree/branch), resolve conflicts, then fast-forward the feature branch (`git merge --ff-only <worker-branch>`). Never create merge commits. Then add the EF Core migration if the model changed (`scripts/add-migration <Name>` once it exists, otherwise `dotnet ef migrations add`), and run `dotnet build` + full `dotnet test`. Send failures back to the worker (SendMessage) or fix yourself. Review the final diff for correctness and `CLAUDE.md` compliance. Tick the done items in `docs/roadmap.md` (`- [x]`).

## 7. Review (ask first)
Ask the user whether to run the review step. If yes, spawn the `reviewer` agent on the feature branch diff against `master`. If a PR exists for the branch, the reviewer posts its findings as PR comments (no confirmation needed); otherwise it returns them to you. Send valid findings back to workers (SendMessage) or fix them yourself, re-run `dotnet test`, and repeat until clean.

## 8. ADR
Write an ADR in `docs/adr/<Name>.md` recording the decisions as actually implemented, following `docs/adr/README.md` (Status / Context / Related ADRs / Decisions with Decision-Alternatives). Status `Accepted. Implemented for issue #<N>.` Skip for small tasks with no architectural decision. Commit it on the feature branch.

## 9. Finish (via a PR merge into master)
The feature branch reaches `master` only through a GitHub PR merged with a **merge commit**. This is the one place a merge commit is allowed; the branch itself stays linear. Never fast-forward or push `master` directly.
1. Ask the user to confirm.
2. Rebase the feature branch onto fresh `origin/master` (`git fetch origin master && git rebase origin/master`), re-run `dotnet test`, and push it (`--force-with-lease` if it was pushed before).
3. Open the PR if there isn't one (`gh pr create --base master`), body with `Closes #<N>` and a short summary, with no AI attribution.
4. Wait for the CI check (`gh pr checks <N> --watch`) and report failures instead of merging.
5. Merge it: `gh pr merge <N> --merge --delete-branch`. Then update the local `master` (`git pull --ff-only`) and remove the local feature branch and worktrees.
Delete worker worktrees/branches. Report the outcome faithfully, including skipped steps and remaining failures.
