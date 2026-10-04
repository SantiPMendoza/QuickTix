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
### T1 — commits `e9d402d` (feat) + `b738531` (test, corrector)
- Ruta: delegated direct (writer sonnet). RED: solo de compilación (CS0234, faltaba `QuickTix.Core.Time`); sin RED de comportamiento → excepción declarada. GREEN: 19/19, luego 20/20.
- Suelo: `dotnet build QuickTix.sln` 0 errores; `dotnet test QuickTix.sln` 20/20 (relanzado por el padre).
- Lectura orquestador: AnalyticsRepository (filtro `VoidedAt == null` en las 4 consultas de ventas; KPIs de abonos vienen de `Subscriptions`, coherente con E12) y LocalBusinessDay.
- RDD: assess medium, `review_due` por `slice_budget_reached` → consentimiento de Santi: granted → 1 lente (reliability, 54 s) → **approved**, acusado (lineage `review-e22b14fd525af4e2`). 3 avisos no bloqueantes (regla J):
  - R3-nondeterministic-default-clock → **sonda**: test Theory con reloj fijo en 2026-12-31T23:30Z → RED (esperado 42, real 32) — defecto real del test (año UTC vs año local). Corregido en `b738531`; diff-review pendiente.
  - R3-dailyrevenue-kind-change → sonda de código: único consumidor `PanelViewModel.BuildRevenueBars` usa `d.Date.ToString("ddd")` sin conversión de zona; Mobile no lo consume. Confirmación visual en smoke (etiquetas de barras del Panel).
  - R3-tz-resolution-failure → pendiente sonda con Docker arriba: `aspnet:8.0` (Dockerfile.txt) ¿trae tzdata? En Windows resuelve (tests verdes).

- diff-review de `b738531` (capa tras corrector, 4f): fix correcto, 0 graves/medios; 3 bajos (expectativa calculada con el mismo helper, reloj por defecto latente, 2 comentarios) → aplicados en T2.

### T2 — rama `feature/s2-t2-api` (apilada sobre T1)
- Ruta: delegated direct (writer sonnet). RED de comportamiento: stubs compilables → 17 fallos / 22 verdes. GREEN 39/39 (destapó y corrigió `Sum` decimal en SQL en `GetTicketHistoryAsync`, sin test previo sobre SQLite).
- Suelo: `dotnet build QuickTix.sln --no-incremental` 0 errores (177 warnings preexistentes); `dotnet test` 39/39 (relanzado por el padre).
- Lectura orquestador: `VoidAsync` (UPDATE condicionado `VoidedAt == null`, atómico; distingue NotFound/AlreadyVoided), endpoint void (invalida caché de Analytics), override Update/Delete; SaleItemController solo GET.
- Hueco declarado: guard del PUT y mapeo viven en API (tests solo referencian DAL) → comprobación en smoke (Swagger PUT sobre venta anulada).
- Para T3: `PaymentMethod` se serializa como número; líneas del informe agrupadas por venta+concepto+precio.

- Commit `e08502b`. RDD: assess medium, `slice_budget_reached` → consentimiento Santi: granted → 1 lente (reliability, 1 min 32 s) → **approved**, acusado (`review-05331ded6fe4f84f`). 5 avisos (regla J):
  - W SaleController:218-229 guard PUT sin test + check-then-act → **smoke** (Swagger PUT sobre venta anulada → 400; PUT sobre no anulada conserva VoidedAt/PaymentMethod).
  - W SaleController:254-294 void/delete sin aserción HTTP (mapeo 200/404/409/400, invalidación en controller, userId de claims) → **smoke** (E7, E8, E9 desde Desktop/Swagger).
  - S ReportsController:51-67 parseo de fechas sin test → **smoke** (from ausente / `2026-13-01` → 400 con envelope).
  - S ApiRoutes:205 cultura actual al formatear fechas → **test** en T3 (cultura no gregoriana).
  - S VoidSaleDTO:14 MaxLength sin recortar vs repo recortando → **test** en T3 (DataAnnotations).

### T3 — rama `feature/s2-t3-desktop` (apilada sobre T2), commit `3307f94`
- Ruta: delegated direct (writer sonnet). RED 4a/4b: 3/6 fallan (th-TH → `2569-07-15`, ar-SA → `1448-02-01`, 200 chars con espacios rechazado) → GREEN 6/6. Desktop sin tests de VM: solo build.
- Suelo: build 0 errores (177 warnings preexistentes); `dotnet test` 45/45 (relanzado por el padre).
- RDD: assess medium, `slice_budget_reached` → Santi: granted → 1 lente (reliability, 1 min 42 s) → **approved**, acusado (`review-c0641b30ae64eadd`). 6 avisos (regla J, pendientes de sonda):
  - W CashCloseCsvBuilder:48-103 sin test (BOM, `;`, decimales, Sí/No, escape).
  - W SalesHistoryViewModelBase:133-165 excepción no-ApiException tras POST confirmado → error falso; nuevo `PostAsync` sin resultado sin test.
  - W CashCloseViewModel:118-137 informe anterior sobrevive a un fallo / a otra sesión (singleton), export habilitado sobre datos viejos.
  - S CashCloseViewModel:154 ErrorMessage no se limpia al reexportar.
  - S CashCloseCsvBuilder:68-70 InjectionOptions.Escape incluye «-» → importe negativo como texto.
  - S MainViewModel:57-62 ítem admin oculto si el VM se construye con sesión ya iniciada.
- verify:directed (opus) sobre `fe15aef..3307f94`: en curso.

- verify:directed (opus, 6 min 38 s): Parte A 9 afirmaciones — confirmadas en código; 1b Down parcial (rollback des-anula); 5 refutada «en espíritu» (B1); 4/8/9 sin verificación HTTP/runtime. Parte B: B1 MAJOR force-delete Ticket/Subscription borra SaleItems (cualquier rol); B2 MAJOR POST api/Sale genérico; B3 MAJOR? borrar Venue cascada a Sales; B4 PUT ventas no persiste; B5 400 ProblemDetails sin envelope; B6 carrera de invalidación de caché; B7 Historial en hora UTC vs arqueo Madrid; B8 Logout no llama IAuthService.Logout; B9 «hoy» fijado al crear el singleton; B10 warning EF HasDefaultValue; B11 tz en Docker sin ruta de contenedor funcional; B12 Mobile caducidad por día UTC (fuera de alcance).

### Ampliación de alcance (Santi, 2026-10-04): B1, B2, B3+B4, B7 + proyecto `QuickTix.Desktop.Tests`
- **E14** Dado un abono o entrada con líneas de venta, cuando cualquiera intenta borrarlo (con o sin `force`), entonces 409 y nada se borra; solo admin llega al endpoint; Desktop (Clientes) ya no ofrece «forzar».
- **E15** Dado un manager o admin, cuando hace `POST api/Sale` genérico, entonces se rechaza (las ventas entran por `sell/*`).
- **E16** Dado un recinto con ventas, cuando se intenta borrar, entonces se rechaza; `PUT api/Sale/{id}` se rechaza siempre.
- **E17** Dada una venta a las 21:30 UTC en verano, cuando se mira en «Historial de ventas», entonces muestra 23:30 (hora de Madrid), igual que el arqueo.

### T4 — correcciones (rama `feature/s2-t4-fixes`, apilada sobre T3)
- Ruta: delegated direct (writer sonnet). Alcance: B1, B2, B3, B4, B5, B6 (si es barato), B7, B8, B9 + 6 avisos RDD de T3 + Desktop.Tests (CSV builder, EsFormat). B10/B11/B12 → board.

- Resultado writer: B1 (RED 3/6 → 6/6 DeleteGuardTests, venue incluido), B3 (400, no 409: no hay excepción de conflicto en Core), B6 hecho (contador de generación; RED «real 8, esperado 0» con interceptor → verde), B7 (RED 3/3 → verde; sin `.ToLocalTime()` en consumidores, Mobile no usa esos DTOs), R1/R5 (Desktop.Tests 21/21; RED 1 en importe negativo), R2 (HttpJsonClientTests). Sin test (capa API/VM WPF) → smoke: B2, B4, B5, B8, B9, R3, R4, R6.
- Suelo: build 0 errores; `dotnet test` QuickTix.Tests 55/55 + QuickTix.Desktop.Tests 21/21 = 76/76 (relanzado por el padre).
- Nuevo para el board: borrar un Client ¿cascada a Subscriptions → SaleItems? (misma clase que B1/B3, sin comprobar).

- Commit `1ecff6e`. RDD: assess medium `slice_budget_reached` → Santi: granted → 1 lente (reliability, 1 min 30 s) → **approved**, acusado (`review-127eab504f252771`). 4 avisos (regla J):
  - W CashCloseViewModel:116-122 `EnsureDefaultDates` pisa AMBAS fechas si se vacía una (determinista, introducido) → corrector.
  - W ApiBehaviorExtensions:19-38 fábrica B5 sin test real de la API → smoke (Swagger void con body vacío) o test directo de la fábrica.
  - S SaleController:218-219 overrides POST/PUT sin test de rutas/roles → smoke.
  - S VenueRepository:177-179 la guarda solo mira `Sales.VenueId`; tickets/abonos del recinto con SaleItems en ventas de OTRO recinto caen en cascada; check-then-act no atómico → sonda/test en corrector.
- diff-review de `1ecff6e`: 1 MAJOR introducido (`EnsureDefaultDates` en «Consultar» salta a D+1 tras medianoche: arqueo/CSV del día equivocado; el marcador «tocado» nunca se limpia), 3 MINOR (una fecha null pisa ambas — mismo que RDD; venue 400 no 409 + check-then-act; test DiaSemanaString tautológico). **Contradice** el aviso RDD de venue (SaleItem→Ticket/Subscription Restrict → error FK → 409) — por regla J se prueba con test, no se acepta razonado. Rutas de borrado B1/B3 cerradas; B6 correcto; B7 sin doble conversión. Hueco global: otros VMs singleton conservan datos tras logout (preexistente → board).

### T4b — corrector 2 (misma rama)
- Ruta: delegated direct (writer sonnet). Alcance: fix de fechas por defecto (solo en navegación, respeta edición, validación con una null), sonda venue cruzado, sonda borrar Client con abono vendido, test tautológico.

## Siguiente paso
T4b → diff-review (capa tras corrector) → smoke con Santi.
