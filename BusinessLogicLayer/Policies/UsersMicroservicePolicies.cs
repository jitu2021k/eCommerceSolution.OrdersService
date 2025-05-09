using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;
using System.Text.Json;
using System.Text;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies
{
    public class UsersMicroservicePolicies : IUsersMicroservicePolicies
    {
        private readonly IPollyPolicies _pollyPolicies;
        private readonly ILogger<UsersMicroservicePolicies> _logger;

        public UsersMicroservicePolicies(IPollyPolicies pollyPolicies, ILogger<UsersMicroservicePolicies> logger)
        {
           _pollyPolicies = pollyPolicies;
            _logger = logger;
        }

        public IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
        {
            var retryPolicy = _pollyPolicies.GetRetryPolicy(5);
            var circuitBreakerPolicy = _pollyPolicies.GetCircuitBreakerPolicy(3,TimeSpan.FromMinutes(2));
            var timeoutPolicy = _pollyPolicies.GetTimeoutPolicy(TimeSpan.FromSeconds(5));

            AsyncPolicyWrap<HttpResponseMessage> wrappedPolicy =
                Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
            return wrappedPolicy;
        }

        public IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy()
        {
            AsyncFallbackPolicy<HttpResponseMessage> policy =
            Policy.HandleResult<HttpResponseMessage>(res => !res.IsSuccessStatusCode)
                .FallbackAsync(async (context) => {
                    _logger.LogWarning("Fallback triggered: The request failed, returning dummy data");

                    UserDTO user = new UserDTO
                                        (
                                            UserID: Guid.Empty,
                                            PersonName: "Temporarily Unavailable",
                                            Email: "Temporarily Unavailable",
                                            Gender: "Temporarily Unavailable"
                                        );
                    var response = new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(user),
                                                        Encoding.UTF8, "application/json")
                    };
                    return response;
                });
            return policy;
        }
    }
}
