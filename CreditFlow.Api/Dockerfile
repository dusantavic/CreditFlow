FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CreditFlow.Api/CreditFlow.Api.csproj", "CreditFlow.Api/"]
COPY ["CreditFlow.Application/CreditFlow.Application.csproj", "CreditFlow.Application/"]
COPY ["CreditFlow.Domain/CreditFlow.Domain.csproj", "CreditFlow.Domain/"]
COPY ["CreditFlow.Infrastructure/CreditFlow.Infrastructure.csproj", "CreditFlow.Infrastructure/"]
RUN dotnet restore "./CreditFlow.Api/CreditFlow.Api.csproj"
COPY . .
WORKDIR "/src/CreditFlow.Api"
RUN dotnet build "./CreditFlow.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CreditFlow.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CreditFlow.Api.dll"]