using FluentValidation;

namespace Rabbitcs.Api.V1.Dtos
{
    /// <summary>
    /// Validator for a new OrderRequest.
    /// </summary>
    public class OrderRequestValidator : AbstractValidator<OrderRequest>
    {
        public OrderRequestValidator()
        {
            RuleFor(orderRequest => orderRequest.OrderItems).NotEmpty();
            RuleForEach(orderRequest => orderRequest.OrderItems).ChildRules(orderItems =>
            {
                orderItems.RuleFor(item => item.Sku).NotNull();
                orderItems.RuleFor(item => item.Qty).GreaterThan(0);
            });
        }
    }
}