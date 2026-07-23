namespace WebAPI.Services;

public interface IRequestContextService
{
    RequestContextInfo GetRequestContext();
}

public record RequestContextInfo(
    string DeviceType,
    string Browser,
    string OS,
    string Location,
    string? AppVersion);
