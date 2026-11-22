namespace ArtisanHub.Models;

/// <summary>One shipment per artist per order, because each artist ships their own items.</summary>
public class Shipment
{
    public int ShipmentID { get; set; }
    public int OrderID { get; set; }
    public int ArtistID { get; set; }
    public string? CourierName { get; set; }
    public string? TrackingNo { get; set; }
    public string Status { get; set; } = ShipmentStatus.Preparing;
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public Order Order { get; set; } = null!;
    public Artist Artist { get; set; } = null!;
}
