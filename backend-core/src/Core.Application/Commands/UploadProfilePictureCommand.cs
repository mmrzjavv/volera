using MediatR;
using System.IO;

namespace Core.Application.Commands;

public class UploadProfilePictureCommand : IRequest<string>
{
    public required Stream FileStream { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
}
