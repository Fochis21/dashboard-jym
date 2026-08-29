# Dashboard JYM — Estudio Jurídico

Sistema interno de gestión (clientes, procesos, pagos, agenda, mensajes,
notificaciones y auditoría) para el Estudio Jurídico JYM. ASP.NET Core MVC
(.NET 8) + PostgreSQL.

## Requisitos

- .NET 8 SDK
- PostgreSQL 14+

## Configuración local

1. Crea la base de datos `estudio_juridico_jym` en PostgreSQL.
2. Ejecuta `schema_completo.sql` sobre esa base (crea todas las tablas,
   roles y tipos de proceso iniciales).
3. Copia tu cadena de conexión real a `DashboardJym/appsettings.Development.json`
   (este archivo no se sube al repositorio):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=estudio_juridico_jym;Username=postgres;Password=TU_PASSWORD"
     }
   }
   ```
4. `cd DashboardJym && dotnet restore && dotnet run`

## Despliegue en Render

El repositorio ya incluye un `Dockerfile` listo para Render:

1. Crea una base de datos PostgreSQL en Render.
2. Crea un Web Service en Render conectado a este repositorio (detecta el
   Dockerfile automáticamente).
3. En las variables de entorno del Web Service, agrega:
   `ConnectionStrings__DefaultConnection` con la cadena de conexión de la
   base de datos de Render (formato Npgsql, con `SSL Mode=Require;Trust Server Certificate=true`).
4. Ejecuta `schema_completo.sql` sobre la base de datos de Render antes del
   primer despliegue (o después, no importa el orden).

## Estructura

- `DashboardJym/` — proyecto ASP.NET Core MVC
- `schema_completo.sql` — script único con todo el esquema de base de datos
- `Dockerfile` — para desplegar en Render u otra plataforma con contenedores
