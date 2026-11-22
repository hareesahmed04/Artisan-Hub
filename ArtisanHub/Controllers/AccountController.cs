using System.Security.Claims;
using ArtisanHub.Data;
using ArtisanHub.Helpers;
using ArtisanHub.Models;
using ArtisanHub.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArtisanHub.Controllers;

public class AccountController : Controller
{
    private readonly ArtisanHubContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public AccountController(ArtisanHubContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // ------------------------------------------------------------------ login / logout

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToHome();
        return View(new LoginVm { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim();
        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.Artist)
            .FirstOrDefaultAsync(u => u.Email == email);

        var result = user == null
            ? PasswordVerificationResult.Failed
            : _hasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password);

        if (user == null || result == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Incorrect email or password.");
            return View(vm);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account has been deactivated. Please contact the ArtisanHub admin.");
            return View(vm);
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, vm.Password);
            await _db.SaveChangesAsync();
        }

        await SignInAsync(user, vm.RememberMe);
        TempData["Success"] = $"Welcome back, {user.FullName.Split(' ')[0]}!";

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)) return Redirect(vm.ReturnUrl);
        return RedirectForRole(user.Role.RoleName);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "You have been signed out.";
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    // ------------------------------------------------------------------ register

    [HttpGet]
    public async Task<IActionResult> Register(string? type = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToHome();
        var vm = new RegisterVm
        {
            AccountType = type == RoleNames.Artist ? RoleNames.Artist : RoleNames.Customer,
            Cities = await CityOptionsAsync()
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        var isArtist = vm.AccountType == RoleNames.Artist;
        var email = vm.Email.Trim();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            ModelState.AddModelError(nameof(vm.Email), "An account with this email already exists.");

        if (isArtist)
        {
            if (string.IsNullOrWhiteSpace(vm.StoreName))
                ModelState.AddModelError(nameof(vm.StoreName), "Please enter your store name.");
            else if (await _db.Artists.AnyAsync(a => a.StoreName == vm.StoreName.Trim()))
                ModelState.AddModelError(nameof(vm.StoreName), "This store name is already taken.");

            if (vm.CityID == null)
                ModelState.AddModelError(nameof(vm.CityID), "Please choose the city you work in.");
        }

        if (!ModelState.IsValid)
        {
            vm.Cities = await CityOptionsAsync();
            return View(vm);
        }

        var role = await _db.Roles.FirstAsync(r => r.RoleName == (isArtist ? RoleNames.Artist : RoleNames.Customer));
        var user = new User
        {
            Role = role,
            FullName = vm.FullName.Trim(),
            Email = email,
            Phone = string.IsNullOrWhiteSpace(vm.Phone) ? null : vm.Phone.Trim(),
            CreatedAt = DateTime.Now,
            IsActive = true
        };
        user.PasswordHash = _hasher.HashPassword(user, vm.Password);

        if (isArtist)
        {
            user.Artist = new Artist
            {
                StoreName = vm.StoreName!.Trim(),
                CityID = vm.CityID!.Value,
                Bio = string.IsNullOrWhiteSpace(vm.Bio) ? null : vm.Bio.Trim(),
                JoinDate = DateTime.Today,
                IsVerified = false
            };
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await SignInAsync(user, true);
        TempData["Success"] = isArtist
            ? "Your store has been created! You can add products now; they appear in the shop once the admin verifies your store."
            : "Welcome to ArtisanHub! Your account is ready.";

        return RedirectForRole(role.RoleName);
    }

    // ------------------------------------------------------------------ profile

    [Authorize, HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.GetUserId();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == userId);
        if (user == null) return await ForceLogout();

        var addresses = await _db.Addresses
            .Include(a => a.City)
            .Where(a => a.UserID == userId)
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.AddressID)
            .ToListAsync();

        var inUse = await _db.Orders
            .Where(o => o.UserID == userId)
            .Select(o => o.AddressID)
            .Distinct()
            .ToListAsync();

        var vm = new ProfilePageVm
        {
            Email = user.Email,
            RoleName = user.Role.RoleName,
            MemberSince = user.CreatedAt,
            Profile = new ProfileVm { FullName = user.FullName, Phone = user.Phone },
            Addresses = addresses,
            AddressesInUse = inUse.ToHashSet(),
            NewAddress = new AddressFormVm { IsDefault = addresses.Count == 0 },
            Cities = await CityOptionsAsync()
        };
        return View(vm);
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Profile")] ProfileVm vm)
    {
        if (!ModelState.IsValid) return BackToProfile(FirstError());

        var user = await _db.Users.FindAsync(User.GetUserId());
        if (user == null) return await ForceLogout();

        user.FullName = vm.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(vm.Phone) ? null : vm.Phone.Trim();
        await _db.SaveChangesAsync();

        // Refresh the cookie so the new name shows in the header.
        await SignInAsync(await LoadForSignInAsync(user.UserID), true);
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordVm vm)
    {
        if (!ModelState.IsValid) return BackToProfile(FirstError());

        var user = await _db.Users.FindAsync(User.GetUserId());
        if (user == null) return await ForceLogout();

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, vm.CurrentPassword) == PasswordVerificationResult.Failed)
            return BackToProfile("Your current password is not correct.");

        user.PasswordHash = _hasher.HashPassword(user, vm.NewPassword);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Your password has been changed.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize(Roles = RoleNames.Customer), HttpPost]
    public async Task<IActionResult> AddAddress([Bind(Prefix = "NewAddress")] AddressFormVm vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return BackToProfile(FirstError());

        var userId = User.GetUserId();
        var hasAny = await _db.Addresses.AnyAsync(a => a.UserID == userId);
        var makeDefault = vm.IsDefault || !hasAny;

        if (makeDefault)
        {
            await ClearDefaultAsync(userId);
        }

        _db.Addresses.Add(new Address
        {
            UserID = userId,
            CityID = vm.CityID!.Value,
            AddressLine = vm.AddressLine.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(vm.PostalCode) ? null : vm.PostalCode.Trim(),
            IsDefault = makeDefault
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Address saved.";
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Profile));
    }

    [Authorize(Roles = RoleNames.Customer), HttpPost]
    public async Task<IActionResult> SetDefaultAddress(int id)
    {
        var userId = User.GetUserId();
        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.AddressID == id && a.UserID == userId);
        if (address == null) return NotFound();

        await ClearDefaultAsync(userId);
        address.IsDefault = true;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Default address updated.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize(Roles = RoleNames.Customer), HttpPost]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var userId = User.GetUserId();
        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.AddressID == id && a.UserID == userId);
        if (address == null) return NotFound();

        if (await _db.Orders.AnyAsync(o => o.AddressID == id))
            return BackToProfile("This address is used by one of your orders, so it is kept for your records.");

        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync();

        if (address.IsDefault)
        {
            var next = await _db.Addresses.Where(a => a.UserID == userId).OrderBy(a => a.AddressID).FirstOrDefaultAsync();
            if (next != null)
            {
                next.IsDefault = true;
                await _db.SaveChangesAsync();
            }
        }

        TempData["Success"] = "Address removed.";
        return RedirectToAction(nameof(Profile));
    }

    // ------------------------------------------------------------------ helpers

    private async Task ClearDefaultAsync(int userId)
    {
        var defaults = await _db.Addresses.Where(a => a.UserID == userId && a.IsDefault).ToListAsync();
        foreach (var a in defaults) a.IsDefault = false;
    }

    private async Task<User> LoadForSignInAsync(int userId)
        => await _db.Users.Include(u => u.Role).Include(u => u.Artist).FirstAsync(u => u.UserID == userId);

    private async Task SignInAsync(User user, bool persistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.RoleName)
        };
        if (user.Artist != null)
        {
            claims.Add(new Claim(ClaimsExtensions.ArtistIdClaim, user.Artist.ArtistID.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = persistent });
    }

    private async Task<IActionResult> ForceLogout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToHome()
    {
        if (User.IsInRole(RoleNames.Admin)) return RedirectToAction("Index", "Admin");
        if (User.IsInRole(RoleNames.Artist)) return RedirectToAction("Index", "Seller");
        return RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectForRole(string roleName) => roleName switch
    {
        RoleNames.Admin => RedirectToAction("Index", "Admin"),
        RoleNames.Artist => RedirectToAction("Index", "Seller"),
        _ => RedirectToAction("Index", "Home")
    };

    private IActionResult BackToProfile(string error)
    {
        TempData["Error"] = error;
        return RedirectToAction(nameof(Profile));
    }

    private string FirstError()
        => ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
           ?? "Please check the form and try again.";

    private async Task<IEnumerable<SelectListItem>> CityOptionsAsync()
        => await _db.Cities
            .OrderBy(c => c.CityName)
            .Select(c => new SelectListItem(c.CityName + ", " + c.Province, c.CityID.ToString()))
            .ToListAsync();
}
