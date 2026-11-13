# Multi-stage, reproducible build for Railway (ASP.NET Core 10).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/CompuCare.Domain/CompuCare.Domain.csproj src/CompuCare.Domain/
COPY src/CompuCare.Application/CompuCare.Application.csproj src/CompuCare.Application/
COPY src/CompuCare.Infrastructure/CompuCare.Infrastructure.csproj src/CompuCare.Infrastructure/
COPY src/CompuCare.Api/CompuCare.Api.csproj src/CompuCare.Api/
RUN dotnet restore src/CompuCare.Api/CompuCare.Api.csproj
COPY src/ src/
RUN dotnet publish src/CompuCare.Api/CompuCare.Api.csproj -c Release --no-restore -o /app/publish --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
# Railway injects PORT. Listen on every interface instead of localhost.
ENTRYPOINT ["sh","-c","exec dotnet CompuCare.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
