using System.Net;
using System.Text.RegularExpressions;
using HomeworkPlatform.Web.Tests.Infrastructure;
using HomeworkPlatform.Web.Services.AI;
using HomeworkPlatform.Web.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace HomeworkPlatform.Web.Tests;
public class CourseMaterialTests
{
 class Processor:IMaterialProcessor
 {
  public bool Fail; public int Calls;
  public Task<MaterialProcessingResult> ProcessAsync(CourseMaterial m,Stream input){Calls++;return Task.FromResult(Fail?new MaterialProcessingResult(false,0,"Servis kapalı"):new(true,2,null));}
 }
 static async Task<HttpResponseMessage> Upload(HttpClient c,int courseId,string name="lesson.txt",byte[]? data=null,bool token=true)
 {
  using var form=new MultipartFormDataContent();
  if(token){var html=await c.GetStringAsync($"/CourseMaterials/Index?courseId={courseId}");var t=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");Assert.True(t.Success);form.Add(new StringContent(WebUtility.HtmlDecode(t.Groups[1].Value)),"__RequestVerificationToken");}
  form.Add(new ByteArrayContent(data??"Türkçe ders notu"u8.ToArray()),"upload",name);
  return await c.PostAsync($"/CourseMaterials/Upload?courseId={courseId}",form);
 }
 [Fact] public async Task OwnerUploadReadyStudentAccessAndOwnership()
 {
  var p=new Processor();using var f=await OwnershipFixture.CreateAsync(a=>a.ConfigureServices=s=>s.AddSingleton<IMaterialProcessor>(p));using var c=await f.TeacherAsync();
  Assert.Equal(HttpStatusCode.OK,(await c.GetAsync($"/CourseMaterials/Index?courseId={f.CourseAId}")).StatusCode);
  Assert.Equal(HttpStatusCode.Redirect,(await Upload(c,f.CourseAId)).StatusCode);
  var m=await f.ReadAsync(d=>d.CourseMaterials.SingleAsync());Assert.Equal(MaterialStatus.Ready,m.Status);Assert.Equal(2,m.ChunkCount);Assert.Equal(1,p.Calls);
  Assert.Equal("Türkçe ders notu",await c.GetStringAsync($"/CourseMaterials/Download/{m.Id}"));
  using var other=await f.TeacherAsync(true);Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync($"/CourseMaterials/Download/{m.Id}")).StatusCode);
  Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync($"/CourseMaterials/Index?courseId={f.CourseAId}")).StatusCode);
  using var student=await f.StudentAsync();Assert.Equal(HttpStatusCode.OK,(await student.GetAsync($"/CourseMaterials/Download/{m.Id}")).StatusCode);
  Assert.Equal(HttpStatusCode.NotFound,(await student.GetAsync($"/CourseMaterials/Index?courseId={f.CourseBId}")).StatusCode);
  Assert.Equal(HttpStatusCode.Conflict,(await FormClient.PostAsync(c,$"/CourseMaterials/Retry/{m.Id}",new(),$"/CourseMaterials/Index?courseId={f.CourseAId}")).StatusCode);
 }
 [Fact] public async Task FailedProcessingCanRetryAndStudentCannotSeeFailed()
 {
  var p=new Processor{Fail=true};using var f=await OwnershipFixture.CreateAsync(a=>a.ConfigureServices=s=>s.AddSingleton<IMaterialProcessor>(p));using var c=await f.TeacherAsync();
  await Upload(c,f.CourseAId);var m=await f.ReadAsync(d=>d.CourseMaterials.SingleAsync());Assert.Equal(MaterialStatus.Failed,m.Status);
  using var student=await f.StudentAsync();Assert.Equal(HttpStatusCode.NotFound,(await student.GetAsync($"/CourseMaterials/Download/{m.Id}")).StatusCode);
  p.Fail=false;var url=$"/CourseMaterials/Retry/{m.Id}";
  Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(c,url,new(),$"/CourseMaterials/Index?courseId={f.CourseAId}")).StatusCode);
  m=await f.ReadAsync(d=>d.CourseMaterials.SingleAsync());Assert.Equal(MaterialStatus.Ready,m.Status);Assert.Null(m.ErrorMessage);Assert.Equal(2,p.Calls);
 }
 [Theory][InlineData("bad.exe","MZ")][InlineData("bad.pdf","invalid")][InlineData("bad.docx","invalid")][InlineData("bad.pptx","invalid")][InlineData("empty.txt","")]
 public async Task InvalidFileAndMissingAntiforgeryCannotPersist(string name,string body)
 {
  var p=new Processor();using var f=await OwnershipFixture.CreateAsync(a=>a.ConfigureServices=s=>s.AddSingleton<IMaterialProcessor>(p));using var c=await f.TeacherAsync();
  await Upload(c,f.CourseAId,name,System.Text.Encoding.UTF8.GetBytes(body));Assert.False(await f.ReadAsync(d=>d.CourseMaterials.AnyAsync()));Assert.Equal(0,p.Calls);
  Assert.Equal(HttpStatusCode.BadRequest,(await Upload(c,f.CourseAId,token:false)).StatusCode);
 }

 [Fact] public async Task InactiveCourseRejectsUploadAndStudentPostIsForbidden()
 {
  var p=new Processor();using var f=await OwnershipFixture.CreateAsync(a=>a.ConfigureServices=s=>s.AddSingleton<IMaterialProcessor>(p));using var c=await f.TeacherAsync();
  await f.ReadAsync(async db=>{await db.Courses.Where(x=>x.Id==f.CourseAId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.IsActive,false));return true;});
  Assert.Equal(HttpStatusCode.Conflict,(await Upload(c,f.CourseAId)).StatusCode);Assert.Equal(0,p.Calls);
  using var student=await f.StudentAsync();Assert.Equal(HttpStatusCode.Redirect,(await student.PostAsync($"/CourseMaterials/Upload?courseId={f.CourseAId}",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
 }
 [Fact] public async Task OwnerCannotUploadOrRetryIntoOtherTeachersCourse()
 {
  var p=new Processor();using var f=await OwnershipFixture.CreateAsync(a=>a.ConfigureServices=s=>s.AddSingleton<IMaterialProcessor>(p));using var c=await f.TeacherAsync();
  var html=await c.GetStringAsync($"/CourseMaterials/Index?courseId={f.CourseAId}");var token=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
  using var form=new MultipartFormDataContent();form.Add(new StringContent(WebUtility.HtmlDecode(token)),"__RequestVerificationToken");form.Add(new ByteArrayContent("abc"u8.ToArray()),"upload","file.txt");
  Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsync($"/CourseMaterials/Upload?courseId={f.CourseBId}",form)).StatusCode);Assert.Equal(0,p.Calls);
 }
 [Fact] public async Task ProcessingLeaseBlocksDuplicateRetryAndExpiredLeaseRecovers()
 {
  var p=new Processor{Fail=true};using var f=await OwnershipFixture.CreateAsync(a=>a.ConfigureServices=s=>s.AddSingleton<IMaterialProcessor>(p));using var c=await f.TeacherAsync();
  await Upload(c,f.CourseAId);var m=await f.ReadAsync(db=>db.CourseMaterials.SingleAsync());
  await f.ReadAsync(async db=>{await db.CourseMaterials.Where(x=>x.Id==m.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,MaterialStatus.Processing).SetProperty(x=>x.ProcessingStartedAt,DateTime.UtcNow));return true;});
  var url=$"/CourseMaterials/Retry/{m.Id}";var tokenPage=$"/CourseMaterials/Index?courseId={f.CourseAId}";
  Assert.Equal(HttpStatusCode.Conflict,(await FormClient.PostAsync(c,url,new(),tokenPage)).StatusCode);Assert.Equal(1,p.Calls);
  await f.ReadAsync(async db=>{await db.CourseMaterials.Where(x=>x.Id==m.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.ProcessingStartedAt,DateTime.UtcNow.AddMinutes(-6)));return true;});p.Fail=false;
  Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(c,url,new(),tokenPage)).StatusCode);Assert.Equal(2,p.Calls);
  Assert.Equal(MaterialStatus.Ready,await f.ReadAsync(db=>db.CourseMaterials.Select(x=>x.Status).SingleAsync()));
 }
 [Fact] public async Task IndexedCourseCannotChangeEducationMetadata()
 {
  var p=new Processor();using var f=await OwnershipFixture.CreateAsync(a=>a.ConfigureServices=s=>s.AddSingleton<IMaterialProcessor>(p));using var c=await f.TeacherAsync();await Upload(c,f.CourseAId);
  var r=await FormClient.PostAsync(c,$"/Courses/Edit/{f.CourseAId}",new(){["Name"]="Changed",["EducationLevel"]="University",["GradeLevel"]="1",["IsActive"]="true"});
  Assert.Equal(HttpStatusCode.OK,r.StatusCode);Assert.Contains("validation-summary-errors",await r.Content.ReadAsStringAsync());
  var course=await f.ReadAsync(db=>db.Courses.SingleAsync(x=>x.Id==f.CourseAId));Assert.Equal(EducationLevel.HighSchool,course.EducationLevel);Assert.Equal(9,course.GradeLevel);Assert.NotEqual("Changed",course.Name);
 }
 [LocalMaterialFact] public async Task ActualWebUploadReachesRealPythonAndBge()
 {
  var url=Environment.GetEnvironmentVariable("VELI_MATERIAL_PYTHON_URL")!;Assert.True(new Uri(url).IsLoopback);
  using var f=await OwnershipFixture.CreateAsync(a=>a.ConfigureServices=s=>s.Configure<AIServiceOptions>(o=>{o.BaseUrl=url;o.TimeoutSeconds=120;}));using var c=await f.TeacherAsync();
  Assert.Equal(HttpStatusCode.Redirect,(await Upload(c,f.CourseAId)).StatusCode);
  var m=await f.ReadAsync(db=>db.CourseMaterials.SingleAsync());Assert.Equal(MaterialStatus.Ready,m.Status);Assert.True(m.ChunkCount>0);Assert.Null(m.ErrorMessage);
 }
}
public sealed class LocalMaterialFactAttribute:FactAttribute
{
 public LocalMaterialFactAttribute(){if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VELI_MATERIAL_PYTHON_URL")))Skip="Requires an isolated running Python/BGE service; opt in with VELI_MATERIAL_PYTHON_URL.";}
}
