# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy solutions and restore as distinct layers
COPY ["ResumeAnalyzer.sln", "./"]
COPY ["src/ResumeAnalyzer.Domain/ResumeAnalyzer.Domain.csproj", "src/ResumeAnalyzer.Domain/"]
COPY ["src/ResumeAnalyzer.Infrastructure/ResumeAnalyzer.Infrastructure.csproj", "src/ResumeAnalyzer.Infrastructure/"]
COPY ["src/ResumeAnalyzer.Web/ResumeAnalyzer.Web.csproj", "src/ResumeAnalyzer.Web/"]

RUN dotnet restore

# Copy everything else and build website
COPY . .
WORKDIR /app/src/ResumeAnalyzer.Web
RUN dotnet publish -c Release -o /app/out

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ResumeAnalyzer.Web.dll"]
