using Comments.Domain.Abstractions;
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

        return services;
    }
}