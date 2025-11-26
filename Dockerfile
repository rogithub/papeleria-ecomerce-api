# --- Etapa 1: Build con AOT ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Instalar dependencias para AOT en ARM64
RUN apt-get update && apt-get install -y \
    clang \
    zlib1g-dev \
    libssl-dev \
    binutils \
    && rm -rf /var/lib/apt/lists/*

# Copiar archivos de proyecto
COPY ["Ro.Inventario.Api/Ro.Inventario.Api.csproj", "Ro.Inventario.Api/"]
COPY nuget.config .
RUN dotnet restore "Ro.Inventario.Api/Ro.Inventario.Api.csproj"

# Copiar código fuente
COPY . .
WORKDIR "/src/Ro.Inventario.Api"

# Configurar entorno para AOT
ENV CC=clang
ENV CXX=clang++

# Publicar CON AOT para ARM64
RUN dotnet publish "Ro.Inventario.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:PublishAot=true \
    --runtime linux-arm64 \
    /p:StripSymbols=true  

# DEBUG: Ver qué se generó
RUN echo "=== ARCHIVOS GENERADOS ===" && \
    ls -la /app/publish && \
    echo "=== BINARIOS EJECUTABLES ===" && \
    find /app/publish -type f -executable && \
    echo "=== VERIFICANDO BINARIO ===" && \
    file /app/publish/Ro.Inventario.Api 2>/dev/null || echo "Binario no encontrado"

# Runtime deps para AOT - USAR DEBIAN EN LUGAR DE ALPINE
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Verificar que el binario existe y es ejecutable en runtime
RUN echo "=== EN RUNTIME ===" && \
    ls -la && \
    file ./Ro.Inventario.Api && \
    ldd ./Ro.Inventario.Api 2>/dev/null || echo "No se puede verificar dependencias"


EXPOSE 8080
ENTRYPOINT ["./Ro.Inventario.Api"]