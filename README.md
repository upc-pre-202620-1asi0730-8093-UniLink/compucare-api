# CompuCare API — UniLink

![Backend CI](https://github.com/upc-pre-202620-1asi0730-8093-UniLink/compucare-api/actions/workflows/dotnet-ci.yml/badge.svg)

Backend REST de **CompuCare**, construido en **C# / ASP.NET Core 10 / Entity Framework Core / PostgreSQL** mediante un **monolito modular DDD**. Se ha utilizado como referencia metodológica el patrón de Bounded Contexts, Commands/Queries, repositorios y REST Resources de los proyectos académicos *Catch-Up Platform* y *Learning Center Platform*. El código se ha desarrollado específicamente para el dominio CompuCare.

**Repositorio frontend:** https://github.com/upc-pre-202620-1asi0730-8093-UniLink/compucare-web-app

## Reparto de trabajo del equipo (13/11/2026)

**Entrega parcial deliberada:** esta rama restaura **51 de los 68 archivos** del backend original (75 % por cantidad de archivos) en siete commits: base DDD, BC04, BC03, BC02, BC05, BC01 e infraestructura. Los **17 archivos restantes** están en un ZIP entregado al equipo para revisión y aportes en nuevos Pull Requests.

En esta etapa **faltan en el repositorio** pruebas unitarias completas, prueba HTTP entre cuentas, algunos workflows, configuración Docker/Railway, documentación complementaria y el controlador de notificaciones. Por tanto, **no se declara completa la validación del backend ni el despliegue** hasta que se integren esos cambios. Los commits y autores originales siguen visibles; la reorganización no pretende atribuir a terceros el trabajo anterior.

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
  CompuCare.Domain.Tests/        # Project references only; test cases in teammate handoff ZIP
# docs/ pending reintegration by the team
```

Los directorios de BC se repiten en las distintas capas; **no** representan cinco microservicios desplegados. El frontend Vue 3 está en otro repositorio.

## Ejecución local desde WebStorm

Requisitos: **.NET SDK 10**, PostgreSQL 16+ o Docker, y terminal integrada. No es necesario Visual Studio.

1. Clona este repositorio en WebStorm.
2. Inicia una instancia de PostgreSQL 16+ instalada localmente. El archivo `docker-compose.yml` está en el ZIP de tareas de despliegue pendiente de integrar.
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

Al activarlo **solo en Development**, se crean cuentas de prueba para los roles `sysadmin`, `technician`, `admin` y `employee`, y un equipo asignado. En producción el seed de usuarios está desactivado **por defecto**. Para una demo académica con datos ficticios, existe una opción de arranque explícita y protegida con contraseña privada: `DemoBootstrap__Enabled=true` y `DemoBootstrap__Password` de al menos 16 caracteres. La guía de Railway está pendiente de reintegración desde el ZIP del equipo. No la habilites con datos reales. Alternativamente, registra una empresa a través de `POST /api/v1/auth/register`.

### Conectar con el frontend

En el repositorio `compucare-web-app`, agrega a `.env.local`:

```dotenv
VITE_UNILINK_API_URL=http://localhost:5147/api/v1
```

Reinicia Vite tras modificar el entorno. La API utiliza **JWT Bearer**. Las solicitudes y sus datos se comparten por una sola base de datos, pero **el servidor filtra cada consulta por el rol y la empresa del token**. El frontend nunca decide por sí mismo a qué empresa pertenece un ticket.

## REST y documentación

Los endpoints compatibles con el frontend usan `/api/v1`; el contrato detallado está temporalmente en el ZIP asignado al equipo.

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

GitHub Actions ejecuta la compilación .NET. Los casos de prueba unitarios y el workflow de integración con PostgreSQL están asignados al equipo y no se encuentran en esta entrega parcial. No debe interpretarse un `dotnet test` sin casos como cobertura de pruebas aprobada.

La prueba HTTP original del flujo entre cuatro cuentas (`scripts/smoke_cross_accounts.py`) se transfirió al ZIP para que los compañeros la revisen, amplíen y vuelvan a subir con evidencia verificable. Hasta entonces ese flujo **no está validado en esta versión parcial**.

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

`main` contiene la versión parcial resultante de la reorganización; `dev` es una rama de integración previa y las nuevas aportaciones deben entrar mediante ramas `feature/*` y Pull Requests reales, con sus correspondientes comprobaciones.

**Mantenimiento:** antes de desplegar, generar/aplicar migraciones EF Core, configurar CORS, HTTPS, SMTP y secretos JWT seguros, y comprobar los recorridos de los cuatro roles con el frontend.
