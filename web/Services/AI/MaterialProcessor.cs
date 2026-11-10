using System.Globalization;
using HomeworkPlatform.Web.Models.Domain;
namespace HomeworkPlatform.Web.Services.AI;
public record MaterialProcessingResult(bool Success,int ChunkCount,string? Error);
public interface IMaterialProcessor
{
 Task<MaterialProcessingResult> ProcessAsync(CourseMaterial material,Stream input);
}
public class MaterialProcessor(AIServiceClient client):IMaterialProcessor
{
 public async Task<MaterialProcessingResult> ProcessAsync(CourseMaterial material,Stream input)
 {
  using var content=new MultipartFormDataContent();
  content.Add(new StreamContent(input),"file",material.OriginalFileName);
  content.Add(new StringContent(material.Id.ToString(CultureInfo.InvariantCulture)),"material_id");
  content.Add(new StringContent(material.CourseId.ToString(CultureInfo.InvariantCulture)),"course_id");
  content.Add(new StringContent(material.Course.EducationLevel.ToString()),"education_level");
  content.Add(new StringContent(material.Course.GradeLevel.ToString(CultureInfo.InvariantCulture)),"grade_level");
  var result=await client.PostMultipartAsync<MaterialResponse>("documents/process",content);
  var d=result.Data;
  if(!result.Success)return new(false,0,result.Error?.Message??"İşleme tamamlanamadı.");
  bool valid=d!=null && d.MaterialId==material.Id && d.ChunkCount>0 && d.EmbeddingDimensions>0 && !string.IsNullOrWhiteSpace(d.EmbeddingModel)
   && d.Chunks!=null && d.Chunks.Count==d.ChunkCount && d.Chunks.All(m=>m.MaterialId==material.Id && m.CourseId==material.CourseId && m.EducationLevel==material.Course.EducationLevel.ToString() && m.GradeLevel==material.Course.GradeLevel && m.PageNumber>0 && !string.IsNullOrWhiteSpace(m.ChunkId))
   && d.Chunks.Select(m=>m.ChunkId).Distinct().Count()==d.ChunkCount;
  return valid?new(true,d!.ChunkCount,null):new(false,0,"AI servisinin materyal yanıtı doğrulanamadı.");
 }
 public record MaterialResponse(int MaterialId,int ChunkCount,string EmbeddingModel,int EmbeddingDimensions,List<Metadata>? Chunks);
 public record Metadata(int CourseId,int MaterialId,string EducationLevel,int GradeLevel,int PageNumber,string ChunkId);
}
