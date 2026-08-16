FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["API-MiPresupuesto/API-MiPresupuesto/API-MiPresupuesto.csproj", "API-MiPresupuesto/API-MiPresupuesto/"]
COPY ["API-MiPresupuesto/MiPresupuesto.Application/MiPresupuesto.Application.csproj", "API-MiPresupuesto/MiPresupuesto.Application/"]
COPY ["API-MiPresupuesto/MiPresupuesto.Domain/MiPresupuesto.Domain.csproj", "API-MiPresupuesto/MiPresupuesto.Domain/"]
COPY ["API-MiPresupuesto/MiPresupuesto.Infrastructure/MiPresupuesto.Infrastructure.csproj", "API-MiPresupuesto/MiPresupuesto.Infrastructure/"]
RUN dotnet restore "API-MiPresupuesto/API-MiPresupuesto/API-MiPresupuesto.csproj"

COPY API-MiPresupuesto/ API-MiPresupuesto/
WORKDIR /src/API-MiPresupuesto/API-MiPresupuesto
RUN dotnet publish "API-MiPresupuesto.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false /p:SkipFrontendBuild=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "API-MiPresupuesto.dll"]
