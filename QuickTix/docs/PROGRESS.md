<!-- docs: PROGRESS.md v1.0.0 — 2026-07-03 — bootstrap inicial de project-docs -->

# Progress: QuickTix

## Current State

Fase 1 (sprint de demo para Raquel), S0 listo para arrancar en modo **frontend-only**:
Santi está fuera (remote control) y no puede probar — trabajar en rama `feature/vibra-s0`,
verificar solo con `dotnet build`, NO mergear a main hasta que él pruebe en casa. Diseño
Vibra versionado en `reference/`. Sin bloqueos duros; la deuda de seguridad es bloqueante
solo de cara a piloto con datos reales, no para la demo.

**Last updated**: 2026-07-03

## Board

### TODO
Sprint demo, en orden (0 → 1 → 2 → 3). Diseño: **dirección "1b Vibra"** — handoff completo con
tokens, tipografías y specs por pantalla en `reference/App redesign directions/design_handoff_quicktix_vibra/README.md`:
- [ ] (S0) Higiene: restos de plantilla MAUI (MainPage "Welcome to .NET MAUI", splash morado, info debug en TicketsPage)
- [ ] (S0) Higiene: StaticResources no definidos en Mobile (posible XamlParseException en TicketsPage)
- [ ] (S0) Fix: `CurrentManagerId = 1` hardcodeado en Desktop (`ClientsViewModel.cs`)
- [ ] (S0) Tema Vibra — base: tokens de color/degradados/radios/sombras como ResourceDictionary de tema (WPF `App.xaml`) y `Colors.xaml`/`Styles.xaml` (MAUI); fuentes Space Grotesk + DM Sans + IBM Plex Mono empaquetadas; logo y appicon del handoff (sustituyen Impact y el splash morado)
- [ ] (S0) Tema Vibra — restyling por pantalla (sin tocar ViewModels/bindings): Mobile → LoginPage (2d), SubscriptionsPage carrusel (2a/2f), TicketsPage POS; Desktop → shell/sidebar/titlebar, SalesView (3b, pestañas segmentadas), ClientsView (3c, dos columnas), PricingView (3d), UsersView (GroupBox → tarjetas)
- [ ] (S1) Panel (3a): nueva vista inicial en Desktop (añadir "Panel" a `MainViewModel.NavigationItems`) — 4 KPI cards + gráfica ingresos 7 días + donut ventas por tipo + tabla ventas recientes; endpoint read-only `/api/analytics/summary` (no dispara ADR-002). KPI "Accesos validados" del mock NO entra (pase digital aparcado) — sustituir por "Aforo estimado hoy"
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
- [ ] Sprint de demo (sesión autónoma) — generar diseño en claude.ai/design y aplicarlo

### DONE
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

## Session Log

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
