# CompuCare API — UniLink

![Backend CI](https://github.com/upc-pre-202620-1asi0730-8093-UniLink/compucare-api/actions/workflows/dotnet-ci.yml/badge.svg)

Backend REST de **CompuCare**, construido en **C# / ASP.NET Core 10 / Entity Framework Core / PostgreSQL** mediante un **monolito modular DDD**. Se ha utilizado como referencia metodológica el patrón de Bounded Contexts, Commands/Queries, repositorios y REST Resources de los proyectos académicos *Catch-Up Platform* y *Learning Center Platform*. El código se ha desarrollado específicamente para el dominio CompuCare.

**Repositorio frontend:** https://github.com/upc-pre-202620-1asi0730-8093-UniLink/compucare-web-app

## Bounded Contexts

| Prioridad estratégica | Código | Bounded Context | Agregado principal |
| --- | --- | --- | --- |
| **Core** | BC04 | Service Requests | `ServiceRequest` |
| **Core** | BC03 | Subscriptions and Coverage | `Subscription` |
| **Supporting** | BC02 | Companies and Equipment | `Equipment`, `Company` |
| **Supporting** | BC05 | Quotations and Payments | `Quotation` |
| **Generic** | BC01 | Identity and Access | `UserAccount` |

Cada BC contiene clases de dominio, servicios de aplicación y adaptadores REST propios. Los proyectos separan las dependencias técnicamente:

```text
src/
  CompuCare.Domain/              # Domain: Aggregates, Entities, domain rules
  CompuCare.Application/         # Application: Commands, Queries, DTOs, repository ports
  CompuCare.Infrastructure/      # Infrastructure: EF Core, PostgreSQL, security, email
  CompuCare.Api/                 # Interfaces: ASP.NET controllers, JWT, composition
tests/
  CompuCare.Domain.Tests/        # Invariant-focused tests for BC01 through BC05
docs/
  architecture.md
  frontend-contract.md
```

Los directorios de BC se repiten en las distintas capas; **no** representan cinco microservicios desplegados. El frontend Vue 3 está en otro repositorio.

## Ejecución local desde WebStorm

Requisitos: **.NET SDK 10**, PostgreSQL 16+ o Docker, y terminal integrada. No es necesario Visual Studio.

1. Clona este repositorio en WebStorm.
2. Ejecuta `docker compose up -d` para iniciar la base de datos local.
3. Ejecuta `dotnet restore CompuCare.sln`.
4. Ejecuta `dotnet run --project src/CompuCare.Api` (entorno `Development`).
5. Abre `http://localhost:5147/swagger` o `http://localhost:5147/health`.

El archivo `appsettings.Development.json` contiene **únicamente** parámetros de desarrollo. Los valores de producción (JWT, base de datos, SMTP y CORS) se deben configurar mediante secretos del entorno, nunca en GitHub.

### Crear cuentas de demostración

El seed del catálogo de planes es automático en `Development`. La creación de cuentas de demostración es **opcional** y requiere establecer estas variables en el entorno local, sin subir la contraseña al repositorio:

```text
DemoSeed__Enabled=true
DemoSeed__Password=<tu contraseña temporal segura de 12 caracteres o más>
```

Al activarlo **solo en Development**, se crean cuentas de prueba para los roles `sysadmin`, `technician`, `admin` y `employee`, y un equipo asignado. En producción el seed de usuarios está desactivado **por defecto**. Para una demo académica con datos ficticios, existe una opción de arranque explícita y protegida con contraseña privada: `DemoBootstrap__Enabled=true` y `DemoBootstrap__Password` de al menos 16 caracteres. Consulta [Railway Deployment](docs/railway-deployment.md). No la habilites con datos reales. Alternativamente, registra una empresa a través de `POST /api/v1/auth/register`.

### Conectar con el frontend

En el repositorio `compucare-web-app`, agrega a `.env.local`:

```dotenv
VITE_UNILINK_API_URL=http://localhost:5147/api/v1
```

Reinicia Vite tras modificar el entorno. La API utiliza **JWT Bearer**. Las solicitudes y sus datos se comparten por una sola base de datos, pero **el servidor filtra cada consulta por el rol y la empresa del token**. El frontend nunca decide por sí mismo a qué empresa pertenece un ticket.

## REST y documentación

Los endpoints compatibles con el frontend se describen en [docs/frontend-contract.md](docs/frontend-contract.md).

| BC | Funciones |
| --- | --- |
| BC01 | Login, registro, recuperación, empleados, técnicos, perfil |
| BC02 | Equipos, sedes, asignaciones, historial |
| BC03 | Planes, cobertura, suscripciones, renovación, mantenimientos |
| BC04 | Crear y consultar tickets, asignar técnico, diagnosticar y cerrar |
| BC05 | Crear/aprobar/rechazar cotizaciones y registrar pagos de prueba |

Los endpoints se exponen bajo `/api/v1/`; Swagger está disponible en `/swagger` solo en Development.

## Verificación

```bash
dotnet build CompuCare.sln
dotnet test CompuCare.sln
```

GitHub Actions compila y ejecuta las pruebas unitarias. La comprobación de migraciones valida la creación del modelo EF Core.

Se añadió una **prueba HTTP de integración con PostgreSQL** en `scripts/smoke_cross_accounts.py`, ejecutada por `.github/workflows/integration-ci.yml`. Comprueba el siguiente flujo con JWT, cuatro cuentas y una misma base de datos: empleado registra ticket → administrador UniLink asigna técnico → técnico diagnostica y cotiza → administrador empresarial aprueba → técnico finaliza → empleado ve el estado y se descuenta exactamente una hora de cobertura. Además, verifica que un empleado no pueda asignar técnicos ni aprobar cotizaciones.

**La prueba necesita PostgreSQL y el seed de desarrollo**. No es prueba de despliegue a Internet: la API todavía debe publicarse y conectarse a Netlify.

El catálogo de desarrollo se corresponde con la landing pública: **Básico S/240 (4 horas, 1 visita)**, **Empresarial S/550 (8 horas, 2 visitas)**, **Integral S/600 (16 horas, 4 visitas)**. Si ya existe una base local creada con valores anteriores, ajusta los planes mediante una migración de datos revisada o crea una nueva base **solo de desarrollo**; el seeder no reemplaza planes existentes.

## Seguridad y límites del prototipo

- Password hashes PBKDF2-SHA256 con sal aleatoria y 210 000 iteraciones; contraseñas nunca se almacenan en texto plano.
- JWT firmado con clave configurada externamente en producción; permisos y tenencia validados en Application.
- Recuperación con token de un solo uso y expiración; su valor solo aparece en la respuesta durante Development.
- Envío de emails configurable mediante SMTP: en producción falla explícitamente si no está configurado.
- Los pagos son **simulados**; nunca se recopilan números completos, CVV o vencimiento de tarjetas.
- Las notificaciones internas se persisten en la base de datos; entrega en tiempo real (WebSocket/SSE) y correo transaccional adicional requieren integración posterior.
- No existe todavía un despliegue de producción verificado. La documentación debe distinguir código implementado, tests y servicios desplegados.

## GitFlow

`main` es la versión estable, `dev` la rama de integración y `feature/backend-implement-bc01-bc05` la rama de implementación. Los merges a `dev` o `main` deben realizarse con revisión y CI aprobado.

**Mantenimiento:** antes de desplegar, generar/aplicar migraciones EF Core, configurar CORS, HTTPS, SMTP y secretos JWT seguros, y comprobar los recorridos de los cuatro roles con el frontend.
