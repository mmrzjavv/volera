using MediatR;

namespace Core.Application.Queries;

public class GetCompanyContentQuery : IRequest<IReadOnlyList<CompanyContentBlockDto>>
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}

public class CompanyContentBlockDto
{
    public Guid Id { get; init; }
    public string ContentSnippet { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? JobId { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
}
