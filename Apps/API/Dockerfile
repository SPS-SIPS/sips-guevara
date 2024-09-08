# Use the base image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Install the required LDAP packages
RUN apt-get update && apt-get install -y libldap-2.5-0 liblber-2.5-0 && rm -rf /var/lib/apt/lists/*

EXPOSE 80
EXPOSE 443

# Use the SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Apps/API/API.csproj", "Apps/API/"]
COPY ["Core/Domain/Domain.csproj", "Core/Domain/"]
COPY ["Core/Infrastructure/Infrastructure.csproj", "Core/Infrastructure/"]
COPY ["Core/App/App.csproj", "Core/App/"]
RUN dotnet restore "Apps/API/API.csproj"
COPY . .
WORKDIR "/src/Apps/API"
RUN dotnet build "API.csproj" -c Release -o /app/build

# Publish the build output
FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the base image to run the final application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SPSS.API.dll"]
