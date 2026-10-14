from typing import Literal

from pydantic import BaseModel, ConfigDict, StrictInt, model_validator


class AgentContext(BaseModel):
    model_config = ConfigDict(extra="forbid")

    course_id: StrictInt
    education_level: Literal["HighSchool", "University"]
    grade_level: StrictInt

    @model_validator(mode="after")
    def validate_grade_level(self):
        if self.education_level == "HighSchool":
            if not 9 <= self.grade_level <= 12:
                raise ValueError(
                    "HighSchool grade_level must be between 9 and 12."
                )
        elif not 1 <= self.grade_level <= 4:
            raise ValueError(
                "University grade_level must be between 1 and 4."
            )

        return self
