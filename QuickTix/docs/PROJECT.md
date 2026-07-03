<!-- docs: PROJECT.md v1.0.0 — 2026-07-03 — bootstrap inicial de project-docs -->

# QuickTix — Visión de producto

## Vision

QuickTix digitaliza la gestión de entradas y abonos de piscinas municipales pequeñas,
sustituyendo el papel y las hojas de cálculo de las taquillas de pueblo por un sistema
central con venta en taquilla, abonos por temporada y control multi-recinto. Nació como
TFG de DAM inspirado en tres veranos trabajando en la piscina de Nalda (La Rioja), y su
objetivo comercial es convertirse en el sistema de la piscina de Nalda, con potencial de
extenderse a otros municipios.

## Problem Statement

Las piscinas municipales pequeñas gestionan entradas y abonos a mano: tickets de papel,
listados de abonados en Excel, cuadre de caja manual. Eso produce colas, errores de cobro,
cero trazabilidad de aforo y ningún dato para el ayuntamiento. Las soluciones comerciales
existentes están pensadas para instalaciones grandes y su coste/complejidad no encaja en
un municipio de unos cientos de vecinos.

## Target Users

- **Primary — Manager de taquilla**: personal de temporada que vende entradas y abonos en
  la piscina. Necesita vender rápido (colas al sol), con precios correctos por tipo de
  entrada y sin conocimientos técnicos. Usa la app de escritorio (taquilla) o la móvil.
- **Primary — Administrador (ayuntamiento)**: configura recintos, precios, managers y
  consulta históricos de venta. Usa la app de escritorio.
- **Secondary — Abonado/vecino (client)**: consulta sus abonos desde el móvil. A futuro,
  compra online.
- **Explicitly not**: grandes instalaciones deportivas con torniquetes y hardware de
  control de accesos integrado.

## Core Features

1. **Venta en taquilla**: el manager vende entradas (individual o batch multi-línea) y
   abonos en segundos, con precios resueltos automáticamente por recinto, tipo y contexto.
2. **Abonos por temporada**: alta de abonados con NIF, categorías (niño/adulto/jubilado/
   familia numerosa) y duraciones (quincenal/mensual/temporada), con fecha de caducidad calculada.
3. **Entradas de invitado de abonado**: un abonado puede invitar acompañantes con precio
   distinto — regla de negocio real de las piscinas de pueblo.
4. **Precios por recinto**: cada piscina define su propia tabla de precios (entradas y
   abonos); el sistema soporta varios recintos desde el modelo de datos.
5. **Históricos de venta**: consulta de ventas de tickets y abonos con detalle por venta,
   base del futuro cierre de caja.
6. **Roles**: admin / manager / client con vistas y permisos separados en cada aplicación.

## MVP Scope

### In MVP (estado actual)
- Venta de tickets y abonos desde Desktop (taquilla) y Mobile (manager)
- Consulta de abonos propios desde Mobile (client, solo lectura)
- Gestión de recintos, precios, managers y clientes (admin, Desktop)
- Auth con Identity + JWT, roles admin/manager/client
- Históricos de ventas

### Deferred (post-MVP — candidatos para las iteraciones con Raquel)
- **QR de validación de acceso** (pieza clave para la venta al ayuntamiento)
- Control de aforo en tiempo real por recinto
- Cierre de caja e informes para el ayuntamiento
- Compra online desde la app de cliente (requiere pasarela de pago)
- Renovación automática/aviso de caducidad de abonos

## Success Criteria

### Demo (verano 2026)
- Una venta batch de taquilla se completa en menos de 30 segundos de principio a fin
- La demo ante Raquel corre con datos de muestra sin errores visibles ni textos de plantilla
- La estética de ambos clientes es coherente y presentable (sin restos de template MAUI)

### Producto (piloto)
- Cero ventas parciales: una venta fallida no deja rastro en base de datos (cubierto por test de atomicidad)
- Un manager nuevo aprende a vender sin manual en menos de 10 minutos
- El cuadre de caja de un día se obtiene en menos de 1 minuto
- Ningún secreto (claves, connection strings) en el repositorio antes de manejar datos reales

## Data & Privacy Model

El sistema tratará datos personales de vecinos, **incluidos menores** (abonos infantiles).
Antes de cualquier piloto con datos reales, esto es bloqueante (RGPD).

| Dato | Quién lo lee | Almacenamiento | Racional |
|---|---|---|---|
| NIF/NIE | Admin y manager del recinto | BD (índice único filtrado) | Identificación del abonado; también es la semilla de password inicial (deuda a eliminar) |
| Nombre, teléfono, email | Admin y manager | BD, texto plano | Contacto y gestión de abonos |
| Datos de menores (abonos niño) | Admin y manager | BD, texto plano | Categoría de abono; requiere consentimiento del tutor en piloto |
| Password | Nadie (hash Identity) | Hash en BD; **hoy también en claro en clientes** (deuda crítica) | Autenticación |
| Historial de compras | Admin y manager | BD | Históricos y cierre de caja |

## Constraints

1. **Presupuesto municipal pequeño**: el coste de infraestructura debe ser mínimo (un
   servidor modesto o hosting económico).
2. **Usuarios no técnicos**: managers de temporada; la UI de taquilla debe ser obvia.
3. **RGPD antes de piloto**: secretos fuera del repo, passwords seguras, datos de menores
   con base legal — no negociable con datos reales.
4. **Estacionalidad**: la piscina abre en verano; las ventanas de cambio grandes son
   fuera de temporada.

## Development Phases

### Phase 0 — TFG (completada)
Arquitectura por capas, API completa, clientes Desktop y Mobile funcionales, defensa del TFG.

### Phase 1 — Sprint de demo (actual, verano 2026)
Estética y pulido de ambos clientes, eliminación de restos de plantilla, features de
"producto vivo" para la demo con Raquel (alcaldesa de Nalda). Iteración continua con su feedback.

### Phase 2 — Cierre de producto
QR de acceso, aforo en tiempo real, cierre de caja, seguridad de producción (secretos,
passwords, JWT), compra online si Raquel la valida.

### Phase 3 — Piloto en Nalda
Despliegue real, datos reales (RGPD resuelto), temporada completa en producción.

## Terminology

Los términos de dominio se usan siempre en inglés, tal como están en el código:

- **Venue**: recinto/piscina (con `Capacity`). El sistema es multi-venue desde el modelo.
- **Ticket**: entrada individual de un día. 8 tipos (`TicketType`: niño/adulto/jubilado ×
  laboral/festivo, familiar, grupo).
- **TicketContext**: `Normal` o `InvitadoAbonado` (entrada de invitado de un abonado, con ClientId asociado).
- **Subscription**: abono. Categorías (`SubscriptionCategory`) y duraciones (`SubscriptionDuration`).
- **Sale / SaleItem**: venta (cabecera) y sus líneas; una línea referencia un Ticket o una Subscription.
- **Manager**: taquillero/a asignado a un Venue. **Client**: abonado/vecino. **Admin**: ayuntamiento.

## Open Questions

- Estrategia de QR: ¿QR por ticket/abono validado desde la app móvil del manager, o hardware dedicado?
- Pasarela de pago para compra online: ¿cuál encaja con un ayuntamiento (coste/contratación)?
- Hosting del piloto: ¿servidor municipal, VPS, o nube gestionada?
- ¿Necesita Raquel informes específicos (formato/periodicidad) para intervención municipal?
