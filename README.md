# Logistics System API

Enterprise-grade Logistics & Supply Chain Management Web API built with **.NET 10 (C# 14)**, **ASP.NET Core**, and **Clean Architecture**.

---

## Features & Architecture

- **Clean Architecture & DDD**: Clear separation into `Domain`, `Application`, `Infrastructure`, and `Api` layers.
- **CQRS & MediatR**: Command and query separation with FluentValidation pipeline behaviors.
- **Authentication & RBAC**: JWT Bearer authentication with refresh tokens and fine-grained Role-Based Access Control (`Admin`, `Dispatcher`, `Driver`, `Customer`).
- **Real-Time Tracking & Telematics**: SignalR Hubs (`/hubs/tracking`, `/hubs/notifications`) for real-time GPS streaming and status broadcasts.
- **Background Jobs**: Hangfire job scheduler with SQL Server storage for automated driver dispatch assignment timeouts and retries.
- **Resilience & Rate Limiting**: Partitioned rate limiting (global, authentication, admin, tracking), exponential backoff retries, and RFC 7807 problem details error handling.
- **Enterprise Observability**: Serilog structured logging, rolling file retention, `X-Correlation-ID` header tracing, and health check probes (`/health/live`, `/health/ready`, `/health`).
- **Production Hardened**: Multi-stage Docker container (`106 MB`), non-root user execution, SQL parameter log masking, and automated GitHub Actions CI/CD workflows.

---

## Getting Started (Local Development)

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server 2022 / LocalDB / Docker](https://www.microsoft.com/sql-server)
- [Docker Desktop](https://www.docker.com/) (optional)

### Running with Docker Compose
```bash
docker-compose up --build
```
API runs on `http://localhost:8080` (or `https://localhost:7001`).

### Running Locally with .NET CLI
```bash
# Restore & build
dotnet restore
dotnet build

# Apply database migrations
dotnet ef database update --project src/LogisticsSystem.Infrastructure --startup-project src/LogisticsSystem.Api

# Run the API
dotnet run --project src/LogisticsSystem.Api
```

### Running Tests
```bash
dotnet test
```
All **461 Unit and Integration Tests** will run against the solution with 100% pass rate.

---

## Production Deployment & Operations

For complete production configuration, Docker container setup, environment variables reference, and operations runbook, refer to the **[Production Deployment & Operations Playbook](docs/DEPLOYMENT_GUIDE.md)**.