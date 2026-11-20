using System.Security.Claims;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Models;
using HomeworkPlatform.Web.Models.Work;
using HomeworkPlatform.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class CoursesController(ApplicationDbContext database, UserManager<ApplicationUser> users) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IQueryable<Course> Owned => database.Courses.Where(c => c.TeacherId == UserId);

    [HttpGet] public async Task<IActionResult> Index()
    {
        ViewData["EnrollmentCounts"] = await database.StudentCourses.Where(e => e.Course.TeacherId == UserId)
            .GroupBy(e => e.CourseId).ToDictionaryAsync(g => g.Key, g => g.Count());
        return View(await Owned.AsNoTracking().OrderBy(c => c.Name).ToListAsync());
    }
    [HttpGet] public async Task<IActionResult> Details([FromRoute] int id)
    {
        var course = await Owned.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        return course == null ? NotFound() : await DetailsView(course);
    }
    [HttpGet] public async Task<IActionResult> Create()
    {
        var teacher = await users.GetUserAsync(User);
        if (teacher == null) return Challenge();
        await LoadStudentChoices(teacher.EducationLevel);
        return View(new CourseForm { EducationLevel = teacher.EducationLevel,
            GradeLevel = EducationRules.GradesFor(teacher.EducationLevel).First() });
    }
    [HttpPost] public async Task<IActionResult> Create(CourseForm model)
    {
        var teacher = await users.GetUserAsync(User);
        if (teacher == null) return Challenge();
        ApplyTeacherEducation(model, teacher);
        var students = await LoadStudentChoices(teacher.EducationLevel);
        var selected = model.StudentIds.Distinct().ToArray();
        if (selected.Except(students.Select(s => s.Id)).Any())
            ModelState.AddModelError(nameof(model.StudentIds), "Choose existing students from the list.");
        if (!model.IsActive && selected.Length > 0)
            ModelState.AddModelError(nameof(model.StudentIds), "Activate the course before enrolling students.");
        if (!ModelState.IsValid) return View(model);
        var course = new Course { Name = model.Name.Trim(), Description = model.Description ?? "", TeacherId = UserId, EducationLevel = model.EducationLevel, GradeLevel = model.GradeLevel, IsActive = model.IsActive };
        database.Courses.Add(course);
        foreach (var studentId in selected)
            database.StudentCourses.Add(new StudentCourse { Course = course, StudentId = studentId });
        await database.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = course.Id });
    }
    [HttpGet] public async Task<IActionResult> Edit([FromRoute] int id)
    {
        var course = await Owned.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound();
        var teacher = await users.GetUserAsync(User);
        if (teacher == null) return Challenge();
        if (course.EducationLevel != teacher.EducationLevel) return Forbid();
        return View(new CourseForm { Name = course.Name, Description = course.Description, EducationLevel = teacher.EducationLevel, GradeLevel = course.GradeLevel, IsActive = course.IsActive });
    }
    [HttpPost] public async Task<IActionResult> Edit([FromRoute] int id, CourseForm model)
    {
        var course = await Owned.SingleOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound();
        var teacher = await users.GetUserAsync(User);
        if (teacher == null) return Challenge();
        if (course.EducationLevel != teacher.EducationLevel) return Forbid();
        ApplyTeacherEducation(model, teacher);
        if (!ModelState.IsValid) return View(model);
        if ((course.EducationLevel != model.EducationLevel || course.GradeLevel != model.GradeLevel)
            && await database.CourseMaterials.AnyAsync(m=>m.CourseId==id))
        {
            ModelState.AddModelError("", "The year or grade of a course with materials cannot be changed; its source metadata must remain consistent.");
            return View(model);
        }
        course.Name = model.Name.Trim(); course.Description = model.Description ?? "";
        course.EducationLevel = model.EducationLevel; course.GradeLevel = model.GradeLevel; course.IsActive = model.IsActive;
        await database.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpGet] public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var course = await Owned.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        return course == null ? NotFound() : View(course);
    }
    [HttpPost, ActionName("Delete")] public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
    {
        var course = await Owned.SingleOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound();
        if (await database.Assignments.AnyAsync(a => a.CourseId == id)) return Conflict("Remove empty assignments before deleting this course. Student work will not be deleted.");
        database.Courses.Remove(course);
        try { await database.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict("This course has dependent records and cannot be deleted."); }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> Enroll([FromRoute] int id, EnrollmentForm model)
    {
        var course = await Owned.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound();
        var teacher = await users.GetUserAsync(User);
        if (teacher == null) return Challenge();
        if (course.EducationLevel != teacher.EducationLevel) return Forbid();
        if (!course.IsActive) return Conflict("Activate the course before enrolling students.");
        if (!ModelState.IsValid) return await DetailsView(course);
        var student = await users.FindByEmailAsync(model.Email.Trim());
        if (student == null || !await users.IsInRoleAsync(student, AppRoles.Student) || student.EducationLevel != course.EducationLevel)
        {
            ModelState.AddModelError("", "Choose a registered student at the same education level as this course.");
            return await DetailsView(course);
        }
        if (await database.StudentCourses.AnyAsync(e => e.CourseId == id && e.StudentId == student.Id))
        {
            TempData["CourseNotice"] = "This student is already enrolled in this course.";
            return RedirectToAction(nameof(Details), new { id });
        }
        database.StudentCourses.Add(new StudentCourse { CourseId = id, StudentId = student.Id });
        try { await database.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict("Enrollment could not be saved. Reload the course and try again."); }
        TempData["CourseNotice"] = "Student enrolled successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }
    private void ApplyTeacherEducation(CourseForm model, ApplicationUser teacher)
    {
        model.EducationLevel = teacher.EducationLevel;
        // Old clients may still post this field. It never overrides the stored account level.
        if (Request.HasFormContentType && Request.Form.TryGetValue(nameof(model.EducationLevel), out var posted)
            && (posted.Count != 1 || !Enum.TryParse<EducationLevel>(posted[0], true, out var level) || level != teacher.EducationLevel))
            ModelState.AddModelError(nameof(model.EducationLevel), "Education level is determined by your teacher account.");
        if (!EducationRules.IsValidGrade(teacher.EducationLevel, model.GradeLevel))
            ModelState.AddModelError(nameof(model.GradeLevel), teacher.EducationLevel == EducationLevel.University
                ? "Choose a university year from 1 to 4." : "Choose a high school grade from 9 to 12.");
    }
    private async Task<IList<ApplicationUser>> LoadStudentChoices(EducationLevel? level = null)
    {
        level ??= (await users.GetUserAsync(User))!.EducationLevel;
        var students = (await users.GetUsersInRoleAsync(AppRoles.Student)).Where(s => s.EducationLevel == level).ToList();
        ViewData["AvailableStudents"] = students.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ThenBy(s => s.Email).ToList();
        return students;
    }
    private async Task<IActionResult> DetailsView(Course course)
    {
        var enrolled = await (from enrollment in database.StudentCourses.AsNoTracking()
            join student in database.Users.AsNoTracking() on enrollment.StudentId equals student.Id
            where enrollment.CourseId == course.Id
            orderby student.LastName, student.FirstName, student.Email
            select student).ToListAsync();
        var enrolledIds = enrolled.Select(s => s.Id).ToHashSet();
        var available = (await users.GetUsersInRoleAsync(AppRoles.Student))
            .Where(s => s.EducationLevel == course.EducationLevel && !enrolledIds.Contains(s.Id))
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ThenBy(s => s.Email).ToList();
        static CourseStudentViewModel Student(ApplicationUser user) => new(user.Id, user.FirstName ?? "", user.LastName ?? "", user.Email ?? "", user.StudentClass);
        var assignmentCount = await database.Assignments.CountAsync(a => a.CourseId == course.Id);
        var materialCount = await database.CourseMaterials.CountAsync(m => m.CourseId == course.Id);
        return View("Details", new CourseRosterViewModel(course, enrolled.Select(Student).ToArray(), available.Select(Student).ToArray(), assignmentCount, materialCount));
    }
}
