namespace HomeworkPlatform.Web.Models.Domain;

public static class EducationRules
{
    public static int[] GradesFor(EducationLevel level) => level switch
    {
        EducationLevel.University => new[] { 1, 2, 3, 4 },
        EducationLevel.HighSchool => new[] { 9, 10, 11, 12 },
        _ => Array.Empty<int>()
    };
    public static bool IsValidGrade(EducationLevel level, int grade) => GradesFor(level).Contains(grade);
    public static bool IsValidClass(EducationLevel level, string? studentClass) =>
        GradesFor(level).Any(grade => studentClass == $"{level}{grade}");
    public static string Label(EducationLevel level) => level == EducationLevel.University ? "University" : "High School";
}
