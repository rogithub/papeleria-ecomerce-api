# --- Etapa 1: Build con AOT ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Instalar dependencias para AOT en ARM64
RUN apt-get update && apt-get install -y \
    clang \
    zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

# Copiar archivos de proyecto
COPY ["Ro.Inventario.Api/Ro.Inventario.Api.csproj", "Ro.Inventario.Api/"]
COPY nuget.config .
RUN dotnet restore "Ro.Inventario.Api/Ro.Inventario.Api.csproj"

# Copiar código fuente
COPY . .
WORKDIR "/src/Ro.Inventario.Api"

# Publicar CON AOT para ARM64
RUN dotnet publish "Ro.Inventario.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:PublishAot=true \
    --runtime linux-arm64 \
    /p:StripSymbols=true  

# Runtime deps (porque es AOT)
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["./Ro.Inventario.Api"]