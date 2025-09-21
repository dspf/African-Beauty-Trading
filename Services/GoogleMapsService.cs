using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace African_Beauty_Trading.Services
{
    public class GoogleMapsService
    {
        private readonly string apiKey;

        public GoogleMapsService()
        {
            apiKey = System.Configuration.ConfigurationManager.AppSettings["AIzaSyD37peVYp50j5FzDnNr6m1JLHGK-UAHUJg"];
        }

        public async Task<LatLng> GetCoordinates(string address)
        {
            string url = $"https://maps.googleapis.com/maps/api/geocode/json?address={HttpUtility.UrlEncode(address)}&key={apiKey}";

            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync(url);
                dynamic json = JsonConvert.DeserializeObject(response);

                if (json.status == "OK")
                {
                    double lat = json.results[0].geometry.location.lat;
                    double lng = json.results[0].geometry.location.lng;

                    return new LatLng { Latitude = lat, Longitude = lng };
                }
            }
            return null;
        }
    }

    public class LatLng
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
