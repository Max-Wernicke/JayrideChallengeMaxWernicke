using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;


namespace Jayride_Coding_Challenge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MainController : ControllerBase
    {

        private const string IpStackAddress = "http://api.ipstack.com/";
        private const string JayrideAddress = "https://jayridechallengeapi.azurewebsites.net/";
        private const string IpStackAccessKey = "ccc792e41bf8828a69ae127d391f9b13";

        [Route("candidate")]
        [HttpGet]
        public IActionResult GetCandidate()
        {
            var candidate = new { name = "test", phone = "test" };
            return Ok(candidate);
        }

        [Route("location")]
        [HttpGet]
        public async Task<IActionResult> GetLocation(string ip)
        {
            if (string.IsNullOrEmpty(ip))
            {
                return BadRequest("IP address must not be empty");
            }

            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri(IpStackAddress);
                    var response = await client.GetAsync($"{ip}?access_key={IpStackAccessKey}");
                    response.EnsureSuccessStatusCode();

                    var stringResult = await response.Content.ReadAsStringAsync();
                    var rawData = JsonConvert.DeserializeObject<dynamic>(stringResult);
                    string city = rawData.city;

                    return Ok(city);
                }
                catch (HttpRequestException httpRequestException)
                {
                    return BadRequest($"Error getting location for IP address: {httpRequestException.Message}");
                }
            }
        }

        [Route("listings")]
        [HttpGet]
        public async Task<IActionResult> GetListings(int passengers)
        {
            if (passengers <= 0)
            {
                return BadRequest("Number of passengers must be higher than 0");
            }

            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri(JayrideAddress);
                    var response = await client.GetAsync("api/QuoteRequest");
                    response.EnsureSuccessStatusCode();

                    var stringResult = await response.Content.ReadAsStringAsync();
                    var rawData = JsonConvert.DeserializeObject<QuoteRequest>(stringResult);

                    if (rawData == null || rawData.listings == null)
                    {
                        return BadRequest("Error: The response is not in the expected format.");
                    }

                    var filteredListings = rawData.listings
                        .Where(x => x.vehicleType.maxPassengers >= passengers)
                        .Select(x => new Listing
                        {
                            name = x.name,
                            pricePerPassenger = x.pricePerPassenger,
                            vehicleType = x.vehicleType,
                            TotalPrice = x.pricePerPassenger * passengers
                        })
                        .OrderBy(x => x.TotalPrice);

                    if (!filteredListings.Any())
                    {
                        return NotFound("No listings found for the number of passengers selected.");
                    }

                    return Ok(filteredListings);
                }
                catch (HttpRequestException httpRequestException)
                {
                    return BadRequest($"Error getting listings: {httpRequestException.Message}");
                }
            }
        }
        public class QuoteRequest
        {
            public string from { get; set; }
            public string to { get; set; }

            public List<Listing> listings;
        }
        public class Listing
        {
            public string name { get; set; }
            public double pricePerPassenger { get; set; }
            public VehicleType vehicleType { get; set; }
            public double TotalPrice { get; set; }
        }

        public class VehicleType
        {
            public string name { get; set; }
            public long maxPassengers { get; set; }
        }
    }
}

