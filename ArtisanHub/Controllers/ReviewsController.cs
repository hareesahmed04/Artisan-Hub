using ArtisanHub.Data;
using ArtisanHub.Helpers;
using ArtisanHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtisanHub.Controllers;

/// <summary>A customer can review a product once its parcel has been delivered to them.</summary>
[Authorize(Roles = RoleNames.Customer)]
public class ReviewsController : Controller
{
    private readonly ArtisanHubContext _db;

    public ReviewsController(ArtisanHubContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Save(int productId, int rating, string? comment, string? returnUrl)
    {
        var userId = User.GetUserId();
        var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductID == productId);
        if (product == null) return NotFound();

        IActionResult Back() => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Details", "Products", new { id = productId });

        if (rating < 1 || rating > 5)
        {
            TempData["Error"] = "Please choose a rating from 1 to 5 stars.";
            return Back();
        }

        var received = await _db.OrderItems.AnyAsync(oi =>
            oi.ProductID == productId &&
            oi.Order.UserID == userId &&
            oi.Order.Shipments.Any(s => s.ArtistID == product.ArtistID && s.Status == ShipmentStatus.Delivered));

        if (!received)
        {
            TempData["Error"] = "You can review this item after it has been delivered to you.";
            return Back();
        }

        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (comment != null && comment.Length > 1000) comment = comment.Substring(0, 1000);

        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.ProductID == productId && r.UserID == userId);
        if (review == null)
        {
            _db.Reviews.Add(new Review
            {
                ProductID = productId,
                UserID = userId,
                Rating = (byte)rating,
                Comment = comment,
                ReviewDate = DateTime.Now
            });
            TempData["Success"] = "Thanks for your review!";
        }
        else
        {
            review.Rating = (byte)rating;
            review.Comment = comment;
            review.ReviewDate = DateTime.Now;
            TempData["Success"] = "Your review has been updated.";
        }

        await _db.SaveChangesAsync();
        return Back();
    }
}
