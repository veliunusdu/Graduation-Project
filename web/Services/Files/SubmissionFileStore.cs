using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Options;

namespace HomeworkPlatform.Web.Services.Files;

public class SubmissionFileStore
{
    public static readonly string[] AllowedExtensions = [".pdf", ".zip", ".txt", ".md", ".cs", ".py", ".java", ".js", ".ts", ".tsx", ".jsx",
        ".c", ".cpp", ".h", ".hpp", ".html", ".css", ".sql", ".json", ".xml", ".ipynb", ".razor", ".cshtml", ".sln", ".csproj", ".yml", ".yaml"];
    public string RootPath { get; }
    public long MaxBytes { get; }
    private readonly ILogger<SubmissionFileStore> logger;

    public SubmissionFileStore(IWebHostEnvironment environment, IOptions<SubmissionFileOptions> options, ILogger<SubmissionFileStore> logger)
    {
        this.logger = logger;
        RootPath = Path.GetFullPath(options.Value.RootPath, environment.ContentRootPath);
        var webRoot = Path.GetFullPath(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"));
        if (RootPath.Equals(webRoot, StringComparison.OrdinalIgnoreCase)
            || RootPath.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Submission files must be stored outside wwwroot.");
        MaxBytes = options.Value.MaxFileSizeMB * 1024L * 1024L;
    }

    public async Task<StoredSubmissionFile> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var originalName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(originalName) || originalName.Length > 200 || originalName.Any(char.IsControl))
            throw new SubmissionFileException("Geçerli ve en fazla 200 karakterli bir dosya adı kullanın.");
        if (!AllowedExtensions.Contains(extension))
            throw new SubmissionFileException("Bu dosya türü desteklenmiyor. PDF, ZIP veya desteklenen bir metin/kod dosyası seçin.");
        if (file.Length <= 0) throw new SubmissionFileException("Boş dosya yüklenemez.");
        if (file.Length > MaxBytes) throw new SubmissionFileException($"Dosya en fazla {MaxBytes / 1024 / 1024} MB olabilir.");

        Directory.CreateDirectory(RootPath);
        var storedName = Guid.NewGuid().ToString("N") + ".upload";
        var path = PathFor(storedName);
        try
        {
            long size = 0;
            await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            await using (var input = file.OpenReadStream())
            {
                var buffer = new byte[81920];
                int count;
                while ((count = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    size += count;
                    if (size > MaxBytes) throw new SubmissionFileException($"Dosya en fazla {MaxBytes / 1024 / 1024} MB olabilir.");
                    await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                }
            }
            if (size == 0) throw new SubmissionFileException("Boş dosya yüklenemez.");
            ValidateContents(path, extension);
            return new(storedName, originalName, size);
        }
        catch
        {
            DeleteIfPresent(storedName);
            throw;
        }
    }

    private static void ValidateContents(string path, string extension)
    {
        if (extension == ".pdf")
        {
            using var input = File.OpenRead(path);
            Span<byte> signature = stackalloc byte[5];
            if (input.Read(signature) != 5 || !signature.SequenceEqual("%PDF-"u8))
                throw new SubmissionFileException("Dosyanın içeriği PDF biçiminde değil.");
        }
        else if (extension == ".zip")
        {
            try
            {
                using var archive = ZipFile.OpenRead(path);
                if (archive.Entries.Count is < 1 or > 2000 || archive.Entries.Sum(e => (decimal)e.Length) > 200 * 1024 * 1024)
                    throw new SubmissionFileException("ZIP boş olmamalı; en fazla 2000 kayıt ve açılmış halde 200 MB içermelidir.");
                foreach (var entry in archive.Entries)
                {
                    var name = entry.FullName.Replace('\\', '/');
                    if (name.StartsWith('/') || name.Contains(':') || name.Split('/').Contains("..")
                        || ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000)
                        throw new SubmissionFileException("ZIP içinde güvensiz dosya yolu veya bağlantı bulunuyor.");
                }
            }
            catch (InvalidDataException) { throw new SubmissionFileException("Geçerli bir ZIP dosyası seçin."); }
        }
        else
        {
            // Code is stored as text, never compiled or executed. Reject binary content disguised as code.
            try
            {
                using var reader = new StreamReader(path, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: false);
                var buffer = new char[8192];
                int count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                    for (var i = 0; i < count; i++)
                        if (char.IsControl(buffer[i]) && buffer[i] is not '\t' and not '\r' and not '\n')
                            throw new SubmissionFileException("Kod dosyası UTF-8 metin biçiminde olmalıdır.");
            }
            catch (DecoderFallbackException) { throw new SubmissionFileException("Kod dosyasını UTF-8 metin olarak kaydedip tekrar yükleyin."); }
        }
    }

    private string PathFor(string name)
    {
        if (!name.EndsWith(".upload", StringComparison.Ordinal) || name.Length != 39
            || !Guid.TryParseExact(name[..32], "N", out _))
            throw new ArgumentException("Invalid stored file identifier.", nameof(name));
        return Path.Combine(RootPath, name);
    }

    public Stream? OpenRead(string storedName)
    {
        try { return new FileStream(PathFor(storedName), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    public void DeleteIfPresent(string? storedName)
    {
        if (storedName == null) return;
        try { File.Delete(PathFor(storedName)); }
        catch (IOException exception) { logger.LogWarning(exception, "An unreferenced submission file could not be removed."); }
        catch (UnauthorizedAccessException exception) { logger.LogWarning(exception, "An unreferenced submission file could not be removed."); }
    }
}
