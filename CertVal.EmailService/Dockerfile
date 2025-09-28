FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["CertVal.EmailService/CertVal.EmailService.csproj", "CertVal.EmailService/"]
COPY ["CertVal.Core/CertVal.Core.csproj", "CertVal.Core/"]
COPY ["CertVal.ServiceDefaults/CertVal.ServiceDefaults.csproj", "CertVal.ServiceDefaults/"]
RUN dotnet restore "./CertVal.EmailService/CertVal.EmailService.csproj"
COPY . .
WORKDIR "/src/CertVal.EmailService"
RUN dotnet build "./CertVal.EmailService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CertVal.EmailService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CertVal.EmailService.dll"]