using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies
{
    public class PollyPolicies : IPollyPolicies
    {
        private readonly ILogger<PollyPolicies> _logger;

        public PollyPolicies(ILogger<PollyPolicies> logger)
        {
            _logger = logger;
        }

        public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
        {
            AsyncRetryPolicy<HttpResponseMessage> policy =
                    Policy.HandleResult<HttpResponseMessage>(res => !res.IsSuccessStatusCode)
                    .WaitAndRetryAsync(retryCount: retryCount, //No of retries
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), //Delay between
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        _logger.LogInformation($"Retry {retryAttempt} after {timespan.TotalSeconds} seconds");
                    });
            return policy;
        }
        public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int handledEventsAllowedBeforeBreaking,
                                                    TimeSpan durationOfBreak)
        {
            AsyncCircuitBreakerPolicy<HttpResponseMessage> policy =
                   Policy.HandleResult<HttpResponseMessage>(res => !res.IsSuccessStatusCode)
                   .CircuitBreakerAsync(
                   handledEventsAllowedBeforeBreaking: handledEventsAllowedBeforeBreaking,  
                   durationOfBreak: durationOfBreak, //Half Open State allow once
                   onBreak: (outcome, timespan) =>
                   {
                       _logger.LogInformation($"Circuir breaker opened for {timespan.TotalMinutes} minutes " +
                           $"due to consucutive 3 failures. The subsequest requests will be blocked");
                   }, onReset: () =>
                   { 
                       _logger.LogInformation($"Circuir breaker closed. " +
                           $"The Subsequest request will be allowed.");
                   });
            return policy;
        }

     

        public IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
        {
            AsyncTimeoutPolicy<HttpResponseMessage> policy =
            Policy.TimeoutAsync<HttpResponseMessage>(timeout);
            return policy;
        }
    }
}
