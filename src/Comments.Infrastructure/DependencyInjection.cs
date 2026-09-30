using Comments.Domain.Abstractions;
using Comments.Infrastructure.Captcha;
using Comments.Infrastructure.Files;
using Comments.Infrastructure.Persistence;
using Comments.Infrastructure.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Comments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));

        services.AddSingleton<IMarkupValidator, XhtmlMarkupValidator>();
        services.AddScoped<IMessageSanitizer, HtmlMessageSanitizer>();
        services.AddMemoryCache();
        services.AddSingleton<ICaptchaStore, MemoryCaptchaStore>();
        services.AddSingleton<ICaptchaCodeGenerator, RandomCaptchaCodeGenerator>();
        services.AddSingleton<CaptchaImageRenderer>();
        services.AddSingleton<ICaptchaService, CaptchaService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
        services.AddSingleton<ITextFileProcessor, TextFileProcessor>();

        return services;
    }
}