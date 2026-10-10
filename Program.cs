using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ProjectWork.Application.Services;
using ProjectWork.Infrastructure;
using ProjectWork.Infrastructure.Persistence;
using ProjectWork.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    var applicationXmlFilename = $"{typeof(IEventService).Assembly.GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, applicationXmlFilename));
});
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IBookingService, BookingService>();

var app = builder.Build();

// Миграции создают базу и последовательно обновляют её схему при запуске.
await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
