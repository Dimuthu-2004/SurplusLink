FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["SurplusLink.Api.csproj", "./"]

RUN dotnet restore "SurplusLink.Api.csproj"

COPY . .

RUN dotnet publish "SurplusLink.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

ENTRYPOINT ["sh", "-c", "exec dotnet SurplusLink.Api.dll --urls http://0.0.0.0:${PORT}"]