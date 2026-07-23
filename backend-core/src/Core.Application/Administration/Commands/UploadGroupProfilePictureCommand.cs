using MediatR;
using System;

namespace Core.Application.Administration.Commands;

public class UploadGroupProfilePictureCommand : IRequest<string>
{
    public Guid GroupId { get; set; }
    public System.IO.Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
}
