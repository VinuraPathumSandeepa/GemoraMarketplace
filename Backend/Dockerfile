# ============================================================
# Stage 1: Build
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY Gemora.sln ./
COPY Gemora.API/Gemora.API.csproj Gemora.API/
COPY Gemora.Application/Gemora.Application.csproj Gemora.Application/
COPY Gemora.Domain/Gemora.Domain.csproj Gemora.Domain/
COPY Gemora.Infrastructure/Gemora.Infrastructure.csproj Gemora.Infrastructure/

# Restore NuGet packages
RUN dotnet restore

# Copy all source code
COPY . .

# Publish in Release mode to /app/publish
RUN dotnet publish Gemora.API/Gemora.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ============================================================
# Stage 2: Runtime
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output from the build stage
COPY --from=build /app/publish .

# Render injects PORT; ASP.NET Core reads ASPNETCORE_URLS
# We expose 8080 as the default but Render will override PORT.
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

ENTRYPOINT ["dotnet", "Gemora.API.dll"]
