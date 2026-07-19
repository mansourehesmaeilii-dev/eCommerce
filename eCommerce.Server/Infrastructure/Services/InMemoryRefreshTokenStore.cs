using System.Collections.Concurrent;
using eCommerce.Server.Application.Interfaces;

namespace eCommerce.Server.Infrastructure.Services;

public class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<string, (string UserId, DateTime Expiry)> _refreshTokens = new();

    public void Set(string refreshToken, string userId, DateTime expiry) =>
        _refreshTokens[refreshToken] = (userId, expiry);

    public bool TryGet(string refreshToken, out string userId, out DateTime expiry)
    {
        if (_refreshTokens.TryGetValue(refreshToken, out var stored))
        {
            userId = stored.UserId;
            expiry = stored.Expiry;
            return true;
        }

        userId = string.Empty;
        expiry = default;
        return false;
    }

    public bool Remove(string refreshToken) =>
        _refreshTokens.TryRemove(refreshToken, out _);
}
