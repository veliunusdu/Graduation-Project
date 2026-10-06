# Submission analysis fixtures

These are synthetic static-analysis snippets, not runnable web applications. No student code has been executed and no model results have been generated.

Each folder contains source code, a contract-compatible Requirement-only request, and human-authored expected results. request.json intentionally includes the same source text as the adjacent .cs file so it can be supplied directly to the future analysis module. Keep them synchronized when editing.

- mostly-correct: all three requirements are satisfied within the supplied method.
- incomplete: deadline validation is missing.
- problematic: input guards exist, but SQL concatenation violates req-3; synthetic credential logging and raw-title logging provide security review cases.

Initial evaluation: compare requirement statuses and evidence against expected.json. Exact wording and finding IDs need not match. Expected severity is intentionally not fixed before calibration. Judge must merge the same SQL issue reported by multiple agents without double-counting it.

For later agent evaluation, change requested_agents to ["requirement", "code_review", "security", "judge", "feedback"] and use a fresh request_id. No HTTP endpoint or runner is implemented yet.

False-positive traps: do not demand a controller, EF Core, connection opening, or an exception-swallowing catch; none is required by this assignment. Do not treat the synthetic credential as a real secret. Code quality checks should report concrete issues, not enforce unrelated style preferences.

This three-case set is a development smoke test, not a representative benchmark. Add missing-context, malformed-output, failure, and language-level cases before making academic quality claims.
