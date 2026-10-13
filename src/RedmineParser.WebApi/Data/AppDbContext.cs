using Microsoft.EntityFrameworkCore;
using RedmineParser.WebApi.Redmine;

namespace RedmineParser.WebApi.Data;

public sealed class AppDbContext : DbContext
{
    public DbSet<IssueEntity> Issues => Set<IssueEntity>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
}
