using AutoMapper;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;
using eCommerce.OrdersMicroservice.DataAccessLayer.RepositoryContracts;
using FluentValidation;
using FluentValidation.Results;
using MongoDB.Driver;
using ZstdSharp.Unsafe;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Services
{
    public class OrdersService : IOrdersService
    {
        private readonly IOrdersRepository _ordersRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<OrderAddRequest> _orderAddRequestValidator;
        private readonly IValidator<OrderItemAddRequest> _orderItemAddRequestValidator;
        private readonly IValidator<OrderUpdateRequest> _orderUpdateRequestValidator;
        private readonly IValidator<OrderItemUpdateRequest> _orderItemUpdateRequestValidator;
        private readonly UsersMicroserviceClient _usersMicroserviceClient;
        private readonly ProductsMicroserviceClient _productsMicroserviceClient;

        public OrdersService(IOrdersRepository ordersRepository, 
                             IMapper mapper,
                             IValidator<OrderAddRequest> orderAddRequestValidator,
                             IValidator<OrderItemAddRequest> orderItemAddRequestValidator,
                             IValidator<OrderUpdateRequest> orderUpdateRequestValidator,
                             IValidator<OrderItemUpdateRequest> orderItemUpdateRequestValidator,
                             UsersMicroserviceClient usersMicroserviceClient,
                             ProductsMicroserviceClient productsMicroserviceClient)
        {
            _ordersRepository = ordersRepository;
            _mapper = mapper;
            _orderAddRequestValidator = orderAddRequestValidator;
            _orderItemAddRequestValidator = orderItemAddRequestValidator;
            _orderUpdateRequestValidator = orderUpdateRequestValidator;
            _orderItemUpdateRequestValidator = orderItemUpdateRequestValidator;
            _usersMicroserviceClient = usersMicroserviceClient;
            _productsMicroserviceClient = productsMicroserviceClient;
        }
        public async Task<OrderResponse?> AddOrder(OrderAddRequest orderAddRequest)
        {
            //Check for null param
            if(orderAddRequest == null)
            {
                throw new ArgumentNullException(nameof(orderAddRequest));
            }

            //Validate OrderAddRequest using Fluent Validation
            ValidationResult orderAddRequestValidationResult = await _orderAddRequestValidator.ValidateAsync(orderAddRequest);

            if (!orderAddRequestValidationResult.IsValid)
            {
                string errors = string.Join(", ",orderAddRequestValidationResult.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException(errors);
            }

            List<ProductDTO?> products = new List<ProductDTO?>();

            //Validate order items using Fluent Validator
            foreach(OrderItemAddRequest orderItemAddRequest in orderAddRequest.OrderItems)
            {
                ValidationResult orderItemAddRequestValidationResult = await _orderItemAddRequestValidator.ValidateAsync(orderItemAddRequest);
                if (!orderItemAddRequestValidationResult.IsValid)
                {
                    string errors = string.Join(", ", orderItemAddRequestValidationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException(errors);
                }

                //To Do: Add logic for checking if ProductID exists in Products Microservice
                ProductDTO? product = await _productsMicroserviceClient.GetProductByProductID(orderItemAddRequest.ProductID);

                if(product == null)
                {
                    throw new ArgumentException("Invalid Product ID");
                }

                products.Add(product);
            }

            //To Do : Add logic for checking if UserID Exists in Users microservice
            UserDTO? userDTO = await _usersMicroserviceClient.GetUserByUserID(orderAddRequest.UserID);
            if (userDTO == null)
            {
                throw new ArgumentException("Invalid User ID");
            }

            //Convert data from OrdrAddRequest to Order
            
            Order orderInput = _mapper.Map<Order>(orderAddRequest);

            //Generate Values
            foreach(OrderItem orderItem in orderInput.OrderItems)
            {
                orderItem.TotalPrice = orderItem.Quantity * orderItem.UnitPrice;
            }
            orderInput.TotalBill = orderInput.OrderItems.Sum(temp => temp.TotalPrice);

            //Invoke Repository
            Order? addedOrder = await _ordersRepository.AddOrder(orderInput);

            if (addedOrder == null)
            {
                return null;
            }

            OrderResponse addedOrderResponse =  _mapper.Map<OrderResponse>(addedOrder);


            //Load Product Name and Category in OrderItem
            if (addedOrderResponse != null)
            {
                foreach (OrderItemResponse orderItemResponse in addedOrderResponse.OrderItems)
                {
                    ProductDTO? productDTO = products.Where(temp => temp.ProductID == orderItemResponse.ProductID).FirstOrDefault();

                    if (productDTO == null)
                        continue;

                    _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
                }

                //To Load UserPersonName and Email from users
                if (userDTO != null)
                    _mapper.Map<UserDTO, OrderResponse>(userDTO, addedOrderResponse);

            }
            return addedOrderResponse;
        }

        public async Task<bool> DeleteOrder(Guid orderID)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderID,orderID);
            Order? existingOrder = await _ordersRepository.GetOrderByCondition(filter);
            if (existingOrder == null)
            {
                return false;
            }

            bool isDeleted = await _ordersRepository.DeleteOrder(orderID);
            return  isDeleted;
        }

        public async Task<OrderResponse?> GetOrderByCondition(FilterDefinition<Order> filter)
        {
            Order? order = await _ordersRepository.GetOrderByCondition(filter);
            if (order == null)
            {
                return null;
            }

            OrderResponse orderResponse = _mapper.Map<OrderResponse>(order);

            //To Do : Load ProductName and Category in each OrerItem

            if (orderResponse != null)
            {
                foreach (OrderItemResponse orderItemResponse in orderResponse.OrderItems)
                {
                    ProductDTO? productDTO = await _productsMicroserviceClient.GetProductByProductID(orderItemResponse.ProductID);

                    if (productDTO == null)
                        continue;

                    _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
                }
            }

            if(orderResponse != null)
            {
                //To Load UserPersonName and Email from users
                UserDTO? userDTO = await _usersMicroserviceClient.GetUserByUserID(orderResponse.UserID);

                if (userDTO != null)
                    _mapper.Map<UserDTO, OrderResponse>(userDTO, orderResponse);
            }

            return orderResponse;
        }

        public async Task<List<OrderResponse?>> GetOrders()
        {
            IEnumerable<Order?> orders = await _ordersRepository.GetOrders();
            if (orders == null)
            {
                return null;
            }

            IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse>>(orders);

            //To Do : Load ProductName and Category in each OrerItem

            foreach(OrderResponse? orderResponse in orderResponses)
            {
                if(orderResponse == null)
                {
                    continue;
                }

              

                foreach(OrderItemResponse orderItemResponse in orderResponse.OrderItems)
                {
                    ProductDTO? productDTO = await _productsMicroserviceClient.GetProductByProductID(orderItemResponse.ProductID);
                    
                    if (productDTO == null)
                        continue;

                    _mapper.Map<ProductDTO,OrderItemResponse>(productDTO,orderItemResponse);
                }


                //To Load UserPersonName and Email from users
                UserDTO? userDTO = await _usersMicroserviceClient.GetUserByUserID(orderResponse.UserID);

                if (userDTO != null)
                    _mapper.Map<UserDTO, OrderResponse>(userDTO, orderResponse);
            }

            return orderResponses.ToList();
        }

        public async Task<List<OrderResponse?>> GetOrdersByCondition(FilterDefinition<Order> filter)
        {
            IEnumerable<Order?> orders = await _ordersRepository.GetOrdersByCondition(filter);
            if (orders == null)
            {
                return null;
            }

            IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse>>(orders);


            //To Do : Load ProductName and Category in each OrerItem

            foreach (OrderResponse? orderResponse in orderResponses)
            {
                if (orderResponse == null)
                {
                    continue;
                }

                foreach (OrderItemResponse orderItemResponse in orderResponse.OrderItems)
                {
                    ProductDTO? productDTO = await _productsMicroserviceClient.GetProductByProductID(orderItemResponse.ProductID);

                    if (productDTO == null)
                        continue;

                    _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
                }

                //To Load UserPersonName and Email from users
                UserDTO? userDTO = await _usersMicroserviceClient.GetUserByUserID(orderResponse.UserID);

                if (userDTO != null)
                    _mapper.Map<UserDTO, OrderResponse>(userDTO, orderResponse);
            }

            return orderResponses.ToList();
        }

        public async Task<OrderResponse?> UpdateOrder(OrderUpdateRequest orderUpdateRequest)
        {
            //Check for null param
            if (orderUpdateRequest == null)
            {
                throw new ArgumentNullException(nameof(orderUpdateRequest));
            }

            //Validate OrderAddRequest using Fluent Validation
            ValidationResult orderUpdateRequestValidationResult = await _orderUpdateRequestValidator.ValidateAsync(orderUpdateRequest);

            if (!orderUpdateRequestValidationResult.IsValid)
            {
                string errors = string.Join(", ", orderUpdateRequestValidationResult.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException(errors);
            }

            List<ProductDTO?> products = new List<ProductDTO?>();

            //Validate order items using Fluent Validator
            foreach (OrderItemUpdateRequest orderItemUpdateRequest in orderUpdateRequest.OrderItems)
            {
                ValidationResult orderItemUpdateRequestValidationResult = await _orderUpdateRequestValidator.ValidateAsync(orderUpdateRequest);
                if (!orderItemUpdateRequestValidationResult.IsValid)
                {
                    string errors = string.Join(", ", orderItemUpdateRequestValidationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException(errors);
                }

                //To Do: Add logic for checking if ProductID exists in Products Microservice
                ProductDTO? product = await _productsMicroserviceClient.GetProductByProductID(orderItemUpdateRequest.ProductID);

                if (product == null)
                {
                    throw new ArgumentException("Invalid Product ID");
                }

                products.Add(product);
            }

            //To Do : Add logic for checking if UserID Exists in Users microservice
            UserDTO? userDTO = await _usersMicroserviceClient.GetUserByUserID(orderUpdateRequest.UserID);
            if (userDTO == null)
            {
                throw new ArgumentException("Invalid User ID");
            }

            //Convert data from OrdrUpdateRequest to Order
            Order orderInput = _mapper.Map<Order>(orderUpdateRequest);

            //Generate Values
            foreach (OrderItem orderItem in orderInput.OrderItems)
            {
                orderItem.TotalPrice = orderItem.Quantity * orderItem.UnitPrice;
            }
            orderInput.TotalBill = orderInput.OrderItems.Sum(temp => temp.TotalPrice);

            //Invoke Repository
            Order? updatedOrder = await _ordersRepository.UpdateOrder(orderInput);

            if (updatedOrder == null)
            {
                return null;
            }

            OrderResponse updatedOrderResponse = _mapper.Map<OrderResponse>(updatedOrder);

            //Load Product Name and Category in OrderItem
            if (updatedOrderResponse != null)
            {
                foreach (OrderItemResponse orderItemResponse in updatedOrderResponse.OrderItems)
                {
                    ProductDTO? productDTO = products.Where(temp => temp.ProductID == orderItemResponse.ProductID).FirstOrDefault();

                    if (productDTO == null)
                        continue;

                    _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
                }

                //To Load UserPersonName and Email from users
                if (userDTO != null)
                    _mapper.Map<UserDTO, OrderResponse>(userDTO, updatedOrderResponse);
            }

            return updatedOrderResponse;
        }
    }
}
