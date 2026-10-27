# Veli Week 7 RAG preparation

User asked to begin next week after Week6 preparation. Retrieval still absent in main. Use writing-plans/verification-before-completion; no production feature changes, API assumptions or model downloads.

- [x] Branch from main with merged Week6 PR10; preserve root README/unrelated local work.
- [x] Reuse three same-topic TXT inputs unchanged; seed recipe with logical IDs and authorized empty course, two HS courses to detect education-only filtering.
- [x] Prepare12 query expectations and18 blocked product scenarios. Unrelated queries do not require zero results absent an agreed relevance threshold; check retrieved sources/metadata, not just LLM prose.
- [x] Verify hash/sentinel/query/template/Markdown consistency and original reference fixtures. Product calls remain0.
- [ ] Publish Week7-only preparation PR, no merge/reviewer assignment.

Every actual result, execution time and unconfirmed contract field remains null. Embedding/vector DB/source material ingestion depend on teammates. Existing sourcebody IDs are labels only; replace with actual course/material/chunk mapping, never treat source prose as metadata. No semantic benchmark quality claim.

VerificationPASS:3unchangedTXT hashes/sentinels;4logicalcourses;12queries;18unique blockedcases; authcasebinds unenrolled_actor to real-hs-b recipe; actualfields/contractsnull. Original19fixturecheckerPASS andlocalMarkdownlinksPASS. Productcalls0; no semanticbenchmark claim.
