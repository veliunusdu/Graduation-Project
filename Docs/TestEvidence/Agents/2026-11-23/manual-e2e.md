# Course-material agents: manual end-to-end verification

Date: 2026-11-23
Branch: `development_ayse`

## Setup

- Web application: `http://localhost:5094`
- AI service: `http://127.0.0.1:8000`
- Course: Introduction to Programming
- Teacher: `teacher@edupilot.local`
- Student: `student1@edupilot.local`

The teacher uploaded these sample files from `Docs/SampleCourseMaterials`:

- `Programming_Variables_Conditions.txt`
- `Programming_Loops_Functions.txt`

Both materials reached the `Ready` processing state.

## Assignment draft

The teacher requested an assignment about processing student grades with Python lists, a `for` loop, conditions, functions, empty-list handling, and boundary-value tests.

The RAG assignment agent created:

- Title: `Python'da Öğrenci Notları İşleme Programı`
- Due date set by the teacher: `2026-11-30`
- Local database assignment ID during this run: `3`

The teacher saved the generated draft successfully.

## Student visibility

The student dashboard showed the new assignment under assignments to complete. The Introduction to Programming course page showed:

- The generated assignment
- `Programming_Basics.txt`
- `Programming_Loops_Functions.txt`
- `Programming_Variables_Conditions.txt`

The student could open the assignment details and see its requirements and due date.

## AI Tutor

The student asked for guidance on planning `calculate_average` and `count_passing`, using a loop, and handling an empty list without receiving a complete solution.

The AI Tutor:

- Returned step-by-step guidance
- Did not write the complete homework
- Cited `Programming_Loops_Functions.txt`, page 1

Result: material upload and processing, RAG assignment generation, assignment persistence, student visibility, material visibility, and grounded AI Tutor help all completed successfully.

The SQLite database and runtime uploads remain intentionally excluded from Git because they contain local Identity data and user-uploaded content.
