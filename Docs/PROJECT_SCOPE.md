# AI Assisted Homework Management and Learning Platform

## Project Goal
Support student learning and help teachers review homework with evidence-backed AI findings. Teachers decide final feedback and grades.

This scope follows the supplied roadmap and task distribution. Veli reported team agreement on scope and collaboration conventions on October 6 2026, recorded in TEAM_REVIEW.md. Supervisor-specific requirements and separate technical decisions remain unconfirmed.

## Target Users
Teachers and students in enrolled courses.

## Education Levels
HighSchool grades 9-12 and University years 1-4. Course context determines analysis expectations and explanation depth.

## Core Features
Authentication, Teacher/Student roles, ownership checks, courses, enrollment, assignments, requirements, rubrics, materials, submissions, and teacher review.

## AI Features
Course-filtered RAG, learning-oriented Tutor, teacher-approved assignment drafts, Requirement/Code Review/Security analysis, Judge reconciliation, Feedback, and analytics from structured findings.

## Veli Responsibilities
Scope and collaboration conventions; Requirement, Code Review, and Security agents; Judge and Feedback logic; tests and experiments; analytics aggregation logic.

Ayşe owns ASP.NET, persistence, authorization, integration, and analytics UI/API. Ayşegül owns the shared Python service, model client, LangGraph infrastructure, RAG, Tutor, and Generator. Shared interfaces require team agreement.

## First Milestone
Sample source files and explicit requirements enter the Requirement Agent. Structured requirement results with evidence come out and are checked against expected results.

Working October 13 2026 target: independently demonstrable analysis modules that can integrate with the shared service. Exact submission requirements still need confirmation.

## Proposed Analysis Contract
Input: submission/assignment identifiers, education/grade context, requirements, rubric, and source files with relative paths and text. Exact field names and ZIP transport require team agreement.

Finding: agent type, category, severity, title, description, evidence, suggestion. Evidence includes source path, line numbers when available, and excerpt.

Requirement results: met, partially met, not met, unable to assess. Missing context does not prove success or failure. No findings does not prove all requirements are met.

Output: status, findings, requirement results, summary, student feedback, model/configuration metadata. Failures remain visible. Judge reconciles duplicate/conflicting findings without inventing a final grade.

## Sample Submissions and Evaluation
Prepare three synthetic projects for one assignment: mostly correct, missing requirements, and seeded code-quality/security issues. Keep expected findings and locations beside each fixture.

Measure coverage, false positives, missed seeded issues, evidence accuracy, schema validity, latency, and repeatability. Separate deterministic contract tests from actual model quality evaluation.

## Technology Stack
ASP.NET Core MVC, EF Core, SQLite; Python, LangGraph, REST/JSON; Ollama; BGE-M3 for shared RAG.

Roadmap models: Qwen3 8B and Qwen2.5-Coder 7B. Proposed starting models for 16 GB RAM / RTX 3050 Ti: Qwen3 4B and Qwen2.5-Coder 3B, pending benchmark and team agreement. Sequential requests, bounded source context, explicit reporting of omitted content.

## Out of Scope
Automatic final grades, complete student homework generation, mobile app, separate React/Vue frontend, voice, parent dashboard, Kubernetes, executing student code initially, guaranteed detection of all defects, and implementing teammates' entire subsystems for Veli's first milestone.

## Agreed Collaboration Conventions
See [CONTRIBUTING.md](../CONTRIBUTING.md) for branch names, PR/issue title formats, ownership, and review conventions. Reusable issue templates and a PR template are stored in `.github/`. Record actual team responses in [TEAM_REVIEW.md](TEAM_REVIEW.md). Veli reported team agreement on these conventions on October 6 2026.

## Open Questions
Exact October 13 deliverables; initial submission languages; shared schema/transport agreement; additional supervisor requirements.
