using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuickTix.Core.Models.DTOs;
using QuickTix.Core.Models.Entities;
using QuickTix.Core.Interfaces;


namespace QuickTix.API.Controllers
{
    //[Authorize(Roles = "admin")]
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : BaseController<Admin, AdminDTO, CreateAdminDTO>
    {
        public AdminController(IAdminRepository repository, IMapper mapper, ILogger<AdminController> logger)
            : base(repository, mapper, logger)
        {
        }
    }
}
