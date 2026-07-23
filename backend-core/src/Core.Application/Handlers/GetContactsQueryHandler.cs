using MediatR;
using Core.Application.Queries;
using Core.Application.DTOs;
using Core.Domain.Interfaces;
using Core.Domain.Entities;
using AutoMapper;

namespace Core.Application.Handlers;

public class GetContactsQueryHandler : IRequestHandler<GetContactsQuery, IEnumerable<ContactDto>>
{
    private readonly IContactRepository _contactRepository;
    private readonly IMapper _mapper;

    public GetContactsQueryHandler(IContactRepository contactRepository, IMapper mapper)
    {
        _contactRepository = contactRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ContactDto>> Handle(GetContactsQuery request, CancellationToken cancellationToken)
    {
        var contacts = await _contactRepository.GetContactsByUserIdAsync(request.UserId);

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ContactStatus>(request.Status, true, out var statusEnum))
        {
            contacts = contacts.Where(c => c.Status == statusEnum);
        }

        return contacts.Select(c =>
        {
            var dto = _mapper.Map<ContactDto>(c);
            if (c.ContactUser != null)
            {
                dto.ContactUser = _mapper.Map<UserDto>(c.ContactUser);
            }
            return dto;
        });
    }
}
