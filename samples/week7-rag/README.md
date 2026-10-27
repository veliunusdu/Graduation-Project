# Week 7 RAG isolation and retrieval test preparation

No product retrieval/embedding/index calls are made by this package.
`corpus-mapping-template.json` references three unchanged synthetic TXT files from
../ai-rag-smoke, proposes three same-topic courses plus a real authorized empty course,
and leaves actual IDs empty. `queries.json` holds12 logical query expectations.
The18 product scenarios remain BLOCKED in the [result template](../../Docs/VELI_WEEK7_RESULTS_TEMPLATE.json).

See the [checklist](../../Docs/VELI_WEEK7_RAG_CHECKLIST.md). This tiny sentinel corpus
supports isolation smoke checks, not a representative semantic retrieval benchmark.
Do not send these mapping/expectation files as product request payloads or ingest them
as course documents. Ingest only the three referenced TXT documents into their own courses.
