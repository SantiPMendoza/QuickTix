# Investigación de mercado: aplicaciones de venta y gestión de entradas/abonos para piscinas municipales en España — posicionamiento competitivo de QuickTix

## Resumen ejecutivo

QuickTix (TFG en .NET 8, orientado a la piscina municipal de Nalda, La Rioja, ~1.100 habitantes) compite en un nicho real pero fragmentado. El competidor directo de referencia es **EntradasPiscina** (de Perception Technologies S.L., Barcelona), cuyo modelo publicado es **800 € de alta + una cuota variable según la población del municipio**, con puesta en marcha "en 24 h". Su punto débil no es funcional sino de experiencia (reseñas negativas por reembolsos, bugs de pago y lentitud). Por encima de este nicho hay un ecosistema de gestión deportiva municipal generalista (i2A-Cronos, DeporWin/T-Innova, eAgora, ReservaDeportes, nsreserva) que también vende entradas y abonos, pero apunta a instalaciones mayores y con control de accesos por hardware. La conclusión central: **para un municipio del tamaño de Nalda, QuickTix puede ganar por precio, simplicidad para usuarios no técnicos y ajuste al marco de contrato menor (< 15.000 € sin IVA), siempre que cierre tres gaps prioritarios: QR de validación, cierre de caja/informes para intervención, y una figura clara de encargado del tratamiento (RGPD).**

Las tres recomendaciones inmediatas: (1) priorizar QR + cierre de caja/informes exportables antes de la demo de verano; (2) cotizar como contrato menor de suministro/servicios por debajo de 15.000 € sin IVA; (3) resolver el rol RGPD (QuickTix como encargado del tratamiento con contrato art. 28 RGPD) porque los competidores SaaS ya lo ofrecen resuelto.

## Hallazgos clave

- **El nicho exacto existe y tiene un líder claro pero pequeño.** EntradasPiscina, nacido en 2020 por la COVID, cerró su primera temporada (según el propio fabricante, balance de octubre de 2020) con **23 piscinas usuarias —22 de ellas en Cataluña y una en la provincia de Teruel—, 11.033 usuarios y cerca de 47.000 compras/pedidos**; su app Android supera 10.000 descargas. Es un producto especializado, no un gigante: su cuota de mercado real en municipios pequeños es limitada y desigual geográficamente (fuerte en Cataluña, con implantaciones sueltas en Toledo, Zaragoza, Madrid).
- **El modelo de precios del nicho es asequible y transparente:** EntradasPiscina publica "800 € + cuota variable según la población"; los generalistas usan cuota anual por módulos (p. ej. módulo "piscinas municipales (aforo, abonos)" desde 350 €/año en ReservaDeportes) o licencia + mantenimiento.
- **El marco legal favorece a QuickTix:** el artículo 118.1 de la Ley 9/2017 (redacción del RD-ley 3/2020) permite adjudicar directamente como contrato menor cualquier suministro/servicio de valor estimado inferior a 15.000 € (sin IVA, por exclusión expresa del art. 101 LCSP), lo que encaja perfectamente con la venta a un ayuntamiento pequeño sin licitación.
- **Los gaps de QuickTix son exactamente las funciones que el sector considera estándar:** QR de validación, control de aforo en tiempo real, compra online con pasarela de pago y cierre de caja/informes. Son también las que más valora la intervención municipal.
- **El RGPD es un factor comercial, no solo técnico:** los SaaS ya actúan como encargado del tratamiento con su DPA (contrato art. 28 RGPD). QuickTix debe definir explícitamente su modelo (encargado del tratamiento, ubicación de datos, medidas de seguridad, plan de salida) o quedará en desventaja en la comparación.
- **La situación de partida de Nalda (papel/Excel) es el verdadero competidor a batir:** el argumento más fuerte no es contra EntradasPiscina, sino contra la gestión manual: trazabilidad, cuadre de caja automático e informes de recaudación para el ayuntamiento.

## Detalle

### 1. Nivel 1 — Nicho exacto (análisis en profundidad)

#### EntradasPiscina (Perception Technologies S.L.) — referencia obligada

**Quién es.** Producto de PERCEPTION TECHNOLOGIES, S.L. (CIF B62370473; Crta. Cànoves 16, Les Franqueses del Vallès, Barcelona), estudio de desarrollo digital fundado en 2000, ~15 profesionales, también creador de la plataforma de ticketing **Entrápolis** (según su aviso legal, "utilizada por más de 2.500 organizadores y a través de la cual más de 750.000 personas han comprado sus entradas"). Opera en dos dominios: entradaspiscina.com (castellano) y entradespiscina.cat (catalán). El producto nació en 2020 como respuesta a las necesidades de aforo/COVID de las piscinas municipales.

**Funcionalidades (según su web).** Venta 100% online + "modo presencial" para taquilla; abonos y packs personalizados por edad, horario y promociones; control de aforo en tiempo real y por franjas horarias; validación de acceso por código QR escaneado con smartphone; base de datos centralizada con notificaciones masivas; y —textualmente— "simplifica el cierre de caja y reduce los errores manuales". Dispone de app de usuario para Android e iOS (id `cat.entradespiscina.usuaris`). Puesta en marcha "lista en 24h".

**Clientes conocidos.** Piscina Municipal de Argés (Toledo, piscina-arges.com), Boquiñeni (Zaragoza), Nuevo Baztán (Madrid), Sant Llorenç Savall – Piscina de Comabella (Barcelona), Sant Feliu de Codines – Piscina de Solanes (Barcelona), L'Ametlla del Vallès (Barcelona) y el Ayuntamiento de Lleida (adoptante temprano en 2020). El patrón es de municipios pequeños y medianos, con núcleo fuerte en Cataluña (en el balance de su primera temporada, 22 de 23 piscinas estaban en Cataluña).

**Modelo de precios.** Publicado en su propia web: **"Transforma tu piscina por sólo: 800 € + Cuota variable según la población del municipio"**. Es decir, un pago de alta/onboarding (~800 €) más una cuota recurrente escalada por población. No publican comisión por entrada al comprador; la contratación con municipios pequeños parece hacerse de forma directa (coherente con importes por debajo del umbral de publicación del contrato menor), y no se encontró ninguna licitación/contrato público que nombre a "EntradasPiscina" o "Perception Technologies" para software de piscina.

**Fortalezas.** Especialización real en el nicho; onboarding rápido; respaldo de una plataforma de ticketing consolidada (Entrápolis); cubre online + taquilla + abonos + aforo + QR + cierre de caja; SaaS que resuelve el rol de encargado del tratamiento.

**Debilidades visibles (reseñas en tiendas).** La app tiene pocas valoraciones en App Store (insuficientes para mostrar puntuación) y reseñas negativas concretas: pagos cobrados sin emisión de la entrada, políticas de "no reembolso" (solo cambio de fecha o "bono monedero") y problemas de UX. Reseña textual en la App Store española: *"Aplicación lenta, difícil de tramitar cuando estás en frente del personal de piscina, caótico cuando tienes que tramitar varias entradas al mismo tiempo y encima muchas palabras están en catalán. A ver si el ayuntamiento cambia pronto de sistema de gestión…"*. Estas fricciones de UX y de política de reembolso son el flanco competitivo más claro.

#### Otros actores del mismo nicho

- **Piscify (Pixel Innova, agencia de marketing/desarrollo).** Solución nacida también en la COVID para compra de entradas y control/gestión de aforo de piscinas municipales: pago telemático, canje por QR personal, aforo en tiempo real, configuración de días/franjas y ventajas para empadronados. "Probada por varios ayuntamientos locales". Pixel Innova ha obtenido la certificación del Esquema Nacional de Seguridad (ENS), un diferenciador relevante para vender a administración pública. No publica precios.
- **nsreserva (nsreserva.com).** Gestión de piscinas con control de aforo y acceso por QR, pulsera o tarjeta (RFID); panel con entradas, aforo, reservas y estadísticas en tiempo real; orientado tanto a piscinas públicas como a comunidades de vecinos. Incorpora hardware de control de acceso, lo que lo acerca a instalaciones con torno.
- **eAgora (eagora.app).** Plataforma "all-in-one" para ayuntamientos (declara +550 municipios y +180 funcionalidades) con un "Módulo de Entradas y Bonos" que gestiona venta de entradas, bonos e inscripciones, pago con tarjeta o Bizum, validación por QR y TPV propio del ayuntamiento. No es específico de piscinas, pero cubre el caso de uso "bonos de piscina para el verano". Modelo por módulos según plan contratado.
- **ReservaDeportes (reservadeportes.com).** SaaS para ayuntamientos y clubes con módulo específico **"Piscinas municipales (aforo, abonos) +350 €/año"**, más otros módulos (app propia +250 €/plataforma, etc.); "sin comisiones, sin permanencia", alta operativa en 48 h. Buen comparador de precio para el rango bajo.

### 2. Nivel 2 — Gestión deportiva municipal generalista (panorama)

| Proveedor | Qué cubre | Tamaño objetivo | Precio / modelo | Presencia pública |
|---|---|---|---|---|
| **i2A-Cronos** (i2A Proyectos Informáticos S.A.) | Gestión integral de instalaciones deportivas y control de accesos: reservas, escuelas, socios, competiciones, caja, recibos, entradas/bonos, abonos, monedero; app Cronos Global | Desde gimnasio modesto hasta ciudad deportiva; +400 centros | Licencia + hardware de control de accesos y periféricos; no público | Marbella, Oviedo, Sevilla (IMD), Coria, Juegos Deportivos Municipales de Madrid |
| **DeporWin / T-Innova** | Suite modular: clientes, abonos, productos, servicios, personal, accesos, gestión económica, CRM, explotación de datos; integra tornos, biometría, impresoras de tickets | Desde un gimnasio a cadenas y pabellones municipales; +24 años, 448 instalaciones | Licencia modular; no público | Amplia base en centros deportivos; aparece en licitaciones de mantenimiento |
| **eAgora** | App municipal all-in-one; módulo entradas y bonos (piscina, eventos), Bizum/tarjeta, QR, TPV | Ayuntamientos de todo tamaño | Por módulos/plan | +550 municipios (autodeclarado) |
| **ReservaDeportes** | Reservas pádel/tenis/piscina/gimnasio, pagos, torneos, app propia, domótica de pistas | Ayuntamientos y clubes pequeños/medianos | Cuota anual por módulos (piscinas 350 €/año) | +400.000 usuarios (autodeclarado) |
| **nsreserva** | Control de aforo/acceso piscinas (QR, RFID), reservas, estadísticas | Piscinas públicas, privadas y comunidades | No público | — |
| **Apeiron Software** | Gestión administrativa de la piscina municipal: padrones fiscales (tasa/precio público), abonos, recibos, domiciliaciones, listados de recaudación diaria, exportación a Excel, conexión con padrón de habitantes | Ayuntamientos pequeños | No público | — |

Contexto de sistemas propietarios de grandes administraciones: **DEPORTESCM** (Comunidad de Madrid), **Madrid Móvil** (Ayuntamiento de Madrid, con compra vía tarjeta, monedero y Bizum), y **Deportes ZGZ** (Zaragoza, con validación en Tarjeta Ciudadana o QR y consulta de ocupación en tiempo real). Son referentes de features estándar (venta online mayoritaria, aforo dinámico, QR, taquilla residual para colectivos con brecha tecnológica), no competidores para Nalda.

### 3. Nivel 3 — Ticketing generalista (contexto de UX/features)

Entrápolis (del mismo fabricante que EntradasPiscina), Eventbrite y similares definen el estándar de UX del sector: compra online con pasarela, entrega de QR por email/app, validación por escaneo, y panel de informes/estadísticas de venta. No compiten en piscina municipal, pero fijan las expectativas de los usuarios (compra en 3 clics, QR inmediato).

### 4. Eje 1 — Cierre de caja e informes para intervención municipal (peso alto)

Es el punto donde una solución "de pueblo" gana o pierde credibilidad ante la secretaría-intervención. Lo que el sector resuelve y lo que espera un ayuntamiento:

- **Cuadre/arqueo de caja diario:** el arqueo compara el saldo teórico (tickets vendidos según el sistema) con el saldo real (efectivo + tarjeta/Bizum). Los TPV/software modernos automatizan el cierre por turno y por usuario/cajero, reduciendo descuadres. EntradasPiscina promete "simplificar el cierre de caja"; i2A-Cronos y DeporWin integran módulos de caja y gestión económica.
- **Informes de recaudación:** listados de tickets vendidos, altas/bajas de abonados y recaudación diaria acumulada, exportables (típicamente a Excel/PDF). Apeiron lo plantea explícitamente para la gestión de la tasa/precio público de la piscina y su exportación.
- **Integración con intervención/tesorería:** el circuito municipal exige acta de arqueo, relación de cobros, y conciliación con las cuentas. Ningún producto de nicho "conecta" nativamente con la contabilidad municipal (SICAL); lo habitual es **exportar** (Excel/PDF/CSV) para que intervención concilie. Ese es el nivel de integración que espera un municipio pequeño: no una conexión contable, sino informes fiables y trazables.
- **Aforo y asistencia:** informes de ocupación por franja/día para el ayuntamiento (control sanitario y de aforo).

**Implicación para QuickTix:** el "cierre de caja e informes para el ayuntamiento" ya está en el roadmap; conviene priorizarlo y diseñarlo con el vocabulario de intervención (arqueo diario, recaudación por tipo de entrada/abono, desglose efectivo vs. tarjeta, export a Excel/PDF). Es el entregable que convierte una demo simpática en una compra defendible ante el secretario-interventor.

### 5. Eje 2 — Modelo de precios y comercialización a ayuntamientos (peso alto)

**Modelos de tarificación del sector:**
- **Alta + cuota variable por población:** EntradasPiscina (800 € + cuota variable).
- **Cuota anual por módulos:** ReservaDeportes (piscinas 350 €/año; app propia +250 €), eAgora (por plan).
- **Licencia + mantenimiento (+ hardware):** i2A-Cronos, DeporWin (orientados a instalaciones con tornos/lectores).
- **Comisión por entrada:** habitual en ticketing generalista; en el nicho de piscina no aparece publicada como modelo dominante (la fricción al comprador se ve más en políticas de no reembolso).

**Marco de contratación pública (clave para Nalda).** El artículo 118.1 de la Ley 9/2017 de Contratos del Sector Público (redacción del RD-ley 3/2020) define como **contrato menor** los de valor estimado *"inferior a 40.000 euros, cuando se trate de contratos de obras, o a 15.000 euros, cuando se trate de contratos de suministro o de servicios"*. El contrato menor se adjudica **directamente**, con tramitación simplificada (informe de necesidad del órgano de contratación + aprobación del gasto + factura), sin licitación. El límite se refiere al **valor estimado sin IVA** (el art. 101 LCSP excluye expresamente el IVA del valor estimado). Está prohibido el fraccionamiento para eludir umbrales.

Esto significa que **QuickTix puede venderse a Nalda como contrato menor** (adjudicación directa) siempre que el importe anual quede holgadamente por debajo de 15.000 € sin IVA — algo natural en un municipio de ~1.100 habitantes con piscina de temporada. Conviene además que la solución encaje como **suministro/servicio** (licencia de software + soporte), no como concesión (la explotación de la piscina por un tercero que cobra las entradas sí es concesión de servicios y no cabe como contrato menor).

**Ejemplos reales de contratación relacionada (importes):**
- **Comunidad de Madrid, "Servicios de gestión de taquilla y control de accesos en las piscinas de verano, temporada 2026"** (exp. C-336A/038-25 / A/SER-045819/2025; CPV 79992000-4; procedimiento abierto, 115 días): presupuesto base de licitación **265.614,40 € con impuestos** (219.516,03 € sin IVA), 8 ofertas, adjudicado a **Triangle Servicios Auxiliares, S.L.** (NIF B84495837) por **172.298,18 € sin IVA** (208.480,80 € con IVA), contrato formalizado el 25/03/2026. Objeto textual: *"el desempeño de las funciones de control de acceso, con expedición de entradas y gestión de caja, para la temporada de verano del año 2026"*. *Importante: es un contrato de personal/servicios de taquilla, no de software; ilustra el gasto de una gran administración, no el de un pueblo.*
- **Ayuntamiento de Bargas (Toledo), "Elaboración e impresión de entradas y abonos para piscina municipal, temporada 2025"** (CPV 30199700): adjudicado a **Grafox Imprenta, S.L.** Es impresión de entradas físicas — el "competidor de papel" que QuickTix busca sustituir.
- **Piscina de San Vitero (Zamora), 2026:** concesión de la gestión de la piscina y quiosco (entrada máxima 1,50 €/día), canon desde 1 €. Ilustra lo ajustado de la economía de una piscina de pueblo.

**Implicación:** el presupuesto real de un municipio pequeño para digitalizar su piscina es de pocos miles de euros. QuickTix debe cotizar en ese rango (p. ej. licencia anual + soporte de temporada) y dejar por escrito que cabe en contrato menor, facilitando al ayuntamiento el informe de necesidad.

### 6. Eje 3 — Roadmap de features: matriz comparativa

| Funcionalidad | ¿Estándar del sector? | EntradasPiscina | i2A-Cronos / DeporWin | QuickTix hoy | QuickTix pendiente |
|---|---|---|---|---|---|
| Venta en taquilla (escritorio) | Sí | Sí (modo presencial) | Sí | **Sí** (app escritorio Windows) | — |
| Abonos por categorías y duraciones | Sí | Sí | Sí | **Sí** (niño/adulto/jubilado/familia numerosa; quincenal/mensual/temporada) | — |
| Entradas de invitado de abonado | Parcial | — | Parcial | **Sí** | — |
| Multi-recinto (precios por venue) | Parcial | Sí | Sí | **Sí** | — |
| Históricos de venta | Sí | Sí | Sí | **Sí** | — |
| Roles admin/manager/cliente | Sí | Sí | Sí | **Sí** | — |
| App móvil (Android) | Sí | Sí (Android + iOS) | Sí | **Sí** (Android managers/abonados) | iOS |
| **QR de validación de acceso** | **Sí (estándar)** | Sí | Sí | No | **Planificado** |
| **Control de aforo en tiempo real** | **Sí (estándar)** | Sí | Sí | No | **Planificado** |
| **Cierre de caja e informes para ayuntamiento** | **Sí (crítico)** | Sí | Sí | No | **Planificado** |
| **Compra online + pasarela de pago** | **Sí (estándar)** | Sí | Sí | No | **Planificado (requiere pasarela)** |
| Reservas por franjas horarias | Sí | Sí | Sí | No | A valorar |
| Bonos de N baños | Sí | Sí | Sí | No | A valorar |
| Control de accesos por torno/hardware | En instalaciones grandes | Vía QR/RFID | Sí (tornos, biometría) | No (fuera de alcance) | **No (decisión de diseño)** |
| RGPD: encargado del tratamiento (DPA) | Sí (SaaS) | Sí | Sí | No definido | **A definir (crítico)** |

**Gaps más importantes de QuickTix (prioridad):** (1) QR de validación; (2) cierre de caja/informes; (3) compra online con pasarela; (4) definición del rol RGPD. **Posibles diferenciadores:** foco extremo en simplicidad para taquilleros no técnicos; entradas de invitado de abonado ya resueltas; multi-recinto; y —potencialmente— un modelo de datos on-premise/soberano (el ayuntamiento como responsable, con datos alojados de forma controlada), frente a SaaS de terceros.

### 7. Eje 4 — Argumentario comercial

**QuickTix frente a papel y Excel (situación actual de Nalda):**
- Trazabilidad total de cada entrada/abono y de la recaudación, frente a hojas manuales propensas a error y descuadres.
- Cuadre de caja automático y **informes de recaudación listos para intervención** (desglose por tipo de entrada/abono, efectivo vs. tarjeta), que hoy no existen o se hacen a mano.
- Gestión de abonos por categoría y duración sin fichas de papel; históricos consultables.
- Reducción de colas y de manipulación de efectivo cuando se active la compra online + QR.

**QuickTix frente a los competidores del nicho:**
- **Precio y encaje en contrato menor:** cotizable por debajo de 15.000 € sin IVA, adjudicación directa, sin licitación ni comisión por entrada.
- **Simplicidad para usuarios no técnicos y estacionalidad:** diseñado para taquilleros de verano y una piscina de temporada, sin la complejidad de suites deportivas pensadas para ciudades deportivas.
- **Cercanía y soporte:** desarrollo a medida y trato directo frente a SaaS estandarizados; posibilidad de adaptar informes al formato exacto que pida la intervención de Nalda.
- **UX cuidada:** aprovechar los puntos débiles reportados de EntradasPiscina (lentitud con varias entradas, mezcla de idiomas, políticas de reembolso rígidas) para diferenciarse con un flujo rápido y en castellano.

**Consideraciones RGPD (factor comercial):** los competidores SaaS ya actúan como **encargado del tratamiento** con su contrato del art. 28 RGPD (DPA), asumiendo medidas de seguridad, subencargados, notificación de brechas y devolución/borrado de datos al finalizar. QuickTix debe **definir su modelo**: si se vende como software instalado/gestionado, precisar si el ayuntamiento es responsable del tratamiento y QuickTix encargado; aportar cláusula de encargo, ubicación de los datos (idealmente en España/UE), medidas de seguridad (alineadas con el ENS, dado que Pixel Innova ya lo esgrime como diferenciador) y plan de salida. No resolver esto es una desventaja competitiva directa y un riesgo para el ayuntamiento (sanciones del art. 83 RGPD, de hasta 10 millones de euros o el 2% del volumen de negocio por infracciones del art. 28).

## Recomendaciones

1. **Antes de la demo de verano, cerrar los tres gaps de mayor impacto en la decisión municipal, en este orden:**
   - **QR de validación de acceso** (feature "de mesa" que todos esperan ver).
   - **Cierre de caja diario + informe de recaudación exportable a Excel/PDF** con el vocabulario de intervención (arqueo, desglose por tipo y por medio de pago). Es lo que convence al secretario-interventor.
   - **Control de aforo en tiempo real** (aunque sea básico).
   - *Benchmark que cambia la prioridad:* si Nalda no exige compra online el primer año (piscina de temporada, público local), la pasarela de pago puede posponerse a la fase 2; si exige venta anticipada online, súbela a prioridad 1.

2. **Cotizar como contrato menor de suministro/servicios.** Preparar una oferta anual (licencia + soporte de temporada) claramente por debajo de 15.000 € sin IVA y entregar al ayuntamiento un borrador de justificación de necesidad para agilizar la adjudicación directa. Evitar cualquier estructura que parezca concesión.
   - *Umbral de referencia:* si el alcance creciera (varias piscinas de la comarca, mantenimiento plurianual) y superara 15.000 €/año sin IVA, habría que pasar a procedimiento abierto simplificado; planificarlo antes de escalar a otros municipios.

3. **Resolver el modelo RGPD ya.** Redactar un contrato de encargo del tratamiento (art. 28 RGPD), definir ubicación y seguridad de datos, y —si es viable— alinear con el Esquema Nacional de Seguridad. Convertirlo en argumento de venta ("cumplimiento RGPD/ENS incluido"), no en una nota al pie.

4. **Construir el argumentario sobre "papel/Excel → QuickTix", no solo contra EntradasPiscina.** El decisor de Nalda compara sobre todo con su gestión manual actual; liderar con trazabilidad, cuadre de caja e informes para el ayuntamiento.

5. **Explotar las debilidades de UX del líder.** En la demo, mostrar explícitamente: venta rápida de varias entradas en taquilla, interfaz íntegra en castellano y política de anulación/cambio flexible configurable por el ayuntamiento.

6. **Plan de expansión escalonado.** Usar Nalda como caso de referencia y replicar en municipios pequeños de La Rioja (Alberite, Albelda, Villamediana, etc.) con el mismo esquema de contrato menor. *Benchmark de escalado:* a partir de ~10 municipios o de la necesidad de compra online generalizada, evaluar migrar a un modelo SaaS multi-tenant con encargado del tratamiento formalizado.

## Advertencias y limitaciones

- **Precios no siempre públicos:** salvo EntradasPiscina (800 € + cuota variable) y ReservaDeportes (módulo piscinas 350 €/año), los importes de i2A-Cronos, DeporWin, eAgora, nsreserva y Piscify no están publicados y requerirían solicitud directa. Las cifras de tracción (nº de piscinas, usuarios, municipios) son en su mayoría autodeclaradas por los fabricantes.
- **Las cifras de tracción de EntradasPiscina (23 piscinas, 11.033 usuarios, ~47.000 pedidos) son de la temporada 2020**; probablemente hoy sean mayores, pero no se ha localizado cifra pública actualizada.
- **El contrato de la Comunidad de Madrid (265.614,40 €) NO es de software** sino de personal de taquilla/control de accesos, adjudicado a Triangle Servicios Auxiliares; se cita como contexto de mercado, no como precio de una herramienta comparable a QuickTix.
- **No se localizó ninguna licitación/contrato público que nombre a EntradasPiscina o Perception Technologies** para software de piscina, lo que sugiere contratación directa por importes bajos (por debajo del umbral de publicación).
- **La integración con la contabilidad municipal (SICAL) no es estándar en ningún producto del nicho**; lo realista es exportación de informes para conciliación por intervención.
- Algunas afirmaciones de marketing de los fabricantes ("la solución más inteligente", "líder del mercado") son autopromocionales y no verificables de forma independiente.