using Sub2SubApi.Application.Models;

namespace Sub2SubApi.Application.Services;

public interface IAuthService
{
    Task<bool> CreateUserAsync(UserDto user);
    Task<UserDto?> AuthenticateAsync(string username, string passwordHash);
    Task<UserDto?> GetUserByIdAsync(string id);
    Task<bool> UpdateAvatarUrlAsync(string id, string? avatarUrl);
}
