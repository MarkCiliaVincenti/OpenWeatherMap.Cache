# OpenWeatherMap.Cache
A library that allows you to cache temperature, humidity and pressure readings for [OpenWeatherMap](https://openweathermap.org/), with in-built resiliency that can extend the cache lifetime.

## Initialization with Dependency Injection
In your Startup.cs (ConfigureServices):
```c#
services.AddOpenWeatherMapCache("[API KEY]", 9_500, 300_000);
```

Then you can inject IOpenWeatherMapCache.

## Initialization without Dependency Injection
Create your own instance:
```c#
var openWeatherMapCache = new OpenWeatherMapCache("[API KEY]", 9_500, 300_000);
```

## Usage
```c#
var location = new OpenWeatherMap.Cache.Location(47.6371, -122.1237);
if (openWeatherMapCache.TryGetReadings(location, out var readings))
{
	...
}
```