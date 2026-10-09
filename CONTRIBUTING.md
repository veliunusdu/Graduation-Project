# Team collaboration conventions

These are the shared collaboration conventions. Veli reported team agreement on October 6 2026; the confirmation and its scope are recorded in docs/TEAM_REVIEW.md.

## Branches

Use lowercase English words separated by hyphens:

| Work | Pattern | Example |
| --- | --- | --- |
| Feature | feature/<description> | feature/requirement-agent |
| Bug fix | fix/<description> | fix/submission-ownership |
| Documentation | docs/<description> | docs/analysis-contract |
| Tests or evaluation | test/<description> | test/requirement-fixtures |
| Setup or tooling | chore/<description> | chore/python-environment |

Keep one coherent change per branch. Target the shared integration branch; main is the proposed name when the repository is created. The main project folder now contains a Git repository with main and origin configured. Create feature branches for implementation work; do not reset another teammate's changes.

## Pull request titles

Use `<type>: <short description>` with feat, fix, docs, test, or chore. Examples: `feat: add requirement analysis`, `fix: reject cross-course submissions`, `docs: define analysis contract`.

Use .github/pull_request_template.md. Explain the problem and resulting behavior, list relevant validation, and identify interface changes. Link the related issue when one exists. Use a draft PR while work is incomplete.

## Issues

Use the Task template for planned work and the Bug report template for unexpected behavior. Name issues `task: <outcome>` or `bug: <observed problem>`. Identify the responsible owner, dependencies, and a concrete completion criterion. Bug reports include reproduction, expected/actual behavior, environment, and evidence.

GitHub templates are stored locally in .github/ISSUE_TEMPLATE. They become available when committed to the appropriate branch of the GitHub repository; these templates do not create online issues automatically.

## Responsibilities and review

- Ayşe: web application, authentication/authorization, persistence, REST integration, analytics UI/API.
- Ayşegül: shared Python service, model client, LangGraph infrastructure, RAG, Tutor, Generator.
- Veli: analysis agents, Judge/Feedback logic, tests and experiments, analytics aggregation logic.

Request review from a teammate affected by a change. Changes to shared analysis fields need Ayşe and Ayşegül to review them before dependent code adopts them. Document the agreed contract version and update matching examples and tests together. If two teammates need to change the same file, agree on ownership and sequence first.

Merge after the relevant reviewer approves, checks appropriate to the change pass, and conflicts are resolved. Report checks that were not run instead of marking them passed. The teacher retains final grading authority; AI findings remain review suggestions.

## Week 1 closure

Use docs/TEAM_REVIEW.md to record scope and convention decisions. Saved documents alone do not demonstrate that all three teammates share the same understanding. Do not mark another person's approval on their behalf.
