FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Service/Service.csproj src/Service/
COPY src/Data.Postgres/Data.Postgres.csproj src/Data.Postgres/
COPY src/Util/Util.csproj src/Util/
COPY src/Facade/Facade.csproj src/Facade/
RUN dotnet restore src/Facade/Facade.csproj
COPY src ./src
RUN dotnet publish src/Facade/Facade.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Facade.dll"]
