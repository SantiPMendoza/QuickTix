using AutoMapper;
using QuickTix.Contracts.DTOs.UserAuthDTO;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Core.Models.Entities;

namespace QuickTix.API.AutoMapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Admin
            CreateMap<Admin, AdminDTO>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.AppUser.Email))
                .ForMember(dest => dest.Nif, opt => opt.MapFrom(src => src.AppUser.Nif))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.AppUser.PhoneNumber));

            CreateMap<CreateAdminDTO, Admin>()
                .ForMember(dest => dest.AppUser, opt => opt.Ignore());

            // Manager
            CreateMap<Manager, ManagerDTO>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.AppUser.Email))
                .ForMember(dest => dest.Nif, opt => opt.MapFrom(src => src.AppUser.Nif))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.AppUser.PhoneNumber))
                .ForMember(dest => dest.VenueName, opt => opt.MapFrom(src => src.Venue.Name));

            CreateMap<CreateManagerDTO, Manager>()
                .ForMember(dest => dest.AppUser, opt => opt.Ignore());

            // Client
            CreateMap<Client, ClientDTO>().ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.AppUser.Email))
                .ForMember(dest => dest.Nif, opt => opt.MapFrom(src => src.AppUser.Nif))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.AppUser.PhoneNumber));

            CreateMap<CreateClientDTO, Client>()
                .ForMember(dest => dest.AppUser, opt => opt.Ignore());

            // Venue
            CreateMap<Venue, VenueDTO>().ReverseMap();
            CreateMap<Venue, CreateVenueDTO>().ReverseMap();

            // Ticket
            CreateMap<Ticket, TicketDTO>()
                .ForMember(dest => dest.VenueName, opt => opt.MapFrom(src => src.Venue.Name))
                .ReverseMap();
            CreateMap<Ticket, CreateTicketDTO>().ReverseMap();

            // Subscription
            CreateMap<Subscription, SubscriptionDTO>().ReverseMap();
            CreateMap<Subscription, CreateSubscriptionDTO>().ReverseMap();

            // Sale
            CreateMap<Sale, SaleDTO>().ReverseMap();
            CreateMap<Sale, CreateSaleDTO>().ReverseMap();

            // SaleItem
            CreateMap<SaleItem, SaleItemDTO>().ReverseMap();
            CreateMap<SaleItem, CreateSaleItemDTO>().ReverseMap();


            // UserDTO para login / auth
            CreateMap<AppUser, UserDTO>()
                .ForMember(dest => dest.Role, opt => opt.Ignore());
        }
    }
}
