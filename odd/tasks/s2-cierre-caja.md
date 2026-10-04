# S2 — Cierre de caja (arqueo) + anulación lógica + PaymentMethod

> Piloto v7 (gentle-ai v4 ODD + pila sAI-Stack) · **fila 4** (migración EF sobre datos existentes + anulación contable) · slug ledger `quicktix`
> Rama base: `main` @ `fe15aef` (tras mergear vibra-s0, PR #22). Entrega: **stacked-to-main** (3 slices).

## Objetivo
Que el admin pueda cerrar caja: informe por rango de días (hora local Europe/Madrid) con desglose por recinto → vendedor (gestor o «Administración»), por tipo de concepto y por medio de pago, exportable a CSV; y que pueda **anular** una venta sin borrarla, quedando fuera de los totales del arqueo y del Panel.

## Problema / por qué
- Hoy no existe anulación: solo un DELETE físico genérico (`BaseController.Delete`) que cualquier admin o manager puede llamar y que borra el agregado sin rastro → falsea cualquier arqueo.
- Todas las fechas se consultan en UTC: una venta a las 23:30 de Madrid en verano computa como «mañana».
- `PaymentMethod` no existe; la reunión con Raquel decidirá datáfono/Bizum. S2 lo diseña sin activarlo (todo Cash).

## Decisiones de Santi (2026-10-04)
- Anulación **lógica, solo admin**, sin límite de día, motivo obligatorio. Ventas anuladas fuera de totales del arqueo y del Panel; el arqueo las lista aparte. DELETE físico de ventas bloqueado. Ticket/Subscription asociados NO se tocan (política de Raquel pendiente).
- Panel (Analytics) pasa también a «día» en hora de Madrid con el mismo helper.
- Superficie: **página nueva en Desktop «Cierre de caja»** (solo admin) + acción «Anular» en Historial de ventas. MAUI sin pantalla nueva.
- Entrega stacked-to-main.

## Alcance autorizado
- Core/Contracts: enum `PaymentMethod` (Cash, Card, Bizum), campos en `Sale`: `PaymentMethod` (default Cash), `VoidedAt` (DateTime? UTC), `VoidedByUserId` (string?), `VoidReason` (string?, máx. 200).
- DAL: migración `S2CashCloseAndVoid` (columnas nuevas; filas existentes → Cash, no anuladas). Helper de zona horaria (día local → rango UTC `[inicio, fin)`, correcto en cambios de hora).
- API: `POST api/Sale/{id}/void` (admin), DELETE de ventas → rechazado; history DTOs exponen `IsVoided`/`VoidReason`/`PaymentMethod`; `GET api/Reports/cash-close?from=&to=` (admin, fechas locales inclusivas) → `ApiResponse<CashCloseReportDTO>`. Analytics: excluye anuladas y usa día local; la anulación invalida la caché de Analytics.
- Desktop: página «Cierre de caja» (rango, totales, desglose, sección de anuladas, exportar CSV con CsvHelper); en «Historial de ventas» badge «Anulada» y botón «Anular» con motivo (VibraDialog).
- Fuera de alcance: selector de medio de pago en clientes, reglas de anulación por manager/mismo día, revertir Ticket/Subscription, PDF, cierre por turno.

## Escenarios de aceptación (Dado / Cuando / Entonces)

### Superficie principal — Desktop · página «Cierre de caja» (admin)
- **E1** Dado ventas de hoy en la Piscina (gestor) y una venta de abono hecha por el admin, cuando el admin abre «Cierre de caja» con rango hoy–hoy, entonces ve el total del día, y el desglose Piscina → «Gestor Piscina» y → «Administración», por concepto y con medio de pago «Efectivo».
- **E2** Dado una venta hecha a las 23:30 hora de Madrid (21:30 UTC en verano), cuando se consulta el arqueo de ese día, entonces la venta entra en ese día y no en el siguiente; y una venta a las 00:30 de Madrid (22:30 UTC del día anterior) entra en el día siguiente.
- **E3** Dado un arqueo cargado, cuando el admin pulsa «Exportar CSV», entonces se guarda un fichero con una fila por línea de venta (fecha local, recinto, vendedor, concepto, cantidad, precio, subtotal, medio de pago, anulada) cuyos importes suman el total mostrado (las anuladas marcadas y fuera del total).
- **E4** Dado un usuario manager, cuando llama a `GET api/Reports/cash-close`, entonces recibe 403; sin token, 401.
- **E5** Dado `from > to` o un rango mayor de 366 días, entonces la API responde 400 con mensaje en español.

### Otra superficie — Desktop · «Historial de ventas» (pestañas Entradas y Abonos)
- **E6** Dado una venta no anulada, cuando el admin pulsa «Anular» e indica motivo, entonces la fila queda marcada «Anulada» (con motivo) en la pestaña correspondiente y sigue visible en el historial.
- **E7** Dado una venta ya anulada, cuando se intenta anular de nuevo, entonces la API responde 409 y Desktop muestra el aviso en VibraDialog; sin motivo → 400.
- **E8** Dado un manager autenticado, cuando llama a `POST api/Sale/{id}/void` o a `DELETE api/Sale/{id}`, entonces 403 / rechazo; y como admin, `DELETE api/Sale/{id}` también se rechaza (la vía es anular).

### Otra superficie — Desktop · «Panel» (Analytics)
- **E9** Dado una venta incluida en los KPIs de hoy, cuando el admin la anula en «Historial de ventas» y pulsa «Actualizar» en el Panel, entonces los ingresos de hoy, el desglose por tipo/donut y el acumulado de temporada bajan en su importe **sin esperar al TTL de 30 s**.
- **E10** Dado una venta a las 23:30 de Madrid, entonces el Panel la cuenta en «hoy» igual que el arqueo (misma definición de día).

### Otra superficie — MAUI · gestor (`TicketsPage`, venta en lote)
- **E11** Dado el gestor vende un lote de entradas desde el móvil, cuando el admin refresca «Cierre de caja», entonces aparece la venta bajo Piscina → «Gestor Piscina» con medio «Efectivo» (default del servidor; el móvil no cambia).

### Otra superficie — MAUI · cliente (`SubscriptionsPage`)
- **E12** Dado un abono vendido a un cliente y luego anulada su venta, cuando el cliente abre «Mis abonos», entonces el abono **sigue apareciendo** (comportamiento documentado: revertir el abono depende de la política de Raquel) — y el arqueo/Panel ya no cuentan su importe.

### Datos existentes (migración)
- **E13** Dado la base de datos con ventas previas (copia de la BD real), cuando la API arranca y aplica la migración, entonces todas las ventas existentes quedan `PaymentMethod = Cash`, no anuladas, y el historial y el Panel muestran los mismos totales que antes.

## Checks
- Suelo: `dotnet build QuickTix.sln` y `dotnet test QuickTix.sln` verdes antes de cada commit; registrados en el ledger.
- Test-first (xUnit, SQLite in-memory, ADR-005): RED → GREEN para helper de zona horaria (incl. días de cambio de hora), void (ok / doble anulación / sin motivo), exclusión de anuladas en arqueo y Analytics, agrupación «Administración», rango inclusivo local.
- **Smoke que cruza superficies** (con Santi, sobre COPIA de la BD real `QuickTix_SQL_Server`):
  1. Copia de la BD → arrancar API (migración aplicada) → comprobar E13 (totales de historial/Panel iguales a antes).
  2. MAUI (emulador) como gestor: vender un lote → Desktop admin «Cierre de caja»: aparece (E11).
  3. Desktop admin: vender un abono como Administración → arqueo lo muestra en «Administración» (E1).
  4. Desktop «Historial de ventas»: anular la venta del paso 2 → «Panel» → Actualizar: KPIs bajan al momento (E9) → «Cierre de caja»: sale de totales y aparece en anuladas (E6).
  5. Exportar CSV y cuadrar la suma con el total (E3).
  6. MAUI como cliente: el abono anulado sigue visible (E12).
  7. Swagger: 401/403 de manager en cash-close, void y delete (E4, E8).

## Tareas
Ruta por tarea y evidencia del disparador anotadas en cada una. Previsión: ~950 líneas escritas (sin migración generada) → stacked-to-main.

- [ ] **T1 — Modelo, migración y día local** (slice 1, rama `feature/s2-cierre-caja`)
  `PaymentMethod` + campos de anulación en `Sale`, migración `S2CashCloseAndVoid`, helper Europe/Madrid, Analytics a día local + exclusión de anuladas + invalidación de caché expuesta. Tests RED→GREEN.
  Ruta: delegated direct (writer; >2 ficheros no triviales: entidad, DbContext, helper, AnalyticsRepository, tests).
- [ ] **T2 — API de anulación y arqueo** (slice 2)
  `POST void`, DELETE de ventas bloqueado, DTOs de historial con `IsVoided`/`PaymentMethod`, `ReportsController` + `CashCloseRepository` + DTOs + rutas en `ApiRoutes`. Tests RED→GREEN.
  Ruta: delegated direct (writer).
- [ ] **T3 — Desktop** (slice 3)
  Página «Cierre de caja» (VM + View + DI + menú), CSV con CsvHelper, «Anular» y badge en «Historial de ventas».
  Ruta: delegated direct (writer).
- [ ] **T4 — Verificación y cierre**
  verify:directed (opus) sobre el rango completo, RDD según `review assess`, smoke con Santi, pasada `human`, ledger + mem_save.

## Progreso / evidencia
(vacío)

## Siguiente paso
T1.
