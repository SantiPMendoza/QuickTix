# Handoff: QuickTix — Lavado de cara visual (dirección "1b Vibra")

## Overview
QuickTix es un sistema de venta y control de acceso a recintos con dos clientes:
- **App móvil (MAUI)** — para clientes finales (ver abonos, presentar el pase para entrar) y gestores (venta de entradas en taquilla, validación de accesos).
- **App de escritorio (WPF)** — back-office administrativo (panel, historial de ventas, clientes, precios, usuarios).

Este handoff define un **rediseño puramente visual** ("lavado de cara") sobre la funcionalidad existente. La dirección elegida se llama **1b Vibra**: fresca y con energía, apoyada en la paleta cool de marca (teal / cyan / periwinkle / lavender), degradados suaves reservados para acciones y acentos, tipografía Space Grotesk + DM Sans, esquinas redondeadas y sombras de color.

> **No se cambia la lógica, los ViewModels, los bindings ni la navegación.** El objetivo es re-estilizar: colores, tipografías, radios, sombras, espaciado, y el tratamiento visual de tarjetas/tablas/botones. Un cambio destacable de *interacción* es el gesto "deslizar el abono para entrar" (ver sección Interacciones), pero se apoya sobre el flujo de validación ya existente.

## About the Design Files
Los archivos de este bundle son **referencias de diseño creadas en HTML** — prototipos que muestran el aspecto y comportamiento deseados, **no** código de producción para copiar tal cual. La tarea es **recrear estos diseños en el entorno real del proyecto**:
- Móvil → **.NET MAUI / XAML** (recursos en `QuickTix.Mobile/Resources/Styles/`).
- Escritorio → **WPF + WPF-UI (Fluent)** (`QuickTix.Desktop/Views/…` y sus estilos).

Se deben usar los patrones y componentes ya establecidos (ResourceDictionary, Styles, DynamicResource, DataTemplates, GridView/DataGrid, etc.). El HTML solo comunica intención visual; las medidas en px son orientativas y deben traducirse a las unidades y controles nativos correspondientes.

## Fidelity
**Alta fidelidad (hifi)** para color, tipografía, radios, sombras y jerarquía. Reprodúcelos con precisión aplicándolos a los controles nativos existentes. El layout de cada pantalla ya existe en el código actual — **no hay que reestructurar la funcionalidad**, solo re-vestirla. Los datos mostrados en los mocks son de ejemplo.

---

## Design Tokens

Definir como recursos globales. **MAUI:** añadir/ajustar `Resources/Styles/Colors.xaml`. **WPF:** un `ResourceDictionary` de tema fusionado en `App.xaml`.

### Colores
| Token | Hex | Uso |
|---|---|---|
| `BrandTeal` | `#60E0CF` | Acento primario, inicio de degradados |
| `BrandTealDeep` | `#3CC9C0` | Degradado de acción (inicio), estados hover |
| `BrandCyan` | `#60C7E0` | Degradado (medio) |
| `BrandPeriwinkle` | `#7C9BF7` | Acento secundario, fin de degradados |
| `BrandLavender` | `#CCAFEB` | Acento terciario (chips, categorías) |
| `BrandSky` | `#ABD6FF` | Fondo suave, gráficas |
| `Ink` | `#14233B` | Texto principal / azul marino de marca |
| `InkSidebar` | `#0F1B33` | Fondo sidebar escritorio |
| `TitleBar` | `#14233B` | Barra de título ventana escritorio |
| `TextMuted` | `#8393AC` | Texto secundario |
| `TextMuted2` | `#4A5163` | Texto párrafo |
| `PageBg` | `#F2F6FF` | Fondo de página |
| `Surface` | `#FFFFFF` | Tarjetas / superficies |
| `SurfaceAlt` | `#FAFCFF` | Filas alternas de tabla |
| `TableHeadBg` | `#F7F9FE` | Cabecera de tabla |
| `Border` | `#E7ECF7` | Bordes de tarjeta/tabla |
| `BorderStrong` | `#DCE6FF` | Borde de campo enfocado/activo |
| `ChipBlueBg` / `ChipBlueFg` | `#EEF3FF` / `#4E67C7` | Chip informativo azul |
| `ChipTealBg` / `ChipTealFg` | `#EAFBF8` / `#0F8C82` | Chip/estado positivo (teal) |
| `ChipLavBg` / `ChipLavFg` | `#F4F1FC` / `#7A5CB0` | Chip lavanda (categoría) |
| `Success` | `#16A34A` | Éxito, validado, tendencias ▲ |
| `SuccessBg` | `#EAFBF8` | Fondo de estado activo/validado |
| `DangerBg` / `DangerFg` | `#F1EAF0` / `#B0447A` | Acción destructiva suave (Rechazar/Eliminar) |
| `ExpiredBg` / `ExpiredFg` | `#F1F0F3` / `#9098A8` | Estado caducado (chip) |
| `ExpiredCard1` / `ExpiredCard2` | `#B4BAC4` / `#9CA3AF` | Degradado tarjeta abono caducado |

### Degradados
- **Acción primaria (botón principal):** `linear-gradient(90deg, #3CC9C0, #60C7E0)` o `linear-gradient(90deg, #7C9BF7, #60C7E0)`.
- **Tarjeta de abono / header cliente:** `linear-gradient(150deg, #60E0CF 0%, #60C7E0 45%, #7C9BF7 100%)`.
- **Avatar/badge marca:** `linear-gradient(150deg, #60E0CF, #7C9BF7)`.
- **Chip "NUEVA SOLICITUD":** `linear-gradient(90deg, #3CC9C0, #7C9BF7)`.
- En XAML: `LinearGradientBrush` con `GradientStop` equivalentes (ojo al ángulo: convertir a `StartPoint`/`EndPoint`).

### Tipografía
- **Titulares / cifras destacadas:** **Space Grotesk** (600/700), `letter-spacing: -0.5px`.
- **Cuerpo / UI:** **DM Sans** (400/500/600/700).
- **Números tabulares / referencias / códigos:** **IBM Plex Mono** (500/600).
- Registrar las fuentes en `MauiProgram.cs` (`ConfigureFonts`) y en WPF como fuentes de recurso (`/Fonts/#Space Grotesk`). Sustituir el uso actual de **Impact** (barra de título desktop) y **OpenSans** por lo anterior.
- Escala aprox. (móvil): H1 24–27, H2 17–20, cuerpo 13–14, secundario 11–12. (Escritorio): título página 22, título tarjeta 15, cuerpo 13, cabecera tabla 11 uppercase +0.5 tracking.

### Radios
- Móvil: tarjetas 20–26, botones 14–16, chips 20 (pill), campos 12–14.
- Escritorio: ventana 16, tarjetas 16–18, botones/campos 9–12, chips 10–20, KPI 18.
- Frame de dispositivo (solo mock): no aplica en nativo.

### Sombras (de color, sutiles)
- Tarjeta estándar: `0 6px 16px rgba(96,150,224,.07)`.
- Tarjeta elevada / solicitud entrante: `0 10px 26px -8px rgba(124,155,247,.4)`.
- Botón de acción: `0 8px 16px rgba(60,199,192,.35)` / `0 10px 20px rgba(124,155,247,.4)`.
- Ventana escritorio (mock): no reproducir; usar la sombra nativa de la ventana.

### Espaciado
Escala base 4px. Padding de tarjeta 14–22, gap entre tarjetas 12–20, padding de fila de tabla 12–14 vertical / 16–20 horizontal.

---

## Screens / Views

Nomenclatura: `2x` = pantallas móvil, `3x` = pantallas escritorio (coinciden con los badges del mock).

### MÓVIL — Cliente

#### 2a · Mis abonos (carrusel) — `QuickTix.Mobile/Views/Client/SubscriptionsPage.xaml`
- **Propósito:** el cliente ve sus abonos y presenta uno para entrar.
- **Layout:** cabecera (título "Mis abonos" + avatar iniciales) sobre un `CarouselView` con peek (~40px) de tarjetas vecinas; `IndicatorView` de puntos debajo; asidero inferior fijo.
- **Tarjeta de abono (activa):** fondo degradado `150deg #60E0CF→#60C7E0→#7C9BF7`, radio 24–26, sombra `0 18px 34px -12px rgba(96,150,224,.6)`. Contenido:
  - Chip estado `ACTIVO` (fondo `rgba(255,255,255,.4)`, texto `#0A2233`, bold, pill) + a la derecha "Temporada 25/26".
  - "Abonado" (13, semibold, opacidad .85) → Nombre grande (Space Grotesk 26, bold).
  - "Abono General · Anual" (14, semibold).
  - "Válido hasta 30 jun 2026" (13, bold).
  - Footer: referencia mono `#AB-2041` + logo pequeño.
- **Peeks:** slivers laterales redondeados con degradados (`#CCAFEB→#ABD6FF` izq, `#60E0CF→#60C7E0` der), opacidad .65.
- **Indicador:** punto activo alargado (18×6) color `#7C9BF7`, inactivos 6×6 `#C7D2F0`.
- **Asidero (affordance):** barra 44×5 `#B9C6FF` + texto "Desliza arriba para entrar ↑" (13, bold, `#4E67C7`), sobre degradado de desvanecido hacia `PageBg`.
- **Estado caducado (2f):** misma tarjeta con degradado gris `#B4BAC4→#9CA3AF`, texto blanco, chip `CADUCADA`, "Caducó el 30 jun 2025", botón pill blanco "Renovar abono →", y el asidero cambia a texto muted "Abono no válido para acceso" (deshabilita el gesto de entrada). Mapea a la lógica `IsExpired` ya existente (el código actual ya fuerza gris con un `DataTrigger`).

#### 2b · Presentando pase (pantalla completa)
- **Propósito:** modo "mostrar en el acceso" tras deslizar el abono hacia arriba.
- **Layout:** página a pantalla completa con **fondo degradado** `160deg #60E0CF→#60C7E0→#7C9BF7`; contenido centrado.
- **Componentes:**
  - Título "Muéstralo en el acceso" (Space Grotesk 20, bold, `#0A2233`).
  - Chip de estado: fondo `rgba(255,255,255,.35)`, punto + "En espera de validación…" (12, bold).
  - **Tarjeta de pase** (blanca, radio 24, sombra `0 20px 40px -14px rgba(10,34,51,.4)`): etiqueta "PASE DE ACCESO" (11, `#7C9BF7`, uppercase, tracking 1.5), nombre (Space Grotesk 23), "Abono General · Anual" (13, muted), separador, **código de barras** (barras verticales — en nativo, usar librería de barcode/QR real; en el mock es un patrón), referencia mono `#AB-2041` (15) + "Pabellón Sócon".
  - Asidero inferior "Desliza para cerrar ↓".
- **Nota:** en producción, la representación del pase debería ser un **QR/código real** verificable, no decorativo.

#### 2d · Acceso (login) — `QuickTix.Mobile/Views/LoginPage.xaml`
- **Layout:** cabecera degradada `150deg #60E0CF→#60C7E0→#7C9BF7` con esquinas inferiores redondeadas (34), logo (78px, drop-shadow) + "QuickTix" (Space Grotesk 22). Debajo formulario.
- **Componentes:** "Hola de nuevo" (Space Grotesk 22) + subtítulo muted; campo email (tarjeta blanca radio 14, sombra suave); campo contraseña con icono ojo; checkbox "Recordar usuario" (cuadro 20 con degradado + ✓ cuando activo); botón "Iniciar sesión" ancho, degradado `90deg #7C9BF7→#60C7E0`, radio 16, sombra de color. Bindings actuales: `Username`, `Password`, `RememberUser`, `CheckLoginCommand`, `IsLoginEnabled`.

### MÓVIL — Gestor

#### Venta de entradas (POS) — `QuickTix.Mobile/Views/Manager/TicketsPage.xaml`
*(Mostrada en el turno 1 del mock como dirección 1b; reestilizar la pantalla existente.)*
- **Layout actual (se mantiene):** cabecera con header degradado ("Pabellón Sócon" + avatar + "Vender entradas"), tarjeta "Nueva línea" (2 pickers Tipo/Contexto, Cantidad, Precio, botón "+ Añadir línea"), cabecera "Líneas" + resumen (chip pill `#EEF3FF/#4E67C7`), lista de líneas (`CollectionView`), barra inferior fija con "Vender (batch)" (degradado) + "Limpiar" (gris).
- **Tarjetas de línea:** blancas, radio 16, **borde izquierdo de acento** 4px (teal `#60E0CF` para General, periwinkle `#7C9BF7` para Abonado), sombra `0 4px 12px rgba(96,150,224,.1)`. Título bold + total a la derecha; subinfo muted; acción "Quitar" en `DangerFg`.
- **Chips de picker por color:** Tipo → fondo `#EAFBF8` texto `#0F8C82`; Contexto → `#EEF3FF/#4E67C7`; Cantidad → `#F4F1FC/#7A5CB0`. (Solo estética; los `Picker`/`Entry` siguen igual.)
- Bindings intactos: `TicketTypes`, `NewLineTicketType`, `AddLineCommand`, `Lines`, `SellBatchCommand`, `ResetCommand`, etc.

#### 2c · Solicitud de acceso (validación)
- **Propósito:** al presentar el cliente su abono (2b), aparece aquí la solicitud para validar/rechazar. Nueva pantalla del gestor apoyada en el flujo de validación de accesos.
- **Layout:** cabecera "Accesos" + chip "● En vivo" (`ChipTealBg/Fg`). Tarjeta de solicitud entrante grande + lista "Últimos accesos".
- **Tarjeta solicitud (elevada):** blanca, radio 22, borde `#DCE6FF`, sombra `0 10px 26px -8px rgba(124,155,247,.4)`. Contiene:
  - Chip degradado "NUEVA SOLICITUD" + "hace 2 s" (muted).
  - Avatar iniciales (46, radio 16, degradado marca) + Nombre (Space Grotesk 17) + "Abono General · Anual".
  - Banda de verificación: fondo `SuccessBg`, ✓ verde + "Abono activo · válido hasta 30 jun 2026".
  - Referencia mono `#AB-2041`.
  - Botones: **Validar acceso** (degradado teal, ancho) + **Rechazar** (`DangerBg/Fg`).
- **Últimos accesos:** filas blancas radio 14, cuadro ✓ (`SuccessBg`), nombre + tipo + hora.

#### 2e · Acceso concedido (confirmación)
- **Layout:** fondo `170deg #EAFBF8→#F2F6FF`, contenido centrado: círculo 96 con degradado teal y ✓ blanco (sombra de color), "Acceso concedido" (Space Grotesk 26), "Carlos Méndez · Abono General"; tarjeta resumen (hora registrada, referencia mono); botón inferior "Siguiente acceso" (degradado).

### ESCRITORIO (WPF) — Shell común
- **Ventana:** radio 16, barra de título `#14233B` (40px) con los controles de ventana nativos (en el mock se ven como semáforos macOS — usar los de Windows/WPF-UI) y "QuickTix Admin".
- **Sidebar (230px):** fondo `#0F1B33`, cabecera logo + "QuickTix" (Space Grotesk 17) + "ADMIN" (10, tracking 2, `#60C7E0`). Ítems de nav: Panel, Usuarios, Ventas, Precios, Clientes con icono de línea 19px. **Activo:** fondo `linear-gradient(90deg, rgba(96,199,224,.22), rgba(124,155,247,.14))`, texto blanco, barra izquierda 3px degradado `#60E0CF→#7C9BF7`, radio 12. Inactivo: texto `#AEB8CC`. Footer: avatar degradado "AD" + "Admin / Cerrar sesión".
- **Área contenido:** fondo `#F2F6FF`; topbar blanca (borde inferior `#E7ECF7`) con título de página (Space Grotesk 22) + buscador pill + selectores.
- Estos ítems mapean a la navegación real (`MainViewModel.NavigationItems`: Usuarios, Historial de ventas, Precios, Clientes) — **añadir "Panel"** como nueva vista inicial.

#### 3a · Panel (dashboard) — nueva vista
- **KPIs:** grid de 4 tarjetas (radio 18, borde `#E7ECF7`, sombra estándar): etiqueta muted + icono en cuadro de color (30, radio 10), cifra grande (Space Grotesk 28), delta ("▲ 12% vs ayer" en `Success`, o "— estable" muted). Valores mock: Ingresos hoy 3.240 €, Entradas vendidas 428, Abonos activos 1.204, Accesos validados 312.
- **Gráfica ingresos 7 días:** tarjeta con barras (una por día Lun–Dom), degradado vertical `#7C9BF7→#ABD6FF`, días pico en `#3CC9C0→#60C7E0`. En WPF usar LiveCharts/ScottPlot o un `ItemsControl` de barras.
- **Donut "Ventas por tipo":** anillo (`conic-gradient` en mock; en WPF, arcos) 62% Entradas `#7C9BF7` / 38% Abonos `#60E0CF`, centro con total (632) + leyenda.
- **Ventas recientes:** tabla compacta (ID mono, Recinto bold, Vendido por, Nº centrado, Total derecha, bold).

#### 3b · Historial de ventas — `QuickTix.Desktop/Views/Pages/SalesView.xaml`
- **Layout:** título + buscador/filtro; **pestañas segmentadas** (píldora) Entradas / Suscripciones (fondo `#E7ECF7`, activa blanca con sombra); tabla en tarjeta (radio 16). Cabecera `TableHeadBg`, texto 11 uppercase muted; filas alternas `SurfaceAlt`; ID en mono; Recinto en bold; Total derecha bold. Paginación inferior (chips 30×30, activo `#4E67C7`).
- Columnas reales: ID, Fecha de venta, Día Semana, Recinto, Vendido por, Nº entradas, Total (Suscripciones: Cliente, Categoría, Precio). Reestilizar el `ListView`/`GridView` actual (o migrar a `DataGrid`) sin tocar bindings `TicketSales`/`SubscriptionSales`.

#### 3c · Clientes — `QuickTix.Desktop/Views/Pages/ClientsView.xaml`
- **Layout:** dos columnas. **Lista** (340px, superficie blanca): cabecera "Clientes" + botón pill degradado "+ Añadir" + buscador; filas con avatar iniciales (38, radio 11 — degradado marca en el seleccionado, gris en el resto), nombre + teléfono; fila seleccionada fondo `#EEF3FF`. **Detalle** (resto): tarjeta cabecera con avatar 64 (radio 18, degradado), nombre (Space Grotesk 22), datos (email/NIF/teléfono, muted) + botones Editar / Eliminar (`DangerBg`); pestañas segmentadas Abonos/Entradas/Relacionados; acciones "Vender abono" (degradado) / "Cancelar abono"; tabla de abonos (ID, Categoría, Duración, Inicio, Fin, Precio, Estado con chip `Activo`=teal / `Caducado`=gris).
- Bindings reales intactos: `Items`, `SelectedItem`, `SubscriptionsVM`, comandos de flyout.

#### 3d · Precios — `QuickTix.Desktop/Views/Pages/PricingView.xaml`
- **Layout:** toolbar en tarjeta (radio 16): "Recinto" + combo (`#F2F6FF`, radio 11) + botón "Cargar" (`#EEF3FF/#4E67C7`) + "Guardar" (degradado, sombra) + estado "✓ Cambios guardados…" en `Success`. Pestañas segmentadas Tickets/Abonos. Tabla editable: Tipo (bold), Contexto (muted), Precio (celda editable = campo con borde `#DCE6FF` activo / `#E7ECF7` normal, fuente mono). Reestilizar el `DataGrid` existente conservando `TicketPrices`/`SubscriptionPrices`, `LoadPriceMapCommand`, `SavePriceMapCommand`.

#### Usuarios — `QuickTix.Desktop/Views/Pages/UsersView.xaml` (no mockeada, aplicar mismo sistema)
- Dos `GroupBox` (Administradores / Gestores) → convertir a tarjetas con tablas al mismo estilo que 3b. Botones "Añadir/Editar/Eliminar" con el sistema de botones (primario degradado, secundario borde, destructivo `DangerBg`). Popups/flyouts (alta/edición) → tarjeta blanca radio 16, campos con borde `Border`/`BorderStrong`, botones Guardar (degradado) / Cancelar (borde).

---

## Interactions & Behavior
- **Gesto "deslizar para entrar" (nuevo, cliente):** desde 2a, un swipe-up sobre la tarjeta de abono activo abre 2b (modo pase a pantalla completa). Recomendado: `SwipeGestureRecognizer` (Direction=Up) o `PanGestureRecognizer` con transición modal de subida (~250–300 ms, ease-out). Solo habilitado si el abono `IsExpired == false`; si está caducado, mostrar el texto "Abono no válido para acceso" (2f) y no abrir el pase.
- **Presentar → validar:** al entrar en 2b, el pase se registra como solicitud de acceso y aparece en la app del gestor (2c) prácticamente en tiempo real. Implementar sobre el canal ya usado para validación de accesos (polling o push según backend). El estado del pase en 2b pasa de "En espera de validación…" a validado/rechazado según respuesta del gestor.
- **Validar / Rechazar (gestor, 2c):** "Validar acceso" → pantalla 2e (Acceso concedido) y la fila baja a "Últimos accesos" con ✓ y hora. "Rechazar" → notificar al cliente en 2b.
- **Carrusel (2a):** swipe horizontal cambia de abono (`CarouselView` ya soporta); `IndicatorView` refleja posición.
- **Pestañas segmentadas (escritorio):** cambio instantáneo de contenido; pestaña activa con fondo blanco + sombra suave.
- **Estados de fila de tabla:** hover → fondo `#FAFCFF`; seleccionada (clientes) → `#EEF3FF`.
- **Botones:** estado normal con sombra de color; hover → oscurecer ~6–8% el degradado; disabled → gris (`#EEF0F6` fondo / `#8393AC` texto), como ya hace el estilo actual.
- **Transiciones:** suaves y cortas (150–250 ms). Sin animaciones llamativas en el back-office.

## State Management
No cambia respecto al código actual. ViewModels y comandos existentes se mantienen:
- Móvil: `SubscriptionsViewModel` (+ `SubscriptionCardViewModel` con `IsExpired`, `StatusText`, etc.), `TicketsViewModel`, `LoginViewModel`.
- Escritorio: `MainViewModel` (añadir ítem "Panel"), `ClientsViewModel`, `PricingViewModel`, `SubscriptionsViewModel`, `UsersViewModel`.
- **Nuevo estado** para el flujo de acceso: estado del pase presentado (En espera / Validado / Rechazado) y la cola de solicitudes entrantes del gestor. El Panel (3a) necesita datos agregados (ingresos por día, conteos, distribución por tipo) — exponer endpoints/consultas de resumen.

## Design Tokens
Ver sección **Design Tokens** arriba (colores, degradados, tipografía, radios, sombras, espaciado). Centralizarlos: MAUI `Colors.xaml` + `Styles.xaml`; WPF un diccionario de tema en `App.xaml`.

## Assets
- `assets/logo.png` — logotipo QuickTix (dos entradas formando una "S", azules). Usado en login móvil, sidebar y tarjetas de escritorio. Reemplaza el uso de Impact para el wordmark.
- `assets/appicon.png` — icono de app.
- `assets/Paleta.png` — referencia de la paleta explorada (teal/cyan/periwinkle/lavender/sky).
- **Iconos de navegación (escritorio):** iconos de línea simples (Panel=grid, Usuarios=persona+, Ventas=recibo, Precios=etiqueta, Clientes=persona). En WPF usar `SymbolIcon` de WPF-UI (equivalentes Fluent) o un set de iconos de línea consistente.
- **Fuentes:** Space Grotesk, DM Sans, IBM Plex Mono (Google Fonts / SIL OFL). Empaquetarlas en cada app.
- **Barcode/QR del pase:** usar una librería real de generación (no el patrón decorativo del mock).

## Files
- `Direcciones QuickTix.dc.html` — prototipo con las tres direcciones (turno 1), las vistas móviles + flujo de acceso 1b (turno 2) y el escritorio administrativo 1b (turno 3). **La dirección a implementar es 1b (Vibra).**
- Código fuente existente a reestilizar:
  - `QuickTix.Mobile/Resources/Styles/Colors.xaml`, `Styles.xaml`
  - `QuickTix.Mobile/Views/LoginPage.xaml`, `Views/Client/SubscriptionsPage.xaml`, `Views/Manager/TicketsPage.xaml`
  - `QuickTix.Desktop/Views/MainWindow.xaml`, `Views/Pages/*.xaml`
