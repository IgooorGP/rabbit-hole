using System.Collections.Generic;

namespace Rabbitcs.Api.V1.Dtos
{
    /// <summary>
    /// Dto used to model a new order creation request.
    /// </summary>
    public class OrderRequest
    {
        public List<OrderItem> OrderItems { get; set; }
    }
    /// <summary>
    /// Dto used to model a single order item of a new order creation.
    /// </summary>
    public class OrderItem
    {
        public string Sku { get; set; }
        public int Qty { get; set; }
    }
}
