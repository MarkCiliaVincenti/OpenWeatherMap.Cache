using System;

namespace OpenWeatherMap.Cache.Models
{
    /// <summary>
    /// Class for exceptions during API calls
    /// </summary>
    [Serializable]
    public sealed class OpenWeatherMapCacheException : Exception
    {
        /// <summary>
        /// The error code returned by the API
        /// </summary>
        public int? ApiErrorCode { get; internal set; }

        /// <summary>
        /// The error message returned by the API
        /// </summary>
        public string ApiErrorMessage { get; internal set; }

        internal OpenWeatherMapCacheException()
        { }

        internal OpenWeatherMapCacheException(string message)
            : base($"Exception during API call: {message}")
        { }

        internal OpenWeatherMapCacheException(ApiErrorResult apiErrorResult)
            : base($"Exception during API call: {apiErrorResult.Message}")
        {
            ApiErrorCode = apiErrorResult.Cod;
            ApiErrorMessage = apiErrorResult.Message;
        }
    }
}
