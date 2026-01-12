using System;
using System.Net.Http;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LibreHardwareMonitorPlugin
{
    public class LibreHardwareMonitorClient
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly JavaScriptSerializer _serializer;

        public LibreHardwareMonitorClient(string baseUrl = "http://localhost:8085")
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
            _http = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            _serializer = new JavaScriptSerializer();
        }

        public async Task<(bool ok, double? value, string format, string error)> GetSensorAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return (false, null, null, "id is required");
            var url = $"/Sensor?action=Get&id={Uri.EscapeDataString(id)}";
            HttpResponseMessage resp;
            try
            {
                resp = await _http.GetAsync(url).ConfigureAwait(false);
            }
            catch (Exception ex) { return (false, null, null, ex.Message); }

            if (!resp.IsSuccessStatusCode) return (false, null, null, $"HTTP {(int)resp.StatusCode}");

            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                var obj = _serializer.Deserialize<Dictionary<string, object>>(json);

                // Check if the response contains the expected keys
                if (obj.ContainsKey("value") && obj.ContainsKey("format"))
                {
                    double? value = obj["value"] != null ? Convert.ToDouble(obj["value"]) : (double?)null;
                    string format = obj["format"].ToString();
                    return (true, value, format, null);
                }
                else
                {
                    return (false, null, null, "Unexpected response format");
                }
            }
            catch (Exception ex)
            {
                // Log the full JSON response for debugging
                Console.WriteLine("Invalid JSON Response: " + json);
                return (false, null, null, "invalid json: " + ex.Message);
            }
        }

        public async Task<(bool ok, string error)> SetSensorAsync(string id, string value /* use "null" for clearing */)
        {
            if (string.IsNullOrEmpty(id)) return (false, "id is required");
            var url = $"/Sensor?action=Set&id={Uri.EscapeDataString(id)}&value={Uri.EscapeDataString(value ?? "null")}";
            HttpResponseMessage resp;
            try
            {
                resp = await _http.GetAsync(url).ConfigureAwait(false);
            }
            catch (Exception ex) { return (false, ex.Message); }

            if (!resp.IsSuccessStatusCode) return (false, $"HTTP {(int)resp.StatusCode}");

            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                var obj = _serializer.Deserialize<dynamic>(json);
                if (obj["result"] != "ok")
                {
                    return (false, obj["message"] ?? "unknown error");
                }
                return (true, null);
            }
            catch (Exception ex) { return (false, "invalid json: " + ex.Message); }
        }
    }
}