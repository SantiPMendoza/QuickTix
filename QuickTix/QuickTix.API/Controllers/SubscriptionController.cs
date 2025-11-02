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
    public class SubscriptionController : BaseController<Subscription, SubscriptionDTO, CreateSubscriptionDTO>
    {
        public SubscriptionController(ISubscriptionRepository repository, IMapper mapper, ILogger<SubscriptionController> logger)
            : base(repository, mapper, logger)
        {
        }
    }
}
