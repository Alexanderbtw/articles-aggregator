FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ArticlesAggregator.Worker/ArticlesAggregator.Worker.csproj", "ArticlesAggregator.Worker/"]
RUN dotnet restore "ArticlesAggregator.Worker/ArticlesAggregator.Worker.csproj"
COPY . .
WORKDIR "/src/ArticlesAggregator.Worker"
RUN dotnet build "./ArticlesAggregator.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ArticlesAggregator.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ArticlesAggregator.Worker.dll"]
