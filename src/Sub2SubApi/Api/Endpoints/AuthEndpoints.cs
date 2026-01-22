using System.Text.Json;
using Sub2SubApi.Application.Models;
using Sub2SubApi.Application.Services;

namespace Sub2SubApi.Api.Endpoints;

public static class AuthEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Map(ApiRouter router, IAuthService authService)
    {
        // POST /auth/signup
        router.Map("POST", "/auth/signup", async ctx =>
        {
            var body = ctx.Request.Body;
            if (string.IsNullOrWhiteSpace(body))
                return HttpResults.BadRequest(new { message = "Body required" });

            SignupRequest? req;
            try
            {
                req = JsonSerializer.Deserialize<SignupRequest>(body, JsonOptions);
            }
            catch
            {
                return HttpResults.BadRequest(new { message = "Invalid JSON body" });
            }

            if (req is null)
                return HttpResults.BadRequest(new { message = "Invalid signup request" });

            var user = new UserDto
            {
                Id = Guid.NewGuid().ToString(),
                Username = req.Username,
                Email = req.Email,
                PasswordHash = req.PasswordHash,
                AvatarUrl = null,
                Credits = 0
                ,
                Type = "User"
            };

            var ok = await authService.CreateUserAsync(user);
            if (!ok) return HttpResults.BadRequest(new { message = "User already exists" });

            return HttpResults.Ok(new AuthResponse { Ok = true, Username = user.Username, Email = user.Email, AvatarUrl = user.AvatarUrl, Credits = user.Credits, Type = user.Type });
        });

        // POST /auth/login
        router.Map("POST", "/auth/login", async ctx =>
        {
            var body = ctx.Request.Body;
            if (string.IsNullOrWhiteSpace(body))
                return HttpResults.BadRequest(new { message = "Body required" });

            LoginRequest? req;
            try
            {
                req = JsonSerializer.Deserialize<LoginRequest>(body, JsonOptions);
            }
            catch
            {
                return HttpResults.BadRequest(new { message = "Invalid JSON body" });
            }

            if (req is null)
                return HttpResults.BadRequest(new { message = "Invalid login request" });

            var user = await authService.AuthenticateAsync(req.Username, req.PasswordHash);
            if (user is null) return HttpResults.BadRequest(new { message = "Invalid credentials" });

            return HttpResults.Ok(new AuthResponse { Ok = true, Username = user.Username, Email = user.Email, AvatarUrl = user.AvatarUrl, Credits = user.Credits, Type = user.Type });
        });
    }
}
