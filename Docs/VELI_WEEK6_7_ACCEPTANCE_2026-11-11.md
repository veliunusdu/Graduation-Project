# Week 6–7 acceptance progress — 11 November 2026

- The isolated ASP.NET test uploaded real synthetic files into two high-school
  courses and one university course. Real Python and BGE-M3 processed them.
- A corrupt PDF returned 422 from Python and was recorded as Failed in ASP.NET.
  A single HTTP 503 injected by the web test handler also produced Failed.
  The teacher retried the latter file against the real service and it became Ready.
  The test did not stop the actual Python service to simulate the 503.
- The enrolled student could download the ready file. The failed file and the
  other teacher's course material were unavailable to that student.
- Six real retrieval requests tested top-k 1 and 3 on the three courses. Each
  returned only its own course's materials. The high-school A course had two
  results at k=3 and one at k=1. Metadata, expected markers, descending scores
  and first result consistency passed.
- Table-only and mixed PPTX regression cases preserve text and slide numbers.
  SQLite refuses a model digest mismatch, dimension mismatch or legacy vectors
  without provenance. Existing vectors are preserved and the API reports 409.
- Final verification: 161 web tests passed (2 opt-in skips), 83 Python tests
  passed (4 live tests deselected), one dedicated expanded live web test passed.
  The final live run shut down its temporary server and cleaned up the database.

[Captured web states and retrieval metadata](TestEvidence/CourseMaterials/2026-11-11/acceptance-clean/acceptance.json)

The dedicated live run exercises ASP.NET material access and Python retrieval
separately. A student-facing retrieval authorization path is not part of this
PR. Unrelated-query abstention and the entire earlier 21/18-case matrices have
not been signed off. Existing populated indexes require reindexing in a new
database, as documented in COURSE_MATERIALS.md.
