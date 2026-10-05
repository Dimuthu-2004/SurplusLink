# Render should use the repository root as the Docker build context.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["backend/SurplusLink.Api/SurplusLink.Api.csproj", "backend/SurplusLink.Api/"]
RUN dotnet restore "backend/SurplusLink.Api/SurplusLink.Api.csproj"

COPY . .
RUN dotnet publish "backend/SurplusLink.Api/SurplusLink.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000

# Render supplies PORT at runtime; expand it in the container shell.
ENTRYPOINT ["sh", "-c", "exec dotnet SurplusLink.Api.dll --urls http://0.0.0.0:${PORT}"]
