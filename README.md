# CompuCare API — UniLink

Backend inicial para **CompuCare**, proyecto de Aplicaciones Web de UniLink (1ASI0730). 

**Estado:** scaffold arquitectónico DDD, **no** es una API funcional lista para producción. Implementa la estructura por capas, un ejemplo de Aggregate Root, persistencia relacional EF Core y el endpoint de salud. Los endpoints de tickets, autenticación y demás reglas todavía deben desarrollarse y probarse con el frontend oficial.

## Bounded Contexts (orden estratégico)

| Tipo | BC | Responsabilidad |
| --- | --- | --- |
| **Core** | **BC04 Service Requests** | Tickets, asignaciones, diagnóstico y cierre |
| **Core** | **BC03 Subscriptions and Coverage** | Planes, horas de soporte y mantenimientos |
| **Supporting** | **BC02 Companies and Equipment** | Empresas, sedes y activos |
| **Supporting** | **BC05 Quotations and Payments** | Cotizaciones, aprobaciones y pagos simulados |
| **Generic** | **BC01 Identity and Access** | Identidad, autenticación y permisos |

La clasificación no cambia los identificadores del informe. Los módulos no equivalen necesariamente a microservicios: esta solución empieza como **monolito modular DDD**.

## Estructura

```text
CompuCare.sln
src/
  CompuCare.Domain/           # Reglas puras, agregados, Value Objects y Domain Events
  CompuCare.Application/      # Use cases, DTOs y puertos abstractos
  CompuCare.Infrastructure/   # EF Core, PostgreSQL y adaptadores a terceros
  CompuCare.Api/              # Endpoints, validación HTTP, DI y Swagger

tests/
  CompuCare.Domain.Tests/     # Pruebas de invariantes del dominio

docs/
  architecture.md             # Context Map y dependencias
  frontend-contract.md        # Contratos a integrar con la SPA
```

## Requisitos

- .NET SDK 10 (LTS)
- Docker para PostgreSQL de desarrollo, o PostgreSQL 16+ local
- Git

## Primer arranque

```bash
# Crear una copia de la configuración de ejemplo y ajustar valores locales
cp .env.example .env

# Levantar PostgreSQL local (credenciales ficticias exclusivamente para desarrollo)
docker compose up -d

# Exportar la cadena de conexión en una terminal antes de ejecutar la API
# Bash / macOS / Linux:
export ConnectionStrings__CompuCare='Host=localhost;Port=5432;Database=compucare;Username=compucare;Password=compucare_dev_only'

# Windows PowerShell:
# $env:ConnectionStrings__CompuCare='Host=localhost;Port=5432;Database=compucare;Username=compucare;Password=compucare_dev_only'

dotnet restore CompuCare.sln
dotnet build CompuCare.sln
# Aplicar migraciones de EF Core DESPUÉS de generarlas (no vienen incluidas en el scaffold)
dotnet tool install --global dotnet-ef --version 10.0.0
# dotnet ef migrations add InitialCreate --project src/CompuCare.Infrastructure --startup-project src/CompuCare.Api --output-dir Persistence/Migrations
# dotnet ef database update --project src/CompuCare.Infrastructure --startup-project src/CompuCare.Api

dotnet run --project src/CompuCare.Api
```

> `.env` **no se carga automáticamente** en el proceso .NET. Exporta las variables en la terminal, configura tu IDE o utiliza un gestor de secretos. No subas credenciales reales al repositorio.

Endpoints iniciales: `GET /health` y, en modo Development, Swagger `/swagger`. Los endpoints de negocio se implementarán por etapas y **no están disponibles todavía**.

## Orden técnico recomendado

1. BC01: ASP.NET Core Identity, JWT seguro, roles `employee`, `admin`/`company_manager`, `technician`, `sysadmin`; políticas de acceso.
2. BC02: empresas, sedes, equipos y asignaciones, validando aislamiento de empresa.
3. BC04: crear solicitud, bandejas por rol, asignar técnico, iniciar diagnóstico, cerrar, cancelar, timeline, estado de cotización.
4. BC03: planes, coberturas, consumo idempotente de horas, mantenimiento preventivo.
5. BC05: cotizar, aprobar/rechazar, pago **simulado** y restricción de cierre con cotizaciones pendientes.
6. Conectar la SPA `compucare-web-app`, habilitar CORS específico, Swagger, pruebas de integración y despliegue.

### Integración con el frontend

Frontend oficial: https://github.com/upc-pre-202620-1asi0730-8093-UniLink/compucare-web-app

Su cliente HTTP usa `VITE_UNILINK_API_URL`. El contrato propuesto se documenta en [`docs/frontend-contract.md`](docs/frontend-contract.md). El backend debe aplicar autorización en cada consulta y comando, nunca confiar solo en los guards de Vue.

## Advertencias de seguridad

- No hay login, API de tickets ni migraciones iniciales **implementados** en este scaffold.
- Proteger con JWT/roles/tenant filtering antes de publicar endpoints de negocio.
- No copiar contraseñas en texto plano ni la emisión de tokens de demostración del JSON Server antiguo.
- Los pagos son **simulados**. No solicitar ni almacenar datos reales de tarjetas.

## Trabajar con WebStorm

Puedes clonar este repositorio con **WebStorm → Get from VCS** y pegar:

```text
https://github.com/upc-pre-202620-1asi0730-8093-UniLink/compucare-api.git
```

Abre una terminal integrada y utiliza `dotnet restore`, `dotnet build`, `dotnet test` y `dotnet run`. **No es obligatorio abrir Visual Studio**: el archivo `CompuCare.sln` solo agrupa los proyectos de .NET. Para autocompletado, depuración y refactorizaciones avanzadas en C# se recomienda Rider o Visual Studio, pero WebStorm permite editar el código y ejecutar el SDK desde la terminal.

Los cambios futuros deben pasar por ramas `feature/bcXX-*` y Pull Requests al repositorio oficial.
