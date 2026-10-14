import pytest
from pydantic import ValidationError

from schemas.context import AgentContext


@pytest.mark.parametrize(
    ("education_level", "grade_level"),
    [
        ("HighSchool", 9),
        ("HighSchool", 12),
        ("University", 1),
        ("University", 4),
    ],
)
def test_valid_agent_context(education_level, grade_level):
    context = AgentContext(
        course_id=1,
        education_level=education_level,
        grade_level=grade_level,
    )
    assert context.course_id == 1


@pytest.mark.parametrize(
    ("education_level", "grade_level"),
    [
        ("HighSchool", 8),
        ("HighSchool", 13),
        ("University", 0),
        ("University", 5),
    ],
)
def test_invalid_grade_level(education_level, grade_level):
    with pytest.raises(ValidationError):
        AgentContext(
            course_id=1,
            education_level=education_level,
            grade_level=grade_level,
        )


def test_invalid_education_level():
    with pytest.raises(ValidationError):
        AgentContext(
            course_id=1,
            education_level="PrimarySchool",
            grade_level=5,
        )


def test_missing_context_field():
    with pytest.raises(ValidationError):
        AgentContext(
            course_id=1,
            education_level="University",
        )


