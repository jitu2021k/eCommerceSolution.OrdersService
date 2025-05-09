using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using FluentValidation;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators
{
    public class OrderAddRequestValidator : AbstractValidator<OrderAddRequest>
    {
        public OrderAddRequestValidator()
        {
            RuleFor(temp=>temp.UserID).NotEmpty().WithErrorCode("User ID cann't be blank");
            RuleFor(temp=>temp.OrderDate).NotEmpty().WithErrorCode("Order Date cann't be blank");
            RuleFor(temp=>temp.OrderItems).NotEmpty().WithErrorCode("Order Item cann't be blank");
        }
    }
}
