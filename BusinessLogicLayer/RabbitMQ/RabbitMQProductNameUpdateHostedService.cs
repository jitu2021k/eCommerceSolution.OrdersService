using Microsoft.Extensions.Hosting;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ
{
    public class RabbitMQProductNameUpdateHostedService : IHostedService
    {
        private readonly IRabbitMQProductNameUpdateConsumer _rabbitMQProductNameUpdateConsumer;

        public RabbitMQProductNameUpdateHostedService(IRabbitMQProductNameUpdateConsumer consumer)
        {
            _rabbitMQProductNameUpdateConsumer = consumer;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _rabbitMQProductNameUpdateConsumer.Consume();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _rabbitMQProductNameUpdateConsumer.Dispose();
            return Task.CompletedTask;
        }
    }
}
