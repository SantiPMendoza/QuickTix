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

**Last updated**: 2026-07-03 (sesión 2, remote control)

## Pruebas manuales pendientes (antes de mergear `feature/vibra-s0`)

Checklist para cuando Santi llegue a casa. Si algo falla, se corrige en la propia rama.

**0. Builds/tests que faltaron en la sesión**
- [ ] `dotnet build QuickTix.Mobile` con target **Android** (instalar Android SDK API 34 si falta)
- [ ] `dotnet test` completo (los tests de ventas deben seguir verdes; Analytics solo lee)

**1. Mobile (emulador o dispositivo)**
- [ ] Splash y appicon nuevos (resizetizer: recorte del icono adaptativo, tamaño del logo en splash)
- [ ] Fuentes Space Grotesk / DM Sans / IBM Plex Mono renderizan en Android
- [ ] Login: degradado llega al borde superior (sin NavigationBar), sombras de campos/botón, botón deshabilitado (gris) con campos vacíos
- [ ] Abonos: carrusel con peek, punto activo alargado del indicador (si no se estira, degrada a puntos uniformes — aceptable), tarjeta caducada gris vía `IsExpired`, legibilidad del texto sobre degradado, doble título navbar+página (¿molesta?)
- [ ] POS gestor: chips de color envolviendo pickers (altura en Android), acento teal 4px respeta el radio de tarjeta, venta batch funciona igual que antes
- [ ] Sin XamlParseException en TicketsPage (los 5 StaticResources rotos ya están definidos — confirmar en runtime)

**2. Desktop (WPF)**
- [ ] Sidebar oscuro: pane del NavigationView transparente sobre el fondo InkSidebar; alternancia al login/logout (se togglea en code-behind)
- [ ] Ítem activo del sidebar con degradado + barra izquierda; los 5 iconos mapeados por DataTrigger aparecen (ojo "Historial de\nventas")
- [ ] Si aparece título/breadcrumb duplicado sobre las páginas → añadir `HeaderVisibility="Collapsed"` al NavigationView
- [ ] TitleBar oscura: contraste del hover en min/max/close
- [ ] Popups de UsersView/ClientsView: abren centrados y con sombra (se quitó el velo azulado)
- [ ] PricingView: doble clic en Precio → editor mono con borde de foco; guardar precios funciona
- [ ] Tablas: filas alternas, hover, selección; formato `N2 €` y fechas `dd/MM/yyyy`
- [ ] **Fix managerId**: vender abono como MANAGER usa su id real del JWT; como ADMIN ahora da error inline (antes vendía en silencio como manager 1) → decidir producto: ¿admin elige manager o la API acepta ventas de admin?
- [ ] Panel: tras login se aterriza en "Panel"; KPIs/barras/donut/tabla con datos del seed; con BD limpia no crashea; con la API apagada muestra error inline y "Actualizar" recupera
- [ ] Swagger: GET `/api/Analytics/summary` → 401 sin token, 403 como manager/client, 200 como admin; caché TTL 30 s (una venta tarda ≤30 s en reflejarse)
- [ ] Zona horaria: `Sale.Date` es UTC — una venta a última hora (España = UTC+2 en verano) computa como "mañana" en los KPIs de hoy. ¿Molesta para la demo?

**3. Si todo pasa**: merge de `feature/vibra-s0` a main + borrar la rama.

## Board

### TODO
Sprint demo, en orden (0 → 1 → 2 → 3). Diseño: **dirección "1b Vibra"** — handoff completo con
tokens, tipografías y specs por pantalla en `reference/App redesign directions/design_handoff_quicktix_vibra/README.md`:
- [ ] (S2) Cierre de caja: informe por día/rango y venue/manager + export CSV (CsvHelper ya referenciado en Desktop)
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
