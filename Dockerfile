# --- Etapa 1: Build con AOT ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivos de proyecto
COPY ["Ro.Inventario.Api/Ro.Inventario.Api.csproj", "Ro.Inventario.Api/"]
RUN dotnet restore "Ro.Inventario.Api/Ro.Inventario.Api.csproj"

# Copiar código fuente
COPY . .
WORKDIR "/src/Ro.Inventario.Api"

# Publicar con AOT (genera binario nativo)
RUN dotnet publish "Ro.Inventario.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:PublishAot=true

# --- Etapa 2: Runtime mínimo ---
# Con AOT no necesitas el runtime de .NET, solo dependencias del sistema
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine AS final
WORKDIR /app

# Copiar el binario compilado
COPY --from=build /app/publish .

# Exponer puerto
EXPOSE 8080

# Ejecutar el binario nativo directamente
ENTRYPOINT ["./Ro.Inventario.Api"]