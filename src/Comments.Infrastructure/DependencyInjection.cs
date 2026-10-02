using Comments.Domain.Abstractions;
using Comments.Domain.Events;
using Comments.Infrastructure.Caching;
using Comments.Infrastructure.Captcha;
using Comments.Infrastructure.Events;
using Comments.Infrastructure.Files;
using Comments.Infrastructure.Persistence;
using Comments.Infrastructure.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

namespace Comments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string redisConnectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.EnableRetryOnFailure()));

        services.AddSingleton<IMarkupValidator, XhtmlMarkupValidator>();
        services.AddScoped<IMessageSanitizer, HtmlMessageSanitizer>();

        services.AddSingleton<ICaptchaStore, RedisCaptchaStore>();
        services.AddSingleton<ICaptchaCodeGenerator, RandomCaptchaCodeGenerator>();
        services.AddSingleton<CaptchaImageRenderer>();
        services.AddSingleton<ICaptchaService, CaptchaService>();

        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
        services.AddSingleton<ITextFileProcessor, TextFileProcessor>();
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddScoped<IEventDispatcher, InProcessEventDispatcher>();
        services.AddScoped<IEventHandler<CommentCreatedEvent>, CommentListCacheInvalidationHandler>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redisConnectionString);

            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });

        return services;
    }
}