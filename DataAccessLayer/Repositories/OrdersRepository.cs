using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;
using eCommerce.OrdersMicroservice.DataAccessLayer.RepositoryContracts;
using MongoDB.Driver;

namespace eCommerce.OrdersMicroservice.DataAccessLayer.Repositories
{
    public class OrdersRepository : IOrdersRepository
    {
        private readonly IMongoCollection<Order> _orders;
        private readonly string collectionName = "orders";
        public OrdersRepository(IMongoDatabase mongoDatabase)
        {
            _orders = mongoDatabase.GetCollection<Order>(collectionName);
        }
        public async Task<Order?> AddOrder(Order order)
        {
            order.OrderID = Guid.NewGuid();
            order._Id = order.OrderID;

            foreach(OrderItem orderItem in order.OrderItems)
            {
                orderItem._Id = Guid.NewGuid();
            }

            await _orders.InsertOneAsync(order);
            return order;
        }
        public async Task<bool> DeleteOrder(Guid OrderID)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderID,OrderID);
            Order? existingOrder = (await _orders.FindAsync(filter)).FirstOrDefault();
            if (existingOrder == null)
            {
                return false;
            }
            DeleteResult deleteResult = await _orders.DeleteOneAsync(filter);

            return deleteResult.DeletedCount > 0;
        }

        public async Task<Order?> GetOrderByCondition(FilterDefinition<Order> filter)
        {
            Order existingOrder = (await _orders.FindAsync(filter)).FirstOrDefault();
            return existingOrder;
        }

        public async Task<IEnumerable<Order>> GetOrders()
        {
            return (await _orders.FindAsync(Builders<Order>.Filter.Empty)).ToList();
        }

        public async Task<IEnumerable<Order>> GetOrdersByCondition(FilterDefinition<Order> filter)
        {
            List<Order> existingOrders = (await _orders.FindAsync(filter)).ToList();
            return existingOrders;
        }

        public async Task<Order?> UpdateOrder(Order order)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderID, order.OrderID);
            Order? existingOrder = (await _orders.FindAsync(filter)).FirstOrDefault();
            if (existingOrder == null)
            {
                return null;
            }
            order._Id = existingOrder._Id;

            ReplaceOneResult replaceOneResult = await _orders.ReplaceOneAsync(filter, order);
            return order;
        }
    }
}
