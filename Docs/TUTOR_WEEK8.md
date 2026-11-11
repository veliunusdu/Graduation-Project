# Week 8 Tutor implementation and quality matrix

The student course search form now offers Tutor explanations. Active enrollment
and Ready materials are checked by ASP.NET before sending a profile and eligible
source excerpts to Python `/tutor/answer`. Python is an internal service and should
be reachable only from the backend. `TUTOR_MODEL` defaults to qwen3:1.7b for this
iteration. The shared general/code models retain their existing settings.

Missing source produces a deterministic no-source response without a model call.
Explicit full-homework requests in the tested wording produce a deterministic
learning prompt. The pattern match covers specific wordings and requires further
paraphrase and adversarial testing. Normal answers use JSON schema, 1536 output
tokens, an 8192 context, temperature 0 and seed 42. Truncated output or unknown
citation IDs return 502. Citation identity checks establish reference membership,
while semantic correctness is assessed separately.

## Actual product verification

Five identical questions were submitted through the real student web flow for
HighSchool 9 and University 1. Synthetic C# notes were uploaded and embedded by
real BGE-M3. Both courses had actual DB IDs and active student enrollment.
All ten requests reached a result page, and the explicit homework/no-source
controls passed. Results and the four-dimensional assistant scoring are in
`Docs/TestEvidence/Week8/2026-11-11/product-review.json`.

The quality matrix is completed with open findings. Q04 failed retrieval in both
profiles despite relevant uploaded notes. The current 0.60 threshold needs broader
calibration. University answers still confuse casts and loop types. The model is
not approved for Tutor quality. These findings require follow-up before product
acceptance. Earlier direct-model probes remain a separate baseline.

Start with a Python environment containing ai-service requirements and set
TUTOR_MODEL if another installed model is selected. Build/run the ASP.NET app
from this branch, open an enrolled course and select Tutor ile açıkla.
For repeatable real acceptance use VELI_WEEK8=1 with scripts/verify_material_live.py.
The script starts an owned Python server and uses disposable web/vector databases.
Ollama must be available with bge-m3 and qwen3:1.7b installed. A full matrix may
take several minutes. Raw HTML evidence stays local because pages contain
short-lived test antiforgery fields. The committed review keeps answer text only.
