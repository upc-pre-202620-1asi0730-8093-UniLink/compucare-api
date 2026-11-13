# Integración HTTP futura con el frontend

**Atención:** Esta tabla es un contrato **planeado**, no un conjunto de endpoints que este starter ya atienda. Se debe reconciliar con `src/shared/infrastructure/base-api.js` y los clientes HTTP de cada BC antes de desarrollar controladores.

| Contexto | Operación | Endpoint propuesto | Acceso esperado |
| --- | --- | --- | --- |
| BC01 | Login | `POST /api/v1/auth/login` | Público con límite de intentos |
| BC01 | Usuario actual | `GET /api/v1/auth/me` | Autenticado |
| BC02 | Listar equipos | `GET /api/v1/equipment` | Empleado solo propios; admin empresa solo su empresa |
| BC02 | Registrar equipo | `POST /api/v1/equipment` | Admin empresa |
| BC04 | Registrar ticket | `POST /api/v1/tickets` | Empleado dueño del equipo |
| BC04 | Consultar bandeja | `GET /api/v1/tickets` | Filtrado por rol y tenant en el servidor |
| BC04 | Asignar técnico | `POST /api/v1/tickets/{id}/assign` | Administrador UniLink |
| BC04 | Iniciar diagnóstico | `POST /api/v1/tickets/{id}/start` | Técnico asignado |
| BC04 | Completar atención | `POST /api/v1/tickets/{id}/complete` | Técnico asignado; sin cotización pendiente |
| BC03 | Ver cobertura | `GET /api/v1/subscriptions/current` | Admin empresa |
| BC05 | Listar cotizaciones | `GET /api/v1/quotations` | Solo involucrados autorizados |
| BC05 | Aprobar cotización | `POST /api/v1/quotations/{id}/approve` | Responsable de la misma empresa |

## Estados de tickets

Transiciones de la muestra de dominio: `PendingAssignment → Assigned → InDiagnosis → Resolved` y `PendingAssignment → Cancelled`. Los estados del mock actual usan etiquetas españolas. La integración debe establecer **un mapeo explícito de contrato** o migrar ambos lados a enums estables, sin traducir los valores almacenados arbitrariamente.

## Progreso de integración

- [x] Modelo de dominio BC04 de ejemplo en C#.
- [x] PostgreSQL + EF Core configurados como infraestructura.
- [ ] Autenticación y roles implementados y probados.
- [ ] Endpoints BC01–BC05 implementados.
- [ ] Correspondencia exacta de DTOs/estados con la SPA.
- [ ] Persistencia migrada, datos de prueba, pruebas de integración.
- [ ] CORS de producción, despliegue HTTPS, entorno separado.
