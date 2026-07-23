namespace WebAPI.DTOs;

public class IceServersResponse
{
    public List<IceServerDto> IceServers { get; set; } = new();
}

public class IceServerDto
{
    public List<string> Urls { get; set; } = new();
    public string? Username { get; set; }
    public string? Credential { get; set; }
}
