using RedmineParser.WebApi.Redmine;

namespace RedmineParser.WebApi;

public sealed class Worker : BackgroundService
{
    private const int TIMEOUT_SEC = 300;

    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _provider;

    public Worker(ILogger<Worker> logger, IServiceProvider provider)
    {
        _logger = logger;
        _provider = provider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<RedmineService>();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!await service.IsExistsIssues(stoppingToken))
                await service.SaveAllIssuesAsync(stoppingToken);
            else
                await service.SaveNewIssuesAsync(stoppingToken);
            await Task.Delay(TIMEOUT_SEC * 1000, stoppingToken);
        }
    }
}
