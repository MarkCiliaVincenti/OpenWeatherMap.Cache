# ![OpenWeatherMap.Cache](https://raw.githubusercontent.com/MarkCiliaVincenti/OpenWeatherMap.Cache/master/logo32.png) OpenWeatherMap.Cache
[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/MarkCiliaVincenti/OpenWeatherMap.Cache/dotnet.yml?branch=master&logo=github&style=flat)](https://actions-badge.atrox.dev/MarkCiliaVincenti/OpenWeatherMap.Cache/goto?ref=master) [![NuGet](https://img.shields.io/nuget/v/OpenWeatherMap.Cache?label=NuGet&logo=nuget&style=flat)](https://www.nuget.org/packages/OpenWeatherMap.Cache) [![NuGet](https://img.shields.io/nuget/dt/OpenWeatherMap.Cache?logo=nuget&style=flat)](https://www.nuget.org/packages/OpenWeatherMap.Cache) [![Codacy Grade](https://img.shields.io/codacy/grade/97ab1ec47ec4454084fa28069a5070a5?style=flat)](https://app.codacy.com/gh/MarkCiliaVincenti/OpenWeatherMap.Cache/dashboard) [![Codecov](https://img.shields.io/codecov/c/github/MarkCiliaVincenti/OpenWeatherMap.Cache?label=Coverage&logo=codecov&style=flat)](https://app.codecov.io/gh/MarkCiliaVincenti/OpenWeatherMap.Cache)

An asynchronous .NET Standard 2.0 library that allows you to fetch & cache current weather readings from the [OpenWeather](https://openweathermap.org/) API, with built-in resiliency that can extend the cache lifetime in case the API is unreachable.

Supports .NET Framework 4.6.1 or later, .NET Core 2.0 or later, and .NET 5.0 or later.

## Installation
The recommended means is to use [NuGet](https://www.nuget.org/packages/OpenWeatherMap.Cache), but you could also download the source code from [here](https://github.com/MarkCiliaVincenti/OpenWeatherMap.Cache/releases).

## Choose the fetch mode
If the time elapsed since the last fetch for the given location exceeds the cache period (i.e. a new API call is required) but is within the resiliency period (i.e. the previous readings are still available in the cache), the API reported measured time in the cache value is sometimes more recent than the latest value fetched from the API. There are three modes to tackle this issue:

With *FetchMode.AlwaysUseLastMeasured*, the value still available in the cache is returned. The implication is that if you immediately request another reading it will also hit the API again. **IMPORTANT:** Frequent calls may impact your API usage.

With *FetchMode.AlwaysUseLastMeasuredButExtendCache* (default, recommended), the value still available in the cache is returned but in order to protect impact on your API usage, this setting updates the cache value's fetched date and extends the cache lifetime.

With *FetchMode.AlwaysUseLastFetchedValue*, the last fetched API result is returned anyway, even though it is being reported to be older by the API.

## Initialization with Dependency Injection
In your Startup.cs (ConfigureServices):
```csharp
services.AddOpenWeatherMapCache("[API KEY]", 9_500, FetchMode.AlwaysUseLastMeasuredButExtendCache, 300_000);
```

Then you can inject IOpenWeatherMapCache.

## Initialization without Dependency Injection
Create your own instance:
```csharp
var openWeatherMapCache = new OpenWeatherMapCache("[API KEY]", 9_500, FetchMode.AlwaysUseLastMeasuredButExtendCache, 300_000);
```

## Usage in asynchronous methods (recommended)
```csharp
var locationQuery = new OpenWeatherMap.Cache.Models.Location(47.6371, -122.1237);
var readings = await openWeatherMapCache.GetReadingsAsync(locationQuery);
if (readings.IsSuccessful)
{
    ...
}
else
{
    var apiErrorCode = readings.Exception?.ApiErrorCode;
    var apiErrorMessage = readings.Exception?.ApiErrorMessage;
}
```

or by zip code (post code) and country code:
```csharp
var locationQuery = new OpenWeatherMap.Cache.Models.ZipCode("94040", "us");
var readings = await openWeatherMapCache.GetReadingsAsync(locationQuery);
if (readings.IsSuccessful)
{
    ...
}
else
{
    var apiErrorCode = readings.Exception?.ApiErrorCode;
    var apiErrorMessage = readings.Exception?.ApiErrorMessage;
}
```

## Usage in synchronous methods
```csharp
var locationQuery = new OpenWeatherMap.Cache.Models.Location(47.6371, -122.1237);
var readings = openWeatherMapCache.GetReadings(locationQuery);
if (readings.IsSuccessful)
{
    ...
}
else
{
    var apiErrorCode = readings.Exception?.ApiErrorCode;
    var apiErrorMessage = readings.Exception?.ApiErrorMessage;
}
```

or by zip code (post code) and country code:
```csharp
var locationQuery = new OpenWeatherMap.Cache.Models.ZipCode("94040", "us");
var readings = openWeatherMapCache.GetReadings(locationQuery);
if (readings.IsSuccessful)
{
    ...
}
else
{
    var apiErrorCode = readings.Exception?.ApiErrorCode;
    var apiErrorMessage = readings.Exception?.ApiErrorMessage;
}
```
