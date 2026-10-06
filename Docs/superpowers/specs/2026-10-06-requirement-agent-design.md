# Requirement Agent First Milestone Design

Date: October 6 2026
Status: Proposed for Veli review

## Purpose and Constraints

Veli needs demonstrable analysis work by October 13. Implement the first Requirement Agent against docs/ANALYSIS_CONTRACT.md and the three saved sample submissions. Target Veli's 16 GB RAM / RTX 3050 Ti with sequential calls to an externally managed Ollama instance. Do not assume Ollama or a model is installed.

The output is a usable local Python module and command-line demo, not a new ASP.NET app or Ayşegül's entire service. Submission-language coverage initially demonstrated by the fixtures is C#; do not claim broad language evaluation.

## Approaches Considered

1. Recommended: independent Python module, configurable Ollama adapter, and CLI. It demonstrates Veli's first milestone without waiting for the shared web service and can be wrapped by a LangGraph node later.
2. Implement a FastAPI/LangGraph service immediately. This adds integration and dependency work already assigned to Ayşegül before the agent itself has been evaluated.

Use approach 1. Keep orchestration and transport separate from assessment so the team can reuse the agent without invoking the CLI.

## Files and Boundaries

Proposed ai-service structure:
- pyproject.toml: Python package metadata, dependency constraints, and CLI entry point.
- src/submission_analysis/schemas.py: strict request, model-output, and response validation.
- src/submission_analysis/agents/requirement.py: assessment orchestration and provenance checks.
- src/submission_analysis/llm/ollama.py: configured HTTP calls; injectable interface for tests.
- src/submission_analysis/prompts/requirement.py: versioned instructions and source/context serialization.
- src/submission_analysis/cli.py: read request, invoke analysis, write response, return an appropriate process exit code.
- tests/: meaningful validation, failure, and evidence tests.
- README.md: setup, environment variables, sample commands, integration notes, and limits.

The public analyze function accepts a validated request and an injected model adapter. Ayşegül can call it from the shared service or graph. No filesystem paths chosen by a browser are accepted by the agent. The CLI reads only the local request file explicitly supplied by its user.

## First Supported Request

Support requested_agents equal to ["requirement"] only. Reject requests for unimplemented agents explicitly before a model call. Preserve the version 1.0.0 envelope and all required output fields; do not pretend Judge or Feedback ran. Use Pydantic for strict structural validation and explicit semantic validators for IDs, rubric totals, education/grade combinations, paths, and evidence references.

Reject duplicate JSON keys and non-finite numbers. Normalize line endings before source referencing; preserve other content. Bound request bytes, files, source text, and model output with documented configurable limits. For this milestone reject an over-limit request rather than silently truncate it.

## Assessment Flow

1. Validate the request and supported mode.
2. Prepare a redacted source view before sending prompts; retain path/line correspondence. Apply conservative credential-pattern redaction, disclose any substitutions, and document that redaction is not guaranteed to find every secret.
3. Send assignment context, rubric, and line-numbered source through the model adapter. Student files are untrusted data; never execute them or honor instructions inside them.
4. Request structured requirement_results, findings, and a short summary. Start with qwen2.5-coder:3b as a configurable default, not a hardcoded model requirement.
5. Validate output against the requested requirements and supplied source evidence. Reject unknown IDs, missing/duplicate results, inconsistent finding links, unsupported provenance, and invented excerpts or line ranges. Permit substantiated absence findings with no excerpt as defined in the contract; uncertainty must remain unable_to_assess.
6. Construct timestamps, analysis ID, agent run, actual attempted model metadata, and source coverage in code. The model must not invent service metadata.
7. Return a completed envelope for valid output, or a failed envelope with no successful findings/results when the model attempt fails. Never use expected.json as input to the assessment.

The model adapter uses a configurable URL and finite timeout, with streaming disabled. Do not retry model calls automatically in the first version. Report unavailable model/service, timeout, and invalid output separately. The CLI exits nonzero for pre-execution errors or failed analyses and saves a structured error/result when an output path was supplied.

## Missing Context and Claims

An incomplete inventory or omitted relevant source may justify unable_to_assess. A model cannot claim it compiled, ran, or tested student code. A valid result is not proof of correctness; source-reference checks cannot establish whether a model's reasoning is sound.

The first milestone returns student_feedback null. Its summary is a requirements-only assessment. Teacher grading remains outside this module.

## Verification

Deterministic tests use an injected fake model adapter, explicitly identified as simulated responses. Cover:
- Valid request and complete response construction.
- Invalid education/grade, rubric, paths, duplicate IDs/JSON keys, and unsupported agent selections.
- Missing or duplicate requirement results and broken finding references.
- Fabricated evidence, wrong lines, and invented file paths.
- Redacted credential evidence preserving line correspondence.
- Source/context limits and incomplete-inventory uncertainty.
- Model timeout/unavailability and malformed structured output.
- CLI writing JSON and signaling errors with exit status.

Separately run the real model on each saved fixture if Ollama and the configured model are available. Compare requirement statuses, evidence accuracy, false positives, omissions, and latency with expected.json in an evaluation command. Save real outcomes separately from deterministic tests. A missing model is an explicit limitation, never replaced by fixture-derived answers disguised as AI output.

## Done Criteria

The package can be installed using a local environment, its documented CLI accepts the saved request JSON, structural/evidence tests pass, and the output conforms to the proposed contract. Document whether real model runs have been completed and their actual results. Remaining agents, HTTP endpoints, archive ingestion, shared graph orchestration, and analytics remain later milestones.