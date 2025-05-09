using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using FluentValidation;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators
{
    public class OrderItemAddRequestValidator : AbstractValidator<OrderItemAddRequest>
    {
        public OrderItemAddRequestValidator()
        {
            RuleFor(temp => temp.ProductID).NotEmpty().WithErrorCode("Product ID cann't be blank");
            RuleFor(temp => temp.UnitPrice).NotEmpty().WithErrorCode("Unit Price cann't be blank")
                .GreaterThan(0).WithErrorCode("Unit Price cann't be less than or equal to zero");
            RuleFor(temp => temp.Quantity).NotEmpty().WithErrorCode("Quantity cann't be blank")
               .GreaterThan(0).WithErrorCode("Quantity cann't be less than or equal to zero");
        }
    }
}
