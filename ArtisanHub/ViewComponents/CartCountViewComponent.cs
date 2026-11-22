using ArtisanHub.Data;
using ArtisanHub.Helpers;
using ArtisanHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtisanHub.ViewComponents;

/// <summary>Shows the number of items in the signed-in customer's cart in the navbar.</summary>
public class CartCountViewComponent : ViewComponent
{
    private readonly ArtisanHubContext _db;

    public CartCountViewComponent(ArtisanHubContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var count = 0;
        if (UserClaimsPrincipal.Identity?.IsAuthenticated == true && UserClaimsPrincipal.IsInRole(RoleNames.Customer))
        {
            var userId = UserClaimsPrincipal.GetUserId();
            count = await _db.CartItems.Where(c => c.UserID == userId).SumAsync(c => (int?)c.Quantity) ?? 0;
        }

        return View(count);
    }
}
