FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
RUN mkdir -p /home/app/dataprotection-keys \
    && chown -R $APP_UID:$APP_UID /home/app/dataprotection-keys
USER $APP_UID


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["CertVal.ApiService/CertVal.ApiService.csproj", "CertVal.ApiService/"]
COPY ["CertVal.Infrastructure/CertVal.Infrastructure.csproj", "CertVal.Infrastructure/"]
COPY ["CertVal.Application/CertVal.Application.csproj", "CertVal.Application/"]
COPY ["CertVal.Core/CertVal.Core.csproj", "CertVal.Core/"]
COPY ["CertVal.ServiceDefaults/CertVal.ServiceDefaults.csproj", "CertVal.ServiceDefaults/"]
RUN dotnet restore "./CertVal.ApiService/CertVal.ApiService.csproj"
COPY . .
WORKDIR "/src/CertVal.ApiService"
RUN dotnet build "./CertVal.ApiService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CertVal.ApiService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CertVal.ApiService.dll"]