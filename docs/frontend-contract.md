# CompuCare REST API — contrato HTTP de integración

**Código implementado en el backend**, salvo las funcionalidades específicamente marcadas como pendientes. La base es `/api/v1`. Los cuerpos/respuestas JSON usan **camelCase**.

## BC01 — Identity and Access

| Método | Ruta | Cuerpo | Acceso |
|---|---|---|---|
| POST | `/auth/login` | `{email,password}` | Público |
| POST | `/auth/register` | `{company:{name,ruc},admin:{name,email,password},planId?,payment?:{holder,last4}}` | Público |
| POST | `/auth/forgot-password` | `{email}` | Público |
| POST | `/auth/reset-password` | `{token,password}` | Público |
| GET | `/auth/me` | — | Autenticado |
| GET | `/employees` | — | Admin de empresa |
| POST | `/employees` | `{name,email,position?}` | Admin de empresa |
| GET | `/technicians` | — | Sysadmin UniLink |
| PATCH | `/users/{id}` | `{name,phone?}` | El mismo usuario |
| PUT | `/users/{id}/password` | `{currentPassword,newPassword}` | El mismo usuario |

La respuesta de login es `{token,tokenType,user}`, con `user:{id,companyId,name,email,role,phone,position}`. Los roles son `sysadmin`, `technician`, `admin`, `company_manager` y `employee`; no se confía en el rol enviado por el navegador.

## BC02 — Companies and Equipment

| Método | Ruta | Cuerpo | Acceso |
|---|---|---|---|
| GET | `/locations` | — | Mi empresa |
| POST | `/locations` | `{name,desk}` | Admin empresa |
| PATCH | `/locations/{id}` | `{active?,name?,desk?}` | Admin empresa |
| GET | `/equipments` | `?status&type&locationId&q` | Admin: empresa; empleado: solo propios |
| POST | `/equipments` | `{name,serialNumber,type,locationId?}` | Admin empresa |
| PUT | `/equipments/{id}/assign` | `{employeeId}` o `{employeeId:null}` | Admin empresa |
| GET | `/equipments/{id}/history` | — | Admin empresa |

Una sede activa y el empleado de la misma compañía son precondiciones obligatorias. Los historiales muestran asignaciones, tickets relacionados, cotizaciones y mantenimiento.

## BC03 — Subscriptions and Coverage

| Método | Ruta | Cuerpo | Acceso |
|---|---|---|---|
| GET | `/plans` | — | Público |
| GET | `/subscriptions/balance` | — | Admin empresa |
| POST | `/subscriptions` | `{planId,billing:{name,email}}` | Admin empresa |
| POST | `/subscriptions/renew` | — | Admin empresa |
| GET | `/maintenances` | — | Admin empresa |
| POST | `/maintenances` | `{date,time,equipmentIds:[...]}` | Admin empresa |

`balance` incluye `hoursTotal`, `hoursUsed`, `hoursRemaining`, `percentUsed`, `preventiveTotal`, `preventiveUsed`, `preventiveRemaining`, `startedAt` y `renewsAt`. El consumo por ticket se registra de forma idempotente dentro de `Subscription`.

## BC04 — Service Requests

| Método | Ruta | Cuerpo | Acceso |
|---|---|---|---|
| GET | `/tickets` | — | Empleado: propios; técnico: asignados; admin empresa: su empresa; sysadmin: todos |
| POST | `/tickets` | `{equipmentId,category,description}` | Empleado dueño del equipo |
| PUT | `/tickets/{id}/cancel` | — | Empleado solicitante antes de asignación |
| PUT | `/tickets/{id}/assign` | `{technicianId}` | Sysadmin |
| PUT | `/tickets/{id}/start` | — | Técnico asignado |
| PUT | `/tickets/{id}/diagnosis` | `{diagnosis,workDone,hours}` | Técnico asignado |

**Estados HTTP actuales** (manteniendo el contrato del frontend): `Pendiente de Asignación`, `Asignado`, `En Diagnóstico`, `Atendido`, `Cancelado`. Se preserva el enum interno del agregado; traducir UI **no** modifica los códigos persistidos.

El cierre verifica que no existan cotizaciones pendientes o rechazadas; consume horas de la suscripción y genera una notificación de umbral del 80 %, cuando corresponde.

## BC05 — Quotations and Payments

| Método | Ruta | Cuerpo | Acceso |
|---|---|---|---|
| GET | `/quotes` | — | Admin: empresa; técnico: propias; sysadmin: todas |
| POST | `/quotes` | `{ticketId,items:[{description,cost}]}` | Técnico asignado |
| PUT | `/quotes/{id}/status` | `{status:"APPROVED"|"REJECTED"}` | Admin de la empresa titular |
| POST | `/quotes/{id}/pay` | `{holder,last4}` | Admin de la empresa titular |

Los estados son `PENDING`, `APPROVED`, `REJECTED`, `PAID`. El pago es académico, no financiero; solo registra un recibo simulado.

## Notificaciones compartidas

`GET /notifications` y `PUT /notifications/{id}/read` solo acceden a mensajes del usuario autenticado. No se afirma disponibilidad de emails automáticos sin SMTP, ni WebSockets/SSE.

## Pendientes de integración externa

- Desplegar API HTTPS y PostgreSQL real; configurar `VITE_UNILINK_API_URL` de Netlify.
- Verificar login y flujo completo con los cuatro roles en entornos distintos.
- Crear/aplicar migraciones definitivas y ejecutar smoke tests HTTP.
- Añadir límites de intentos, rotación de tokens, validación exhaustiva y observabilidad de producción.
