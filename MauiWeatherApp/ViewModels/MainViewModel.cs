using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiWeatherApp.Models;
using Microsoft.Maui.Devices.Sensors;

namespace MauiWeatherApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private string _cityQuery = string.Empty;
    private CityInfo? _selectedCity;
    private WeatherInfo? _currentWeather;
    private bool _isLoading;

    public string CityQuery
    {
        get => _cityQuery;
        set
        {
            if (_cityQuery != value)
            {
                _cityQuery = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<CityInfo> CityResults { get; } = new();

    public CityInfo? SelectedCity
    {
        get => _selectedCity;
        set
        {
            if (_selectedCity != value)
            {
                _selectedCity = value;
                OnPropertyChanged();

                // Automatically fetch weather when a city is chosen
                if (_selectedCity != null)
                    _ = FetchWeatherAsync(_selectedCity.Latitude, _selectedCity.Longitude, $"{_selectedCity.Name}, {_selectedCity.Country}");
            }
        }
    }

    public WeatherInfo? CurrentWeather
    {
        get => _currentWeather;
        set { _currentWeather = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public ICommand GetWeatherByCityCommand { get; }
    public ICommand GetWeatherByLocationCommand { get; }
    public ICommand ShowWeatherForSelectedCityCommand { get; }
    public MainViewModel()
    {
        GetWeatherByCityCommand = new Command(async () => await GetWeatherByCityAsync());
        GetWeatherByLocationCommand = new Command(async () => await GetWeatherForCurrentLocationAsync());
        ShowWeatherForSelectedCityCommand = new Command(async () =>
        {
            if (SelectedCity != null)
                await FetchWeatherAsync(SelectedCity.Latitude, SelectedCity.Longitude, $"{SelectedCity.Name}, {SelectedCity.Country}");
        });
    }

    // 🌍 GPS Mode
    private async Task GetWeatherForCurrentLocationAsync()
    {
        try
        {
            IsLoading = true;

            var request = new GeolocationRequest(GeolocationAccuracy.Medium);
            var location = await Geolocation.GetLocationAsync(request);

            if (location == null)
            {
                await Shell.Current.DisplayAlert("Error", "Unable to get location.", "OK");
                return;
            }

            // Reverse geocode for city name
            var placemarks = await Geocoding.GetPlacemarksAsync(location.Latitude, location.Longitude);
            var place = placemarks?.FirstOrDefault();
            var cityName = place?.Locality ?? "My Location";
            var country = place?.CountryName ?? "";

            await FetchWeatherAsync(location.Latitude, location.Longitude, $"{cityName}, {country}");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // 🏙 Manual City Search
    private async Task GetWeatherByCityAsync()
    {
        if (string.IsNullOrWhiteSpace(CityQuery))
        {
            await Shell.Current.DisplayAlert("Info", "Please enter a city name.", "OK");
            return;
        }

        try
        {
            IsLoading = true;
            CityResults.Clear();

            using var http = new HttpClient();
            var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={CityQuery}&count=5&language=en";
            var json = await http.GetStringAsync(geoUrl);
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("results", out var results))
            {
                foreach (var r in results.EnumerateArray())
                {
                    CityResults.Add(new CityInfo
                    {
                        Name = r.GetProperty("name").GetString() ?? "",
                        Country = r.GetProperty("country").GetString() ?? "",
                        Latitude = r.GetProperty("latitude").GetDouble(),
                        Longitude = r.GetProperty("longitude").GetDouble()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // 🧭 Fetch weather by coordinates
    private async Task FetchWeatherAsync(double lat, double lon, string locationLabel)
    {
        try
        {
            using var http = new HttpClient();
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current_weather=true";
            var json = await http.GetFromJsonAsync<JsonElement>(url);

            if (json.TryGetProperty("current_weather", out var cw))
            {
                int code = cw.GetProperty("weathercode").GetInt32();
                CurrentWeather = new WeatherInfo
                {
                    Location = locationLabel,
                    Temperature = cw.GetProperty("temperature").GetDouble(),
                    WindSpeed = cw.GetProperty("windspeed").GetDouble(),
                    Condition = WeatherDescription(code),
                    Icon = WeatherIcon(code)
                };
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private static string WeatherDescription(int code) => code switch
    {
        0 => "Clear sky",
        1 or 2 or 3 => "Partly cloudy",
        45 or 48 => "Foggy",
        51 or 53 or 55 => "Drizzle",
        61 or 63 or 65 => "Rain",
        71 or 73 or 75 => "Snow",
        95 => "Thunderstorm",
        _ => "Unknown"
    };

    private static string WeatherIcon(int code) => code switch
    {
        0 => "sunny.png",
        1 or 2 or 3 => "partlycloudy.png",
        45 or 48 => "fog.png",
        51 or 53 or 55 => "drizzle.png",
        61 or 63 or 65 => "rain.png",
        71 or 73 or 75 => "snow.png",
        95 => "thunder.png",
        _ => "unknown.png"
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}