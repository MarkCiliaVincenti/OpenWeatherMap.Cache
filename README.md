# OpenWeatherMap.Cache
An asynchronous .NET Standard library that allows you to fetch & cache current weather readings for [OpenWeatherMap](https://openweathermap.org/), with in-built resiliency that can extend the cache lifetime.

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

## Usage in async methods (recommended)
```c#
var location = new OpenWeatherMap.Cache.Location(47.6371, -122.1237);
var readings = await openWeatherMapCache.GetReadingsAsync(location);
if (readings.IsSuccessful)
{
	...
}
```

## Usage in non-async methods
```c#
var location = new OpenWeatherMap.Cache.Location(47.6371, -122.1237);
var readings = openWeatherMapCache.GetReadingsAsync(location).Result;
if (readings.IsSuccessful)
{
	...
}
```
