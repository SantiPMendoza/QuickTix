using AutoMapper;
using QuickTix.Core.Models.DTOs;
using QuickTix.Core.Models.DTOs.UserAuthDTO;
using QuickTix.Core.Models.Entities;

namespace QuickTix.API.AutoMapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Admin
            CreateMap<Admin, AdminDTO>().ReverseMap();
            CreateMap<Admin, CreateAdminDTO>().ReverseMap();

            // Manager
            CreateMap<Manager, ManagerDTO>()
                .ForMember(dest => dest.VenueName, opt => opt.MapFrom(src => src.Venue.Name))
                .ReverseMap();
            CreateMap<Manager, CreateManagerDTO>().ReverseMap();

            // Client
            CreateMap<Client, ClientDTO>().ReverseMap();
            CreateMap<Client, CreateClientDTO>().ReverseMap();

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
