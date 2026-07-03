<!-- docs: ARCHITECTURE_ADRS.md v1.0.0 — 2026-07-03 — bootstrap inicial de project-docs -->

# Architecture Decision Records: QuickTix

Las decisiones ADR-001 a ADR-004 se tomaron durante el desarrollo del TFG (2025-2026) y se
documentaron a posteriori el 2026-07-03; ADR-005 es de esa misma fecha.

## ADR-005 — SQLite in-memory para tests de integración

**Date**: 2026-07-03
**Status**: Accepted

### Context
Los tests de integración del flujo de ventas necesitan una base de datos real porque
`SaleRepository` usa transacciones explícitas (`BeginTransactionAsync`). Opciones: provider
EF InMemory, SQLite in-memory, SQL Server en contenedor.

### Decision
SQLite in-memory (`Microsoft.EntityFrameworkCore.Sqlite`, conexión `DataSource=:memory:`
mantenida abierta durante el test).

### Rationale
El provider InMemory no soporta transacciones — un test de atomicidad sobre él pasaría en
verde aunque el rollback estuviera roto (test que no puede fallar no es un test). SQL Server
en contenedor sería el máximo realismo pero encarece cada ejecución. SQLite da transacciones
reales con coste cero de arranque. Verificado: los índices filtrados con sintaxis de corchetes
de SQL Server (`[Nif] IS NOT NULL`) funcionan en SQLite (acepta identificadores entre corchetes).

### Consequences
- **Easier**: tests rápidos, sin dependencias externas, ejecutables en cualquier máquina/CI.
- **Harder**: diferencias de dialecto SQL Server/SQLite pueden ocultar problemas específicos
  del motor real (collations, tipos de fecha). Si aparece un bug dependiente de motor, se
  añadirá una suite pequeña contra el contenedor.

---

## ADR-004 — Cliente HTTP artesanal + catálogo de rutas compartido

**Date**: 2026-07-03 (decisión original: desarrollo TFG)
**Status**: Accepted

### Context
Desktop y Mobile necesitan consumir la API. Alternativas: cliente generado desde OpenAPI
(NSwag/Kiota), refit-style, o wrapper artesanal.

### Decision
Wrapper manual `HttpJsonClient` en cada cliente + constantes de ruta compartidas en
`QuickTix.Contracts/Routes/ApiRoutes.cs` + deserialización del envelope `ApiResponse<T>`.

### Rationale
Control total y cero toolchain de generación; las rutas compartidas por Contracts eliminan
strings mágicos. El coste de duplicar el wrapper entre los dos clientes es asumible con dos
consumidores propios.

### Consequences
- **Easier**: sin dependencia de generadores; el contrato vive en código compartido tipado.
- **Harder**: el wrapper está duplicado casi idéntico en Desktop y Mobile (cambios dobles);
  no hay validación automática de drift entre rutas declaradas y rutas reales de la API —
  los tests de integración de cliente serían la red futura.

---

## ADR-003 — Envelope propio `ApiResponse<T>` en lugar de ProblemDetails

**Date**: 2026-07-03 (decisión original: desarrollo TFG)
**Status**: Accepted

### Context
Se necesitaba un formato uniforme de respuesta para éxito y error consumible por ambos
clientes. El estándar sería HTTP status + ProblemDetails (RFC 9457) para errores.

### Decision
Envelope propio `ApiResponse<T>` (Ok/Fail, StatusCode, ErrorMessages, TraceId) en el 100%
de los endpoints, con `ApiExceptionFilter` centralizando la traducción de excepciones.

### Rationale
Uniformidad total (éxito Y error con la misma forma) y control del contrato. Como todos los
clientes son propios, el coste de no ser estándar es casi nulo. ProblemDetails solo cubre
errores y no aporta el TraceId+payload homogéneo que consumen los `HttpJsonClient`.

### Consequences
- **Easier**: deserialización única en clientes; correlación por TraceId; mensajes en español controlados.
- **Harder**: no estándar para consumidores terceros (irrelevante hoy); status HTTP duplicado
  dentro del body; convive un `ApiErrorResponseOLD` legacy pendiente de limpieza.

---

## ADR-002 — Frontera de transacción en cada repositorio

**Date**: 2026-07-03 (patrón original: desarrollo TFG)
**Status**: Accepted — **bajo revisión** (se reevaluará junto con la posible capa de servicios)

### Context
Cada repositorio implementa `IRepository<TEntity>` y persiste sus propios cambios
(`SaveAsync` → `SaveChangesAsync`). No hay Unit of Work compartido ni capa de servicios.
El `DbContext` de EF Core ya es un Unit of Work (ChangeTracker + SaveChanges atómico);
la cuestión de diseño es quién posee la frontera de transacción.

### Decision
Mantener (por ahora) la frontera dentro de cada repositorio, con la regla de disciplina:
**toda operación de negocio vive entera en un método de un repositorio**, que construye el
agregado completo y guarda una vez (patrón visible en `SaleRepository.SellTicketsBatchAsync`).

### Rationale
Funciona mientras cada operación quepa en un repo — que es el caso actual. Migrar a UoW
explícito (repos sin Save, el orquestador guarda al final) tiene sentido cuando exista un
lugar natural para esa frontera: una capa de servicios. Decidir la frontera antes de decidir
dónde vive la lógica sería ordenar las decisiones al revés.

### Consequences
- **Easier**: repos autocontenidos; cero indirección extra para CRUD simple.
- **Harder**: es disciplina, no estructura — nada impide que un endpoint futuro toque dos
  repos y pierda atomicidad (dos commits separados). El test de atomicidad de ventas vigila
  el invariante en la zona de mayor riesgo. **Trigger de revisión**: la primera operación de
  negocio que necesite dos repositorios (p. ej. venta + descuento de aforo en Venue) obliga
  a resolver esta decisión antes de implementarla.

---

## ADR-001 — Solución por capas con Contracts compartido

**Date**: 2026-07-03 (decisión original: desarrollo TFG)
**Status**: Accepted

### Context
Un backend y dos clientes nativos .NET desarrollados por una sola persona. Se necesitaba
compartir el contrato (DTOs, enums, rutas) sin duplicarlo.

### Decision
Capas API / Core (dominio) / DAL (datos) / Contracts (contrato compartido), donde Contracts
no referencia nada y es lo único que los clientes deberían referenciar (además de Core).

### Rationale
Separación clara de responsabilidades aprendible y defendible en un TFG; Contracts como
proyecto sin dependencias permite a los clientes compilar contra el contrato exacto del
servidor sin arrastrar EF ni ASP.NET.

### Consequences
- **Easier**: DTOs y rutas tipados y compartidos; cambios de contrato rompen en compilación, no en runtime.
- **Harder**: requiere disciplina en las referencias — Desktop la rompió (referencia API/DAL
  directamente, deuda registrada en PROGRESS.md). Core referencia Identity (entidades acopladas
  a ASP.NET Identity), aceptado como pragmatismo.
