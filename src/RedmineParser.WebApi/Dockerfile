FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/RedmineParser.WebApi/*.csproj src/RedmineParser.WebApi/

RUN dotnet restore src/RedmineParser.WebApi/*.csproj

COPY . .

WORKDIR /src/src/RedmineParser.WebApi
RUN dotnet publish -c Release -o /app/publish 


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RedmineParser.WebApi.dll"]