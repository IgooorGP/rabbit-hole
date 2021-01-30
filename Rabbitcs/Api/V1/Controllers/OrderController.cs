using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rabbitcs.Api.V1.Dtos;
using Rabbitcs.Domain.Models;
using Rabbitcs.Infra;
using RabbitHole.Api;

namespace Rabbitcs.Controllers
{
    [ApiController]
    [Route("/api/v1/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IRabbitBus _rabbitBus;
        private readonly IMapper _mapper;
        private readonly SqlContext _db;

        public OrderController(ILogger<OrderController> logger, IRabbitBus rabbitBus, IMapper mapper, SqlContext db)
        {
            _rabbitBus = rabbitBus;
            _logger = logger;
            _mapper = mapper;
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllOrders([FromQuery] PaginationParams paginationParams)
        {
            _logger.LogInformation("Fetching orders from database...");
            var orders = await _db.Orders
                .Include(order => order.OrderItems)
                .OrderBy(order => order.Id)
                .GetPage(paginationParams ??= new PaginationParams())
                .ToListAsync();

            _logger.LogInformation("All good!");
            return Ok(orders);
        }

        [HttpPost]
        public async Task<ActionResult> CreateOrder([FromBody] OrderRequest orderRequest)
        {
            _logger.LogInformation("Received new order request...");
            var newOrder = _mapper.Map<OrderRequest, Order>(orderRequest);

            _logger.LogInformation("Persisting new order...");
            await _db.Orders.AddAsync(newOrder);

            _logger.LogInformation("Opening RabbitMQ transaction...");
            var rabbitTx = await _rabbitBus.BeginTx();

            _logger.LogInformation("Publishing new order...");
            await _rabbitBus.PublishAsync(new { newOrder.Id, newOrder.Status }, "/topic/SomeVirtualTopic", channel: rabbitTx);

            _logger.LogInformation("Commiting DB transaction...");
            await _db.SaveChangesAsync();

            _logger.LogInformation("Committing RabbitMQ transaction...");
            await _rabbitBus.CommitTx(rabbitTx);

            _logger.LogInformation("All good!");
            return StatusCode(201, new { Message = "Successfuly created!" });
        }
    }
}
