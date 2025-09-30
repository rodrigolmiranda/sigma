using Sigma.Application.Contracts;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;

namespace Sigma.Application.Queries;

public sealed record GetTenantByIdQuery(Guid Id) : IQuery<Tenant>;

public sealed class GetTenantByIdQueryHandler : IQueryHandler<GetTenantByIdQuery, Tenant>
{
    private readonly ITenantRepository _repository;

    public GetTenantByIdQueryHandler(ITenantRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Tenant>> HandleAsync(GetTenantByIdQuery query, CancellationToken cancellationToken = default)
    {
        var tenant = await _repository.GetByIdAsync(query.Id, cancellationToken);

        if (tenant == null)
        {
            return Error.NotFound($"Tenant with ID '{query.Id}' not found");
        }

        return tenant;
    }
}