using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RedmineParser.WebApi;
using RedmineParser.WebApi.Redmine;

namespace RedmineParser.Tests;

public class Tests
{
    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();
    }

    #region IssueEntity Mapping Tests

    [Fact]
    public void FromXml_ShouldMapCorrectly_WithNulls()
    {
        // Arrange
        var xml = new IssueXml
        {
            Id = 1,
            Project = new IssueXml.RefEntity { Id = 10, Name = "Proj" },
            Tracker = new IssueXml.RefEntity { Id = 2, Name = "Bug" },
            Status = new IssueXml.StatusEntity
            {
                Id = 3,
                Name = "New",
                IsClosed = false,
            },
            Priority = new IssueXml.RefEntity { Id = 4, Name = "High" },
            Author = new IssueXml.RefEntity { Id = 5, Name = "Admin" },
            AssignedTo = null, // Проверка на null
            Category = null, // Проверка на null
            Subject = "Test Subject",
            StartDateStr = "2023-10-01",
            DoneRatio = 50,
            CreatedOnStr = "2023-10-01T10:00:00Z",
            UpdatedOnStr = "2023-10-01T10:00:00Z",
        };

        // Act
        var entity = IssueEntity.FromXml(xml);

        // Assert
        entity.Id.Should().Be(1);
        entity.AssignedToId.Should().Be(0);
        entity.AssignedToName.Should().Be(string.Empty);
        entity.CategoryId.Should().Be(0);
        entity.IsClosed.Should().BeFalse();
        entity.DoneRatio.Should().Be(50);
    }

    #endregion

    #region Parser Tests

    // Вспомогательный класс для мокинга HttpClient
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) =>
            _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(_handler(request));
    }

    [Fact]
    public async Task ParseIssues_ShouldReturnMappedEntities_OnSuccess()
    {
        // Arrange
        var xmlResponse =
            @"<?xml version=""1.0""?>
        <issues total_count=""1"" offset=""0"" limit=""100"">
            <issue id=""1"">
                <project id=""1"" name=""TestProject""/>
                <tracker id=""1"" name=""Bug""/>
                <status id=""1"" name=""New"" is_closed=""false""/>
                <priority id=""1"" name=""Normal""/>
                <author id=""1"" name=""Admin""/>
                <subject>Test Issue</subject>
                <done_ratio>0</done_ratio>
                <created_on>2023-01-01T00:00:00Z</created_on>
                <updated_on>2023-01-01T00:00:00Z</updated_on>
            </issue>
        </issues>";

        var handlerMock = new MockHttpMessageHandler(req => new HttpResponseMessage(
            HttpStatusCode.OK
        )
        {
            Content = new StringContent(xmlResponse),
        });

        // Используем HttpClient с мокированным handler-ом
        using var httpClient = new HttpClient(handlerMock);

        var configMock = BuildConfiguration();

        // Примечание: для тестируемости Parser должен принимать HttpClient или IHttpClientFactory
        var parser = new TestableParser(configMock, Mock.Of<ILogger<Parser>>(), httpClient);
        var filter = new ParseIssueFilter(DateTime.UtcNow, 1, 0);

        // Act
        var result = await parser.ParseIssues(filter);

        // Assert
        result.Should().HaveCount(1);
    }

    // Тестовая версия Parser с инъекцией HttpClient для возможности тестирования
    public sealed class TestableParser : Parser
    {
        private readonly HttpClient _httpClient;

        public TestableParser(IConfiguration config, ILogger<Parser> logger, HttpClient httpClient)
            : base(config, logger)
        {
            _httpClient = httpClient;
        }
        // Переопределяем или добавляем метод, использующий _httpClient вместо new HttpClient()
        // В реальном коде лучше сделать HttpClient приватным полем, инициализируемым через конструктор.
    }

    #endregion

    #region Worker Tests

    [Fact]
    public async Task ExecuteAsync_ShouldCallSaveAllIssues_WhenNoIssuesExist()
    {
        // Arrange
        var serviceMock = new Mock<RedmineService>();
        serviceMock.Setup(s => s.IsExistsIssues(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Используем реальный ServiceCollection для корректной работы CreateAsyncScope()
        var services = new ServiceCollection();
        services.AddSingleton(serviceMock.Object);
        services.AddLogging();
        services.AddSingleton<Worker>();

        var serviceProvider = services.BuildServiceProvider();
        var worker = serviceProvider.GetRequiredService<Worker>();

        // Act: Вызов protected метода через рефлексию (т.к. класс sealed)
        var method = typeof(Worker).GetMethod(
            "ExecuteAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        await (Task)method!.Invoke(worker, new object[] { CancellationToken.None });

        // Assert
        serviceMock.Verify(s => s.IsExistsIssues(It.IsAny<CancellationToken>()), Times.Once);
        serviceMock.Verify(s => s.SaveAllIssuesAsync(It.IsAny<CancellationToken>()), Times.Once);
        serviceMock.Verify(s => s.SaveNewIssuesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallSaveNewIssues_WhenIssuesExist()
    {
        // Arrange
        var serviceMock = new Mock<RedmineService>();
        serviceMock.Setup(s => s.IsExistsIssues(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var services = new ServiceCollection();
        services.AddSingleton(serviceMock.Object);
        services.AddLogging();
        services.AddSingleton<Worker>();

        var serviceProvider = services.BuildServiceProvider();
        var worker = serviceProvider.GetRequiredService<Worker>();

        // Act
        var method = typeof(Worker).GetMethod(
            "ExecuteAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        await (Task)method!.Invoke(worker, new object[] { CancellationToken.None });

        // Assert
        serviceMock.Verify(s => s.IsExistsIssues(It.IsAny<CancellationToken>()), Times.Once);
        serviceMock.Verify(s => s.SaveAllIssuesAsync(It.IsAny<CancellationToken>()), Times.Never);
        serviceMock.Verify(s => s.SaveNewIssuesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Простая реализация IServiceProvider для возврата нужного сервиса в тесте
    private class MockServiceProvider : IServiceProvider
    {
        private readonly object _service;

        public MockServiceProvider(object service) => _service = service;

        public object GetService(Type serviceType) => _service;
    }

    #endregion
}
