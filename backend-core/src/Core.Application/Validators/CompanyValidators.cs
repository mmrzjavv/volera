using Core.Application.Commands;
using Core.Application.Queries;
using FluentValidation;

namespace Core.Application.Validators;

// ----- Company registration & login -----
public class CompanyLoginCommandValidator : AbstractValidator<CompanyLoginCommand>
{
    public CompanyLoginCommandValidator()
    {
        RuleFor(x => x.MobileNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Token).NotEmpty().MaximumLength(50);
    }
}

// ----- Branch -----
public class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Address));
        RuleFor(x => x.PhoneNumber).MaximumLength(30).When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        RuleFor(x => x.Email).MaximumLength(256).When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Name));
        RuleFor(x => x.Address).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Address));
        RuleFor(x => x.PhoneNumber).MaximumLength(30).When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        RuleFor(x => x.Email).MaximumLength(256).When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public class DeleteBranchCommandValidator : AbstractValidator<DeleteBranchCommand>
{
    public DeleteBranchCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

// ----- Company -----
public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Name));
        RuleFor(x => x.Email).MaximumLength(256).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Address).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Address));
        RuleFor(x => x.LogoUrl).MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.LogoUrl));
    }
}

// ----- Support users -----
public class CreateSupportUserCommandValidator : AbstractValidator<CreateSupportUserCommand>
{
    public CreateSupportUserCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(256).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.PhoneNumber).MaximumLength(30).When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}

public class UpdateSupportUserCommandValidator : AbstractValidator<UpdateSupportUserCommand>
{
    public UpdateSupportUserCommandValidator()
    {
        RuleFor(x => x.SupportUserId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.FirstName));
        RuleFor(x => x.LastName).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.LastName));
        RuleFor(x => x.Email).MaximumLength(256).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.PhoneNumber).MaximumLength(30).When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}

public class DeleteSupportUserCommandValidator : AbstractValidator<DeleteSupportUserCommand>
{
    public DeleteSupportUserCommandValidator()
    {
        RuleFor(x => x.SupportUserId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public class AssignSupportUserToBranchCommandValidator : AbstractValidator<AssignSupportUserToBranchCommand>
{
    public AssignSupportUserToBranchCommandValidator()
    {
        RuleFor(x => x.SupportUserId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public class UnassignSupportUserFromBranchCommandValidator : AbstractValidator<UnassignSupportUserFromBranchCommand>
{
    public UnassignSupportUserFromBranchCommandValidator()
    {
        RuleFor(x => x.SupportUserId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public class SupportUserLoginCommandValidator : AbstractValidator<SupportUserLoginCommand>
{
    public SupportUserLoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public class SupportUserLoginByUsernameCommandValidator : AbstractValidator<SupportUserLoginByUsernameCommand>
{
    public SupportUserLoginByUsernameCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class RefreshSupportUserTokenCommandValidator : AbstractValidator<RefreshSupportUserTokenCommand>
{
    public RefreshSupportUserTokenCommandValidator()
    {
        RuleFor(x => x.AccessToken).NotEmpty();
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

// ----- Company client session & widget -----
public class CreateCompanyClientSessionCommandValidator : AbstractValidator<CreateCompanyClientSessionCommand>
{
    public CreateCompanyClientSessionCommandValidator()
    {
        RuleFor(x => x.WidgetId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.FirstName));
        RuleFor(x => x.LastName).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.LastName));
        RuleFor(x => x.Email).MaximumLength(256).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Mobile).MaximumLength(30).When(x => !string.IsNullOrEmpty(x.Mobile));
    }
}

public class GenerateCompanyWidgetCommandValidator : AbstractValidator<GenerateCompanyWidgetCommand>
{
    public GenerateCompanyWidgetCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class SendCompanyMessageCommandValidator : AbstractValidator<SendCompanyMessageCommand>
{
    public SendCompanyMessageCommandValidator()
    {
        RuleFor(x => x.ClientToken).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(64 * 1024);
        RuleFor(x => x.AttachmentUrl).MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.AttachmentUrl));
        RuleFor(x => x.AttachmentType).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.AttachmentType));
    }
}

// ----- Company queries -----
public class GetCompanyBranchesQueryValidator : AbstractValidator<GetCompanyBranchesQuery>
{
    public GetCompanyBranchesQueryValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public class GetCompanyByIdQueryValidator : AbstractValidator<GetCompanyByIdQuery>
{
    public GetCompanyByIdQueryValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public class GetCompanyWidgetsQueryValidator : AbstractValidator<GetCompanyWidgetsQuery>
{
    public GetCompanyWidgetsQueryValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public class GetWidgetIdByBranchQueryValidator : AbstractValidator<GetWidgetIdByBranchQuery>
{
    public GetWidgetIdByBranchQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class DeleteCompanyWidgetCommandValidator : AbstractValidator<DeleteCompanyWidgetCommand>
{
    public DeleteCompanyWidgetCommandValidator()
    {
        RuleFor(x => x.WidgetId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public class SendSupportReplyCommandValidator : AbstractValidator<SendSupportReplyCommand>
{
    public SendSupportReplyCommandValidator()
    {
        RuleFor(x => x.SupportUserId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.AttachmentUrl).MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.AttachmentUrl));
        RuleFor(x => x.AttachmentType).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.AttachmentType));
    }
}

public class GetCompanyMessagesQueryValidator : AbstractValidator<GetCompanyMessagesQuery>
{
    public GetCompanyMessagesQueryValidator()
    {
        RuleFor(x => x.SupportUserId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, 500);
    }
}

public class GetCompanyClientMessagesQueryValidator : AbstractValidator<GetCompanyClientMessagesQuery>
{
    public GetCompanyClientMessagesQueryValidator()
    {
        RuleFor(x => x.ClientUserId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, 200);
    }
}

public class GetSupportUserBranchesQueryValidator : AbstractValidator<GetSupportUserBranchesQuery>
{
    public GetSupportUserBranchesQueryValidator()
    {
        RuleFor(x => x.SupportUserId).NotEmpty();
    }
}

public class GetSupportBranchMessagesQueryValidator : AbstractValidator<GetSupportBranchMessagesQuery>
{
    public GetSupportBranchMessagesQueryValidator()
    {
        RuleFor(x => x.SupportUserId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, 200);
    }
}

public class GetSupportUsersByCompanyQueryValidator : AbstractValidator<GetSupportUsersByCompanyQuery>
{
    public GetSupportUsersByCompanyQueryValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

// ----- AI Widget -----
public class SetupCompanyAiWidgetCommandValidator : AbstractValidator<SetupCompanyAiWidgetCommand>
{
    public SetupCompanyAiWidgetCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class SubmitCompanyContentCommandValidator : AbstractValidator<SubmitCompanyContentCommand>
{
    public SubmitCompanyContentCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().WithMessage("Content cannot be empty.");
    }
}

public class GetCompanyContentQueryValidator : AbstractValidator<GetCompanyContentQuery>
{
    public GetCompanyContentQueryValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
    }
}
