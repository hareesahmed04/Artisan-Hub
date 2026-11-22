namespace ArtisanHub.Models;

// Keyless entity types mapped to the SQL views created by the database script.

/// <summary>vw_OrderTotals</summary>
public class OrderTotal
{
    public int OrderID { get; set; }
    public int UserID { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal ItemsTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>vw_SalesByArtist</summary>
public class SalesByArtist
{
    public int ArtistID { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public int OrdersCount { get; set; }
    public int ItemsSold { get; set; }
    public decimal TotalSales { get; set; }
}

/// <summary>vw_SalesByCategory</summary>
public class SalesByCategory
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int ItemsSold { get; set; }
    public decimal TotalSales { get; set; }
}

/// <summary>vw_SalesByCity</summary>
public class SalesByCity
{
    public int CityID { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public int ArtistCount { get; set; }
    public int ItemsSold { get; set; }
    public decimal TotalSales { get; set; }
}

/// <summary>vw_ProductRatings</summary>
public class ProductRating
{
    public int ProductID { get; set; }
    public decimal AvgRating { get; set; }
    public int ReviewCount { get; set; }
}
