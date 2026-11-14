using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Models.Domain;
public class AssignmentAttachment
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    [MaxLength(39)] public string StoredFileName { get; set; } = "";
    [MaxLength(200)] public string OriginalFileName { get; set; } = "";
    public long FileSize { get; set; }
}
