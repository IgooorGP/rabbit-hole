using System;
using System.Collections.Generic;

namespace Rabbitcs.Domain.Models
{
    public enum OrderStatus
    {
        Received,
        Processing,
        Finished
    }

    public class OrderItem : BaseEntity
    {
        public Order Order { get; set; } = null!;
        public Guid OrderId { get; set; }
        public string Sku { get; set; }
        public int Qty { get; set; }
    }

    public class Order : BaseEntity
    {
        public List<OrderItem> OrderItems { get; set; } = null!;
        public OrderStatus Status { get; set; } = OrderStatus.Received;
    }
}