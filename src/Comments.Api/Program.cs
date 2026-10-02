using System.Text.Json.Serialization;

using Comments.Api.GraphQL;
using Comments.Api.Hubs;
using Comments.Api.Services;
using Comments.Api.Validators;
using Comments.Domain.Abstractions;
using Comments.Infrastructure;
using Comments.Infrastructure.Persistence;

using FluentValidation;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<CommentQueryService>();
builder.Services.AddScoped<ICommentQueryService, CachedCommentQueryService>();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("Default")!,
    builder.Configuration.GetConnectionString("Redis")!,
    builder.Configuration.GetConnectionString("RabbitMq")!);
builder.Services.AddValidatorsFromAssemblyContaining<CreateCommentRequestValidator>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddSignalR();
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMaxExecutionDepthRule(12);
builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // API доступен только из внутренней сети compose (через nginx), поэтому прокси доверяем
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
var app = builder.Build();
app.UseForwardedHeaders();  

// в контейнере база создаётся и обновляется автоматически
if (app.Configuration.GetValue<bool>("MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();

app.MapControllers();
app.MapHub<CommentsHub>(CommentsHub.Route);
app.MapGraphQL();
app.Run();
