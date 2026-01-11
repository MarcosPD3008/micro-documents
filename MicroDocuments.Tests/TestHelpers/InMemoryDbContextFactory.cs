using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MicroDocuments.Infrastructure.Configuration;
using MicroDocuments.Infrastructure.Persistence;
using Moq;

namespace MicroDocuments.Tests.TestHelpers;

public static class InMemoryDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    public static AppDbContext CreateWithSeed(Action<AppDbContext> seedAction)
    {
        var context = Create();
        seedAction(context);
        context.SaveChanges();
        return context;
    }

    public static Mock<IHttpContextAccessor> CreateHttpContextAccessor()
    {
        var mock = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        mock.Setup(x => x.HttpContext).Returns(httpContext);
        return mock;
    }

    public static Mock<IOptions<ApiKeySettings>> CreateApiKeySettings(bool globalFilter = false)
    {
        var mock = new Mock<IOptions<ApiKeySettings>>();
        var settings = new ApiKeySettings
        {
            GlobalFilter = globalFilter,
            SecretKey = "test-secret-key",
            MasterKey = "test-master-key"
        };
        mock.Setup(x => x.Value).Returns(settings);
        return mock;
    }
}



