# SIGMA - Social Insights & Group Metrics Analytics

A multi-platform chat intelligence and engagement product built with .NET 10, GraphQL-first architecture, and Clean Architecture principles.

## Overview

SIGMA provides unified intelligence and engagement capabilities across major chat platforms for educators and creators. It ingests conversations from Slack, Discord, Telegram (Phase 1), and expands to Microsoft Teams (Phase 3), offering analytics, polls, safety monitoring, RAG-powered Q&A, and cross-platform broadcasting.

## Architecture

- **GraphQL-First API**: All external interactions via GraphQL (using Hot Chocolate)
- **Clean Architecture**: Strict layering (Domain → Application → Infrastructure → API/Workers)
- **In-house CQRS**: Custom command/query pattern without MediatR
- **Multi-tenancy**: Built-in tenant isolation and scoping
- **Event-Driven**: Domain events with outbox pattern
- **.NET Aspire**: Development orchestration (using .NET 9 host)

## Prerequisites

- [.NET 10 SDK RC1](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (for Aspire host)
- PostgreSQL 14+ (for development)
- Docker Desktop (optional, for containerized development)
- Azure subscription (for production deployment)

## Project Structure

```
sigma/
├── src/
│   ├── Domain/          # Core business logic, entities, aggregates
│   ├── Application/     # CQRS handlers, business orchestration
│   ├── Infrastructure/  # EF Core, repositories, external services
│   ├── API/            # GraphQL endpoints, minimal APIs for webhooks
│   ├── Workers/        # Azure Functions for background processing
│   ├── Shared/         # Cross-cutting contracts (MessageEvent, etc.)
│   └── AppHost/        # .NET Aspire orchestration (NET 9)
├── tests/
│   ├── Sigma.Domain.Tests/
│   ├── Sigma.Application.Tests/
│   └── Sigma.API.Tests/
└── docs/               # Architecture and product documentation
```

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd sigma
```

### 2. Build the Solution

```bash
dotnet build
```

### 3. Run Tests

```bash
dotnet test
```

### 4. Run with Aspire (Development)

```bash
dotnet run --project src/AppHost/Sigma.AppHost.csproj
```

This will orchestrate the entire application stack including PostgreSQL and other dependencies.

### 5. Run API Standalone

```bash
dotnet run --project src/API/Sigma.API.csproj
```

Navigate to `https://localhost:5001/graphql` for the GraphQL playground.

## Core Concepts

### CQRS Pattern

Commands and queries are handled separately:

```csharp
// Command
public class CreateTenantCommand : ICommand { ... }
public class CreateTenantCommandHandler : ICommandHandler<CreateTenantCommand> { ... }

// Query
public class GetTenantByIdQuery : IQuery<TenantDto?> { ... }
public class GetTenantByIdQueryHandler : IQueryHandler<GetTenantByIdQuery, TenantDto?> { ... }
```

### Domain Entities

Core aggregates include:
- `Tenant` - Multi-tenant organization
- `Workspace` - Platform-specific workspace
- `Channel` - Communication channel
- `Message` - Normalized message across platforms

### Message Event Schema

Canonical schema for cross-platform message normalization:

```csharp
public class MessageEvent
{
    public Guid Id { get; set; }
    public Platform Platform { get; set; }
    public string PlatformMessageId { get; set; }
    public MessageSenderInfo Sender { get; set; }
    public MessageEventType Type { get; set; }
    public string? Text { get; set; }
    public DateTime TimestampUtc { get; set; }
    // ... more properties
}
```

## Development Guidelines

### Principles

1. **GraphQL-First**: All external API calls go through GraphQL
2. **No MediatR**: Use in-house CQRS interfaces only
3. **Test-First Development**: Write tests before implementation
4. **Zero Warnings Policy**: Treat warnings as build failures
5. **Tenant Isolation**: Enforce at handler and repository level
6. **Idempotency**: All webhooks and side-effect commands

### Code Conventions

- Command names: `VerbNounCommand` (e.g., `CreatePollCommand`)
- Query names: `Get/Find/ListPrefixQuery` (e.g., `ListChannelsQuery`)
- Handlers: `[Name]Handler` in Application layer
- Tests: Mirror namespace with `Should` naming

### Security

- Input validation on all commands/queries
- GraphQL complexity and depth limits
- Webhook signature validation
- PII redaction in logs
- Tenant-scoped queries

## Configuration

Application settings structure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=sigma;..."
  },
  "GraphQL": {
    "MaxDepth": 10,
    "MaxComplexity": 1000
  },
  "Platforms": {
    "Slack": { ... },
    "Discord": { ... },
    "Telegram": { ... }
  }
}
```

## Testing

- **Unit Tests**: Domain logic and handlers (xUnit)
- **Integration Tests**: GraphQL endpoints with TestServer
- **Contract Tests**: GraphQL schema snapshots
- **Performance Tests**: Query latency benchmarks

Run all tests:
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Deployment

### Azure Resources Required

- App Service (API + Admin)
- Azure Functions (Workers)
- PostgreSQL Flexible Server
- Storage Account (Queues, Blobs)
- Key Vault (Secrets)
- Application Insights (Monitoring)

### CI/CD

GitHub Actions workflow included for:
- Build and test
- Security scanning (CodeQL)
- Azure deployment
- Migration management

## Documentation

- [Architecture Technical](docs/Architecture_Technical.md)
- [Product Requirements](docs/PRD_GroupIntelligence.md)
- [Connector Details](docs/Connectors_WhatsApp_Telegram.md)
- [Security & Compliance](docs/Security_Privacy_Compliance.md)
- [AI Development Guide](docs/AI_Initial_Development_Prompt.md)

## Contributing

Please follow the guidelines in [claude.md](claude.md) for AI-assisted development.

## License

[License information to be added]

## Support

For issues and questions, please refer to the project documentation or create an issue in the repository.