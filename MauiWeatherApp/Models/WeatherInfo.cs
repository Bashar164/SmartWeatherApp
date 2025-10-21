using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiWeatherApp.Models;

public class WeatherInfo
{
    public string Location { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double WindSpeed { get; set; }
    public double Humidity { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}