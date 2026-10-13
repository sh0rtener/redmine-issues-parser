using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace RedmineParser.WebApi.Redmine;

public record ParseIssueFilter(DateTime UpdatedAt, int Limit, int Offset);

public class Parser
{
    private readonly IConfiguration _configuration;
    private readonly string _url;
    private readonly RedmineBasicAuth _credentials;
    private readonly ILogger<Parser> _logger;

    public Parser(IConfiguration configuration, ILogger<Parser> logger)
    {
        _url = (
            configuration["Redmine:Url"]
            ?? throw new ArgumentNullException("Не передано значение URL redmine")
        ).Trim('/');
        _configuration = configuration;
        _credentials = new(
            _configuration["Redmine:Basic:Username"]
                ?? throw new ArgumentNullException(
                    "Не передано значение Username в Basic конфигурации"
                ),
            _configuration["Redmine:Basic:Password"]
                ?? throw new ArgumentNullException(
                    "Не передано значение Password в Basic конфигурации"
                )
        );
        _logger = logger;
    }

    public virtual async Task<IEnumerable<IssueEntity>> ParseIssues(
        ParseIssueFilter filter,
        CancellationToken cancellationToken = default
    )
    {
        var url =
            $"{_url}/issues.xml?limit={filter.Limit}&offset={filter.Offset}&status_id=*&updated_on=><{filter.UpdatedAt:yyyy-MM-dd}";
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(
            "Authorization",
            GetBasicAuthHeader(_credentials.Username, _credentials.Password)
        );

        var content = new StringContent(string.Empty);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
        request.Content = content;

        try
        {
            _logger.LogInformation("Отправка запроса: " + url);

            var response = await client.SendAsync(request);
            var issues = new List<IssueEntity>();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Ошибка при отправке запроса: "
                        + response.StatusCode
                        + " "
                        + response.ReasonPhrase
                );
            }
            else
            {
                var responseText = await response.Content.ReadAsStreamAsync(cancellationToken);
                var issuesXml = IssuesCollectionXml.Parse(responseText);
                issues = IssueEntity.FromXml(issuesXml).ToList();
            }
            return issues;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("Ошибка при отправке запроса к redmine. Детализация: " + ex.Message);
            throw;
        }
    }

    public virtual async Task<int> GetAllTotalCount(
        ParseIssueFilter filter,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"{_url}/issues.xml?status_id=*&updated_on=><{filter.UpdatedAt:yyyy-MM-dd}";
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(
            "Authorization",
            GetBasicAuthHeader(_credentials.Username, _credentials.Password)
        );

        var content = new StringContent(string.Empty);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
        request.Content = content;

        try
        {
            _logger.LogInformation("Отправка запроса: " + url);

            var response = await client.SendAsync(request);
            var issuesCount = 0;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Ошибка при отправке запроса: "
                        + response.StatusCode
                        + " "
                        + response.ReasonPhrase
                );
            }
            else
            {
                var responseText = await response.Content.ReadAsStreamAsync(cancellationToken);
                var issuesXml = IssuesCollectionXml.Parse(responseText);
                issuesCount =issuesXml.TotalCount;
            }
            return issuesCount;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("Ошибка при отправке запроса к redmine. Детализация: " + ex.Message);
            throw;
        }
    }

    private string GetBasicAuthHeader(string username, string password)
    {
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        return $"Basic {token}";
    }
}
