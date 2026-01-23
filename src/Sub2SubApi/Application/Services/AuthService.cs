using Amazon.DynamoDBv2.Model;
using Sub2SubApi.Application.Models;
using Sub2SubApi.Data;

namespace Sub2SubApi.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserRepository _repo;

    public AuthService(UserRepository repo)
    {
        _repo = repo;
    }

    public async Task<bool> CreateUserAsync(UserDto user)
    {
        try
        {
            await _repo.CreateUserAsync(user);
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public async Task<UserDto?> AuthenticateAsync(string username, string passwordHash)
    {
        var user = await _repo.GetByUsernameAsync(username);
        if (user is null) return null;
        if (!string.Equals(user.PasswordHash, passwordHash, StringComparison.Ordinal)) return null;
        return user;
    }

    public Task<UserDto?> GetUserByIdAsync(string id)
    {
        return _repo.GetByIdAsync(id);
    }

    public Task<bool> UpdateAvatarUrlAsync(string id, string? avatarUrl)
    {
        return _repo.UpdateAvatarUrlAsync(id, avatarUrl);
    }
}
