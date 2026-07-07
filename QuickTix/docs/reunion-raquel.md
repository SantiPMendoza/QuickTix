<!-- docs: reunion-raquel.md v1.0.0 — 2026-07-07 — agenda de decisiones para la reunión con Raquel -->

# Reunión con Raquel — decisiones que necesitamos de ella

Cada punto tiene: contexto en una línea, la(s) pregunta(s) concreta(s), y qué desbloquea
en el roadmap. Marcar la respuesta en la propia reunión y volcar después al Decision Log
de PROGRESS.md. Contexto competitivo completo en `docs/quicktix_research.md`.

## 1. Flujos de acceso (el bloque más grande — todo aparcado a su decisión)

**QR de validación de acceso**
- Contexto: el sector lo considera estándar ("feature de mesa"), pero nuestro razonamiento
  para Nalda fue que al abonado se le conoce y un gestor mirando el móvil en puerta retrasa.
- Pregunta: ¿quiere QR aunque sea solo para visitantes/entradas sueltas? ¿O lo descartamos
  la primera temporada?
- Desbloquea: prioridad nº 1 del research vs. nuestra decisión actual; backend de validación.

**Pase digital de abonados (pantallas 2b/2c/2e del handoff Vibra)**
- Contexto: aparcado junto al QR; el carrusel visual (2a/2f) ya está hecho.
- Pregunta: ¿aporta algo en un pueblo donde el gestor conoce a los abonados?
- Desbloquea: 3 pantallas Mobile + backend de validación (mismo trigger que el QR).

**Impresora térmica en puerta (entrada de visitantes)**
- Contexto: flujo ya elegido para visitantes de 1-2 días que no se instalarán una app.
- Preguntas: ¿confirma el flujo? ¿Quién imprime — el manager desde el móvil o la taquilla?
  ¿Hay presupuesto para el hardware (~50-150 €)?
- Desbloquea: decisión de hardware (ESC/POS USB vs Bluetooth) y desde qué app se imprime.

**Control de aforo en tiempo real**
- Contexto: estándar del sector; sin registrar accesos solo podemos estimar por ventas.
- Pregunta: ¿le vale el "aforo estimado" (entradas de hoy) o necesita aforo real
  (implica registrar entradas Y salidas → QR o conteo manual)?
- Desbloquea: alcance del KPI del Panel y del flujo de accesos.

## 2. Dinero: pagos, anulaciones y cierre de caja

**Medios de pago en taquilla**
- Contexto: hoy TODO es efectivo implícito; el arqueo del sector desglosa efectivo vs tarjeta/Bizum.
- Preguntas: ¿la taquilla de Nalda tiene datáfono o lo tendrá? ¿Bizum? ¿Primera temporada
  solo efectivo?
- Desbloquea: campo `PaymentMethod` en ventas (diseñado en S2, activado cuando ella confirme)
  y el desglose del informe de cierre.

**Compra online / pasarela de pago**
- Contexto: el research dice que si Nalda no la exige el primer año, se pospone a fase 2;
  si la exige, sube a prioridad 1.
- Pregunta: ¿necesita venta anticipada online la primera temporada, sí o no?
- Desbloquea: TODO el orden del roadmap post-demo + elección de pasarela (bloqueado en PROGRESS).

**Política de anulaciones** *(la anulación entra sí o sí en S2 — lo que necesitamos es la política)*
- Contexto: las reseñas negativas del competidor líder son justo por reembolsos rígidos;
  "política flexible configurable por el ayuntamiento" es nuestro argumento.
- Preguntas: ¿quién puede anular (solo admin, o también el manager que vendió)? ¿Solo el
  mismo día? ¿Devolución de dinero o cambio/vale? ¿Anulación de abonos ya usados?
- Desbloquea: reglas de negocio de la anulación en S2 y su reflejo en el arqueo.

**Cierre de caja: el formato que pide intervención**
- Contexto: es EL entregable que convierte la demo en compra defendible (research §4);
  ningún producto conecta con SICAL — se exporta y ya.
- Preguntas: ¿qué formato/columnas quiere el secretario-interventor? ¿Excel o PDF? ¿Diario,
  semanal o por turno? ¿Quién lo recibe y cómo (email, papel firmado)? ¿Puede enseñarnos
  un arqueo actual en papel para copiarle el vocabulario?
- Desbloquea: spec exacta de S2 — informes con el vocabulario de intervención de Nalda.

## 3. Precios y catálogo real de Nalda

- Tarifas reales: precios de entrada y de cada abono (validar contra nuestro seed).
- Categorías: ¿niño/adulto/jubilado/familia numerosa es el catálogo real? ¿Falta alguna?
- ¿Descuento a empadronados? (competidores lo ofrecen; implicaría verificar padrón).
- Fechas de temporada: apertura y cierre reales (afecta a "acumulado de temporada" del Panel
  y a la duración de abonos).
- Entradas de invitado de abonado: ¿cómo funciona hoy en Nalda? (ya lo soportamos — validar reglas).

## 4. Operativa y personal

- ¿Quiénes venden qué? Confirmar lo implementado hoy: los administrativos del ayuntamiento
  (admins) venden abonos; los taquilleros/managers venden entradas en puerta. ¿Correcto?
- ¿Cuántos taquilleros hay en verano y qué nivel técnico tienen? (afecta a formación y a
  cuánto pulimos el POS).
- ¿Un solo turno de taquilla o varios? (afecta al cierre de caja por turno vs por día).

## 5. Multi-recinto (validación del pitch S3)

- Contexto: somos de los pocos del nicho con multi-recinto ya funcionando; S3 lo generaliza
  ("Pabellón Sócon" en la demo).
- Preguntas: ¿el ayuntamiento gestiona otros espacios con entrada/reserva (pabellón, frontón,
  pistas)? ¿Le interesa gestionarlos con lo mismo?
- Desbloquea: S3 y la decisión aparcada de generalizar TicketType (refactor caro — solo si valida).

## 6. RGPD y datos (factor comercial, no solo técnico)

- Contexto: los SaaS competidores ya lo dan resuelto (encargado del tratamiento, art. 28);
  no tenerlo definido es desventaja directa. Manejaremos datos de vecinos, incluidos menores.
- Preguntas: ¿el ayuntamiento tiene DPD (delegado de protección de datos) o lo lleva la
  diputación/un externo? ¿Quién firmaría el contrato de encargo? ¿Exigen datos alojados
  en España/UE? ¿Consentimientos actuales de los abonados en papel?
- Desbloquea: modelo RGPD (ayuntamiento responsable + QuickTix encargado), redacción del
  contrato art. 28, y decisión de hosting.

## 7. Comercial y contratación

- Contexto: contrato menor = adjudicación directa por debajo de 15.000 € sin IVA (art. 118.1
  LCSP); el presupuesto real de un pueblo son pocos miles de euros/año.
- Preguntas: ¿qué presupuesto anual maneja el ayuntamiento para esto? ¿Prefiere licencia
  anual + soporte de temporada? ¿Quién firma/tramita el contrato menor (secretaría)?
  ¿Le ayudamos con el borrador del informe de necesidad?
- Desbloquea: precio de la oferta y calendario de contratación para la temporada 2027.

## 8. Hosting e infraestructura

- Contexto: producción sin definir; el emulador de decisión es RGPD + coste.
- Preguntas: ¿el ayuntamiento tiene algún servidor/proveedor ya contratado? ¿Preferencia
  por "los datos en un servidor del ayuntamiento" (argumento soberanía) vs cloud gestionado
  por nosotros?
- Desbloquea: hosting del piloto (bloqueado en PROGRESS) y el argumento de venta RGPD.

---

## Para llevar a la reunión

- Demo con datos de Nalda de mentira pero creíbles (precios reales si los conseguimos antes).
- El flujo estrella: venta batch rápida en el POS ("3 adultos + 2 niños, una operación").
- La venta de abono desde administración (lo que ella hará en su día a día).
- El Panel con recaudación — y preguntarle QUÉ número quiere ver ella cada mañana.
- Imprimir esta agenda y marcar las respuestas en papel durante la reunión.
