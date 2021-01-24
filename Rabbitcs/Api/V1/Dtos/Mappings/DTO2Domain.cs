using AutoMapper;
using Rabbitcs.Domain.Models;

namespace Rabbitcs.Api.V1.Dtos.Mappings
{
    public class DTO2Domain : Profile
    {
        public DTO2Domain()
        {
            CreateMap<OrderRequest, Order>();
            CreateMap<OrderItemRequest, OrderItem>();
        }
    }
}