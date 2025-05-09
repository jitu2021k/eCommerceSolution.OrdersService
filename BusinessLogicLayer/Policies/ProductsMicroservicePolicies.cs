using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Bulkhead;
using Polly.Fallback;
using System.Text;
using System.Text.Json;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies
{
    public class ProductsMicroservicePolicies : IProductsMicroservicePolicies
    {
        private readonly ILogger<ProductsMicroservicePolicies> _logger;

        public ProductsMicroservicePolicies(ILogger<ProductsMicroservicePolicies> logger)
        {
            _logger = logger;
        }

        public IAsyncPolicy<HttpResponseMessage> GetBulkheadIsolationPolicy()
        {
            AsyncBulkheadPolicy<HttpResponseMessage> policy =
            Policy.BulkheadAsync<HttpResponseMessage>(
                maxParallelization: 2,  //Allows upto 2 concurrent requests
                maxQueuingActions: 40,  //Queue upto 40 additional requests
                onBulkheadRejectedAsync: (context) =>
                {
                    _logger.LogWarning("BulkheadIsolation triggered. Can't send any more requests" +
                        " since the queue is full");
                    throw new BulkheadRejectedException("Bulk head queue is full");
                });
            return policy;
        }

        public IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy()
        {
            AsyncFallbackPolicy<HttpResponseMessage> policy =
            Policy.HandleResult<HttpResponseMessage>(res => !res.IsSuccessStatusCode)
                .FallbackAsync(async (context) => {
                _logger.LogWarning("Fallback triggered: The request failed, returning dummy data");

                ProductDTO product = new ProductDTO
                                    (
                                        ProductID: Guid.Empty,
                                        ProductName: "Temporarily Unavailable",
                                        Category: "Temporarily Unavailable",
                                        UnitPrice: 0,
                                        QuantityInStock: 0
                                    );
                var response =  new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable) 
                                {
                                   Content = new StringContent(JsonSerializer.Serialize(product),
                                                    Encoding.UTF8,"application/json")
                                };
                return response;
                });

            return policy;
        }
    }
}
