using Core.Application.Administration.Commands;
using Core.Application.Interfaces;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Administration.Handlers;

public class UploadGroupProfilePictureCommandHandler : IRequestHandler<UploadGroupProfilePictureCommand, string>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadGroupProfilePictureCommandHandler(
        IFileStorageService fileStorageService,
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork)
    {
        _fileStorageService = fileStorageService;
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(UploadGroupProfilePictureCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(request.GroupId);
        if (group == null)
            throw new KeyNotFoundException("Group not found.");

        var keyPrefix = $"groups/{request.GroupId}";
        var objectKey = await _fileStorageService.UploadFileAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            keyPrefix);

        group.UpdateProfile(group.Name, group.Description, objectKey);
        _groupRepository.Update(group);
        await _unitOfWork.SaveChangesAsync();

        return _fileStorageService.ResolveClientUrl(objectKey) ?? objectKey;
    }
}
