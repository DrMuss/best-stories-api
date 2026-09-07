# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Central package management: restore fails without these two files present.
COPY Directory.Build.props Directory.Packages.props ./
COPY api/BestStories.Api/BestStories.Api.csproj api/BestStories.Api/
RUN dotnet restore api/BestStories.Api/BestStories.Api.csproj

COPY api/ api/
RUN dotnet publish api/BestStories.Api/BestStories.Api.csproj \
    -c Release -o /app --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# APP_UID is defined by the base image (non-root, uid 1654).
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "BestStories.Api.dll"]
