using System.Security.Claims;

namespace ArtisanHub.Helpers;

public static class ClaimsExtensions
{
    public const string ArtistIdClaim = "ArtistID";

    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    }

    public static int? GetArtistId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ArtistIdClaim)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

    public static string GetDisplayName(this ClaimsPrincipal user)
        => user.FindFirst(ClaimTypes.Name)?.Value ?? "Account";
}
