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
    public class SaleController : BaseController<Sale, SaleDTO, CreateSaleDTO>
    {
        public SaleController(IRepository<Sale> repository, IMapper mapper, ILogger<SaleController> logger)
            : base(repository, mapper, logger)
        {
        }
    }
}
