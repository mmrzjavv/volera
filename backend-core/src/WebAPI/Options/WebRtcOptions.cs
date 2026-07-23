namespace WebAPI.Options;

/// <summary>
/// ICE server config for WebRTC (voice / video / screen share).
/// Prefer self-hosted Coturn (Docker). Clients need a host they can reach
/// (public IP / DNS or the same host they use for the API — not "coturn").
/// </summary>
public class WebRtcOptions
{
    public const string SectionName = "WebRtc";

    /// <summary>
    /// When true and no explicit Stun/Turn URLs are set, build ICE URLs from
    /// PublicHost (or the API request Host) + Port using Coturn credentials.
    /// </summary>
    public bool CoturnEnabled { get; set; } = true;

    /// <summary>
    /// Hostname or IP browsers use to reach Coturn (e.g. 185.x.x.x or chat.example.com).
    /// If empty, GetIceServers uses the request Host (same as how users open the app).
    /// </summary>
    public string? PublicHost { get; set; }

    public int Port { get; set; } = 3478;

    /// <summary>STUN URLs, e.g. stun:turn.example.com:3478 — overrides auto Coturn URLs when set.</summary>
    public string[] StunUrls { get; set; } = Array.Empty<string>();

    /// <summary>TURN URLs, e.g. turn:turn.example.com:3478 — overrides auto Coturn URLs when set.</summary>
    public string[] TurnUrls { get; set; } = Array.Empty<string>();

    public string? TurnUsername { get; set; }

    public string? TurnCredential { get; set; }
}
