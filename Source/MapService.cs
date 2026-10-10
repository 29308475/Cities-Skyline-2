using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CityStreetOverlay
{
    public sealed class ServiceConfig
    {
        public string SearchEndpoint = "https://nominatim.openstreetmap.org/search";
        public string RoadsEndpoint = "https://overpass-api.de/api/interpreter";
    }

    public sealed class MapService : IDisposable
    {
        public static readonly string DataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low",
            "Colossal Order", "Cities Skylines II", "ModsData", "CityStreetOverlay");
        readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(75), MaxResponseContentBufferSize = 32 * 1024 * 1024 };
        readonly SemaphoreSlim gate = new SemaphoreSlim(1);
        readonly ServiceConfig config;
        DateTime nextRequest = DateTime.MinValue;

        public MapService()
        {
            Directory.CreateDirectory(DataDirectory);
            var path = Path.Combine(DataDirectory, "services.json");
            config = File.Exists(path) ? JsonConvert.DeserializeObject<ServiceConfig>(File.ReadAllText(path)) : new ServiceConfig();
            if (config == null) throw new FormatException("services.json ist ungültig.");
            if (!File.Exists(path)) File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CityStreetOverlay/0.5 (CitiesSkylinesII-local-prototype)");
        }

        async Task<string> Request(string endpoint, string parameters, bool post, CancellationToken token)
        {
            var uri = new Uri(endpoint);
            if (uri.Scheme != "https") throw new InvalidOperationException("Kartendienste müssen HTTPS verwenden.");
            string key;
            using (var hash = SHA256.Create()) key = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(endpoint+parameters))).Replace("-", "");
            var file = Path.Combine(DataDirectory, key + ".json");
            await gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (File.Exists(file)) return File.ReadAllText(file);
                var delay = nextRequest - DateTime.UtcNow;
                if (delay > TimeSpan.Zero) await Task.Delay(delay, token).ConfigureAwait(false);
                nextRequest = DateTime.UtcNow.AddSeconds(1.1);
                using (var request = new HttpRequestMessage(post ? HttpMethod.Post : HttpMethod.Get,
                    post ? endpoint : endpoint + "?" + parameters))
                {
                    if (post) request.Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string,string>("data", parameters) });
                    using (var response = await client.SendAsync(request, token).ConfigureAwait(false))
                    {
                        if (!response.IsSuccessStatusCode)
                            throw new HttpRequestException("Kartendienst meldet HTTP " + (int)response.StatusCode + ". Bitte später erneut versuchen.");
                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var parsed = JToken.Parse(content);
                        if (parsed is JObject obj && obj["remark"] != null)
                            throw new InvalidOperationException("Kartendienst: " + obj["remark"]);
                        token.ThrowIfCancellationRequested();
                        File.WriteAllText(file, content);
                        return content;
                    }
                }
            }
            finally { gate.Release(); }
        }

        public async Task<List<Place>> Search(string query, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) throw new ArgumentException("Bitte mindestens zwei Zeichen eingeben.");
            var json = await Request(config.SearchEndpoint, "format=jsonv2&limit=6&featureType=settlement&accept-language=de&q=" + Uri.EscapeDataString(query.Trim()), false, token).ConfigureAwait(false);
            var places = new List<Place>();
            foreach (var item in JArray.Parse(json))
                places.Add(new Place { Name = (string)item["display_name"], Latitude = (double)item["lat"], Longitude = (double)item["lon"] });
            return places;
        }

        public async Task<StreetMap> Load(Place place, double size, CancellationToken token)
        {
            var bounds = Geography.Bounds(place.Latitude, place.Longitude, size);
            // Changed query also gives the cache a new key, so older road-only responses
            // cannot hide the newly supported footways/cycleways.
            var query = "[out:json][timeout:60];way[highway~\"" + RoadTypes.HighwayPattern + "\"](" + bounds + ");out geom;";
            var json = await Request(config.RoadsEndpoint, query, true, token).ConfigureAwait(false);
            return Geography.Parse(json, place, size);
        }

        public void Dispose() { client.Dispose(); }
    }
}
