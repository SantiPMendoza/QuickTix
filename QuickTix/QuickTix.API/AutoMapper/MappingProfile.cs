using AutoMapper;
using QuickTix.Core.Models.DTOs.UserAuthDTO;
using QuickTix.Core.Models.Entities;

namespace QuickTix.API.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // AppUser → DTO
            CreateMap<AppUser, UserDTO>()
                .ForMember(dest => dest.Role, opt => opt.Ignore()); // se carga desde UserManager

            // DTO → AppUser (para registro)
            CreateMap<UserRegistrationDTO, AppUser>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore());
        }
    }
}
