using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectWork.Application.Abstractions.Repositories;
using ProjectWork.DataAccess;
using ProjectWork.DataAccess.Repositories;
using ProjectWork.Application.Services;

namespace ProjectWork.Tests;

internal sealed class TestDatabase : IDisposable
{
    public ServiceProvider Provider { get; }

    public TestDatabase()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        Provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true, ValidateOnBuild = true
        });
    }

    public IServiceScope CreateScope() => Provider.CreateScope();
    public void Dispose() => Provider.Dispose();
}
