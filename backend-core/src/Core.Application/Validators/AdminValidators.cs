using FluentValidation;
using Core.Application.Administration.Commands;
using Core.Application.Administration.Queries;

namespace Core.Application.Validators;

// Query validators
public class GetAdminUserListQueryValidator : AbstractValidator<GetAdminUserListQuery>
{
    public GetAdminUserListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public class GetAdminUserDetailQueryValidator : AbstractValidator<GetAdminUserDetailQuery>
{
    public GetAdminUserDetailQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class GetAdminChatListQueryValidator : AbstractValidator<GetAdminChatListQuery>
{
    public GetAdminChatListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public class SearchMessagesQueryValidator : AbstractValidator<SearchMessagesQuery>
{
    public SearchMessagesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public class GetSystemLimitsQueryValidator : AbstractValidator<GetSystemLimitsQuery> { }

public class GetUserLimitOverridesQueryValidator : AbstractValidator<GetUserLimitOverridesQuery>
{
    public GetUserLimitOverridesQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class GetEffectiveLimitsQueryValidator : AbstractValidator<GetEffectiveLimitsQuery>
{
    public GetEffectiveLimitsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class GetSystemStatsQueryValidator : AbstractValidator<GetSystemStatsQuery> { }

public class GetUsersOverLimitQueryValidator : AbstractValidator<GetUsersOverLimitQuery>
{
    public GetUsersOverLimitQueryValidator()
    {
        RuleFor(x => x.LimitKey).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetAdminAuditLogQueryValidator : AbstractValidator<GetAdminAuditLogQuery>
{
    public GetAdminAuditLogQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

// Command validators
public class DisableUserCommandValidator : AbstractValidator<DisableUserCommand>
{
    public DisableUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}

public class SuspendUserCommandValidator : AbstractValidator<SuspendUserCommand>
{
    public SuspendUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.Until).GreaterThan(DateTime.UtcNow);
    }
}

public class ReactivateUserCommandValidator : AbstractValidator<ReactivateUserCommand>
{
    public ReactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}

public class SetUserRoleCommandValidator : AbstractValidator<SetUserRoleCommand>
{
    public SetUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => new[] { "User", "Moderator", "Admin", "SuperAdmin" }.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role must be one of: User, Moderator, Admin, SuperAdmin");
    }
}

public class AdminUpdateUserCommandValidator : AbstractValidator<AdminUpdateUserCommand>
{
    public AdminUpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
    }
}

public class AdminEditMessageCommandValidator : AbstractValidator<AdminEditMessageCommand>
{
    public AdminEditMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.NewContent).NotEmpty().MaximumLength(2000);
    }
}

public class AdminDeleteMessageCommandValidator : AbstractValidator<AdminDeleteMessageCommand>
{
    public AdminDeleteMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}

public class SetSystemLimitCommandValidator : AbstractValidator<SetSystemLimitCommand>
{
    public SetSystemLimitCommandValidator()
    {
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.LimitKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
    }
}

public class SetUserLimitOverrideCommandValidator : AbstractValidator<SetUserLimitOverrideCommand>
{
    public SetUserLimitOverrideCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.LimitKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
    }
}

public class RemoveUserLimitOverrideCommandValidator : AbstractValidator<RemoveUserLimitOverrideCommand>
{
    public RemoveUserLimitOverrideCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.LimitKey).NotEmpty().MaximumLength(100);
    }
}

// New monitoring and conversation validators
public class GetAdminConversationQueryValidator : AbstractValidator<GetAdminConversationQuery>
{
    public GetAdminConversationQueryValidator()
    {
        RuleFor(x => x.ConversationKey).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}

public class GetExtendedMonitoringStatsQueryValidator : AbstractValidator<GetExtendedMonitoringStatsQuery> { }

public class GetMessagesPerDayQueryValidator : AbstractValidator<GetMessagesPerDayQuery>
{
    public GetMessagesPerDayQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(1, 365);
    }
}

public class GetMostActiveUsersQueryValidator : AbstractValidator<GetMostActiveUsersQuery>
{
    public GetMostActiveUsersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public class GetMostActiveGroupsQueryValidator : AbstractValidator<GetMostActiveGroupsQuery>
{
    public GetMostActiveGroupsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public class GetTableRowCountsQueryValidator : AbstractValidator<GetTableRowCountsQuery> { }

public class GetUserUsageQueryValidator : AbstractValidator<GetUserUsageQuery>
{
    public GetUserUsageQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public class GetUnreadMessagesCountQueryValidator : AbstractValidator<GetUnreadMessagesCountQuery> { }

public class GetChatByKeyQueryValidator : AbstractValidator<GetChatByKeyQuery>
{
    public GetChatByKeyQueryValidator()
    {
        RuleFor(x => x.ConversationKey).NotEmpty().MaximumLength(200);
    }
}

public class AdminPurgeConversationCommandValidator : AbstractValidator<AdminPurgeConversationCommand>
{
    public AdminPurgeConversationCommandValidator()
    {
        RuleFor(x => x.ConversationKey).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}

public class UploadGroupProfilePictureCommandValidator : AbstractValidator<UploadGroupProfilePictureCommand>
{
    public UploadGroupProfilePictureCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.FileStream).NotNull();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
    }
}
