using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.DTOs;
using QuickTix.Core.Models.Entities;

namespace QuickTix.API.Controllers
{
    [Authorize(Roles = "admin,manager")]
    [Route("api/[controller]")]
    [ApiController]
    public class VenueController : BaseController<Venue, VenueDTO, CreateVenueDTO>
    {
        public VenueController(IRepository<Venue> repository, IMapper mapper, ILogger<VenueController> logger)
            : base(repository, mapper, logger)
        {
        }
    }
}
