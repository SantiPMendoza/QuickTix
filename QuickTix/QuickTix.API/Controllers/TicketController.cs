using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.DTOs;
using QuickTix.Core.Models.Entities;

namespace QuickTix.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TicketController : BaseController<Ticket, TicketDTO, CreateTicketDTO>
    {
        public TicketController(IRepository<Ticket> repository, IMapper mapper, ILogger<TicketController> logger)
            : base(repository, mapper, logger)
        {
        }
    }
}
