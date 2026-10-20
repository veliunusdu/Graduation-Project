using System.Net;
using HomeworkPlatform.Web.Security;
using HomeworkPlatform.Web.Services.AI;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkPlatform.Web.Tests;

public class AIStatusTests
{
    private sealed class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("private traceback") });
    }
    private sealed class App : AuthWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services => services.AddHttpClient<AIServiceClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new Handler()));
        }
    }

    [Fact]
    public async Task OnlyTeacherCanSeeStatusAndFailureIsRenderedInPage()
    {
        using var app = new App(); using var teacher = app.NewClient(); using var student = app.NewClient();
        using var anonymous = app.NewClient();
        using (var scope = app.Services.CreateScope())
            Assert.True((await scope.ServiceProvider.GetRequiredService<TeacherProvisioner>()
                .ProvisionAsync("status-teacher@example.test", FormClient.TestPassword)).Success);
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.LoginAsync(teacher, "status-teacher@example.test")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.RegisterAsync(student, "status-student@example.test")).StatusCode);
        var page = await teacher.GetAsync("/AIStatus");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("alert alert-warning", html); Assert.DoesNotContain("private traceback", html);
        Assert.Contains("/AIStatus", await teacher.GetStringAsync("/Teacher"));
        var denied = await student.GetAsync("/AIStatus");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.Contains("AccessDenied", denied.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync("/AIStatus")).StatusCode);
    }
}
