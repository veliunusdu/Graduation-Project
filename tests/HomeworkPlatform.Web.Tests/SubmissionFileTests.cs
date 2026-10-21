using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HomeworkPlatform.Web.Services.Files;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HomeworkPlatform.Web.Tests;

public class SubmissionFileTests
{
    private static string Storage(OwnershipFixture fixture) => Path.Combine(fixture.App.DirectoryPath, "SubmissionFiles");
    private static int FileCount(OwnershipFixture fixture) => Directory.Exists(Storage(fixture)) ? Directory.GetFiles(Storage(fixture)).Length : 0;
    private static async Task<HttpResponseMessage> Upload(HttpClient client, string url, string? fileName, byte[]? bytes,
        Dictionary<string, string>? fields = null, string? tokenPage = null, bool withToken = true)
    {
        using var form = new MultipartFormDataContent();
        if (withToken)
        {
            var html = await client.GetStringAsync(tokenPage ?? url);
            var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            Assert.True(token.Success);
            form.Add(new StringContent(WebUtility.HtmlDecode(token.Groups[1].Value)), "__RequestVerificationToken");
        }
        foreach (var field in fields ?? []) form.Add(new StringContent(field.Value), field.Key);
        if (fileName != null) form.Add(new ByteArrayContent(bytes ?? []), "Upload", fileName);
        return await client.PostAsync(url, form);
    }

    private static byte[] Zip(string name = "project/main.cs")
    {
        using var memory = new MemoryStream();
        using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, true))
        { using var writer = new StreamWriter(zip.CreateEntry(name).Open()); writer.Write("class Program {}"); }
        return memory.ToArray();
    }

    [Theory]
    [InlineData("solution.cs", "class Program {}")]
    [InlineData("answer.PDF", "%PDF-1.7\nexample")]
    [InlineData("notes.txt", "Ödev açıklaması")]
    public async Task FileOnlySubmissionHasAuthorizedDownloadAndSafeStorage(string name, string body)
    {
        using var f = await OwnershipFixture.CreateAsync(); using var student = await f.StudentAsync();
        var data = Encoding.UTF8.GetBytes(body);
        var response = await Upload(student, $"/Submissions/Create?assignmentId={f.FreshAssignmentId}", name, data);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var submission = await f.ReadAsync(db => db.Submissions.SingleAsync(s => s.AssignmentId == f.FreshAssignmentId));
        Assert.Equal(name, submission.OriginalFileName); Assert.Equal(data.Length, submission.FileSize);
        Assert.NotEqual(name, submission.StoredFileName); Assert.True(submission.FileUploadedAt.HasValue);
        Assert.Equal(1, FileCount(f));
        var path = $"/SubmissionFiles/Download/{submission.Id}";
        using var download = await student.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(data, await download.Content.ReadAsByteArrayAsync());
        Assert.Equal("application/octet-stream", download.Content.Headers.ContentType!.MediaType);
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal("nosniff", download.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("no-store", download.Headers.CacheControl!.ToString());
        using var teacher = await f.TeacherAsync(); using var otherTeacher = await f.TeacherAsync(true);
        using var otherStudent = await f.StudentAsync(true); using var anonymous = f.App.NewClient();
        Assert.Equal(HttpStatusCode.OK, (await teacher.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherTeacher.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherStudent.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync("/App_Data/SubmissionFiles/" + submission.StoredFileName)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync("/SubmissionFiles/Download/999999")).StatusCode);
        Assert.Contains("/SubmissionFiles/Download/" + submission.Id, await teacher.GetStringAsync($"/Assignments/Submission/{submission.Id}"));
    }

    [Fact]
    public async Task ZipCanBeUploadedAndDownloadedWithoutExtraction()
    {
        using var f = await OwnershipFixture.CreateAsync(); using var student = await f.StudentAsync();
        var data = Zip();
        Assert.Equal(HttpStatusCode.Redirect, (await Upload(student, $"/Submissions/Create?assignmentId={f.FreshAssignmentId}", "project.zip", data)).StatusCode);
        var id = await f.ReadAsync(db => db.Submissions.Where(s => s.AssignmentId == f.FreshAssignmentId).Select(s => s.Id).SingleAsync());
        Assert.Equal(data, await student.GetByteArrayAsync($"/SubmissionFiles/Download/{id}"));
        Assert.Single(Directory.GetFiles(Storage(f))); Assert.Empty(Directory.GetDirectories(Storage(f)));
    }

    [Theory]
    [InlineData("bad.exe", "MZ")]
    [InlineData("bad.pdf", "not a PDF")]
    [InlineData("bad.zip", "not a ZIP")]
    [InlineData("bad.py", "\0binary")]
    [InlineData("empty.txt", "")]
    public async Task InvalidUploadDoesNotCreateDatabaseRecordOrOrphanFile(string name, string content)
    {
        using var f = await OwnershipFixture.CreateAsync(); using var student = await f.StudentAsync();
        var response = await Upload(student, $"/Submissions/Create?assignmentId={f.FreshAssignmentId}", name, Encoding.UTF8.GetBytes(content));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("validation-summary-errors", await response.Content.ReadAsStringAsync());
        Assert.False(await f.ReadAsync(db => db.Submissions.AnyAsync(s => s.AssignmentId == f.FreshAssignmentId)));
        Assert.Equal(0, FileCount(f));
    }

    [Fact]
    public async Task OversizeEmptyAndUnenrolledSubmissionsAreRejected()
    {
        using var f = await OwnershipFixture.CreateAsync(); using var student = await f.StudentAsync();
        var url = $"/Submissions/Create?assignmentId={f.FreshAssignmentId}";
        Assert.Equal(HttpStatusCode.OK, (await Upload(student, url, "large.txt", new byte[20 * 1024 * 1024 + 1])).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Upload(student, url, null, null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Upload(student, url, null, null, new() { ["StoredFileName"] = "forged.upload", ["ExistingFileName"] = "fake.txt" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Upload(student, $"/Submissions/Create?assignmentId={f.AssignmentBId}", "file.cs", "class Test {}"u8.ToArray(), tokenPage: "/Student")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload(student, url, "file.txt", "test"u8.ToArray(), withToken: false)).StatusCode);
        Assert.Equal(0, FileCount(f));
    }

    [Fact]
    public async Task ZipTraversalIsRejected()
    {
        using var f = await OwnershipFixture.CreateAsync(); using var student = await f.StudentAsync();
        Assert.Equal(HttpStatusCode.OK, (await Upload(student, $"/Submissions/Create?assignmentId={f.FreshAssignmentId}", "bad.zip", Zip("../../escape.cs"))).StatusCode);
        Assert.Equal(0, FileCount(f));
    }

    [Fact]
    public async Task ReplacementRetentionAndRemovalKeepFilesAndReviewConsistent()
    {
        using var f = await OwnershipFixture.CreateAsync(); using var student = await f.StudentAsync();
        Assert.Equal(HttpStatusCode.Redirect, (await Upload(student, $"/Submissions/Create?assignmentId={f.FreshAssignmentId}", "first.cs", "class First {}"u8.ToArray())).StatusCode);
        var s = await f.ReadAsync(db => db.Submissions.SingleAsync(s => s.AssignmentId == f.FreshAssignmentId));
        var url = $"/Submissions/Edit/{s.Id}";
        using var teacher = await f.TeacherAsync();
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.PostAsync(teacher, $"/Assignments/Review/{s.Id}", new() { ["Feedback"] = "Good", ["Grade"] = "80" }, $"/Assignments/Submission/{s.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await Upload(student, url, "second.py", "print('hello')"u8.ToArray())).StatusCode);
        var updated = await f.ReadAsync(db => db.Submissions.SingleAsync(x => x.Id == s.Id));
        Assert.Equal("second.py", updated.OriginalFileName); Assert.Null(updated.ReviewedAt);
        Assert.False(File.Exists(Path.Combine(Storage(f), s.StoredFileName!))); Assert.Equal(1, FileCount(f));
        Assert.Equal(HttpStatusCode.Redirect, (await Upload(student, url, null, null, new() { ["Content"] = "Updated text" })).StatusCode);
        Assert.Equal(updated.StoredFileName, await f.ReadAsync(db => db.Submissions.Where(x => x.Id == s.Id).Select(x => x.StoredFileName).SingleAsync()));
        Assert.Equal(HttpStatusCode.OK, (await Upload(student, url, null, null, new() { ["RemoveFile"] = "true" })).StatusCode);
        Assert.Equal(1, FileCount(f));
        Assert.Equal(HttpStatusCode.Redirect, (await Upload(student, url, null, null, new() { ["RemoveFile"] = "true", ["Content"] = "Keep text" })).StatusCode);
        Assert.Equal(0, FileCount(f));
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/SubmissionFiles/Download/{s.Id}")).StatusCode);
    }

    [Fact]
    public async Task FailedReplacementAndForeignEditsPreserveOriginalFile()
    {
        using var f = await OwnershipFixture.CreateAsync(); using var student = await f.StudentAsync();
        await Upload(student, $"/Submissions/Create?assignmentId={f.FreshAssignmentId}", "first.cs", "class First {}"u8.ToArray());
        var s = await f.ReadAsync(db => db.Submissions.SingleAsync(s => s.AssignmentId == f.FreshAssignmentId));
        var url = $"/Submissions/Edit/{s.Id}";
        Assert.Equal(HttpStatusCode.OK, (await Upload(student, url, "bad.exe", "invalid"u8.ToArray())).StatusCode);
        using var other = await f.StudentAsync(true);
        Assert.Equal(HttpStatusCode.NotFound, (await Upload(other, url, "other.cs", "class Other {}"u8.ToArray(), tokenPage: "/Student")).StatusCode);
        Assert.Equal("class First {}", await student.GetStringAsync($"/SubmissionFiles/Download/{s.Id}"));
        Assert.Equal(1, FileCount(f));
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.PostAsync(student, $"/Submissions/Delete/{s.Id}", new())).StatusCode);
        Assert.Equal(0, FileCount(f));
    }

    [Fact]
    public async Task DatabaseInsertFailureCleansNewFile()
    {
        using var f = await OwnershipFixture.CreateAsync(); using var student = await f.StudentAsync();
        await f.ReadAsync(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectSubmission BEFORE INSERT ON Submissions BEGIN SELECT RAISE(ABORT, 'test failure'); END;"));
        Assert.Equal(HttpStatusCode.OK, (await Upload(student, $"/Submissions/Create?assignmentId={f.FreshAssignmentId}", "file.cs", "class Test {}"u8.ToArray())).StatusCode);
        Assert.False(await f.ReadAsync(db => db.Submissions.AnyAsync(s => s.AssignmentId == f.FreshAssignmentId)));
        Assert.Equal(0, FileCount(f));
    }

    [Fact]
    public async Task DatabaseUpdateFailureCleansReplacementAndKeepsPreviousFile()
    {
        using var f = await OwnershipFixture.CreateAsync(); using var student = await f.StudentAsync();
        await Upload(student, $"/Submissions/Create?assignmentId={f.FreshAssignmentId}", "first.txt", "original"u8.ToArray());
        var s = await f.ReadAsync(db => db.Submissions.SingleAsync(s => s.AssignmentId == f.FreshAssignmentId));
        await f.ReadAsync(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectUpdate BEFORE UPDATE ON Submissions BEGIN SELECT RAISE(ABORT, 'test failure'); END;"));
        Assert.Equal(HttpStatusCode.OK, (await Upload(student, $"/Submissions/Edit/{s.Id}", "second.txt", "replacement"u8.ToArray())).StatusCode);
        Assert.Equal("original", await student.GetStringAsync($"/SubmissionFiles/Download/{s.Id}"));
        Assert.Equal(1, FileCount(f));
    }
}
