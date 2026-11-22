using ArtisanHub.Data;
using ArtisanHub.Helpers;
using ArtisanHub.Models;
using ArtisanHub.Services;
using ArtisanHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArtisanHub.Controllers;

[Authorize(Roles = RoleNames.Customer)]
public class CheckoutController : Controller
{
    private readonly ArtisanHubContext _db;
    private readonly OrderService _orders;

    public CheckoutController(ArtisanHubContext db, OrderService orders)
    {
        _db = db;
        _orders = orders;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        var vm = await BuildAsync(userId, null);
        if (vm.Cart.IsEmpty)
        {
            TempData["Info"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }
        if (vm.Cart.HasProblems)
        {
            TempData["Error"] = "Some items in your cart need attention before you can check out.";
            return RedirectToAction("Index", "Cart");
        }
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Place(PlaceOrderVm form)
    {
        var userId = User.GetUserId();
        int? addressId = form.AddressID;

        if (form.UseNewAddress || addressId == null)
        {
            if (string.IsNullOrWhiteSpace(form.NewAddressLine) || form.NewCityID == null)
            {
                ModelState.AddModelError(string.Empty, "Please enter the delivery address and city.");
            }
        }

        if (!PaymentMethod.All.Contains(form.Method))
        {
            ModelState.AddModelError(nameof(form.Method), "Please choose a payment method.");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildAsync(userId, form));
        }

        if (form.UseNewAddress || addressId == null)
        {
            var hasDefault = await _db.Addresses.AnyAsync(a => a.UserID == userId && a.IsDefault);
            var address = new Address
            {
                UserID = userId,
                CityID = form.NewCityID!.Value,
                AddressLine = form.NewAddressLine!.Trim(),
                PostalCode = string.IsNullOrWhiteSpace(form.NewPostalCode) ? null : form.NewPostalCode.Trim(),
                IsDefault = !hasDefault
            };
            _db.Addresses.Add(address);
            await _db.SaveChangesAsync();
            addressId = address.AddressID;
        }

        var (orderId, error) = await _orders.PlaceOrderAsync(userId, addressId.Value, form.Method);
        if (error != null || orderId == null)
        {
            TempData["Error"] = error ?? "We could not place your order.";
            return RedirectToAction("Index", "Cart");
        }

        TempData["Success"] = $"Thank you! Order #{orderId} has been placed.";
        return RedirectToAction("Details", "Orders", new { id = orderId, placed = true });
    }

    private async Task<CheckoutVm> BuildAsync(int userId, PlaceOrderVm? form)
    {
        var addresses = await _db.Addresses
            .Include(a => a.City)
            .Where(a => a.UserID == userId)
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.AddressID)
            .ToListAsync();

        form ??= new PlaceOrderVm
        {
            AddressID = addresses.FirstOrDefault()?.AddressID,
            UseNewAddress = addresses.Count == 0,
            Method = PaymentMethod.COD
        };

        return new CheckoutVm
        {
            Cart = await CartController.BuildCartAsync(_db, userId),
            Addresses = addresses,
            Form = form,
            Cities = await _db.Cities
                .OrderBy(c => c.CityName)
                .Select(c => new SelectListItem(c.CityName + ", " + c.Province, c.CityID.ToString()))
                .ToListAsync()
        };
    }
}
