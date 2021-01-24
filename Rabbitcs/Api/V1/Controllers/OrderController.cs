using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rabbitcs.Api.V1.Dtos;
using Rabbitcs.Domain.Models;
using Rabbitcs.Infra;

namespace Rabbitcs.Controllers
{
    [ApiController]
    [Route("/api/v1/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IMapper _mapper;
        private readonly SqlContext _db;

        public OrderController(ILogger<OrderController> logger, IMapper mapper, SqlContext db)
        {
            _logger = logger;
            _mapper = mapper;
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllOrders()
        {
            var allOrders = await _db.Orders.Include(order => order.OrderItems).ToListAsync();

            return Ok(allOrders);
        }

        [HttpPost]
        public async Task<ActionResult> CreateOrder([FromBody] OrderRequest orderRequest)
        {
            _logger.LogInformation("Received new order request...");
            var newOrder = _mapper.Map<OrderRequest, Order>(orderRequest);

            _logger.LogInformation("Persisting new order...");
            await _db.Orders.AddAsync(newOrder);

            _logger.LogInformation("Commiting transaction...");
            await _db.SaveChangesAsync();

            _logger.LogInformation("All good!");
            return StatusCode(201, new { Message = "Successfuly created!" });
        }
    }
}
