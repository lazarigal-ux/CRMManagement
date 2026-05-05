# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CRMManagement.sln ./
COPY CRMManagement.Domain/CRMManagement.Domain.csproj ./CRMManagement.Domain/
COPY CRMManagement.Application/CRMManagement.Application.csproj ./CRMManagement.Application/
COPY CRMManagement.Infrastructure/CRMManagement.Infrastructure.csproj ./CRMManagement.Infrastructure/
COPY CRMManagement.Web/CRMManagement.Web.csproj ./CRMManagement.Web/

RUN dotnet restore CRMManagement.Web/CRMManagement.Web.csproj

COPY . ./

RUN dotnet publish CRMManagement.Web/CRMManagement.Web.csproj -c Release -o /out /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8100 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 8100

COPY --from=build /out ./
COPY AiExamples/ ./AiExamples/

HEALTHCHECK --interval=10s --timeout=5s --retries=12 --start-period=40s \
    CMD curl -fsS -o /dev/null http://127.0.0.1:8100/ || exit 1

ENTRYPOINT ["dotnet", "CRMManagement.Web.dll"]
