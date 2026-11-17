FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble AS base
WORKDIR /app

# Install the supported Ubuntu LDAP runtime and trusted CA bundle
RUN apt-get update && apt-get upgrade -y && apt-get install -y libldap2 libldap-common ca-certificates && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0.404-noble AS build
WORKDIR /src
COPY global.json ./
COPY ["Apps/API/API.csproj", "Apps/API/"]
COPY ["Core/Domain/Domain.csproj", "Core/Domain/"]
COPY ["Core/Infrastructure/Infrastructure.csproj", "Core/Infrastructure/"]
COPY ["Core/App/App.csproj", "Core/App/"]
RUN dotnet restore "Apps/API/API.csproj"
COPY . .
WORKDIR "/src/Apps/API"
RUN dotnet build "API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
RUN mkdir -p /app/data /app/logs && chown -R app:app /app
COPY --chown=app:app --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SPSS.API.dll"]
