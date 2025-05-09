using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly.Bulkhead;
using System.Net.Http.Json;
using System.Text.Json;


namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients
{
    public class ProductsMicroserviceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductsMicroserviceClient> _logger;
        private readonly IDistributedCache _distributedCache;

        public ProductsMicroserviceClient(HttpClient httpClient,
                                          ILogger<ProductsMicroserviceClient> logger,
                                          IDistributedCache distributedCache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _distributedCache = distributedCache;
        }

        public async Task<ProductDTO?> GetProductByProductID(Guid productID)
        {
            try
            {
                //key: product:123
                //Value: {ProductName:"...",....}

                string cacheKey = $"product:{productID}";
                string? cachedProduct = await _distributedCache.GetStringAsync(cacheKey);

                if(cachedProduct != null)
                {
                    ProductDTO? productFromCache =
                    JsonSerializer.Deserialize<ProductDTO>(cachedProduct);
                    return productFromCache;
                }

                HttpResponseMessage response = await _httpClient.GetAsync($"/gateway/products/search/product-id/{productID}");
            
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                    {
                        ProductDTO? productFromFallbackPloicy = await response.Content.ReadFromJsonAsync<ProductDTO>();

                        if (productFromFallbackPloicy == null)
                        {
                            throw new NotImplementedException("Fallback policy was not implemented.");
                        }

                        return productFromFallbackPloicy;
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
                        throw new HttpRequestException($"Http request failed with status code {response.StatusCode}");
                    }
                }

                ProductDTO? productDTO = await response.Content.ReadFromJsonAsync<ProductDTO>();
               
                if (productDTO == null)
                {
                    throw new ArgumentException("Invalid Product ID");
                }

                //product:{productID}
                //Value : {ProductName:"..",".;;}

                string productJson = JsonSerializer.Serialize<ProductDTO>(productDTO);

                DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(300))
                    .SetSlidingExpiration(TimeSpan.FromSeconds(100));
                string cacheKeyToWrite = $"product:{productID}";

                await _distributedCache.SetStringAsync(cacheKeyToWrite, productJson,options);
                return productDTO;
            }
            catch(BulkheadRejectedException ex)
            {
                _logger.LogError(ex, "BulkHead isolation blocks the request since queue is full.");
                return new ProductDTO(
                    ProductID: Guid.Empty,
                    ProductName: "Temprarily Unavailable (Bulkhead)",
                    Category: "Temprarily Unavailable (Bulkhead)",
                    UnitPrice: 0,
                    QuantityInStock: 0
                    );
            }
        }
    }
}
