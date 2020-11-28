# OpenWeatherMap.Cache
An asynchronous .NET Standard library that allows you to fetch & cache current weather readings from the [OpenWeatherMap](https://openweathermap.org/) API, with in-built resiliency that can extend the cache lifetime in case the API is unreachable.

## Installation
The recommended means is to use [NuGet](https://www.nuget.org/packages/OpenWeatherMap.Cache), but you could also download the source code from [here](https://github.com/MarkCiliaVincenti/OpenWeatherMap.Cache/releases).

## Choose the fetch mode
If the time elapsed since the last fetch for the given location exceeds the cache period but is within the resiliency period (i.e. still available in the cache), the API reported measured time in the cache value is sometimes more recent than the latest value fetched from the API.

With *FetchMode.AlwaysUseLastMeasured*, the value still available in the cache is returned. **IMPORTANT:** Frequent calls may impact your API usage.

With *FetchMode.AlwaysUseLastMeasuredButExtendCache* (default), the value still available in the cache is returned but in order to protect impact on your API usage, this setting updates the cache value's fetched date and extends the cache lifetime.

With *FetchMode.AlwaysUseLastFetchedValue*, the last fetched API result is returned anyway, even though it is being reported to be older by the API.            

## Initialization with Dependency Injection
In your Startup.cs (ConfigureServices):
```c#
services.AddOpenWeatherMapCache("[API KEY]", FetchMode.AlwaysUseLastMeasuredButExtendCache, 9_500, 300_000);
```

Then you can inject IOpenWeatherMapCache.

## Initialization without Dependency Injection
Create your own instance:
```c#
var openWeatherMapCache = new OpenWeatherMapCache("[API KEY]", FetchMode.AlwaysUseLastMeasuredButExtendCache, 9_500, 300_000);
```

## Usage in asynchronous methods (recommended)
```c#
var location = new OpenWeatherMap.Cache.Location(47.6371, -122.1237);
var readings = await openWeatherMapCache.GetReadingsAsync(location);
if (readings.IsSuccessful)
{
	...
}
```

## Usage in synchronous methods
```c#
var location = new OpenWeatherMap.Cache.Location(47.6371, -122.1237);
var readings = openWeatherMapCache.GetReadingsAsync(location).Result;
if (readings.IsSuccessful)
{
	...
}
```
