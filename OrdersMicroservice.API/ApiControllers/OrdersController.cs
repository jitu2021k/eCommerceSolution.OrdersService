﻿using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace OrdersMicroservice.API.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrdersService _ordersService;

        public OrdersController(IOrdersService ordersService)
        {
            _ordersService = ordersService;
        }

        //GET /api/Orders

        [HttpGet]
        public async Task<IEnumerable<OrderResponse?>> Get()
        {
            List<OrderResponse?> orderResponses = await _ordersService.GetOrders();
            return orderResponses;
        }

        //GET /api/Orders/search/orderid/{orderID}

        [HttpGet("search/orderid/{orderID}")]
        public async Task<OrderResponse?> GetOrderByOrderID(Guid orderID)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderID, orderID);
            OrderResponse? orderResponses = await _ordersService.GetOrderByCondition(filter);
            return orderResponses;
        }

        //GET /api/Orders/search/productid/{productID}

        [HttpGet("search/productid/{productID}")]
        public async Task<IEnumerable<OrderResponse?>> GetOrdersByProductID(Guid productID)
        {
            FilterDefinition<Order> filter = 
                                  Builders<Order>.Filter.ElemMatch(temp => temp.OrderItems, 
                                  Builders<OrderItem>.Filter.Eq(tempProduct => tempProduct.ProductID,productID));
            List<OrderResponse?> orderResponses = await _ordersService.GetOrdersByCondition(filter);
            return orderResponses;
        }

        //GET /api/Orders/search/orderDate/{orderDate}

        [HttpGet("search/orderDate/{orderDate}")]
        public async Task<IEnumerable<OrderResponse?>> GetOrderByOrderDate(DateTime orderDate)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderDate.ToString("yyy-MM-dd"), orderDate.ToString("yyy-MM-dd"));
            List<OrderResponse?> orderResponses = await _ordersService.GetOrdersByCondition(filter);
            return orderResponses;
        }

        //GET /api/Orders/search/userid/{userID}

        [HttpGet("search/userid/{userID}")]
        public async Task<IEnumerable<OrderResponse?>> GetOrdersByUserID(Guid userID)
        {
            FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.UserID,userID);
            List<OrderResponse?> orderResponses = await _ordersService.GetOrdersByCondition(filter);
            return orderResponses;
        }

        //POST /api/orders/
        [HttpPost]
        public async Task<ActionResult<OrderResponse?>> Post(OrderAddRequest orderAddRequest)
        {
            if(orderAddRequest == null)
            {
                return BadRequest("Invalid order data");
            }

            OrderResponse? addedOrderResponse = await _ordersService.AddOrder(orderAddRequest);
            if (addedOrderResponse == null)
            {
                return Problem("Error in adding Product");
            }

            return Created($"api/orders/search/orderid/{addedOrderResponse.OrderID}",addedOrderResponse);
        }

        //Put /api/orders/{orderID}
        [HttpPut("{orderID}")]
        public async Task<ActionResult<OrderResponse?>> Put(Guid orderID,OrderUpdateRequest orderUpdateRequest)
        {
            if (orderUpdateRequest == null)
            {
                return BadRequest("Invalid order data");
            }
            
            if(orderID != orderUpdateRequest.OrderID)
            {
                return BadRequest("OrderID in the URL doesn't match with the OrderID in the Request body");
            }

            OrderResponse? addedOrderResponse = await _ordersService.UpdateOrder(orderUpdateRequest);
            if (addedOrderResponse == null)
            {
                return Problem("Error in adding Product");
            }

            return Ok(addedOrderResponse);
        }

        //Delete /api/orders/{orderID}
        [HttpDelete("{orderID}")]
        public async Task<ActionResult<OrderResponse?>> Delete(Guid orderID)
        {
            if (orderID == Guid.Empty)
            {
                return BadRequest("Invalid Order ID");
            }

            bool isDeleted = await _ordersService.DeleteOrder(orderID);
            if (!isDeleted)
            {
                return Problem("Error in deleting Product");
            }

            return Ok(isDeleted);
        }
    }
}
