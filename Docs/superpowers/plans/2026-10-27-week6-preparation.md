# Veli Week 6 preparation

User approved publishing the existing preparation as a Week6-specific checklist and results template. Use writing-plans and verification-before-completion. No product implementation or API invention.

- [x] Reuse unchanged19 original fixtures/checker; publish their previously untracked package. Keep RAG fixtures solely because existing integrity checker expects the complete package; retrieval testing remains Week7.
- [x] Add one harmless unsupported-extension fixture and a Week6 manifest referencing16 extraction cases plus that fixture.
- [x] Create21 blocked/not-executed product scenarios:16format/empty/corrupt +unsupported +metadata +success/failure states +recovery. Leave actual responses, execution timestamps and service contracts null.
- [x] Verify all hashes, original reference parsers, JSON/template/relative-link consistency. These checks prove preparation only.
- [x] Commit only package/docs/evidence on test/veli-week6-preparation and publish PR; no merge, reviewer assignment, LLM calls or app DB usage.

Decisions: user opted for continuous execution; no new design-approval gate. Existing root README edits and unrelated untracked work stay excluded. Team must supply realCourseMaterial/extraction/chunking endpoints and agree limits/status/error/encoding metadata before product calls. Product cases BLOCKED and overall NOT_RUN; readiness is not completion.

Verified27October: original19reference fixtures PASS; Week6 manifest17hashes PASS; template21unique W6 cases allBLOCKED, actual fieldsnull, contractsunconfirmed, productcalls0. LocalMarkdownlinks PASS. Observed reference runtimePython3.12.14/pypdf6.10.0.

Stagingcheck caught expected PDFxref spaces and whitespace-only negative TXT. Marked PDF/DOCX/PPTX/bin fixtures binary and disabled whitespacecheck only for intentional whitespace.txt; fixturebytes unchanged, not stripped to satisfy source-code lint.

Published https://github.com/veliunusdu/Graduation-Project/pull/10. PRscopechecked: onlyDocs/fixtures andattributes; no productionchanges, no merge/reviewerassignment. Native artifactattachmenttool notavailable; explicit PRlink returned.
