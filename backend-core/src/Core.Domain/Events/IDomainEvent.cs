using MediatR;

namespace Core.Domain.Events;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}