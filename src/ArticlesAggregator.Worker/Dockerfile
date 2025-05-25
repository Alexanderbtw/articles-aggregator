
############################################
# -------- 1. RUNTIME (slim) -------------
############################################
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

############################################
# -------- 2. BUILD -----------------------
############################################
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy shared props/targets that define TargetFramework and central package versions
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

# Copy all csproj files first – this enables layer‑cached restore
COPY ["src/ArticlesAggregator.Worker/ArticlesAggregator.Worker.csproj",                                             "ArticlesAggregator.Worker/"]
COPY ["src/ArticlesAggregator.ExternalServices/ArticlesAggregator.ExternalServices.csproj",                         "ArticlesAggregator.ExternalServices/"]
COPY ["src/ArticlesAggregator.ExternalServices.Parser/ArticlesAggregator.ExternalServices.Parser.csproj",           "ArticlesAggregator.ExternalServices.Parser/"]
COPY ["src/ArticlesAggregator.Application/ArticlesAggregator.Application.csproj",                                   "ArticlesAggregator.Application/"]
COPY ["src/ArticlesAggregator.Domain/ArticlesAggregator.Domain.csproj",                                             "ArticlesAggregator.Domain/"]
COPY ["src/ArticlesAggregator.Infrastructure/ArticlesAggregator.Infrastructure.csproj",                             "ArticlesAggregator.Infrastructure/"]
COPY ["src/ArticlesAggregator.Infrastructure.Abstractions/ArticlesAggregator.Infrastructure.Abstractions.csproj",   "ArticlesAggregator.Infrastructure.Abstractions/"]

# Restore dependencies
RUN dotnet restore "ArticlesAggregator.Worker/ArticlesAggregator.Worker.csproj"

# Copy the rest of the source tree
COPY src/. .

# Build
WORKDIR /src/ArticlesAggregator.Worker
RUN dotnet build "ArticlesAggregator.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

############################################
# -------- 3. PUBLISH ---------------------
############################################
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/ArticlesAggregator.Worker
RUN dotnet publish "ArticlesAggregator.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

############################################
# -------- 4. FINAL -----------------------
############################################
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ArticlesAggregator.Worker.dll"]
