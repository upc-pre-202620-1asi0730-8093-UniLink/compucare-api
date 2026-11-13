# Contributing — UniLink CompuCare API

- Base branch: `dev` for integration, `main` for reviewed stable releases.
- Work in feature branches named `feature/bc01-identity`, `feature/bc04-service-requests`, etc.
- Use Conventional Commits (`feat(bc04): ...`, `test(bc03): ...`, `fix(api): ...`).
- A Pull Request must document its bounded context, tests, screenshots/Swagger and any compatibility change to Vue.
- Domain is not allowed to depend on Application, EF Core, ASP.NET Core, Infrastructure, or Vue.
- Do not commit `.env`, credentials, card data or real-user personal data.
