using MediatR;

namespace Core.Application.Commands;

public class SubmitCompanyContentCommand : IRequest<SubmitCompanyContentResult>
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class SubmitCompanyContentResult
{
    public Guid JobId { get; init; }
    public Guid ContentBlockId { get; init; }
}
