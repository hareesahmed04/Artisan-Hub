namespace ArtisanHub.Models;

// The same values are enforced by CHECK constraints in the database.

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Artist = "Artist";
    public const string Customer = "Customer";
}

public static class OrderStatus
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = { Pending, Paid, Shipped, Delivered, Cancelled };
}

public static class PaymentStatus
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Refunded = "Refunded";
}

public static class PaymentMethod
{
    public const string COD = "COD";
    public const string Card = "Card";
    public const string JazzCash = "JazzCash";
    public const string EasyPaisa = "EasyPaisa";

    public static readonly string[] All = { COD, Card, JazzCash, EasyPaisa };

    public static string Label(string method) => method switch
    {
        COD => "Cash on delivery",
        Card => "Debit / credit card",
        JazzCash => "JazzCash wallet",
        EasyPaisa => "EasyPaisa wallet",
        _ => method
    };
}

public static class ShipmentStatus
{
    public const string Preparing = "Preparing";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";

    public static readonly string[] All = { Preparing, Shipped, Delivered };
}

public static class RequestStatus
{
    public const string Pending = "Pending";
    public const string Quoted = "Quoted";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
    public const string Completed = "Completed";
}
