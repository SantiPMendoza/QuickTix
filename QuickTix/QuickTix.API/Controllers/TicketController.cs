using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;

namespace QuickTix.API.Controllers
{
    /// <summary>
    /// Controlador API para la gestión CRUD de tickets.
    /// Reutiliza las operaciones comunes definidas en BaseController.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TicketController : BaseController<Ticket, TicketDTO, CreateTicketDTO>
    {
        /// <summary>
        /// Inicializa una nueva instancia del <see cref="TicketController"/>.
        /// </summary>
        /// <param name="repository">Repositorio de tickets.</param>
        /// <param name="mapper">Servicio de mapeo entre entidades y DTOs.</param>
        /// <param name="logger">Logger del controlador.</param>
        public TicketController(ITicketRepository repository, IMapper mapper, ILogger<TicketController> logger)
            : base(repository, mapper, logger)
        {
        }
    }
}
