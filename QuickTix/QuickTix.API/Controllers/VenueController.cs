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
    /// Controlador API para la gestión CRUD de recintos (venues).
    /// Reutiliza las operaciones comunes definidas en BaseController.
    /// </summary>
    [Authorize(Roles = "admin,manager")]
    [Route("api/[controller]")]
    [ApiController]
    public class VenueController : BaseController<Venue, VenueDTO, CreateVenueDTO>
    {
        /// <summary>
        /// Inicializa una nueva instancia del <see cref="VenueController"/>.
        /// </summary>
        /// <param name="repository">Repositorio de recintos.</param>
        /// <param name="mapper">Servicio de mapeo entre entidades y DTOs.</param>
        /// <param name="logger">Logger del controlador.</param>
        public VenueController(IVenueRepository repository, IMapper mapper, ILogger<VenueController> logger)
            : base(repository, mapper, logger)
        {
        }
    }
}
