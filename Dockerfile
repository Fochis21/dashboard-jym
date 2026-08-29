# --- Etapa 1: compilar y publicar la app (equivalente a "mvn package") ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos primero solo el .csproj para aprovechar el cache de Docker:
# mientras no cambien las dependencias, "dotnet restore" no se vuelve a
# ejecutar en cada build, y los deploys posteriores son mucho mas rapidos.
COPY DashboardJym/DashboardJym.csproj DashboardJym/
RUN dotnet restore DashboardJym/DashboardJym.csproj

COPY DashboardJym/ DashboardJym/
RUN dotnet publish DashboardJym/DashboardJym.csproj -c Release -o /app/publish --no-restore

# --- Etapa 2: imagen final, liviana, solo con el runtime (sin el SDK completo) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
# Puerto interno por defecto si Render no define PORT (ver CMD abajo).
EXPOSE 8080

# Render inyecta la variable de entorno PORT (10000 por defecto) y espera
# que el proceso escuche ahi en 0.0.0.0. Usamos la forma "shell" del CMD
# para poder interpolar esa variable en tiempo de ejecucion.
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet DashboardJym.dll"]
