using Amazon.Runtime.Internal.Util;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net.Http.Json;
using System.Text.Json;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients
{
    public class UsersMicroserviceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UsersMicroserviceClient> _logger;
        private readonly IDistributedCache _distributedCache;

        public UsersMicroserviceClient(HttpClient httpClient, 
                                       ILogger<UsersMicroserviceClient> logger,
                                       IDistributedCache distributedCache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _distributedCache = distributedCache;
        }

        public async Task<UserDTO?> GetUserByUserID(Guid userID)
        {
            try
            {
                //user:{userID}
                //Value : {PersonName:"..",".;;}

                string cacheKey = $"user:{userID}";
                string? cachedUser = await _distributedCache.GetStringAsync(cacheKey);

                if (cachedUser != null)
                {
                    UserDTO? userFromCache =
                    JsonSerializer.Deserialize<UserDTO>(cachedUser);
                    return userFromCache;
                }

                HttpResponseMessage response = await _httpClient.GetAsync($"/gateway/users/{userID}");

                if (!response.IsSuccessStatusCode)
                {
                    if(response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                    {

                        UserDTO? userFromFallbackPloicy = await response.Content.ReadFromJsonAsync<UserDTO>();

                        if (userFromFallbackPloicy == null)
                        {
                            throw new NotImplementedException("Fallback policy was not implemented.");
                        }

                        return userFromFallbackPloicy;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        throw new HttpRequestException("Bad request", null, System.Net.HttpStatusCode.BadRequest);
                    }
                    else
                    {
                        return new UserDTO(
                            PersonName: "Temporarily Unavailable",
                            Email: "Temporarily Unavailable",
                            Gender: "Temporarily Unavailable",
                            UserID: Guid.Empty
                            );

                        // throw new HttpRequestException($"Http request failed with status code {response.StatusCode}");
                    }
                }

                UserDTO? userDTO = await response.Content.ReadFromJsonAsync<UserDTO>();

                if (userDTO == null)
                {
                    throw new ArgumentException("Invalid User ID");
                }

                //user:{userID}
                //Value : {PersonName:"..",".;;}

                string userJson = JsonSerializer.Serialize<UserDTO>(userDTO);

                DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTimeOffset.UtcNow.AddMinutes(5))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(3));
                string cacheKeyToWrite = $"user: {userID}";

                await _distributedCache.SetStringAsync(cacheKeyToWrite, userJson, options);

                return userDTO;
            }
            catch(BrokenCircuitException ex)
            {
                _logger.LogInformation(ex, "Request failed because of circuit breaks is in Open state." +
                    " Returing dummy data.");
                return new UserDTO(
                            PersonName: "Temporarily Unavailable  (circuit breaker)",
                            Email: "Temporarily Unavailable (circuit breaker)",
                            Gender: "Temporarily Unavailable (Timeout Rejection)",
                            UserID: Guid.Empty
                            );
            }
            catch (TimeoutRejectedException ex)
            {
                _logger.LogInformation(ex, "Timeout occured while fetching user data. Returnig dummy data.");
                return new UserDTO(
                            PersonName: "Temporarily Unavailable (Timeout Rejection)",
                            Email: "Temporarily Unavailable (Timeout Rejection)",
                            Gender: "Temporarily Unavailable (Timeout Rejection)",
                            UserID: Guid.Empty
                            );
            }
        }   
    }
}
