using System.Security.Claims;
using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Services;

namespace ErpCosmetiqueApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentification").RequireAuthorization();

        group.MapPost("/login", async (LoginRequestDto dto, AuthService auth) =>
        {
            var result = await auth.LoginAsync(dto);
            return result is null ? Results.Unauthorized() : Results.Ok(result);
        }).AllowAnonymous();

        group.MapGet("/me", async (ClaimsPrincipal user, AuthService auth) =>
        {
            var id = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = await auth.GetProfileAsync(id);
            return profile is null ? Results.Unauthorized() : Results.Ok(profile);
        }).RequireAuthorization();
    }
}
