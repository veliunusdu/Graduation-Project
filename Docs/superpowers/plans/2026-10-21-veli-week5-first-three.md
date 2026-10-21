# Veli Week 5: first three integration checks

Execute inline using superpowers:executing-plans. User explicitly requested only first three steps; no model setup, downloads, real model calls or push.

Goal: authenticated real ASP.NET status page calls real FastAPI health; Python stopped yields safe warning; actual HTTP timeout/500/503 responses yield safe warnings and matching logs.

- [x] Reuse existing process/login helpers and isolated DB/file configuration; preserve unrelated local work.
- [x] Add a separate repeatable live harness and focused validator tests; existing verify_week5.py and prior evidence remain unchanged.
- [x] Run real service startup, real Python stop, then controlled loopback fault HTTP server for timeout/500/503 (and malformed JSON). Record HTTP page status, message, correlated server log events and fault-request counts.
- [x] Re-run focused AI client/UI tests, independently review harness/evidence, record results and local commit.

Constraints: user database/files never selected; bind loopback ephemeral ports only; terminate only owned processes; fault scenarios explicitly labeled controlled transport, no Ollama/LLM quality claims; persist failed runs with safe errors and clean up owned resources. New evidence output directory must not overwrite old evidence. No production behavior changes planned.

Ledger: base 2ad7f5e; working root README and previous untracked deliverables/docs preserved. Real model validation stays deferred by user.

- RED: new validator suite could not import the absent live harness; implementation then passed 7/7 validator tests.
- Live run: 6/6 passed. Actual FastAPI success; stopped-Python warning (Windows reached timeout2s, correctly reported), controlled real HTTP delay/500/503/malformed JSON with per-request logs.
- Regression verification: focused existing Web AI-client/UI tests14/14; all Python128/128 (one existing dependency deprecation). No product behavior or model setup changed.
- Independent final review: no Important/Critical findings. Post-run actual Uvicorn and launcher/web PIDs absent; temporary directory removed.
- Local commit only, no GitHub push. Entire Week5 remains incomplete pending explicitly deferred real-model scenarios.
