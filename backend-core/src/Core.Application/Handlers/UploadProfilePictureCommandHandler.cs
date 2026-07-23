using Core.Application.Commands;
using Core.Application.Interfaces;
using MediatR;

namespace Core.Application.Handlers;

public class UploadProfilePictureCommandHandler : IRequestHandler<UploadProfilePictureCommand, string>
{
    private readonly IFileStorageService _fileStorageService;

    public UploadProfilePictureCommandHandler(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    public async Task<string> Handle(UploadProfilePictureCommand request, CancellationToken cancellationToken)
    {
        // Persistable object key (private bucket). Clients receive a resolved URL from the controller.
        return await _fileStorageService.UploadFileAsync(
            request.FileStream, request.FileName, request.ContentType, "avatars");
    }
}
