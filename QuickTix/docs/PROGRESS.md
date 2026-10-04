<!-- docs: PROGRESS.md v1.0.0 — 2026-07-03 — bootstrap inicial de project-docs -->

# Progress: QuickTix

## Current State

Fase 1 (sprint de demo para Raquel): **S0 y S1 IMPLEMENTADOS** en la rama `feature/vibra-s0`
(17 commits, pusheada a origin) durante la sesión frontend-only con remote control.
Verificado SOLO con `dotnet build` (todo verde) — **NO mergear a main** hasta pasar la
lista de pruebas manuales de abajo. Gotchas de la sesión: en esta máquina falta el
Android SDK API 34, así que Mobile solo se compiló con el target Windows (el target
Android está SIN compilar); `dotnet test` tampoco se ejecutó (restricción de la sesión).
Siguiente trabajo de sprint: S2 (cierre de caja).

**Last updated**: 2026-07-07 (sesión 3, auditoría estática del front)

## Pruebas manuales pendientes (antes de mergear `feature/vibra-s0`)

Checklist para cuando Santi llegue a casa. Si algo falla, se corrige en la propia rama.

> **Auditoría estática 2026-07-07**: dos agentes revisaron TODO el front (recursos XAML,
> fuentes, bindings contra ViewModels, triggers). Resultado: todos los StaticResource
> resuelven (73 claves Mobile, 0 colgantes en Desktop), todos los bindings apuntan a
> miembros reales, fuentes bien registradas. Se encontró y corrigió 1 bug grave:
> **el pane del sidebar Desktop no volvía a aparecer tras el login** (`IsPaneVisible`
> se ponía a false en LoginView y nunca se restauraba). También: `HeaderVisibility="Collapsed"`
> añadido preventivamente, MainWindow muerto de la raíz eliminado, `Visual="Material"`
> (no-op de Xamarin) fuera de 2 páginas Mobile. Builds verdes: Desktop, Mobile Windows
> y Mobile **Android**. La checklist visual de abajo sigue pendiente de ojos humanos.

**0. Builds/tests que faltaron en la sesión**
- [x] `dotnet build QuickTix.Mobile` con target **Android** — verde tras instalar la plataforma API 34 con `dotnet build -t:InstallAndroidDependencies` (2026-07-07)
- [x] `dotnet test` completo — 2/2 verdes (2026-07-07)

**1. Mobile (emulador o dispositivo)**
- [ ] Splash y appicon nuevos (resizetizer: recorte del icono adaptativo, tamaño del logo en splash)
- [ ] Fuentes Space Grotesk / DM Sans / IBM Plex Mono renderizan en Android
- [ ] Login: degradado llega al borde superior (sin NavigationBar), sombras de campos/botón, botón deshabilitado (gris) con campos vacíos
- [ ] Abonos: carrusel con peek, punto activo alargado del indicador (si no se estira, degrada a puntos uniformes — aceptable), tarjeta caducada gris vía `IsExpired`, legibilidad del texto sobre degradado, doble título navbar+página (¿molesta?)
- [ ] POS gestor: chips de color envolviendo pickers (altura en Android), acento teal 4px respeta el radio de tarjeta, venta batch funciona igual que antes
- [ ] Sin XamlParseException en TicketsPage (los 5 StaticResources rotos ya están definidos — confirmar en runtime)

**2. Desktop (WPF)**
- [ ] Sidebar oscuro: pane del NavigationView transparente sobre el fondo InkSidebar; alternancia al login/logout (FIX 2026-07-07: el pane no reaparecía tras el login — confirmar visualmente que ahora sí)
- [ ] Ítem activo del sidebar con degradado + barra izquierda; los 5 iconos mapeados por DataTrigger aparecen (verificado estáticamente: los 5 valores coinciden, incluido el LF de "Historial de\nventas" vía `&#10;` — solo falta verlo)
- [x] Título/breadcrumb duplicado: `HeaderVisibility="Collapsed"` añadido preventivamente al NavigationView (2026-07-07) — cada página pinta su propio título, así que colapsar es seguro en ambos casos
- [ ] TitleBar oscura: contraste del hover en min/max/close
- [ ] Popups de UsersView/ClientsView: abren centrados y con sombra (se quitó el velo azulado)
- [ ] PricingView: doble clic en Precio → editor mono con borde de foco; guardar precios funciona
- [ ] Tablas: filas alternas, hover, selección; formato `N2 €` y fechas `dd/MM/yyyy`
- [ ] **Fix managerId**: vender abono como MANAGER usa su id real del JWT; como ADMIN ahora da error inline (antes vendía en silencio como manager 1) → decidir producto: ¿admin elige manager o la API acepta ventas de admin?
- [ ] Panel: tras login se aterriza en "Panel"; KPIs/barras/donut/tabla con datos del seed; con BD limpia no crashea; con la API apagada muestra error inline y "Actualizar" recupera
- [ ] Swagger: GET `/api/Analytics/summary` → 401 sin token, 403 como manager/client, 200 como admin; caché TTL 30 s (una venta tarda ≤30 s en reflejarse)
- [ ] Zona horaria: `Sale.Date` es UTC — una venta a última hora (España = UTC+2 en verano) computa como "mañana" en los KPIs de hoy. ¿Molesta para la demo?

**2b. Fixes visuales encontrados en las pasadas (2026-07-07) — corregir en la rama**
- [ ] Sidebar iconos (3er intento, pendiente de VERLO): `Icon` asignado en MainViewModel vía SymbolIcon nativo (los 2 intentos por triggers de plantilla fallaron: SymbolIcon construye el glifo perezosamente y no reacciona a Symbol por trigger) + activo exacto del handoff (gradiente .22/.14, barra 3px, blanco, radio 12)
- [ ] Eliminar abono: el 2º modal era el MessageBox del force-delete 409 — que salta SIEMPRE porque todo abono vendido tiene SaleItems (el "conflicto" es el camino normal). Ahora: 1 confirm + diálogo de conflicto solo si 409 real. Verificar el flujo completo incl. "Eliminar todo"
- [ ] Los 18 MessageBox de Desktop convertidos a VibraDialog (alerta reutilizable en BaseCrudViewModel + confirm de borrado por diálogo). OJO en la pasada: el aviso "Sesión iniciada" del login ahora navega al Panel AL CERRARLO
- [ ] VibraDialog con más relieve (DialogShadow 13/34/0.5 + borde con brillo superior). ¿Suficiente?
- [ ] **Mobile: tarjetas del carrusel por categoría** — Niño=sky (oscurecido para contraste), Adulto=teal héroe, Jubilado=lavanda, FamiliaNumerosa=periwinkle; caducado gris siempre gana. Verificar legibilidad en emulador
- [ ] Limitación conocida (revisión adversaria): los VibraDialog no son modales de verdad — se pueden abrir 2 a la vez (p.ej. confirm de cliente + confirm de abono). El bug de "borrar el registro equivocado" que esto permitía YA está corregido (snapshot PendingDeleteItem); la exclusión mutua de diálogos queda como mejora si molesta en la práctica
- [x] Modales de añadir/editar (Users/Clients): TextBox a ancho completo de tarjeta (2026-07-07)
- [x] Dashboard: tooltip "56.0000" → "56,00 €" preformateado en VM (StringFormat se ignora en ToolTip) (2026-07-07)
- [x] Modal de clientes: chips de abono por color de categoría (2026-07-07)
- [x] Venta de abono como admin bloqueada → CORREGIDO 2026-07-07: en Nalda los administrativos del ayuntamiento (admins de la app) son quienes venden los abonos (ver Decision Log)

**3. Si todo pasa**: merge de `feature/vibra-s0` a main + borrar la rama.

**Seguimiento del fix "venta por admin" (revisión adversaria 2026-07-07)**
- La migración `SaleManagerOptional` tiene `Down()` insegura: si existen ventas con manager null, el rollback falla (NOT NULL sin backfill). Aceptado — en este flujo no se hacen rollbacks de migraciones; si alguna vez hiciera falta, añadir un `Sql()` de backfill antes del `AlterColumn`.
- Hueco de tests: `GetSubscriptionHistoryAsync` (reescrita para evitar SQL APPLY en SQLite) no tiene test del camino CON manager ni de venta con varias líneas (invariante N items → N filas). Añadir cuando se toque ventas otra vez.

**Hallazgos fuera de alcance de la rama (apuntados, NO tocados)**
- SalesView detalle: la columna "INVITADO POR" binde `TicketSales.InvitedByClientName` (escalar del VM vía RelativeSource), no una propiedad por línea → todas las filas muestran el mismo valor. Preexistente al restyling; revisar si una venta puede mezclar líneas invitadas/no invitadas.
- Mobile csproj: `MauiIcon` sin `ForegroundFile`/`BaseSize` — el launcher adaptativo de Android enmascara ~33% del borde; si el icono sale recortado (checklist Mobile punto 1), la solución es separar capa foreground + `ForegroundScale`.
- Mobile `Styles.xaml`: el estilo implícito de `Shadow` usa brush blanco con offset 10,10 — hoy nadie lo usa (todas las sombras Vibra lo sobreescriben), pero un `<Shadow/>` a pelo futuro sería invisible.

## Board

### TODO
Sprint demo, en orden (0 → 1 → 2 → 3). Diseño: **dirección "1b Vibra"** — handoff completo con
tokens, tipografías y specs por pantalla en `reference/App redesign directions/design_handoff_quicktix_vibra/README.md`:
- [ ] (S2) Cierre de caja: informe por día/rango y venue/manager (+ bucket "Administración") + export CSV (CsvHelper ya referenciado en Desktop). Requisitos añadidos 2026-07-07: **anulación de venta entra sí o sí** (política exacta pendiente de Raquel — ver `docs/reunion-raquel.md` §2); el "día" del arqueo se define en hora local Europe/Madrid, NO en UTC; diseñar campo `PaymentMethod` en Sale aunque de momento todo sea efectivo (activación cuando Raquel confirme datáfono/Bizum); vocabulario de intervención (arqueo, desglose por tipo y medio de pago)
- [ ] (S3) Venue genérico, versión demo: `VenueType` + textos de UI + seed con recinto no-piscina (el handoff ya usa "Pabellón Sócon" — refuerza la narrativa) (NO generalizar TicketType — ver Pending Decisions)

Fuera del sprint:
- [ ] Fix: unificar la doble ruta de precios (SubscriptionController usa CalculatePrice [Obsolete]; SaleController usa el sistema real)
- [ ] Feature: impresión térmica de entradas en puerta (flujo de entrada de visitantes elegido — pendiente decisión de hardware)
- [ ] Seguridad pre-piloto: secretos fuera del repo, passwords iniciales, password en claro en clientes, JWT (lote completo en ARCHITECTURE.md § Security)
- [ ] Aprendizaje (tutor) — Tema 2: capas y dónde vive la lógica (JWT en UserRepository, refs Desktop→DAL/API, ¿capa de servicios?) → resuelve la decisión UoW
- [ ] Aprendizaje (tutor) — Tema 3: seguridad de salida a producción
- [ ] Aprendizaje (tutor) — Tema 5: limpieza de contratos (ApiErrorResponseOLD, namespaces DTO duplicados, código [Obsolete])

### IN PROGRESS
- [ ] Validación manual de `feature/vibra-s0` en casa (checklist arriba) → merge a main + borrar rama
- [ ] (S1.5) Panel v2 — quick wins pre-demo del análisis de research: donut en € (no unidades), KPI "abonos que caducan ≤7 días", acumulado de temporada, fix tooltip "56.0000" → "56,00 €"
- [ ] Fixes visuales Desktop de la sección 2b (sidebar iconos + resalte activo, anchura TextBoxes en modales, componente común VibraDialog + confirmación de borrado, colores por categoría de abono)

### DONE
- [x] (S1) Panel (3a): endpoint read-only `/api/Analytics/summary` + vista Panel inicial en Desktop con KPI "Aforo estimado hoy" — en `feature/vibra-s0`, pendiente de prueba manual (2026-07-03)
- [x] (S0) Restyling Vibra Desktop: shell/sidebar/titlebar (NavigationView conservado), SalesView, ClientsView, PricingView, UsersView — en `feature/vibra-s0` (2026-07-03)
- [x] (S0) Restyling Vibra Mobile: LoginPage (2d), SubscriptionsPage carrusel (2a/2f, sin gesto), TicketsPage POS — en `feature/vibra-s0` (2026-07-03)
- [x] (S0) Tema Vibra base: tokens MAUI+WPF, fuentes empaquetadas (Space Grotesk/DM Sans/Plex Mono), logo/appicon/splash — en `feature/vibra-s0` (2026-07-03)
- [x] (S0) Higiene: plantilla MAUI fuera, 5 StaticResources rotos definidos, managerId desde claims del JWT — en `feature/vibra-s0` (2026-07-03)
- [x] Bootstrap de project-docs (CLAUDE.md + docs/) (2026-07-03)
- [x] Primer test de integración: atomicidad de SellTicketsBatchAsync, SQLite in-memory (2026-07-03)
- [x] Limpieza de repo: PDFs/docfx fuera, obj/ des-trackeado, .gitignore restaurado (2026-07-03)
- [x] Tema 1 tutor (transacciones/UoW) completado; decisión UoW aparcada como ADR-002 (2026-07-03)
- [x] Mapa estructural + agenda priorizada de revisión (2026-07-03)

## Decision Log

| Date | Decision | Rationale |
|---|---|---|
| 2026-07-07 | Ventas de abono por ADMIN: `Sale.ManagerId` pasa a nullable; venta admin = manager null, mostrada como "Administración" | Producto: en Nalda los administrativos (admins) venden los abonos. Alternativas descartadas: dar perfil Manager al admin le anclaría a UN venue (`Manager.VenueId` requerido) rompiendo multi-recinto; dropdown de manager atribuiría la venta a quien no vendió (falsearía el cierre de caja S2). Venta de TICKETS sigue siendo solo-manager (POS en puerta). Atribución por admin individual aplazada a la capa de servicios (ADR-002). |
| 2026-07-07 | Medios de pago: TODO efectivo en taquilla hasta decisión de Raquel; anulación de venta entra sí o sí en S2 (política exacta también de Raquel) | Las preguntas concretas viven en `docs/reunion-raquel.md` (agenda de la reunión: accesos/QR, pagos, anulaciones, formato de arqueo, precios reales, RGPD, contratación, hosting). S2 diseña `PaymentMethod` sin activarlo. |
| 2026-07-07 | Bonos de N baños DESCARTADOS: en Nalda no se gestiona así | Aunque el research lo marca como habitual del sector, no aplica al caso real. No retomar por iniciativa propia; quitado de la agenda de Raquel. |
| 2026-07-03 | Pase digital de abonados (handoff 2b/2c/2e) APARCADO junto al QR; carrusel 2a/2f entra (visual puro, sin gesto de entrada) | Mismo razonamiento que el QR: gestor mirando móvil en puerta = retraso, y en un pueblo al abonado se le conoce. Requeriría backend nuevo + trigger ADR-002. |
| 2026-07-03 | Próxima(s) sesión(es): iteración FRONTEND-ONLY en rama `feature/vibra-s0`, sin merge a main hasta que Santi pruebe en casa | Santi fuera con remote control, no puede probar; `dotnet build` como única verificación. |
| 2026-07-03 | Dirección de diseño elegida: "1b Vibra" (teal/cyan/periwinkle, Space Grotesk + DM Sans, degradados en acciones) | Handoff generado en claude.ai/design y versionado en `reference/`. Rediseño puramente visual: no tocar ViewModels ni bindings. |
| 2026-07-03 | Carpeta externa reorganizada: `archivo/{tfg,notas,design}`; repo git externo vacío eliminado; compose duplicado eliminado | Dejar limpio el arranque de la segunda etapa; el único repo es QuickTix/ (interno). |
| 2026-07-03 | QR de acceso APARCADO a decisión de Raquel; entrada de visitantes vía impresora térmica en puerta | Mayoría de vecinos usa abono; visitantes de 1-2 días no se descargarán app; lector tipo torno inviable; lectura por manager = retraso en puerta. |
| 2026-07-03 | Sprint demo = higiene → dashboard → cierre de caja → venue genérico (versión demo) | Máximo impacto visual/comercial con terreno ya ganado (queries de historial, CsvHelper); sin riesgo técnico nuevo. |
| 2026-07-03 | Aparcar el refactor a Unit of Work hasta decidir capa de servicios | La frontera de transacción debe vivir donde viva la lógica; decidirlo antes sería invertir el orden. Promovido a ADR-002 (bajo revisión). |
| 2026-07-03 | Flujo dual: sprints autónomos + sesiones tutor, ambos convergiendo en main con ramas cortas | Rama de aprendizaje de larga vida descartada por divergencia. Tests como red de seguridad compartida. |
| 2026-07-03 | Testing transversal: cada tema de aprendizaje termina con el test que lo demuestra | El hueco de Santi es "cómo se implementa X"; el test ES la implementación de lo aprendido. |
| 2026-07-03 | Diseño visual en claude.ai/design; DesignSync solo como sincronización/referencia | La salida es HTML — se traduce a XAML (WPF-UI/MAUI), el diseño es fuente de verdad visual, no de código. |
| 2026-07-03 | SQLite in-memory para tests de integración | Promovido a ADR-005. InMemory de EF no soporta transacciones. |
| 2026-07-03 | KPI "Aforo estimado hoy" = entradas de uso diario vendidas hoy (UTC), sin factores de abonados | Sin datos de accesos (flujo aparcado) es la única estimación honesta; divergirá de "entradas vendidas" cuando exista control de accesos. Limitación documentada en `AnalyticsRepository`. |
| 2026-07-03 | Shell Desktop: NavigationView de WPF-UI se CONSERVA, reestilizado vía estilo implícito + DataTriggers | La navegación depende de servicios internos de WPF-UI (INavigationService/IPageService), no de bindings — sustituirlo por layout propio la rompería. |
| 2026-07-03 | Fix managerId: leer claim del JWT con `JwtClaimReader` propio en Desktop (sin System.IdentityModel) | Evita apoyarse en las refs Desktop→DAL/API pendientes de eliminar. Efecto: admin sin claim managerId ya no vende en silencio como manager 1 — decisión de producto pendiente. |

## Session Log

### 2026-07-07 — Session 3b (pasada visual con soporte en vivo, tarde)
- Pasada de Santi sobre Desktop → 2 tandas de hallazgos, todos corregidos en la rama (4 commits: `e1f0fad`, `18a48bc`, `668cdd9`, `feab430`):
  - **Venta de abono por ADMIN** (decisión de producto resuelta): `Sale.ManagerId` nullable, venta admin = "Administración"; la revisión adversaria cazó y cerró un bypass en el PUT genérico
  - **Panel v2** (quick wins del research): donut en €, caducidades ≤7d, acumulado temporada, fix tooltip (StringFormat se ignora en ToolTip)
  - **VibraDialog** componente común de modales; 18 MessageBox convertidos; el "2º modal" al borrar abono era el force-delete 409 que saltaba siempre; snapshot `PendingDeleteItem` para no borrar el registro equivocado (revisión adversaria)
  - **Iconos sidebar**: 2 intentos por triggers de plantilla fallidos (SymbolIcon construye el glifo perezosamente) → 3º: `Icon` desde MainViewModel, vía nativa — PENDIENTE de verificar con ojos
  - **Mobile**: tarjetas del carrusel por categoría (mismo mapeo que chips Desktop)
- Gotchas técnicos de la sesión: SQLite no traduce `Sum` decimal (agregados en memoria); la query de historial de abonos generaba SQL APPLY (reescrita); la API necesita SQL Server listo al arrancar (sin retry)
- Estrategia: `docs/reunion-raquel.md` (8 bloques de decisiones para Raquel); research versionado; decisiones: efectivo hasta Raquel, anulación en S2, bonos N baños descartados
- Suite 6/6; todo pusheado. Next: verificar los fixes de la tarde (iconos, flujo borrado, tarjeta sky) → resto de checklist → merge

### 2026-07-07 — Session 3 (auditoría estática del front antes de la pasada manual)
- Auditoría completa Mobile+Desktop con 2 agentes (recursos, fuentes, bindings, triggers, seguridad ante datos vacíos del Panel): 0 claves XAML colgantes, 0 bindings rotos, managerId verificado extremo a extremo (claim `managerId` en UserRepository ↔ JwtClaimReader en Desktop, error inline si admin sin claim)
- **Bug grave encontrado y corregido**: sidebar Desktop no reaparecía tras login (`IsPaneVisible=false` en LoginView sin restaurar en la rama else) — habría matado la demo
- Preventivo: `HeaderVisibility="Collapsed"` en NavigationView (checklist punto 3 Desktop, resuelto sin esperar al runtime)
- Higiene: MainWindow.xaml duplicado de la raíz eliminado (nunca instanciado; DI usa Views.MainWindow), `Visual="Material"` (no-op en MAUI) fuera de SubscriptionsPage/TicketsPage
- Builds verdes: Desktop, Mobile Windows, Mobile Android (primera compilación Android de la rama tras instalar SDK 34)
- Hallazgos preexistentes apuntados sin tocar (ver bloque sobre el Board): columna INVITADO POR, MauiIcon adaptativo, Shadow implícito blanco
- Pasada visual de Santi (en curso): 4 fixes visuales Desktop anotados (sección 2b) + decisión de producto resuelta: **admin vende abonos** → `Sale.ManagerId` nullable (migración `SaleManagerOptional`), venta admin = "Administración", guard en API (sell y PUT genérico: solo admin puede dejar manager null), Desktop decide por rol, 2 tests nuevos (4/4 verdes). La reescritura de `GetSubscriptionHistoryAsync` destapó que EF la traducía a SQL APPLY (no soportado por SQLite en tests)
- Next: pasada visual de Santi con la checklist → merge

### 2026-07-03 — Session 2b (pruebas manuales en casa, inicio)
- Santi arrancó la checklist: API+Desktop OK por CLI; Mobile destapó 2 bugs, ambos corregidos en la rama:
  - `c1af573` fix(mobile): LoginPage se inflaba antes que los recursos de App (XamlParseException PageBg) — gotcha: páginas inyectadas en el ctor de App no pueden usar StaticResource del tema
  - `1552873` fix(dal): el seeder no creaba/asignaba roles manager/client (bug preexistente, destapado por volumen Docker fresco); ahora idempotente, reiniciar la API repara la BD
- Estado al cerrar: **quedan bastantes fixes (sobre todo visuales) por hacer — la checklist de arriba sigue abierta**; la rama NO se mergea todavía
- Next: continuar la checklist, apuntar los fixes visuales encontrados, corregir en la rama → merge cuando pase todo

### 2026-07-03 — Session 2 (sprint demo frontend-only, remote control)
- S0 completo + S1 completo en `feature/vibra-s0` (17 commits, pusheada; SIN merge a main)
- Higiene: MainPage/AppShell de plantilla fuera, splash provisional, debug de TicketsPage fuera, 5 StaticResources rotos definidos, managerId desde JWT (nuevo `JwtClaimReader` en Desktop)
- Tema Vibra base: tokens completos MAUI+WPF (`Themes/VibraTheme.xaml`), 8 TTF estáticos + OFL, logo/appicon/splash (gotcha: el Space Grotesk SemiBold de Google trae metadatos rotos — se instanció desde el variable oficial)
- Restyling: 3 pantallas Mobile + shell y 4 vistas Desktop, bindings intactos; QR/pase digital respetados como aparcados (carrusel sin gesto)
- S1: `/api/Analytics/summary` (read-only, admin, caché 30 s) + PanelView/PanelViewModel como vista inicial (barras y donut sin paquetes de charting)
- Verificación solo `dotnet build` (verde); target Android sin compilar (falta SDK API 34 en la máquina remota); `dotnet test` pendiente
- Next: checklist de pruebas manuales (arriba) → merge → S2 cierre de caja

### 2026-07-03 — Session 1 (revisión tutor + cierre de flujo)
- Mapa estructural completo del repo; preguntas de articulación → gaps confirmados (UoW, capas, ProblemDetails)
- Agenda de 6 temas priorizada; Tema 1 (transacciones/UoW) completado con concept-checks
- Primer test real: `SellTicketsBatchTests` (2 verdes) — commits `1a87dd4`, `0b75d54`, `4c9e78c` en main
- Bootstrap de project-docs; decisiones del día registradas y promovidas a ADRs
- Next: sprint de demo en estilo autónomo (estética + features para Raquel)

## Blocked

| Task | Blocked by | Since |
|---|---|---|
| Piloto con datos reales | Lote de seguridad pre-piloto (secretos, passwords, RGPD menores) | 2026-07-03 |
| Compra online (Mobile client) | Decisión de pasarela de pago | 2026-07-03 |

## Pending Decisions

- **¿Capa de servicios + Unit of Work?** — se decide en Tema 2 de aprendizaje; trigger duro: primera operación multi-repositorio. Ver ADR-002.
- **QR de acceso Y pase digital de abonados (handoff 2b/2c/2e)**: ambos aparcados a decisión de Raquel (racional en Decision Log y PROJECT.md). No retomar por iniciativa propia. El carrusel de abonos (2a/2f) SÍ entra — es visual puro, sin gesto de "deslizar para entrar".
- **Impresora térmica**: modelo y protocolo (ESC/POS USB vs Bluetooth) y desde qué app imprime el manager.
- **Generalizar TicketType por venue**: refactor caro del modelo de precios — solo si Raquel valida el pitch multi-recinto.
- **Gestión de secretos**: user-secrets (dev) + variables de entorno (prod) es el plan tentativo — falta ejecutarlo y rotar los secretos ya expuestos en el historial de git.
- **Hosting del piloto** y pasarela de pago (dependen de conversación con el ayuntamiento).
- **Refs de Desktop** (quitar API/DAL/Core → solo Contracts+Core): decidir si se hace en el sprint de demo o en Tema 2.

## Milestones

- [ ] **Demo Raquel**: ambos clientes presentables + features "al día" — Target: verano 2026 (iterativo)
- [ ] **Cierre de producto**: impresión en puerta + aforo + cierre de caja + lote de seguridad — Target: fin de verano 2026
- [ ] **Piloto Nalda**: temporada real en producción — Target: temporada 2027
