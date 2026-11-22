namespace ArtisanHub.Models;

public class City
{
    public int CityID { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;

    public ICollection<Artist> Artists { get; set; } = new List<Artist>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
