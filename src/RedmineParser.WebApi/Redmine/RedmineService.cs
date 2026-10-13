using Microsoft.EntityFrameworkCore;
using RedmineParser.WebApi.Data;

namespace RedmineParser.WebApi.Redmine;

public class RedmineService
{
    private readonly Parser _parser;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<RedmineService> _logger;

    public RedmineService() { }

    public RedmineService(
        Parser parser,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ILogger<RedmineService> logger
    )
    {
        _parser = parser;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public virtual async Task<bool> IsExistsIssues(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Issues.AnyAsync(cancellationToken);
    }

    public virtual async Task SaveNewIssuesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var lastUpdateDate = await context
            .Issues.OrderBy(x => x.UpdatedOn)
            .Select(x => x.UpdatedOn)
            .LastAsync(cancellationToken);

        _logger.LogInformation(
            "Начинается выгрузка и обновление задач из Redmine за дату " + lastUpdateDate
        );
        var totalCount = await _parser.GetAllTotalCount(
            new(lastUpdateDate, 100, 0),
            cancellationToken
        );

        int limit = 100;
        int offset = 0;

        int total = 0;

        while (offset <= totalCount)
        {
            var issues = await _parser.ParseIssues(
                new(lastUpdateDate, limit, offset),
                cancellationToken
            );

            foreach (var issue in issues)
            {
                if (await context.Issues.AnyAsync(x => x.Id == issue.Id, cancellationToken))
                {
                    context.Issues.Update(issue);
                }
                else
                    await context.Issues.AddRangeAsync(issues, cancellationToken);
            }

            offset += limit;

            await context.SaveChangesAsync(cancellationToken);

            total += issues.Count();
            _logger.LogInformation($"Выгружено {total} записей");
        }

        _logger.LogInformation(
            "Выгрузка завершена. Общее количество задач: " + total + "из " + totalCount
        );
    }

    public virtual async Task SaveAllIssuesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogInformation("Начинается выгрузка всех задач из Redmine");

        var totalCount = await _parser.GetAllTotalCount(
            new(DateTime.MinValue, 100, 0),
            cancellationToken
        );

        int limit = 100;
        int offset = 0;

        int total = 0;

        while (offset <= totalCount)
        {
            var issues = await _parser.ParseIssues(
                new(DateTime.MinValue, limit, offset),
                cancellationToken
            );
            await context.Issues.AddRangeAsync(issues, cancellationToken);

            offset += limit;

            await context.SaveChangesAsync(cancellationToken);

            total += issues.Count();
            _logger.LogInformation($"Выгружено {total} записей");
        }

        _logger.LogInformation(
            "Выгрузка завершена. Общее количество задач: " + total + "из " + totalCount
        );
    }
}
