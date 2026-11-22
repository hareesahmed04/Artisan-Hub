namespace ArtisanHub.Models;

/// <summary>Order totals are not stored; they come from the view vw_OrderTotals.</summary>
public class Order
{
    public int OrderID { get; set; }
    public int UserID { get; set; }
    public int AddressID { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = OrderStatus.Pending;
    public decimal ShippingFee { get; set; }

    public User User { get; set; } = null!;
    public Address Address { get; set; } = null!;
    public Payment? Payment { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
