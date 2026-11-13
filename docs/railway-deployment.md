# CompuCare API — Railway + PostgreSQL deployment

The **frontend remains on Netlify** at https://roaring-crisp-36cca4.netlify.app. The **backend is deployed separately** to Railway (ASP.NET Core 10) using this repository's root `Dockerfile`.

## 1. Railway project

1. In Railway, create a project from GitHub repository `upc-pre-202620-1asi0730-8093-UniLink/compucare-api` and branch `main`.
2. Add a **PostgreSQL** service to the same project.
3. In the API service's **Variables**, set:

| Railway variable | Example / purpose |
| --- | --- |
| `ConnectionStrings__CompuCare` | `Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}}` |
| `Jwt__SigningKey` | **Strong random secret of at least 32 bytes**. Generate securely in Railway; never commit. |
| `Jwt__Issuer` | `CompuCare.Api` |
| `Jwt__Audience` | `CompuCare.Frontend` |
| `Database__RunMigrations` | `true` — apply versioned EF Core migrations and seed the plan catalog on startup |
| `Cors__AllowedOrigins__0` | `https://roaring-crisp-36cca4.netlify.app` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

The database service name in the variable references must match the name in your Railway project. Do **not** turn on `DemoSeed__Enabled` in production. No default credentials are published.

4. After deployment, **Settings → Networking → Generate Domain**. Copy the service's HTTPS host (e.g. `https://<your-railway-domain>`).
5. Confirm `GET /health` returns JSON with `status: healthy`. Health includes PostgreSQL connectivity; `503` indicates a failing database connection.

Railway runs the service at the `PORT` injected by the platform; the Dockerfile passes it explicitly to ASP.NET Core.

## 2. Connect the existing Netlify frontend

In Netlify, open the site for `compucare-web-app` and configure an environment variable for **production** builds:

```text
VITE_UNILINK_API_URL=https://YOUR-RAILWAY-HTTPS-DOMAIN/api/v1
```

Update the placeholder to the **actual generated Railway domain**, without a trailing slash. Redeploy the frontend; Vite embeds this URL at build time. The project already appends the JWT Bearer token through its shared Axios client.

If the domain points at Railway but the browser refuses requests, verify `Cors__AllowedOrigins__0` matches the **exact frontend origin**, including `https://` and no trailing slash.

## 3. Acceptance / cross-account verification

The four roles do **not** have known production passwords. To create demo users locally, enable `DemoSeed__Enabled=true` in Development with private `DemoSeed__Password` (12+ characters). For a **fictitious academic demonstration on Railway only**, explicitly set `DemoBootstrap__Enabled=true` and a private, random `DemoBootstrap__Password` of at least 16 characters; `Database__RunMigrations=true` must also be enabled. Never publish the password, use real customer data with these accounts, or leave the bootstrap enabled after the demonstration.

The optional academic bootstrap creates the four fixed test emails `soporte@unilink.pe` (sysadmin), `tecnico@unilink.pe` (technician), `admin@mype.pe` (company admin), and `empleada@mype.pe` (employee), all with the private configured bootstrap password. It also creates a fictitious company, subscription and employee-assigned equipment. In a production system with actual customers, provision privileged users through a controlled process and leave `DemoBootstrap__Enabled` off.

Functional sequence: employee logs in and reports an issue → UniLink administrator assigns technician → technician sees the assigned ticket → technician issues any necessary quotation → company administrator approves → technician documents work → employee sees updated status → support-hour balance changes.

## 4. Production notes

- The application automatically runs EF Core migrations only when `Database__RunMigrations=true`, and seeds only the public plan catalog.
- Use one API replica during schema migrations, or perform migrations in a separate release job before scaling.
- Credentials for JWT, PostgreSQL and SMTP must be in Railway variables or other secret storage, not Git.
- Real employee invitations/password-reset emails require `Smtp__Host`, `Smtp__Port`, `Smtp__From`, `Smtp__User` and `Smtp__Password`.
- Swagger is enabled only in Development. Production readiness is exposed through `/health`.
- GitHub Actions can validate builds, but **a successful CI run does not prove a Railway service exists or is healthy**.
