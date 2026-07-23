using MediatR;
using System;

namespace Core.Application.Commands;

public record UnsaveMessageCommand(Guid UserId, Guid MessageId) : IRequest;
