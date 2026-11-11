using System.Net;
using HomeworkPlatform.Web.Services.AI;
using HomeworkPlatform.Web.Models.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
namespace HomeworkPlatform.Web.Tests;
public class MaterialProcessorTests
{
 class Handler(string payload,HttpStatusCode status):HttpMessageHandler
 {
  public string Body="";
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
  {Assert.EndsWith("/documents/process",request.RequestUri!.ToString());Body=await request.Content!.ReadAsStringAsync(ct);return new(status){Content=new StringContent(payload)};}
 }
 [Theory][InlineData(7,true)][InlineData(8,false)]
 public async Task MultipartFieldsAndMetadataIdentityAreVerified(int responseCourse,bool expected)
 {
  var json="""
  {"material_id":41,"chunk_count":1,"embedding_model":"bge-m3","embedding_dimensions":1024,"chunks":[{"course_id":COURSE,"material_id":41,"education_level":"HighSchool","grade_level":9,"page_number":1,"chunk_id":"41:1"}]}
  """.Replace("COURSE",responseCourse.ToString());
  var handler=new Handler(json,HttpStatusCode.OK);using var http=new HttpClient(handler){BaseAddress=new Uri("http://127.0.0.1:8000/")};
  var client=new AIServiceClient(http,Options.Create(new AIServiceOptions()),NullLogger<AIServiceClient>.Instance);
  var processor=new MaterialProcessor(client);using var input=new MemoryStream("notes"u8.ToArray());
  var result=await processor.ProcessAsync(new(){Id=41,CourseId=7,OriginalFileName="notes.txt",Course=new(){EducationLevel=EducationLevel.HighSchool,GradeLevel=9}},input);
  Assert.Equal(expected,result.Success);Assert.Contains("material_id",handler.Body);Assert.Contains("course_id",handler.Body);Assert.Contains("HighSchool",handler.Body);Assert.Contains("notes.txt",handler.Body);
 }
 [Theory][InlineData("not json",200)][InlineData("{}",200)][InlineData("private upstream error",502)]
 public async Task BadResponsesNeverMarkReady(string payload,int status)
 {
  using var http=new HttpClient(new Handler(payload,(HttpStatusCode)status)){BaseAddress=new Uri("http://127.0.0.1:8000/")};
  var client=new AIServiceClient(http,Options.Create(new AIServiceOptions()),NullLogger<AIServiceClient>.Instance);using var input=new MemoryStream("a"u8.ToArray());
  var result=await new MaterialProcessor(client).ProcessAsync(new(){Id=41,CourseId=7,OriginalFileName="a.txt",Course=new()},input);
  Assert.False(result.Success);Assert.DoesNotContain("private",result.Error??"");
 }
}
