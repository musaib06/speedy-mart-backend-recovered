using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.Config.Configuration;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.Enums;
using Siffrum.Ecom.ServiceModels.v1;
using Siffrum.Ecom.ServiceModels.v1.GeoLocation;
using System.Text.Json;

namespace Siffrum.Ecom.BAL.Base.Location
{
    public class GeoLocationService : SiffrumBalBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly APIConfiguration _apiConfiguration;
        private readonly IDistributedCache _cache;

        public GeoLocationService(IMapper mapper,ApiDbContext context,IHttpClientFactory httpClientFactory,
            APIConfiguration apiConfiguration, IDistributedCache cache
            )
            : base(mapper, context)
        {
            _httpClientFactory = httpClientFactory;
            _apiConfiguration = apiConfiguration;
            _cache = cache;
        }

        public async Task<UserAddressSM?> GetAddressFromLatLongAsync(
            double latitude, double longitude)
        {
            // Round to 4 decimals (~11m precision) so nearby lookups hit cache
            var cacheKey = $"geo:rev:{Math.Round(latitude, 4)}:{Math.Round(longitude, 4)}";
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (cached != null)
                    return System.Text.Json.JsonSerializer.Deserialize<UserAddressSM>(cached);
            }
            catch { }

            var client = _httpClientFactory.CreateClient();

            var apiKey = _apiConfiguration.GoogleCloudLocation.ApiKey;
            var url =
                $"https://maps.googleapis.com/maps/api/geocode/json" +
                $"?latlng={latitude},{longitude}&key={apiKey}";

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Google Geocode Error: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            

            var googleData =
                JsonSerializer.Deserialize<GoogleGeocodeResponse>(json);

            if (googleData?.Results == null || !googleData.Results.Any())
            {
                return new UserAddressSM();
            }

            
            string? pincode = null;
            string? city = null;
            string? state = null;
            string? country = null;
            string? address = null;
            var preferredTypes = new[] { "street_address", "premise", "route" };

            var bestResult = googleData.Results
                .FirstOrDefault(r => r.FormattedAddress != null && preferredTypes.Any(t => r.FormattedAddress.Contains(t)))
                ?? googleData.Results.First();
            if (bestResult.FormattedAddress.Contains(","))
            {
                address = bestResult.FormattedAddress.Substring(bestResult.FormattedAddress.IndexOf(",") + 1).Trim();
            }
            foreach (var geoResult in googleData.Results)
            {
                if (geoResult?.AddressComponents == null)
                    continue;
                foreach (var component in geoResult.AddressComponents)
                {
                    if (component?.Types == null)
                        continue;

                    if (component.Types.Contains("postal_code"))
                        pincode = component.LongName;

                    if (component.Types.Contains("locality"))
                        city = component.LongName;

                    if (component.Types.Contains("administrative_area_level_1"))
                        state = component.LongName;

                    if (component.Types.Contains("country"))
                        country = component.LongName;
                    
                }
            }
           
            var result = new UserAddressSM
            {
                Address = address ?? "",
                Landmark = "",
                Area = "",
                Pincode = pincode ?? "",
                City = city ?? "",
                State = state ?? "",
                Country = country ?? "",
                Latitude = latitude,
                Longitude = longitude,
                IsDefault = false
            };

            // Cache for 24 hours — addresses don't change
            try
            {
                await _cache.SetStringAsync(cacheKey,
                    System.Text.Json.JsonSerializer.Serialize(result),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) });
            }
            catch { }

            return result;
        }
        public async Task<List<UserAddressSM>> GetAddressFromSearchAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return new List<UserAddressSM>();

            var searchCacheKey = $"geo:search:{searchText.Trim().ToLowerInvariant()}";
            try
            {
                var cached = await _cache.GetStringAsync(searchCacheKey);
                if (cached != null)
                    return System.Text.Json.JsonSerializer.Deserialize<List<UserAddressSM>>(cached);
            }
            catch { }

            var client = _httpClientFactory.CreateClient();
            var apiKey = _apiConfiguration.GoogleCloudLocation.ApiKey;

            var url =
                $"https://maps.googleapis.com/maps/api/geocode/json" +
                $"?address={Uri.EscapeDataString(searchText)}&key={apiKey}";

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Google Geocode Error: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            var googleData =
                JsonSerializer.Deserialize<GoogleGeocodeResponse>(json);

            if (googleData?.Results == null || !googleData.Results.Any())
                return new List<UserAddressSM>();

            var addresses = new List<UserAddressSM>();

            foreach (var result in googleData.Results)
            {
                string? pincode = null;
                string? city = null;
                string? state = null;
                string? country = null;

                if (result.AddressComponents != null)
                {
                    foreach (var component in result.AddressComponents)
                    {
                        if (component.Types == null)
                            continue;

                        if (component.Types.Contains("postal_code"))
                            pincode = component.LongName;

                        if (component.Types.Contains("locality"))
                            city = component.LongName;

                        if (component.Types.Contains("administrative_area_level_1"))
                            state = component.LongName;

                        if (component.Types.Contains("country"))
                            country = component.LongName;
                    }
                }

                addresses.Add(new UserAddressSM
                {
                    Address = result.FormattedAddress ?? "",
                    Landmark = "",
                    Area = "",
                    Pincode = pincode ?? "",
                    City = city ?? "",
                    State = state ?? "",
                    Country = country ?? "",
                    Latitude = result.Geometry?.Location?.Lat ?? 0,
                    Longitude = result.Geometry?.Location?.Lng ?? 0,
                    IsDefault = false
                });
            }

            // Cache search results for 1 hour
            try
            {
                await _cache.SetStringAsync(searchCacheKey,
                    System.Text.Json.JsonSerializer.Serialize(addresses),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
            }
            catch { }

            return addresses;
        }
    }
}