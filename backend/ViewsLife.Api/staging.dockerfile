# ── Build stage ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Restore dependencies first (cached layer if .csproj unchanged)
COPY ViewsLife.Api.csproj ./
RUN dotnet restore ViewsLife.Api.csproj

# Copy remaining source and publish
COPY . ./
RUN dotnet publish ViewsLife.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Runtime stage ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Tell ASP.NET Core to listen on 8080 (Render's expected port)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Staging

EXPOSE 8080

ENTRYPOINT ["dotnet", "ViewsLife.Api.dll"]