using ArtisanHub.Data;
using ArtisanHub.Models;
using Microsoft.EntityFrameworkCore;

namespace ArtisanHub.Services;

/// <summary>Placing, shipping and cancelling orders. Each operation runs in a database transaction.</summary>
public class OrderService
{
    /// <summary>Flat delivery charge for each artist's parcel in an order.</summary>
    public const decimal ShippingPerArtist = 250m;

    private readonly ArtisanHubContext _db;

    public OrderService(ArtisanHubContext db)
    {
        _db = db;
    }

    public static decimal ShippingFor(int artistCount) => ShippingPerArtist * artistCount;

    public static bool CanCancel(Order order)
        => (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Paid)
           && order.Shipments.All(s => s.Status == ShipmentStatus.Preparing);

    /// <summary>
    /// Turns the customer's cart into an order: Orders -> OrderItems (stock trigger fires) ->
    /// Payments -> one Shipment per artist -> empty the cart.
    /// </summary>
    public async Task<(int? OrderId, string? Error)> PlaceOrderAsync(int userId, int addressId, string method)
    {
        var cart = await _db.CartItems
            .Include(c => c.Product).ThenInclude(p => p.Artist)
            .Where(c => c.UserID == userId)
            .ToListAsync();

        if (cart.Count == 0) return (null, "Your cart is empty.");

        foreach (var line in cart)
        {
            if (!line.Product.IsActive || !line.Product.Artist.IsVerified)
                return (null, $"\"{line.Product.Title}\" is no longer available. Please remove it from your cart.");
            if (line.Quantity > line.Product.StockQty)
                return (null, $"Only {line.Product.StockQty} left of \"{line.Product.Title}\". Please update your cart.");
        }

        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.AddressID == addressId && a.UserID == userId);
        if (address == null) return (null, "Please choose a delivery address.");
        if (!PaymentMethod.All.Contains(method)) return (null, "Please choose a payment method.");

        var artistIds = cart.Select(c => c.Product.ArtistID).Distinct().ToList();
        var itemsTotal = cart.Sum(c => c.Quantity * c.Product.Price);
        var shipping = ShippingFor(artistIds.Count);
        var paidOnline = method != PaymentMethod.COD;

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                UserID = userId,
                AddressID = address.AddressID,
                OrderDate = DateTime.Now,
                Status = paidOnline ? OrderStatus.Paid : OrderStatus.Pending,
                ShippingFee = shipping
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var line in cart)
            {
                _db.OrderItems.Add(new OrderItem
                {
                    OrderID = order.OrderID,
                    ProductID = line.ProductID,
                    Quantity = line.Quantity,
                    UnitPrice = line.Product.Price
                });
            }
            await _db.SaveChangesAsync(); // trg_OrderItems_ReduceStock lowers the stock here

            _db.Payments.Add(new Payment
            {
                OrderID = order.OrderID,
                Method = method,
                Amount = itemsTotal + shipping,
                Status = paidOnline ? PaymentStatus.Completed : PaymentStatus.Pending,
                PaidAt = paidOnline ? DateTime.Now : null,
                TransactionRef = paidOnline
                    ? $"AH-{method.Substring(0, 2).ToUpperInvariant()}-{Random.Shared.Next(100000, 999999)}"
                    : null
            });

            foreach (var artistId in artistIds)
            {
                _db.Shipments.Add(new Shipment
                {
                    OrderID = order.OrderID,
                    ArtistID = artistId,
                    Status = ShipmentStatus.Preparing
                });
            }

            _db.CartItems.RemoveRange(cart);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return (order.OrderID, null);
        }
        catch (DbUpdateException ex)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch (Exception)
            {
                // SQL Server already rolled the transaction back (the trigger's THROW does that).
            }
            _db.ChangeTracker.Clear();
            var message = ex.InnerException?.Message ?? ex.Message;
            return (null, message.Contains("Not enough stock", StringComparison.OrdinalIgnoreCase)
                ? "Sorry, one of the items just sold out. Please check your cart and try again."
                : "We could not place your order. Please try again.");
        }
    }

    /// <summary>Artist updates the courier details and status of their part of an order.</summary>
    public async Task<string?> UpdateShipmentAsync(int artistId, int shipmentId, string status, string? courier, string? trackingNo)
    {
        if (!ShipmentStatus.All.Contains(status)) return "Choose a valid shipment status.";

        var shipment = await _db.Shipments
            .Include(s => s.Order).ThenInclude(o => o.Shipments)
            .Include(s => s.Order).ThenInclude(o => o.Payment)
            .FirstOrDefaultAsync(s => s.ShipmentID == shipmentId && s.ArtistID == artistId);

        if (shipment == null) return "Shipment not found.";
        if (shipment.Order.Status == OrderStatus.Cancelled) return "This order was cancelled by the customer.";

        courier = string.IsNullOrWhiteSpace(courier) ? null : courier.Trim();
        trackingNo = string.IsNullOrWhiteSpace(trackingNo) ? null : trackingNo.Trim();

        if (status != ShipmentStatus.Preparing && (courier == null || trackingNo == null))
            return "Enter the courier name and tracking number before marking the parcel as shipped.";

        shipment.CourierName = courier;
        shipment.TrackingNo = trackingNo;

        if (status == ShipmentStatus.Preparing)
        {
            shipment.Status = ShipmentStatus.Preparing;
            shipment.ShippedAt = null;
            shipment.DeliveredAt = null;
        }
        else if (status == ShipmentStatus.Shipped)
        {
            shipment.Status = ShipmentStatus.Shipped;
            shipment.ShippedAt ??= DateTime.Now;
            shipment.DeliveredAt = null;
        }
        else
        {
            shipment.Status = ShipmentStatus.Delivered;
            shipment.ShippedAt ??= DateTime.Now;
            shipment.DeliveredAt = DateTime.Now;
        }

        RecalculateOrderStatus(shipment.Order);
        await _db.SaveChangesAsync();
        return null;
    }

    /// <summary>Customer cancels before anything has shipped; stock goes back on the shelf.</summary>
    public async Task<string?> CancelOrderAsync(int userId, int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Shipments)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == userId);

        if (order == null) return "Order not found.";
        if (!CanCancel(order)) return "This order can no longer be cancelled because part of it has already shipped.";

        await using var transaction = await _db.Database.BeginTransactionAsync();

        foreach (var item in order.Items)
        {
            item.Product.StockQty += item.Quantity;
        }

        _db.Shipments.RemoveRange(order.Shipments);
        order.Status = OrderStatus.Cancelled;

        if (order.Payment != null)
        {
            order.Payment.Status = order.Payment.Status == PaymentStatus.Completed
                ? PaymentStatus.Refunded
                : PaymentStatus.Failed;
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return null;
    }

    private static void RecalculateOrderStatus(Order order)
    {
        if (order.Status == OrderStatus.Cancelled) return;

        var shipments = order.Shipments;
        if (shipments.Count > 0 && shipments.All(s => s.Status == ShipmentStatus.Delivered))
        {
            order.Status = OrderStatus.Delivered;
            if (order.Payment != null && order.Payment.Status == PaymentStatus.Pending)
            {
                // Cash on delivery is collected when the last parcel arrives.
                order.Payment.Status = PaymentStatus.Completed;
                order.Payment.PaidAt = DateTime.Now;
            }
        }
        else if (shipments.Any(s => s.Status != ShipmentStatus.Preparing))
        {
            order.Status = OrderStatus.Shipped;
        }
        else
        {
            order.Status = order.Payment?.Status == PaymentStatus.Completed ? OrderStatus.Paid : OrderStatus.Pending;
        }
    }
}
