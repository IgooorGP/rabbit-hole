using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rabbitcs.Api.V1.Dtos;

namespace Rabbitcs.Controllers
{
    [ApiController]
    [Route("/api/v1/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;

        public OrderController(ILogger<OrderController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> CreateOrder([FromBody] OrderRequest orderRequest)
        {
            return StatusCode(201, new { Message = "Successfuly created!" });
        }
    }
}
