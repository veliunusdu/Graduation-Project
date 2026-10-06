# First sample assignment

Implement AssignmentRepository.Create with an injected DbConnection. The caller supplies an open connection and current UTC time.

1. Reject null, empty, or whitespace-only titles before any SQL execution (30%).
2. Reject deadlines less than or equal to the supplied current time before any SQL execution (30%).
3. Insert title and deadline using database parameters (40%).

Do not embed credentials or log submitted titles. Let failures propagate to the caller; this small assignment does not require controllers, EF Core, a local catch, or connection lifecycle management. The first agent evaluates only the three numbered requirements; Security and Code Review can later assess other concrete issues.

Samples are in samples/submission-analysis. Each includes a static source snippet, request.json, and expected.json. These are reference assets for future agent evaluation, not implemented analysis agents or runnable applications. C# is an initial fixture choice based on the platform stack; other submission languages remain undecided.

Next milestone: implement the Requirement Agent to accept request.json and emit the response defined in ANALYSIS_CONTRACT.md, then compare real model output with these expected results.
