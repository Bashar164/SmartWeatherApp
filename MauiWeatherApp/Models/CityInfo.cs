namespace MauiWeatherApp.Models;

public class CityInfo
{
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public override string ToString() => $"{Name}, {Country}";
}
