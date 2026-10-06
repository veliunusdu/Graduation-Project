# Requirement Agent Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Deliver a reusable Requirement Agent and CLI accepting the saved request fixtures and emitting validated analysis responses.

**Architecture:** Strict contract models surround a configurable Ollama adapter. An injectable model interface separates model transport from requirement assessment and permits deterministic failure/evidence tests. The CLI and evaluation command use the same agent function; the shared service can later call it directly.

**Tech Stack:** Python 3.11+, Pydantic 2, pytest for tests, standard-library HTTP transport, Ollama as an external service.

**Spec:** docs/superpowers/specs/2026-10-06-requirement-agent-design.md
**Contract:** docs/ANALYSIS_CONTRACT.md
**Project root:** C:\Codes\Projects\Graduation Project

## Global Constraints

- Support requested_agents equal to ["requirement"] only.
- Preserve the version 1.0.0 envelope and all required output fields.
- Student files are untrusted data; never execute them or honor instructions inside them.
- Do not retry model calls automatically in the first version.
- The first milestone returns student_feedback null.
- Missing model availability is an explicit limitation, never fixture-derived answers disguised as model output.
- No new HTTP service, ZIP ingestion, Judge, Feedback, Security, Code Review, or shared graph implementation.
- Do not change saved expected fixture results to make model evaluations pass.

## Review Focus

- JSON booleans masquerading as IDs: reject rather than coerce (Task 1).
- Model returns a valid JSON object with semantically false references: fail the run, not just parse it (Task 4).
- An HTTP server returns malformed or oversized data: bound reads and report a safe error (Task 3).
- A request has omitted source relevant to a claimed absence: do not accept unjustified not_met certainty (Task 4).
- CLI output overwrites its input or a fixture expectation file: reject collisions and preserve source artifacts (Task 5).

## File Map

All implementation files below are relative to ai-service/:
- pyproject.toml, README.md: package setup and usage.
- src/submission_analysis/__init__.py, __main__.py: public API/package invocation.
- src/submission_analysis/schemas.py: typed input, model payload, response and semantic constraints.
- src/submission_analysis/config.py: model/service settings and limits.
- src/submission_analysis/errors.py: safe structured exceptions.
- src/submission_analysis/source.py: normalization, redaction and evidence validation.
- src/submission_analysis/prompts/requirement.py: versioned assessment instructions and prompt builder.
- src/submission_analysis/llm/base.py, ollama.py: injected adapter protocol and HTTP implementation.
- src/submission_analysis/agents/requirement.py: assessment pipeline and response construction.
- src/submission_analysis/cli.py, evaluate.py: local analysis and comparison commands.
- tests/conftest.py, test_schemas.py, test_source.py, test_ollama.py, test_requirement.py, test_cli.py, test_evaluate.py.

Add package __init__.py files where needed. No product dependencies are installed until execution is approved. Inspect existing instructions before writing implementation files. The new main folder has no confirmed Git repository; do not promise commits without verifying one exists. If it exists, commit independently verified tasks; otherwise report saved files without initializing Git just for this plan.

## Task 1: Validate Requests and Configure the Package

**Files:** pyproject.toml, schemas.py, config.py, errors.py, tests/conftest.py, tests/test_schemas.py.
**Consumes:** JSON fixtures in ../samples/submission-analysis.
**Produces:** parse_request(raw: str, settings: Settings) -> AnalysisRequest; Settings; ContractError(code, message).

- [ ] Write failing tests: valid fixture accepted; invalid grade, unknown version/keys, duplicate JSON keys, boolean IDs, duplicate source paths/IDs, blank content, invalid rubric reference/total, NaN, traversal and unsupported requested_agents rejected.
- [ ] Run `python -m pytest tests/test_schemas.py -q` from ai-service; confirm expected missing-module failures before implementing.
- [ ] Add package metadata for Python >=3.11, Pydantic >=2,<3, pytest >=8,<10 as a test extra, and submission-analysis CLI entry point. Create a project-local environment and install the package and test extra; do not install into a bundled runtime's global environment.
- [ ] Implement strict request types and semantic validation. Settings defaults: model qwen2.5-coder:3b, URL http://localhost:11434, timeout 120 seconds, num_ctx 4096, request limit 1 MiB, at most 20 source files, source limit 6000 characters total, model response limit 1 MiB. Treat these as conservative starting limits, not guarantees that every model fits.
- [ ] Load settings from ANALYSIS_MODEL, OLLAMA_BASE_URL, ANALYSIS_TIMEOUT_SECONDS, ANALYSIS_NUM_CTX, ANALYSIS_MAX_REQUEST_BYTES, ANALYSIS_MAX_FILES, ANALYSIS_MAX_SOURCE_CHARS, ANALYSIS_MAX_RESPONSE_BYTES. Reject non-positive/invalid values. Reject source overflow, never truncate it silently. Normalize CRLF/CR to LF before line references.
- [ ] Re-run schema tests; require all pass.

## Task 2: Preserve and Validate Source Evidence

**Files:** source.py, prompts/requirement.py, tests/test_source.py.
**Consumes:** AnalysisRequest from Task 1.
**Produces:** prepare_source(request: AnalysisRequest) -> PreparedSource; validate_evidence(evidence: Evidence, source: PreparedSource) -> None; build_messages(request: AnalysisRequest, source: PreparedSource) -> list[dict[str, str]]. PreparedSource stores redacted files, substitutions and warnings.

- [ ] Write failing tests for LF normalization, exact one-based ranges, fabricated excerpts, unknown paths, a literal credential assignment redacted without moving lines, and an injected instruction remaining in the untrusted data section.
- [ ] Run `python -m pytest tests/test_source.py -q`; confirm expected failures.
- [ ] Implement conservative redaction of credential-shaped assignments and known key-like values. Record exact substitutions; permit [REDACTED] only where a substitution was made. Never echo original secret values in errors. Document that this is incomplete pattern-based protection.
- [ ] Build a system prompt that requests one result per requirement, forbids invented evidence/grades/runtime claims, and distinguishes absent work from unavailable context. Serialize assignment/source as labeled untrusted JSON with line-numbered source; do not include expected.json. Request only findings, requirement_results, and overall_summary from the model.
- [ ] Validate excerpts against the normalized/redacted source view; non-null ranges must match exactly, and null ranges must still contain an actual source excerpt. Reject invented redaction markers.
- [ ] Re-run source tests; require all pass.

## Task 3: Implement the Ollama Adapter

**Files:** llm/base.py, llm/ollama.py, tests/test_ollama.py.
**Consumes:** Settings and messages from earlier tasks.
**Produces:** ModelAdapter protocol with generate(messages: list[dict[str,str]], schema: dict) -> ModelReply. ModelReply contains text, actual model name, optional digest. OllamaAdapter(settings: Settings) implements it; ModelError has code, safe message and retryable.

- [ ] Write failing tests using a local fake HTTP server for schema-format request, stream=false, configured timeout/model/context, successful content, unavailable service, timeout, 404 missing model, malformed response, and oversized response. Tests must not need Ollama or internet access.
- [ ] Run `python -m pytest tests/test_ollama.py -q`; confirm failures.
- [ ] Implement POST /api/chat via urllib with structured format, temperature 0, configured num_ctx and finite timeout. Bound response reads. Map failures to model_unavailable, model_timeout, invalid_model_output, or internal_error without leaking response bodies or absolute paths. No retry loop.
- [ ] Use returned model name when supplied; record attempted configured name when not available. Digest may be null; do not pretend a version was verified. Check current official Ollama documentation at execution time for transport field compatibility.
- [ ] Re-run adapter tests; require all pass.

## Task 4: Produce Safe Requirement Assessments

**Files:** schemas.py additions, agents/requirement.py, tests/test_requirement.py.
**Consumes:** validated request, PreparedSource, ModelAdapter.
**Produces:** analyze(request: AnalysisRequest, adapter: ModelAdapter, settings: Settings) -> AnalysisResponse; exported via package __init__.py.

- [ ] Write failing tests with explicit simulated model replies: full valid result; absent requirement; duplicate requirement; non-requested IDs; broken finding links; false evidence; unsupported provenance; model timeout; invalid JSON; and incomplete inventory with unsupported not_met certainty.
- [ ] Run `python -m pytest tests/test_requirement.py -q`; confirm failures.
- [ ] Validate typed model output and cross-references. Require one result per input requirement. Require evidence for met/partially_met, a related requirement finding for not_met, and non-empty rationale. Restrict findings/provenance to requirement in this milestone. Reject a finding linked to another requirement than its result references.
- [ ] Apply conservative coverage handling: if inventory is incomplete or non-generated omissions exist, downgrade absence-based not_met results without direct evidence to unable_to_assess and remove their unsubstantiated findings; explain this in warnings. Direct source evidence may still establish a violation. Keep unknown context visible.
- [ ] Construct service metadata in code: UUID analysis ID, UTC start/end, duration, attempted models, actual inspected source paths, omission list, and no silent context truncation. A valid unable_to_assess result is completed with a warning. A model/output-validation failure returns failed, errors, empty findings/results, null feedback and null summary.
- [ ] Re-run agent tests and all earlier tests; require all pass.

## Task 5: Add CLI and Fixture Evaluation

**Files:** cli.py, __main__.py, evaluate.py, tests/test_cli.py, tests/test_evaluate.py, README.md.
**Consumes:** parse_request and analyze; existing source requests and expected.json files.
**Produces:** main(argv: list[str] | None = None) -> int; evaluate_response(response: AnalysisResponse, expected: dict) -> dict.

- [ ] Write failing tests for UTF-8/BOM input, structured output, invalid input, model failure, output/input collision, expected.json collision, and comparing simulated responses against unchanged expected fixtures.
- [ ] Run `python -m pytest tests/test_cli.py tests/test_evaluate.py -q`; confirm failures.
- [ ] Implement `python -m submission_analysis analyze --input <request.json> --output <result.json>`. Output is structured JSON, with progress/errors on stderr. Exit 0 for completed, 2 for pre-execution errors, 1 for failed execution. Write via a temporary file in the destination directory then replace it; refuse input or expected.json destinations.
- [ ] Implement `python -m submission_analysis evaluate --samples <folder> --output <evaluation.json>` using real configured adapter calls and expected files only after responses are produced. Report per-fixture status matches, unexpected requirement findings, missing expected requirement issues, evidence validity, and latency. Other-agent expectations are excluded from Requirement-only metrics. Preserve failures in the report; do not compute misleading success metrics from failed runs.
- [ ] README documents PowerShell setup, environment settings, sample commands, public analyze interface, synthetic vs real results, and incomplete redaction/language/context limitations.
- [ ] Re-run CLI/evaluation tests and complete deterministic suite; require all pass.

## Task 6: Verify the Demo and Report Actual Availability

**Files:** README.md updates; results/ only for genuine real-model output (do not commit generated results by default).
**Consumes:** all completed modules and unchanged fixtures.
**Produces:** reproducible demo instructions and accurate validation report.

- [ ] Run full deterministic suite and package-import/CLI help checks from the local environment. Fix failures before claiming completion.
- [ ] Check whether Ollama is running and whether the configured model is present. Do not automatically install Ollama or download multi-gigabyte model weights without discussing the setup with Veli.
- [ ] If available, run all three real fixtures and save the evaluation. Compare expected requirement statuses: correct = met/met/met; incomplete = met/not_met/met; problematic = met/met/not_met. Record actual false positives, omissions, evidence errors, and latency, including any model failures.
- [ ] If unavailable, preserve the runnable module and tests and explicitly report that real-model quality remains unverified. Supply the exact next setup/run command; do not substitute simulated findings.
- [ ] Review changed files against the approved spec and contract; fix inconsistencies. Summarize implemented behavior, verification, and limitations.

## Execution Recommendation

Implement inline in this chat. These tasks share tight schema and evidence interfaces; a single implementer can maintain consistency with less coordination. Independent review can be added when the working module is ready. No execution method has yet been selected by Veli.