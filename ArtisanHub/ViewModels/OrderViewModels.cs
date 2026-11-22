using System.ComponentModel.DataAnnotations;
using ArtisanHub.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArtisanHub.ViewModels;

public class CartLineVm
{
    public int ProductID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int StockQty { get; set; }
    public bool Available { get; set; }
    public decimal LineTotal => Price * Quantity;
    public bool HasProblem => !Available || Quantity > StockQty;
}

public class CartGroupVm
{
    public int ArtistID { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public List<CartLineVm> Lines { get; set; } = new();
    public decimal Subtotal => Lines.Sum(l => l.LineTotal);
}

public class CartVm
{
    public List<CartGroupVm> Groups { get; set; } = new();
    public decimal ItemsTotal => Groups.Sum(g => g.Subtotal);
    public decimal Shipping { get; set; }
    public decimal Total => ItemsTotal + Shipping;
    public int ItemCount => Groups.Sum(g => g.Lines.Sum(l => l.Quantity));
    public bool HasProblems => Groups.Any(g => g.Lines.Any(l => l.HasProblem));
    public bool IsEmpty => Groups.Count == 0;
}

public class CheckoutVm
{
    public CartVm Cart { get; set; } = new();
    public List<Address> Addresses { get; set; } = new();
    public PlaceOrderVm Form { get; set; } = new();
    public IEnumerable<SelectListItem> Cities { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class PlaceOrderVm
{
    public int? AddressID { get; set; }

    /// <summary>COD, Card, JazzCash or EasyPaisa.</summary>
    [Required(ErrorMessage = "Please choose a payment method.")]
    public string Method { get; set; } = "COD";

    public bool UseNewAddress { get; set; }

    [StringLength(200)]
    public string? NewAddressLine { get; set; }

    public int? NewCityID { get; set; }

    [StringLength(10)]
    public string? NewPostalCode { get; set; }
}

public class OrderListItemVm
{
    public int OrderID { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal Total { get; set; }
    public string Method { get; set; } = string.Empty;
    public List<string> Thumbnails { get; set; } = new();
}

public class OrderDetailsVm
{
    public Order Order { get; set; } = null!;
    public decimal ItemsTotal { get; set; }
    public decimal Total { get; set; }
    public bool CanCancel { get; set; }
    public bool JustPlaced { get; set; }
    public HashSet<int> ReviewableProductIds { get; set; } = new();
    public HashSet<int> ReviewedProductIds { get; set; } = new();
    public Dictionary<int, string> Images { get; set; } = new();
}

public class CustomRequestFormVm
{
    [Required(ErrorMessage = "Please choose an artist."), Display(Name = "Artist")]
    public int? ArtistID { get; set; }

    [Display(Name = "Category")]
    public int? CategoryID { get; set; }

    [Required, StringLength(2000, MinimumLength = 20, ErrorMessage = "Please describe the piece in at least 20 characters.")]
    [Display(Name = "Describe the piece you want")]
    public string Description { get; set; } = string.Empty;

    [Range(100, 10000000, ErrorMessage = "Budget must be between Rs. 100 and Rs. 10,000,000.")]
    [Display(Name = "Budget (Rs.)")]
    public decimal? Budget { get; set; }

    [DataType(DataType.Date), Display(Name = "Needed by")]
    public DateTime? RequiredBy { get; set; }

    [ValidateNever]
    public IEnumerable<SelectListItem> Artists { get; set; } = Enumerable.Empty<SelectListItem>();

    [ValidateNever]
    public IEnumerable<SelectListItem> Categories { get; set; } = Enumerable.Empty<SelectListItem>();

    public string? ProductTitle { get; set; }
}
