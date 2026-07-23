using MediatR;
using Core.Application.Commands;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class DeleteContactCommandHandler : IRequestHandler<DeleteContactCommand>
{
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteContactCommandHandler(IContactRepository contactRepository, IUnitOfWork unitOfWork)
    {
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteContactCommand request, CancellationToken cancellationToken)
    {
        var contact = await _contactRepository.GetByIdAsync(request.ContactId);

        if (contact == null || contact.OwnerUserId != request.UserId)
        {
            throw new KeyNotFoundException("Contact not found or access denied.");
        }

        _contactRepository.Delete(contact);
        await _unitOfWork.SaveChangesAsync();
    }
}
