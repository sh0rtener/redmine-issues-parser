namespace RedmineParser.WebApi.Redmine;

public interface IRedmineRepository
{
    Task LoadIssues(
        IEnumerable<IssueEntity> entities,
        CancellationToken cancellationToken = default
    );
}
