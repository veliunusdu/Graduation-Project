using System.Buffers.Binary;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Tests;
public class WordSubmissionTests
{
    private static byte[] Docx(bool unsafePath=false)
    {
        using var stream=new MemoryStream();
        using(var zip=new ZipArchive(stream,ZipArchiveMode.Create,true))
        {
            using(var writer=new StreamWriter(zip.CreateEntry("[Content_Types].xml").Open()))writer.Write("<Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'><Override PartName='/word/document.xml' ContentType='application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml'/></Types>");
            using(var writer=new StreamWriter(zip.CreateEntry("word/document.xml").Open()))writer.Write("<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><w:body><w:p><w:r><w:t>Student answer</w:t></w:r></w:p></w:body></w:document>");
            if(unsafePath){using var writer=new StreamWriter(zip.CreateEntry("../../escape.txt").Open());writer.Write("No extraction");}
        }
        return stream.ToArray();
    }
    // Small CFB container with an ordinary-sector WordDocument stream (CFB v3 layout).
    private static byte[] Doc(bool word=true,bool cycle=false)
    {
        var data=new byte[11*512];
        void U16(int offset,ushort value)=>BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset,2),value);
        void U32(int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset,4),value);
        new byte[]{0xD0,0xCF,0x11,0xE0,0xA1,0xB1,0x1A,0xE1}.CopyTo(data,0);
        U16(24,0x3E);U16(26,3);U16(28,0xFFFE);U16(30,9);U16(32,6);U32(44,1);U32(48,1);U32(56,4096);U32(60,0xFFFFFFFE);U32(68,0xFFFFFFFE);
        for(var i=0;i<109;i++)U32(76+i*4,0xFFFFFFFF);U32(76,0);
        for(var i=0;i<128;i++)U32(512+i*4,0xFFFFFFFF);
        U32(512,0xFFFFFFFD);U32(516,cycle?1u:0xFFFFFFFE);
        for(var i=2;i<9;i++)U32(512+i*4,(uint)i+1);U32(512+9*4,0xFFFFFFFE);
        void Entry(int offset,string name,byte type,uint start,uint size)
        {
            Encoding.Unicode.GetBytes(name+"\0").CopyTo(data,offset);U16(offset+64,(ushort)((name.Length+1)*2));data[offset+66]=type;data[offset+67]=1;U32(offset+68,0xFFFFFFFF);U32(offset+72,0xFFFFFFFF);U32(offset+76,0xFFFFFFFF);U32(offset+116,start);U32(offset+120,size);
        }
        Entry(1024,"Root Entry",5,0xFFFFFFFE,0);U32(1024+76,1);
        Entry(1152,word?"WordDocument":"Workbook",2,2,4096);
        U16(1536,0xA5EC);U16(1538,0x00C1);
        return data;
    }
    private static async Task<HttpResponseMessage> Upload(HttpClient client,string url,string name,byte[] data)
    {
        var page=await client.GetStringAsync(url);var token=Regex.Match(page,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");Assert.True(token.Success);
        using var form=new MultipartFormDataContent();form.Add(new StringContent(WebUtility.HtmlDecode(token.Groups[1].Value)),"__RequestVerificationToken");form.Add(new ByteArrayContent(data),"Upload",name);
        return await client.PostAsync(url,form);
    }
    private static int FileCount(OwnershipFixture f){var directory=Path.Combine(f.App.DirectoryPath,"SubmissionFiles");return Directory.Exists(directory)?Directory.GetFiles(directory).Length:0;}
    [Theory]
    [InlineData("answer.doc")]
    [InlineData("answer.DOCX")]
    public async Task WordFileCanBeSubmittedDownloadedAndUsedAsReplacement(string name)
    {
        using var f=await OwnershipFixture.CreateAsync();using var student=await f.StudentAsync();using var teacher=await f.TeacherAsync();using var otherStudent=await f.StudentAsync(true);using var otherTeacher=await f.TeacherAsync(true);
        var data=name.EndsWith("doc",StringComparison.OrdinalIgnoreCase)?Doc():Docx();
        var url=$"/Submissions/Create?assignmentId={f.FreshAssignmentId}";
        var page=await student.GetStringAsync(url);Assert.Contains(".doc,.docx",page);Assert.Contains("PDF, DOC, DOCX, ZIP",page);
        Assert.Equal(HttpStatusCode.Redirect,(await Upload(student,url,name,data)).StatusCode);
        var submission=await f.ReadAsync(db=>db.Submissions.SingleAsync(s=>s.AssignmentId==f.FreshAssignmentId));
        Assert.Equal(name,submission.OriginalFileName);Assert.Equal(data.Length,submission.FileSize);Assert.Equal(1,FileCount(f));
        var download=$"/SubmissionFiles/Download/{submission.Id}";
        using(var response=await student.GetAsync(download)){Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Equal(data,await response.Content.ReadAsByteArrayAsync());Assert.Equal("application/octet-stream",response.Content.Headers.ContentType!.MediaType);Assert.Equal("attachment",response.Content.Headers.ContentDisposition!.DispositionType);}
        Assert.Equal(data,await teacher.GetByteArrayAsync(download));Assert.Equal(HttpStatusCode.NotFound,(await otherStudent.GetAsync(download)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await otherTeacher.GetAsync(download)).StatusCode);
        await FormClient.PostAsync(teacher,$"/Assignments/Review/{submission.Id}",new(){["Feedback"]="Reviewed Word file",["Grade"]="90"},$"/Assignments/Submission/{submission.Id}");
        var replacement=name.EndsWith("doc",StringComparison.OrdinalIgnoreCase)?Docx():Doc();var replacementName=name.EndsWith("doc",StringComparison.OrdinalIgnoreCase)?"updated.docx":"updated.doc";
        Assert.Equal(HttpStatusCode.Redirect,(await Upload(student,$"/Submissions/Edit/{submission.Id}",replacementName,replacement)).StatusCode);
        var updated=await f.ReadAsync(db=>db.Submissions.SingleAsync(s=>s.Id==submission.Id));Assert.Equal(replacementName,updated.OriginalFileName);Assert.Null(updated.ReviewedAt);Assert.Null(updated.TeacherGrade);Assert.Null(updated.TeacherFeedback);Assert.Equal(1,FileCount(f));Assert.False(File.Exists(Path.Combine(f.App.DirectoryPath,"SubmissionFiles",submission.StoredFileName!)));Assert.Equal(replacement,await teacher.GetByteArrayAsync(download));
    }
    [Theory]
    [InlineData("text-as-doc")]
    [InlineData("text-as-docx")]
    [InlineData("excel-as-doc")]
    [InlineData("directory-cycle")]
    [InlineData("header-only")]
    [InlineData("zip-as-docx")]
    [InlineData("unsafe-docx")]
    [InlineData("oversize")]
    public async Task InvalidWordFileDoesNotCreateSubmissionOrLeaveFiles(string kind)
    {
        using var f=await OwnershipFixture.CreateAsync();using var student=await f.StudentAsync();
        var name=kind.Contains("docx")?"invalid.docx":"invalid.doc";
        var data=kind switch {
            "excel-as-doc"=>Doc(false),"directory-cycle"=>Doc(cycle:true),"header-only"=>Doc()[..512],"unsafe-docx"=>Docx(true),"oversize"=>new byte[20*1024*1024+1],
            "zip-as-docx"=>EmptyZip(),_=>Encoding.UTF8.GetBytes("This is plain text, not a Word document.")};
        var response=await Upload(student,$"/Submissions/Create?assignmentId={f.FreshAssignmentId}",name,data);Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Contains("validation-summary-errors",await response.Content.ReadAsStringAsync());Assert.Equal(0,FileCount(f));Assert.False(await f.ReadAsync(db=>db.Submissions.AnyAsync(s=>s.AssignmentId==f.FreshAssignmentId)));
    }
    private static byte[] EmptyZip(){using var memory=new MemoryStream();using(var zip=new ZipArchive(memory,ZipArchiveMode.Create,true)){using var writer=new StreamWriter(zip.CreateEntry("notes.txt").Open());writer.Write("Not a Word package");}return memory.ToArray();}
}
