# --- Etapa 1: Build con AOT ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivos de proyecto
COPY ["Ro.Inventario.Api/Ro.Inventario.Api.csproj", "Ro.Inventario.Api/"]
COPY nuget.config .
RUN dotnet restore "Ro.Inventario.Api/Ro.Inventario.Api.csproj"

# Copiar código fuente
COPY . .
WORKDIR "/src/Ro.Inventario.Api"

# Publicar SIN AOT
RUN dotnet publish "Ro.Inventario.Api.csproj" \
    -c Release \
    -o /app/publish    

# Runtime con .NET completo
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ro.Inventario.Api.dll"]
