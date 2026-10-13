using Microsoft.EntityFrameworkCore;
using RedmineParser.WebApi;
using RedmineParser.WebApi.Data;
using RedmineParser.WebApi.Redmine;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = Host.CreateApplicationBuilder(args);

var connectionString =
    builder.Configuration["ConnectionStrings:Default"]
    ?? throw new ArgumentNullException("Не указана строка подключения");
builder.Services.AddDbContextFactory<AppDbContext>(x => x.UseNpgsql(connectionString));
builder.Services.AddScoped<Parser>();
builder.Services.AddScoped<RedmineService>();
builder.Services.AddHostedService<Worker>();
var app = builder.Build();

app.Run();
