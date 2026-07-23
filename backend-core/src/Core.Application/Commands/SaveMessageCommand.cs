using MediatR;
using System;

namespace Core.Application.Commands;

public record SaveMessageCommand(Guid UserId, Guid MessageId) : IRequest<Guid>;
