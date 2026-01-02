using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;

namespace QuickTix.API.Controllers
{
    [Authorize(Roles = "admin,manager")]
    [Route("api/[controller]")]
    [ApiController]
    public class VenueController : BaseController<Venue, VenueDTO, CreateVenueDTO>
    {
        public VenueController(IVenueRepository repository, IMapper mapper, ILogger<VenueController> logger)
            : base(repository, mapper, logger)
        {
        }
    }
}
