using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace ASimpleTutor.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // 使用绝对路径确保配置文件被正确加载
            var testConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.test.json");
            config.AddJsonFile(testConfigPath, optional: true, reloadOnChange: true);
        });
    }
}
