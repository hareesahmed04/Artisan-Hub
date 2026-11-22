using System.Globalization;

namespace ArtisanHub.Helpers;

/// <summary>Formatting helpers used by the views.</summary>
public static class Fmt
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    public static string Money(decimal value)
        => "Rs. " + (value % 1 == 0 ? value.ToString("N0", Culture) : value.ToString("N2", Culture));

    public static string Money(decimal? value) => value.HasValue ? Money(value.Value) : "-";

    /// <summary>"1 artist" / "3 artists".</summary>
    public static string Plural(int count, string singular, string? plural = null) =>
        $"{count} {(count == 1 ? singular : plural ?? singular + "s")}";

    public static string Date(DateTime value) => value.ToString("dd MMM yyyy", Culture);

    public static string Date(DateTime? value) => value.HasValue ? Date(value.Value) : "-";

    public static string DateTimeText(DateTime value) => value.ToString("dd MMM yyyy, h:mm tt", Culture);

    public static string Rating(decimal? value) => value.HasValue ? value.Value.ToString("0.0", Culture) : "New";

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => char.IsLetter(p[0]))
            .ToArray();
        if (parts.Length == 0) return name.Substring(0, 1).ToUpperInvariant();
        var first = parts[0][0].ToString();
        var second = parts.Length > 1 ? parts[1][0].ToString() : string.Empty;
        return (first + second).ToUpperInvariant();
    }

    /// <summary>CSS class for a coloured status pill.</summary>
    public static string StatusClass(string? status) => status switch
    {
        "Pending" or "Preparing" => "pill-pending",
        "Paid" or "Quoted" => "pill-paid",
        "Shipped" or "Accepted" => "pill-shipped",
        "Delivered" or "Completed" => "pill-done",
        "Cancelled" or "Rejected" or "Refunded" or "Failed" => "pill-stop",
        _ => "pill-pending"
    };

    public static string CategoryImage(int categoryId)
        => categoryId is >= 1 and <= 6 ? $"/images/categories/cat-{categoryId}.svg" : "/images/placeholder.svg";

    public static string CategoryIcon(int categoryId) => categoryId switch
    {
        1 => "bi-cup-hot",
        2 => "bi-flower2",
        3 => "bi-gem",
        4 => "bi-tree",
        5 => "bi-palette",
        6 => "bi-lamp",
        _ => "bi-bag-heart"
    };

    /// <summary>Returns full, half and empty star icon classes for a rating out of 5.</summary>
    public static IEnumerable<string> Stars(decimal? rating)
    {
        var value = rating ?? 0;
        for (var i = 1; i <= 5; i++)
        {
            if (value >= i) yield return "bi-star-fill";
            else if (value >= i - 0.5m) yield return "bi-star-half";
            else yield return "bi-star";
        }
    }
}
