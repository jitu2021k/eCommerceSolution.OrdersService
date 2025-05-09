namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO
{
    public record OrderResponse(Guid OrderID, Guid UserID,decimal TotalBill, DateTime OrderDate,
        List<OrderItemResponse> OrderItems, string? PersonName,string? Email)
    {
        public OrderResponse() : this(default,default, default, default, default, default ,default)
        {

        }
    }
}
