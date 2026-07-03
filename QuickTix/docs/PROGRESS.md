<!-- docs: PROGRESS.md v1.0.0 — 2026-07-03 — bootstrap inicial de project-docs -->

# Progress: QuickTix

## Current State

Fase 1 (sprint de demo para Raquel) arrancando. Backend, Desktop y Mobile funcionales
heredados del TFG; primer test de integración (atomicidad de ventas) verde y mergeado.
Rama actual: `main` (limpia y pusheada). Sin bloqueos duros; la deuda de seguridad es
bloqueante solo de cara a piloto con datos reales, no para la demo.

**Last updated**: 2026-07-03

## Board

### TODO
- [ ] Sprint demo: pulido estético Desktop (WPF-UI) y Mobile alineado al diseño de claude.ai/design
- [ ] Sprint demo: eliminar restos de plantilla MAUI (MainPage "Welcome to .NET MAUI", splash morado, info debug en TicketsPage)
- [ ] Sprint demo: revisar StaticResources no definidos en Mobile (posible XamlParseException en TicketsPage)
- [ ] Fix: `CurrentManagerId = 1` hardcodeado en Desktop (`ClientsViewModel.cs`) — todas las ventas de abono van al manager 1
- [ ] Fix: unificar la doble ruta de precios (SubscriptionController usa CalculatePrice [Obsolete]; SaleController usa el sistema real)
- [ ] Feature clave: QR de validación de acceso (pieza estrella para la venta a Nalda)
- [ ] Feature: control de aforo en tiempo real (⚠️ dispara el trigger de revisión de ADR-002)
- [ ] Feature: cierre de caja / informes
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

- **¿Capa de servicios + Unit of Work?** — se decide en Tema 2 de aprendizaje; trigger duro: primera operación multi-repositorio (aforo). Ver ADR-002.
- **Estrategia de QR**: validación desde móvil del manager vs hardware dedicado.
- **Gestión de secretos**: user-secrets (dev) + variables de entorno (prod) es el plan tentativo — falta ejecutarlo y rotar los secretos ya expuestos en el historial de git.
- **Hosting del piloto** y pasarela de pago (dependen de conversación con el ayuntamiento).
- **Refs de Desktop** (quitar API/DAL/Core → solo Contracts+Core): decidir si se hace en el sprint de demo o en Tema 2.

## Milestones

- [ ] **Demo Raquel**: ambos clientes presentables + features "al día" — Target: verano 2026 (iterativo)
- [ ] **Cierre de producto**: QR + aforo + cierre de caja + lote de seguridad — Target: fin de verano 2026
- [ ] **Piloto Nalda**: temporada real en producción — Target: temporada 2027
