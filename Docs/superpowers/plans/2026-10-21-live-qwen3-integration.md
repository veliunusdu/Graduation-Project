# Live Qwen3 integration verification

User installed Ollama/qwen3:4b after deferred model setup; continue previously requested Week5 actual chain verification. Inline executing-plans, no new production routes/defaults or model downloads.

Observed: Ollama0.40.7, qwen3:4b digest359d7dd4bcdab3d86b87d73ac27966f4dbb9f5efdfcc75d34a8764a09474fae7. Direct project OllamaClient returned INTEGRATION_OK/done=true in3.75s. First tiny direct probe hit output-length limit, and forcing think=false is unsupported by installed model metadata (thinking.values=[true]); no shared transport change made. Default model thinking separates response correctly.

- [x] Add an opt-in .NET test of the real AIServiceClient POST /llm/test, skipped by default when live endpoint is absent; no application DB startup.
- [x] Drive owned Python processes with per-case env: installed model success, deliberately nonexistent model, unreachable Ollama URL, actual upstream timeout. Existing Ollama daemon stays running.
- [x] Save real responses/statuses and failure evidence, run relevant deterministic regressions, independent review, local commit.

Claims bounded: actual ASP.NET service class -> real FastAPI -> real Ollama -> return response, not an implemented web analysis screen. Unreachable URL is a controlled network-unavailability test, not physical shutdown of user's daemon. All negative Python responses map to HTTP502; ASP.NET shows generic http_error. No Requirement Agent quality claim until three real fixtures actually evaluated.

- Actual chain4/4passed, eachTRX1passed/1executed. Regression154passed/1intentional opt-in skip; Python128passed/1existingwarning. TRX notExecuted counter is0 even for skip; verified total155 minusexecuted154 andconsole skip instead.
- Independent reviewnoImportant/Critical. Minorlimitsdocumented: normalinvocationwithout-O; timeoutnotprovenspecificallyread; partialstdouton.NETtimeoutnotpersisted.
- Requirement fixture quality separatelyrunning with real qwen3:4b, timeout180s, outputreserve4096, context16384. No outcomeclaimed untilrecord saved.
