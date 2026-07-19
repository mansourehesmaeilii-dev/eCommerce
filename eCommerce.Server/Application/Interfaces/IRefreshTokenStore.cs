namespace eCommerce.Server.Application.Interfaces;

public interface IRefreshTokenStore
{
    void Set(string refreshToken, string userId, DateTime expiry);
    bool TryGet(string refreshToken, out string userId, out DateTime expiry);
    bool Remove(string refreshToken);
}
