using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuickTix.Core.Models.DTOs;
using QuickTix.Core.Models.Entities;
using QuickTix.Core.Interfaces;


namespace QuickTix.API.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : BaseController<Admin, AdminDTO, CreateAdminDTO>
    {
        public AdminController(IRepository<Admin> repository, IMapper mapper, ILogger<AdminController> logger)
            : base(repository, mapper, logger)
        {
        }
    }
}
