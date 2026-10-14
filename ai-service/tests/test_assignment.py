import pytest
from pydantic import ValidationError

from schemas.assignment import AssignmentOutput


def test_assignment_output_accepts_expected_json():
    payload = {
        "title": "Python Basics",
        "description": "Write a small Python program.",
        "learningObjectives": [
            "Understand variables",
            "Practice conditional statements",
        ],
        "requirements": [
            "Use at least one variable",
            "Include a conditional statement",
        ],
        "rubric": [
            {
                "criterion": "Correctness",
                "description": "The program produces the expected output.",
                "points": 10,
            }
        ],
        "estimatedDifficulty": "Beginner",
    }

    assignment = AssignmentOutput.model_validate(payload)

    assert assignment.title == "Python Basics"
    assert len(assignment.rubric) == 1
    assert assignment.model_dump(by_alias=True) == payload


def test_assignment_output_rejects_missing_fields():
    with pytest.raises(ValidationError):
        AssignmentOutput.model_validate(
            {"title": "Incomplete assignment"}
        )
