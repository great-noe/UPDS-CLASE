# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivos de proyecto
COPY ["pw2-clase5.csproj", "./"]
RUN dotnet restore "pw2-clase5.csproj"

# Copiar código fuente
COPY . .

# Build en Release mode
RUN dotnet build "pw2-clase5.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "pw2-clase5.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime (Final - imagen pequeña)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Reducir tamaño eliminando dependencias innecesarias
RUN apk add --no-cache ca-certificates tzdata

# Copiar solo lo necesario desde publish
COPY --from=publish /app/publish .

# Usuario no root para seguridad
RUN addgroup -g 1001 -S dotnetuser && \
    adduser -S dotnetuser -u 1001 && \
    chown -R dotnetuser:dotnetuser /app
USER dotnetuser

# Variables de entorno
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "pw2-clase5.dll"]