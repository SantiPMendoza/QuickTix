using QuickTix.API.AutoMapper;
using QuickTix.Core.Models.DTOs;
using QuickTix.Core.Models.DTOs.UserAuthDTO;
using QuickTix.Core.Models.Entities;

namespace QuickTix.API.AutoMapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Admin, AdminDTO>().ReverseMap();
            CreateMap<Admin, CreateAdminDTO>().ReverseMap();

        }
    }
}
