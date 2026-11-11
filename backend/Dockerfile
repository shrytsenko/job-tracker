# API image: build with the full SDK, run on the small ASP.NET runtime image.
# Build context: the backend folder (docker compose does this for you).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first, so "dotnet restore" is cached until a .csproj changes.
COPY Directory.Build.props JobTracker.slnx ./
COPY src/JobTracker.Core/JobTracker.Core.csproj src/JobTracker.Core/
COPY src/JobTracker.Infrastructure/JobTracker.Infrastructure.csproj src/JobTracker.Infrastructure/
COPY src/JobTracker.Api/JobTracker.Api.csproj src/JobTracker.Api/
RUN dotnet restore src/JobTracker.Api/JobTracker.Api.csproj

COPY src/ src/
RUN dotnet publish src/JobTracker.Api/JobTracker.Api.csproj -c Release -o /app --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# The .NET images ship a non-root "app" user; never run the API as root.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "JobTracker.Api.dll"]
