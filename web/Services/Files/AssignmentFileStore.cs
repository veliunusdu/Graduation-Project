using Microsoft.Extensions.Options;
namespace HomeworkPlatform.Web.Services.Files;
public class AssignmentFileOptions : SubmissionFileOptions
{
    public new const string SectionName = "AssignmentFiles";
    public AssignmentFileOptions() { RootPath = "App_Data/AssignmentFiles"; }
}
// Share document validation with course materials, using a separate storage directory.
public class AssignmentFileStore
{
    private readonly MaterialFileStore store;
    public AssignmentFileStore(IWebHostEnvironment environment, IOptions<AssignmentFileOptions> options, ILogger<MaterialFileStore> logger)
    {
        store = new MaterialFileStore(environment, Options.Create(new MaterialFileOptions {
            RootPath = options.Value.RootPath, MaxFileSizeMB = options.Value.MaxFileSizeMB }), logger);
    }
    public Task<StoredSubmissionFile> SaveAsync(IFormFile file, CancellationToken token) => store.SaveAsync(file, token);
    public Stream? OpenRead(string name) => store.OpenRead(name);
    public void DeleteIfPresent(string? name) => store.DeleteIfPresent(name);
}
