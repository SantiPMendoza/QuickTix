namespace QuickTix.Contracts.Routes
{
    /// <summary>
    /// Contiene constantes centralizadas con las rutas HTTP de la API de QuickTix, orientadas al consumo desde clientes.
    /// Agrupa endpoints por recurso (User, Admin, Client, Manager, Ticket, Venue, Subscription, Pricing, Sale y SaleItem),
    /// incluyendo rutas CRUD base y rutas específicas (login, cambio de contraseña, históricos, ventas y pricing por Venue).
    /// </summary>
    public static class ApiRoutes
    {
        /// <summary>
        /// Rutas genéricas CRUD para recursos cuya base es /api/{resource}.
        /// Útil para clientes que construyen endpoints dinámicamente (p.ej. ViewModels base).
        /// </summary>
        public static class Crud
        {
            /// <summary>
            /// Construye la ruta base del recurso: /api/{resource}.
            /// </summary>
            /// <param name="resource">Nombre del recurso (p.ej. "Venue").</param>
            public static string Base(string resource) => $"/api/{resource}";

            /// <summary>
            /// Construye la ruta del recurso por id: /api/{resource}/{id}.
            /// </summary>
            /// <param name="resource">Nombre del recurso (p.ej. "Venue").</param>
            /// <param name="id">Identificador.</param>
            public static string ById(string resource, int id) => $"/api/{resource}/{id}";
        }

        public static class User
        {
            public const string Base = "/api/User";

            public const string GetAll = Base;
            public const string GetById = Base + "/{id}";

            public const string Register = Base + "/register";
            public const string Login = Base + "/login";
            public const string ChangePassword = Base + "/change-password";
        }

        public static class Admin
        {
            public const string Base = "/api/Admin";

            public const string GetAll = Base;
            public const string GetById = Base + "/{id:int}";
            public const string Create = Base;
            public const string Update = Base + "/{id:int}";
            public const string Delete = Base + "/{id:int}";
        }

        public static class Client
        {
            public const string Base = "/api/Client";

            public const string GetAll = Base;
            public const string GetById = Base + "/{id:int}";
            public const string Create = Base;
            public const string Update = Base + "/{id:int}";
            public const string Delete = Base + "/{id:int}";
        }

        public static class Manager
        {
            public const string Base = "/api/Manager";

            public const string GetAll = Base;
            public const string GetById = Base + "/{id:int}";
            public const string Create = Base;
            public const string Update = Base + "/{id:int}";
            public const string Delete = Base + "/{id:int}";
        }

        public static class Ticket
        {
            public const string Base = "/api/Ticket";

            public const string GetAll = Base;
            public const string GetById = Base + "/{id:int}";
            public const string Create = Base;
            public const string Update = Base + "/{id:int}";
            public const string Delete = Base + "/{id:int}";
        }

        public static class Venue
        {
            public const string Base = "/api/Venue";

            public const string GetAll = Base;
            public const string GetById = Base + "/{id:int}";
            public const string Create = Base;
            public const string Update = Base + "/{id:int}";
            public const string Delete = Base + "/{id:int}";
        }

        public static class Subscription
        {
            public const string Base = "/api/Subscription";

            public const string GetAll = Base;
            public const string GetById = Base + "/{id:int}";
            public const string Create = Base;
            public const string Update = Base + "/{id:int}";
            public const string Delete = Base + "/{id:int}";

            public const string ByClient = Base + "/by-client/{clientId:int}";

            /// <summary>
            /// Construye la ruta para obtener suscripciones por cliente.
            /// Evita reemplazos manuales de placeholders en consumidores.
            /// </summary>
            /// <param name="clientId">Identificador del cliente.</param>
            public static string ByClientId(int clientId) => Base + $"/by-client/{clientId}";

            public static string DeleteById(int id) => Base + $"/{id}";
        }

        public static class Pricing
        {
            public const string Base = "/api/Pricing";

            public const string GetVenuePriceMap = Base + "/venue/{venueId:int}";
            public const string UpsertVenuePriceMap = Base + "/venue/{venueId:int}";

            /// <summary>
            /// Construye la ruta para obtener el mapa de precios de un recinto.
            /// Evita reemplazos manuales de placeholders en consumidores.
            /// </summary>
            /// <param name="venueId">Identificador del recinto.</param>
            public static string GetVenuePriceMapByVenueId(int venueId) => Base + $"/venue/{venueId}";

            /// <summary>
            /// Construye la ruta para crear o actualizar (upsert) el mapa de precios de un recinto.
            /// Evita reemplazos manuales de placeholders en consumidores.
            /// </summary>
            /// <param name="venueId">Identificador del recinto.</param>
            public static string UpsertVenuePriceMapByVenueId(int venueId) => Base + $"/venue/{venueId}";
        }

        public static class Sale
        {
            public const string Base = "/api/Sale";

            public const string GetAll = Base;
            public const string GetById = Base + "/{id:int}";
            public const string Create = Base;
            public const string Update = Base + "/{id:int}";
            public const string Delete = Base + "/{id:int}";

            public const string HistoryTickets = Base + "/history/tickets";
            public const string HistoryTicketDetail = Base + "/history/tickets/{saleId:int}/detail";
            public const string HistorySubscriptions = Base + "/history/subscriptions";

            public const string SellTickets = Base + "/sell/tickets";
            public const string SellTicketsBatch = Base + "/sell/tickets/batch";
            public const string SellSubscription = Base + "/sell/subscription";

            /// <summary>
            /// Construye la ruta para obtener el detalle de una venta de tickets.
            /// Evita reemplazos manuales de placeholders en consumidores.
            /// </summary>
            /// <param name="saleId">Identificador de la venta.</param>
            public static string HistoryTicketDetailBySaleId(int saleId)
                => Base + $"/history/tickets/{saleId}/detail";
        }

        public static class SaleItem
        {
            public const string Base = "/api/SaleItem";

            public const string GetAll = Base;
            public const string GetById = Base + "/{id:int}";

            public const string Tickets = Base + "/tickets";
            public const string Subscriptions = Base + "/subscriptions";
            public const string BySale = Base + "/by-sale/{saleId:int}";

            /// <summary>
            /// Construye la ruta para obtener items por venta.
            /// Evita reemplazos manuales de placeholders en consumidores.
            /// </summary>
            /// <param name="saleId">Identificador de la venta.</param>
            public static string BySaleId(int saleId) => Base + $"/by-sale/{saleId}";

            

        }
    }
}
