from pydantic import BaseModel, ConfigDict, Field, StrictInt, StrictStr


class RubricCriterion(BaseModel):
    model_config = ConfigDict(extra="forbid")

    criterion: StrictStr
    description: StrictStr
    points: StrictInt = Field(ge=0)


class AssignmentOutput(BaseModel):
    model_config = ConfigDict(extra="forbid")

    title: StrictStr
    description: StrictStr
    learning_objectives: list[StrictStr] = Field(
        alias="learningObjectives",
        min_length=1,
    )
    requirements: list[StrictStr] = Field(min_length=1)
    rubric: list[RubricCriterion] = Field(min_length=1)
    estimated_difficulty: StrictStr = Field(
        alias="estimatedDifficulty"
    )
