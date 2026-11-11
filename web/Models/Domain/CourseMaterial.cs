using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Models.Domain;
public enum MaterialStatus { Uploaded, Processing, Ready, Failed }
public class CourseMaterial
{
 public int Id {get;set;}
 public int CourseId {get;set;}
 public Course Course {get;set;}=null!;
 [MaxLength(200)] public string OriginalFileName {get;set;}="";
 [MaxLength(39)] public string StoredFileName {get;set;}="";
 public long FileSize {get;set;}
 public MaterialStatus Status {get;set;}
 public DateTime CreatedAt {get;set;}=DateTime.UtcNow;
 public DateTime? ProcessingStartedAt {get;set;}
 public int ChunkCount {get;set;}
 [MaxLength(300)] public string? ErrorMessage {get;set;}
}
