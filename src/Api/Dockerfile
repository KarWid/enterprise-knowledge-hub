# Build from the repository root:
# docker build --file src/Api/Dockerfile --tag enterprise-knowledge-hub-api:local .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Api/EnterpriseKnowledgeHub.Api/EnterpriseKnowledgeHub.Api.csproj", "src/Api/EnterpriseKnowledgeHub.Api/"]
COPY ["src/Api/EnterpriseKnowledgeHub.Application/EnterpriseKnowledgeHub.Application.csproj", "src/Api/EnterpriseKnowledgeHub.Application/"]
COPY ["src/Api/EnterpriseKnowledgeHub.Contracts/EnterpriseKnowledgeHub.Contracts.csproj", "src/Api/EnterpriseKnowledgeHub.Contracts/"]
COPY ["src/BuildingBlocks/EnterpriseKnowledgeHub.BuildingBlocks/EnterpriseKnowledgeHub.BuildingBlocks.csproj", "src/BuildingBlocks/EnterpriseKnowledgeHub.BuildingBlocks/"]
COPY ["src/Modules/Identity/EnterpriseKnowledgeHub.Modules.Identity/EnterpriseKnowledgeHub.Modules.Identity.csproj", "src/Modules/Identity/EnterpriseKnowledgeHub.Modules.Identity/"]
COPY ["src/Modules/Organizations/EnterpriseKnowledgeHub.Modules.Organizations/EnterpriseKnowledgeHub.Modules.Organizations.csproj", "src/Modules/Organizations/EnterpriseKnowledgeHub.Modules.Organizations/"]

RUN dotnet restore "src/Api/EnterpriseKnowledgeHub.Api/EnterpriseKnowledgeHub.Api.csproj"

COPY . .
WORKDIR "/src/src/Api/EnterpriseKnowledgeHub.Api"
RUN dotnet publish "EnterpriseKnowledgeHub.Api.csproj" --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "EnterpriseKnowledgeHub.Api.dll"]
