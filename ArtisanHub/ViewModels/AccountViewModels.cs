using System.ComponentModel.DataAnnotations;
using ArtisanHub.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArtisanHub.ViewModels;

public class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}

public class RegisterVm
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20), Display(Name = "Phone (optional)")]
    public string? Phone { get; set; }

    [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>"Customer" or "Artist".</summary>
    [Required]
    public string AccountType { get; set; } = RoleNames.Customer;

    [StringLength(100), Display(Name = "Store name")]
    public string? StoreName { get; set; }

    [Display(Name = "City")]
    public int? CityID { get; set; }

    [StringLength(1000), Display(Name = "About your work")]
    public string? Bio { get; set; }

    [ValidateNever]
    public IEnumerable<SelectListItem> Cities { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class ProfileVm
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? Phone { get; set; }
}

public class ChangePasswordVm
{
    [Required, DataType(DataType.Password), Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password), Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Confirm new password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class AddressFormVm
{
    [Required, StringLength(200), Display(Name = "Street address")]
    public string AddressLine { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a city."), Display(Name = "City")]
    public int? CityID { get; set; }

    [StringLength(10), Display(Name = "Postal code")]
    public string? PostalCode { get; set; }

    [Display(Name = "Make this my default address")]
    public bool IsDefault { get; set; }
}

public class ProfilePageVm
{
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public DateTime MemberSince { get; set; }
    public ProfileVm Profile { get; set; } = new();
    public ChangePasswordVm Password { get; set; } = new();
    public AddressFormVm NewAddress { get; set; } = new();
    public List<Address> Addresses { get; set; } = new();
    public HashSet<int> AddressesInUse { get; set; } = new();
    public IEnumerable<SelectListItem> Cities { get; set; } = Enumerable.Empty<SelectListItem>();
}
