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


}
