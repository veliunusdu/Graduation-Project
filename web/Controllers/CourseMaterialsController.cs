using System.Security.Claims;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Security;
using HomeworkPlatform.Web.Services.AI;
using HomeworkPlatform.Web.Services.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Controllers;
[Authorize(Roles=AppRoles.Teacher+","+AppRoles.Student)]
public class CourseMaterialsController(ApplicationDbContext db,MaterialFileStore files,IMaterialProcessor processor,ILogger<CourseMaterialsController> logger):Controller
{
 string UserId=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
 bool Teacher=>User.IsInRole(AppRoles.Teacher);
 IQueryable<Course> Accessible=>db.Courses.Where(c=>Teacher ? c.TeacherId==UserId : c.IsActive && db.StudentCourses.Any(e=>e.CourseId==c.Id && e.StudentId==UserId));
 [HttpGet] public async Task<IActionResult> Index(int courseId)
 {
  var course=await Accessible.AsNoTracking().SingleOrDefaultAsync(c=>c.Id==courseId);if(course==null)return NotFound();
  ViewData["Course"]=course;ViewData["Teacher"]=Teacher;
  return View(await db.CourseMaterials.AsNoTracking().Where(m=>m.CourseId==courseId).OrderByDescending(m=>m.Id).ToListAsync());
 }
 [HttpPost,Authorize(Roles=AppRoles.Teacher),RequestSizeLimit(SubmissionFileOptions.RequestSizeLimit),RequestFormLimits(MultipartBodyLengthLimit=SubmissionFileOptions.RequestSizeLimit)]
 public async Task<IActionResult> Upload(int courseId,IFormFile? upload)
 {
  var course=await Accessible.SingleOrDefaultAsync(c=>c.Id==courseId);if(course==null)return NotFound();if(!course.IsActive)return Conflict("Önce dersi etkinleştirin.");
  if(upload==null){TempData["MaterialError"]="Dosya seçin.";return RedirectToAction(nameof(Index),new{courseId});}
  StoredSubmissionFile saved;
  try{saved=await files.SaveAsync(upload,HttpContext.RequestAborted);}
  catch(SubmissionFileException ex){TempData["MaterialError"]=ex.Message;return RedirectToAction(nameof(Index),new{courseId});}
  var m=new CourseMaterial{CourseId=courseId,Course=course,OriginalFileName=saved.OriginalName,StoredFileName=saved.StoredName,FileSize=saved.Size};
  db.CourseMaterials.Add(m);
  try{await db.SaveChangesAsync();}catch{files.DeleteIfPresent(saved.StoredName);throw;}
  await Process(m);
  var status=await db.CourseMaterials.AsNoTracking().Where(x=>x.Id==m.Id).Select(x=>x.Status).SingleAsync();
  TempData["MaterialNotice"]=status==MaterialStatus.Ready
   ? "Material uploaded and AI processing completed."
   : "Material uploaded. AI processing did not complete; the file remains available to your students.";
  return RedirectToAction(nameof(Index),new{courseId});
 }
 [HttpPost,Authorize(Roles=AppRoles.Teacher)] public async Task<IActionResult> Retry(int id)
 {
  var m=await db.CourseMaterials.Include(m=>m.Course).SingleOrDefaultAsync(m=>m.Id==id && m.Course.TeacherId==UserId);
  if(m==null)return NotFound();if(!m.Course.IsActive)return Conflict("Önce dersi etkinleştirin.");
  if(!await Process(m))return Conflict("Materyal işleniyor veya yeniden işleme gerekmiyor.");
  return RedirectToAction(nameof(Index),new{courseId=m.CourseId});
 }
 async Task<bool> Process(CourseMaterial m)
 {
  var now=DateTime.UtcNow;var cutoff=now.AddMinutes(-5);
  var claimed=await db.CourseMaterials.Where(x=>x.Id==m.Id && (x.Status==MaterialStatus.Uploaded || x.Status==MaterialStatus.Failed || x.Status==MaterialStatus.Processing && x.ProcessingStartedAt<cutoff))
   .ExecuteUpdateAsync(set=>set.SetProperty(x=>x.Status,MaterialStatus.Processing).SetProperty(x=>x.ProcessingStartedAt,now).SetProperty(x=>x.ErrorMessage,(string?)null));
  if(claimed==0)return false;
  MaterialProcessingResult result;
  try{
   using var input=files.OpenRead(m.StoredFileName);
   result=input==null?new(false,0,"Saklanan dosya bulunamadı."):await processor.ProcessAsync(m,input);
  }catch(Exception ex){logger.LogWarning(ex,"Material processing failed for {MaterialId}.",m.Id);result=new(false,0,"Materyal işlenemedi. Yeniden deneyin.");}
  // A stale request must never overwrite a later retry's result.
  await db.CourseMaterials.Where(x=>x.Id==m.Id && x.Status==MaterialStatus.Processing && x.ProcessingStartedAt==now)
   .ExecuteUpdateAsync(set=>set.SetProperty(x=>x.Status,result.Success?MaterialStatus.Ready:MaterialStatus.Failed).SetProperty(x=>x.ChunkCount,result.Success?result.ChunkCount:0).SetProperty(x=>x.ErrorMessage,result.Error));
  return true;
 }
 [HttpGet] public async Task<IActionResult> Download(int id)
 {
  var m=await db.CourseMaterials.AsNoTracking().SingleOrDefaultAsync(m=>m.Id==id && Accessible.Any(c=>c.Id==m.CourseId));
  if(m==null)return NotFound();var stream=files.OpenRead(m.StoredFileName);if(stream==null)return NotFound();
  Response.Headers["X-Content-Type-Options"]="nosniff";Response.Headers.CacheControl="no-store";
  return File(stream,"application/octet-stream",m.OriginalFileName);
 }
}
