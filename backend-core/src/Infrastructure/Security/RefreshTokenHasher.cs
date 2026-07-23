using System.Security.Cryptography;
using System.Text;
using Core.Application.Interfaces;

namespace Infrastructure.Security;

public class RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken)) return string.Empty;
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
