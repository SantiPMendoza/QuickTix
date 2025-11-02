using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuickTix.Core.Models.DTOs;
using QuickTix.Core.Models.Entities;
using QuickTix.Core.Interfaces;

namespace QuickTix.API.Controllers
{
    [Authorize(Roles = "admin,manager")]
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerController : BaseController<Manager, ManagerDTO, CreateManagerDTO>
    {
        public ManagerController(IRepository<Manager> repository, IMapper mapper, ILogger<ManagerController> logger)
            : base(repository, mapper, logger)
        {
        }
    }
}
