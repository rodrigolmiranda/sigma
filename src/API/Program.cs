using FluentValidation;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sigma.API.GraphQL;
using Sigma.API.Webhooks;
using Sigma.Application.Behaviors;
using Sigma.Application.Commands;
using Sigma.Application.Contracts;
using Sigma.Application.Queries;
using Sigma.Application.Services;
using Sigma.Application.Validators;
using Sigma.Domain.Contracts;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;
using Sigma.Infrastructure.Persistence;
using Sigma.Infrastructure.Persistence.Repositories;
using Sigma.Infrastructure.Services;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"] ?? "https://localhost:5001";
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });
builder.Services.AddAuthorization();

// Configure Entity Framework with pooled factory for DataLoaders
builder.Services.AddPooledDbContextFactory<SigmaDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Database=sigma;Username=sigma;Password=sigma123";
    options.UseNpgsql(connectionString);
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

// Add scoped DbContext from the factory with tenant context
builder.Services.AddScoped<SigmaDbContext>(provider =>
{
    var factory = provider.GetRequiredService<IDbContextFactory<SigmaDbContext>>();
    var context = factory.CreateDbContext();

    // Inject tenant context if available
    var tenantContext = provider.GetService<ITenantContext>();
    if (tenantContext != null)
    {
        context.TenantContext = tenantContext;
    }

    return context;
});

// Register repositories
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
builder.Services.AddScoped<IChannelRepository, ChannelRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITransactionManager, TransactionManager>();

// Register database initializer for development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DatabaseInitializer>();
}

// Register tenant context
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Register correlation context
builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();

// Register CQRS infrastructure
builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();

// Register behaviors (order matters - they execute in reverse order)
builder.Services.AddScoped<ICommandBehavior, TransactionBehavior>();
builder.Services.AddScoped<ICommandBehavior, LoggingBehavior>();
builder.Services.AddScoped<ICommandBehavior, AuthorizationBehavior>();
builder.Services.AddScoped<ICommandBehavior, ValidationBehavior>();

builder.Services.AddScoped<IQueryBehavior, LoggingBehavior>();
builder.Services.AddScoped<IQueryBehavior, AuthorizationBehavior>();
builder.Services.AddScoped<IQueryBehavior, ValidationBehavior>();

// Register handlers
builder.Services.AddScoped<ICommandHandler<CreateTenantCommand, Guid>, CreateTenantCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateWorkspaceCommand, Guid>, CreateWorkspaceCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetTenantByIdQuery, Tenant>, GetTenantByIdQueryHandler>();

// Register validators
builder.Services.AddValidatorsFromAssemblyContaining<CreateTenantCommandValidator>();

// Configure GraphQL
builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<TenantType>()
    .AddType<WorkspaceType>()
    .AddType<ChannelType>()
    .AddType<MessageType>()
    .AddDataLoader<Sigma.API.DataLoaders.WorkspaceByTenantIdDataLoader>()
    .AddDataLoader<Sigma.API.DataLoaders.ChannelByWorkspaceIdDataLoader>()
    .AddDataLoader<Sigma.API.DataLoaders.MessageByChannelIdDataLoader>()
    .AddFiltering()
    .AddSorting()
    .AddProjections()
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = builder.Environment.IsDevelopment();
        opt.ExecutionTimeout = TimeSpan.FromSeconds(30);
    })
    .ModifyPagingOptions(opt =>
    {
        opt.MaxPageSize = 100;
        opt.DefaultPageSize = 20;
        opt.IncludeTotalCount = true;
    })
    .AddMaxExecutionDepthRule(7) // Prevent deeply nested queries
    .BindRuntimeType<DateTime, HotChocolate.Types.DateTimeType>()
    .BindRuntimeType<DateTimeOffset, HotChocolate.Types.DateTimeType>();

// Register hosted services
builder.Services.AddHostedService<Sigma.Infrastructure.Services.OutboxProcessorService>();

// Configure rate limiting
builder.Services.AddRateLimiter(options =>
{
    // Global rate limit
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    // GraphQL-specific rate limit (more restrictive)
    options.AddPolicy("graphql", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Webhook rate limit (per tenant)
    options.AddPolicy("webhook", context =>
    {
        var tenantId = context.Request.RouteValues.TryGetValue("tenantId", out var id)
            ? id?.ToString() ?? "unknown"
            : "unknown";

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"webhook:{tenantId}",
            factory: partition => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 1000,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6
            });
    });

    // Mutation rate limit (most restrictive)
    options.AddPolicy("mutation", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new TokenBucketRateLimiterOptions
            {
                AutoReplenishment = true,
                ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                TokenLimit = 10,
                TokensPerPeriod = 5
            }));

    // Configure rejection response
    options.RejectionStatusCode = 429; // Too Many Requests

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
        }

        await context.HttpContext.Response.WriteAsync(
            "Rate limit exceeded. Please try again later.", token);
    };
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SigmaDbContext>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseMiddleware<Sigma.API.Middleware.CorrelationIdMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// GraphQL endpoint with rate limiting
app.MapGraphQL("/graphql")
    .RequireRateLimiting("graphql")
    .WithOptions(new GraphQLServerOptions
    {
        Tool = { Enable = app.Environment.IsDevelopment() }
    });

// Health endpoints (no rate limiting)
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// Webhook endpoints with signature validation and rate limiting
app.MapPost("/webhooks/slack/{tenantId}", async (string tenantId, HttpContext context) =>
{
    var handler = new SlackWebhookHandler(app.Services);
    return await handler.HandleAsync(tenantId, context);
}).RequireRateLimiting("webhook");

app.MapPost("/webhooks/discord/{tenantId}", async (string tenantId, HttpContext context) =>
{
    var handler = new DiscordWebhookHandler(app.Services);
    return await handler.HandleAsync(tenantId, context);
}).RequireRateLimiting("webhook");

app.MapPost("/webhooks/telegram/{botToken}", async (string botToken, HttpContext context) =>
{
    var handler = new TelegramWebhookHandler(app.Services);
    return await handler.HandleAsync(botToken, context);
}).RequireRateLimiting("webhook");

app.MapPost("/webhooks/whatsapp/{tenantId}", async (string tenantId, HttpContext context) =>
{
    var handler = new WhatsAppWebhookHandler(app.Services);
    return await handler.HandleAsync(tenantId, context);
}).RequireRateLimiting("webhook");

app.Run();
