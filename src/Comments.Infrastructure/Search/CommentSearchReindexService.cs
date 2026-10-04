using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Search;

/// <summary>При старте API переиндексирует комментарии; ждёт, пока Elasticsearch станет доступен.</summary>
public sealed class CommentSearchReindexService(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    ILogger<CommentSearchReindexService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // выключается настройкой Search:ReindexOnStartup=false
        if (string.Equals(configuration["Search:ReindexOnStartup"], "false", StringComparison.OrdinalIgnoreCase))
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var reindexer = scope.ServiceProvider.GetRequiredService<CommentSearchReindexer>();

                var count = await reindexer.ReindexAsync(stoppingToken);
                logger.LogInformation("Search: reindexed {Count} comments", count);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Search reindex failed, retrying in 10 seconds");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}