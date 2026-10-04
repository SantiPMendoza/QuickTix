using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Contracts.Enums
{
    public enum TicketType
    {
        NiñoLaboral,
        NiñoFestivo,
        AdultoLaboral,
        AdultoFestivo,
        JubiladoLaboral,
        JubiladoFestivo,
        Familiar,
        Grupo
    }

    public enum TicketContext
    {
        Normal,
        InvitadoAbonado
    }


    public enum SubscriptionCategory
    {
        Niño,
        Adulto,
        Jubilado,
        FamiliaNumerosa
    }

    public enum SubscriptionDuration
    {
        Quincenal,
        Mensual,
        Temporada
    }

    /// <summary>
    /// Medio de pago de una venta. De momento todo es efectivo; Card/Bizum se
    /// activarán cuando el ayuntamiento confirme datáfono/Bizum.
    /// </summary>
    public enum PaymentMethod
    {
        Cash = 0,
        Card = 1,
        Bizum = 2
    }


}
