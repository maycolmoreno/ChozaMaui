# AUDITORÍA

Alcance: auditoría estática del proyecto MAUI en ChozaMaui, sin cambios de código y sin perfilado en dispositivo. El diagnóstico se basa en estructura MVVM, navegación Shell, XAML, servicios y patrones de carga.

## 🔴 Críticos

1. Archivo: ChozaMaui/ViewModels/PedidosViewModel.cs y ChozaMaui/Services/StompWebSocketService.cs
Problema: la pantalla de pedidos combina polling cada 30 segundos con WebSocket STOMP, y además vuelve a suscribirse al reaparecer. El servicio STOMP acumula callbacks por topic y no limpia el diccionario de suscripciones al desconectar.
Impacto: riesgo real de memoria retenida, recargas duplicadas, alertas repetidas y trabajo extra de UI/red al navegar varias veces por la pantalla.
Solución: introducir suscripción idempotente y ciclo de vida explícito de subscripciones; elegir un canal principal de refresco por rol o pantalla, con polling solo como fallback.

2. Archivo: ChozaMaui/ViewModels/PosViewModel.cs, ChozaMaui/ViewModels/PagoViewModel.cs y ChozaMaui/ViewModels/HistorialCuentasViewModel.cs
Problema: ViewModels sobredimensionados y con demasiadas responsabilidades. PosViewModel tiene 770 líneas, PagoViewModel 588 y HistorialCuentasViewModel 311. Mezclan estado de UI, navegación, validación, reglas de negocio, composición de mensajes, lógica offline y coordinación de APIs.
Impacto: mantenimiento costoso, alta probabilidad de regresiones, sobrecarga de notificaciones de propiedades y dificultad para optimizar partes críticas sin tocar demasiado código.
Solución: partir por casos de uso o coordinadores de pantalla. En POS separar catálogo, carrito, pedido actual y sincronización. En Pago separar carga de cuenta, validación de cobro, flujo de comprobante y cierre de mesa.

3. Archivo: ChozaMaui/Views/PosPage.xaml.cs, ChozaMaui/Views/PagoPage.xaml.cs, ChozaMaui/Views/MapaPage.xaml.cs, ChozaMaui/Views/PedidosPage.xaml.cs y ChozaMaui/Views/HistorialCuentasPage.xaml.cs
Problema: casi todas las pantallas son transitorias y además recargan datos completos en OnAppearing.
Impacto: navegación lenta, recreación innecesaria de árbol visual, repeticiones de llamadas de red y sensación de apertura pesada al volver a pantallas frecuentes.
Solución: mantener páginas costosas vivas mientras el shell siga activo, o al menos cachear datos y usar invalidación incremental en lugar de recarga total en cada OnAppearing.

4. Archivo: ChozaMaui/ViewModels/PosViewModel.cs, ChozaMaui/ViewModels/PagoViewModel.cs y ChozaMaui/ViewModels/HistorialCuentasViewModel.cs
Problema: varias pantallas arrancan bajando colecciones completas para resolver una sola decisión local. POS descarga mesas, pedidos, categorías y productos al abrir. Pago lista cuentas abiertas para encontrar una por mesa. Historial resuelve el pedido a pagar leyendo todos los pedidos y luego pidiendo el detalle.
Impacto: red innecesaria, mayor latencia percibida, más GC y más tiempo de espera antes de interacción útil.
Solución: crear endpoints o consultas específicas por escenario, y agregar cache de sesión para catálogos, cuentas abiertas y pedido actual.

5. Archivo: ChozaMaui/Views/PosPage.xaml, ChozaMaui/Views/PagoPage.xaml, ChozaMaui/Views/PedidoDetallePage.xaml, ChozaMaui/Views/MapaPage.xaml, ChozaMaui/Views/TurnoPage.xaml y ChozaMaui/Views/AdminPage.xaml
Problema: patrón repetido de ScrollView envolviendo CollectionView.
Impacto: pérdida de virtualización efectiva, más costo de medida/layout, scroll menos fluido y peor tiempo de render inicial.
Solución: sustituir esos layouts por Grid con áreas fijas y listas virtualizadas, o mover la cabecera al Header del CollectionView en lugar de envolver la lista en un ScrollView externo.

## 🟡 Mejoras

1. Archivo: ChozaMaui/Services/ApiService.cs
Problema: ApiService funciona como fachada monolítica y además recompone internamente siete servicios de dominio. La capa está duplicada conceptualmente: fachada total más microservicios internos no inyectables de forma independiente.
Impacto: acoplamiento alto, testeo difícil y expansión costosa de políticas transversales como cache, retry o telemetría.
Solución: elegir una de dos rutas. O se usa ApiService como único gateway bien estructurado, o se inyectan directamente servicios por dominio y se elimina la fachada monolítica.

2. Archivo: ChozaMaui/Services/OrderWorkflowService.cs y ChozaMaui/ViewModels/PagoViewModel.cs
Problema: la lógica de asociar pedido a cuenta aparece en más de un punto. OrderWorkflowService intenta adjuntar pedido a cuenta; PagoViewModel vuelve a asegurar esa relación y también crea cuenta si falta.
Impacto: reglas de negocio repartidas, divergencia futura y bugs difíciles de reproducir según el punto de entrada.
Solución: unificar la política pedido-cuenta en un único servicio de dominio o workflow.

3. Archivo: ChozaMaui/ViewModels/PosViewModel.cs
Problema: resolución del pedido actual con secuencia ineficiente. En ciertos flujos busca por id, luego lista todos los pedidos y luego vuelve a pedir detalle.
Impacto: latencia acumulada y repetición de llamadas para un caso de uso muy frecuente.
Solución: resolver pedido actual por id o por mesa con una sola fuente de verdad y cache local corto.

4. Archivo: ChozaMaui/ViewModels/PagoViewModel.cs
Problema: matriz muy densa de NotifyPropertyChangedFor y OnPropertyChanged manuales sobre propiedades derivadas de monto, saldo, cambio y estado de comprobante.
Impacto: invalidación excesiva de bindings y mayor complejidad mental para mantener el flujo de cobro.
Solución: reagrupar derivados por bloques de estado, reducir notificaciones manuales y calcular propiedades secundarias desde uno o dos estados núcleo.

5. Archivo: ChozaMaui/ViewModels/PosViewModel.cs
Problema: PropertyChanged redundantes en POS. Varias propiedades ya derivan de PedidoEnCurso o del carrito, pero además se fuerzan manualmente en múltiples puntos.
Impacto: ruido de UI, refrescos redundantes y mayor riesgo de inconsistencias.
Solución: centralizar recalculo en métodos pequeños y dejar que las propiedades observables generen la mayor parte de cambios.

6. Archivo: ChozaMaui/Views/PosPage.xaml
Problema: las tarjetas de producto resuelven imagen remota directamente en la UI, sin evidencia de cache, downsampling o placeholder más agresivo.
Impacto: render más lento, saltos visuales y mayor presión sobre red al reabrir POS.
Solución: cache de imágenes, downsampling y carga diferida según visibilidad.

7. Archivo: ChozaMaui/ViewModels/MapaViewModel.cs y ChozaMaui/ViewModels/PedidosViewModel.cs
Problema: polling paralelo en más de una pantalla con intervalos fijos y sin coordinación global.
Impacto: tráfico duplicado y competencia por recursos cuando el usuario cambia entre pestañas.
Solución: crear un scheduler de refresco central o feeds compartidos por dominio.

8. Archivo: ChozaMaui/ViewModels/HistorialCuentasViewModel.cs
Problema: reemplazo completo de ObservableCollection al filtrar y al cargar.
Impacto: rerender completo de listas, pérdida de estado visual y más trabajo de binding.
Solución: aplicar diffs o actualizar la colección existente cuando el cambio sea parcial.

## 🟢 Limpieza

1. Archivo: ChozaMaui/Views/PosPage.xaml.bak
Problema: archivo de respaldo dentro del proyecto que contamina búsquedas y auditorías de bindings.
Impacto: ruido técnico y falsos positivos.
Solución: eliminarlo del repositorio o moverlo fuera del árbol del proyecto.

2. Archivo: ChozaMaui/MauiProgram.cs junto con ChozaMaui/Views/AppShell.xaml, ChozaMaui/Views/AppShellCajero.xaml y ChozaMaui/Views/AppRoutes.cs
Problema: hay páginas y ViewModels registrados en DI que no aparecen conectados al flujo Shell actual: Admin, Clientes, Productos y ComedoresMesas.
Impacto: deuda de mantenimiento y superficie innecesaria.
Solución: decidir si son módulos reservados, ocultos o realmente huérfanos; si están fuera de alcance operativo, retirarlos del arranque o documentar su activación.

3. Archivo: ChozaMaui/ViewModels/PosViewModel.cs, ChozaMaui/ViewModels/MapaViewModel.cs y ChozaMaui/ViewModels/TurnoViewModel.cs
Problema: hay propiedades probablemente no usadas o de uso dudoso como BienvenidaTexto, FotoTexto, MesaInfoTexto, InicialUsuario y EstadoCajaEsVerde.
Impacto: bajo, pero añade ruido y complica la lectura.
Solución: verificar bindings reales y eliminar o consolidar las que no aporten.

4. Archivo: ChozaMaui/Resources/Styles/Colors.xaml y ChozaMaui/Resources/Styles/Styles.xaml
Problema: hay alias de colores duplicados y estilos globales con consumo dudoso.
Impacto: bajo, pero el sistema visual pierde claridad y escala peor.
Solución: reducir aliases a los estrictamente necesarios y depurar estilos no usados tras una comprobación final de bindings XAML.

5. Archivo: ChozaMaui/Services/NavigationService.cs y ChozaMaui/Views/AppShell.xaml.cs
Problema: sigue existiendo un punto de visibilidad por rol que hoy está vacío.
Impacto: bajo, pero deja una falsa expectativa de comportamiento dinámico.
Solución: o se elimina, o se reusa para centralizar gating de tabs.

## PLAN DE REFACTORIZACIÓN

[x] Paso 1. Aislar refresco y memoria: corregidas las carreras básicas de conexión/desconexión en STOMP, evitada la desconexión global al salir de una pantalla si otra sigue suscrita y eliminado el doble refresh inmediato en Pedidos ante eventos WebSocket. La estrategia actual queda como WebSocket primario con polling solo como fallback del coordinador.
[ ] Paso 2. Descomponer PosViewModel: separar catálogo, carrito, pedido actual, adjuntos y workflow offline.
	Avance parcial: extraídos PosDraftService y PosOrderStateService, y centralizado el refresco derivado de carrito/mesa para reducir OnPropertyChanged dispersos. Pendiente: mover más lógica de catálogo, navegación y adjuntos fuera del ViewModel.
[ ] Paso 3. Descomponer PagoViewModel: separar carga de cuenta, validación de cobro, flujo de pago y flujo de comprobante.
	Avance parcial: extraídos PagoComprobanteService y PagoValidationService, movida al PagoWorkflowService la coordinación principal de preparar cuenta, registrar cobro y cerrar/liberar mesa, y compactado el estado UI del comprobante y los refrescos de propiedades derivadas del formulario. Pendiente: el ViewModel aún conserva bastante estado visual, pero ya no concentra la mayor parte de la lógica de dominio.

[ ] Paso 3b. Descomponer HistorialCuentasViewModel: separar filtro, estadísticas y operaciones de cliente/cobro.
	Avance parcial: extraídos HistorialCuentasPresentationService, HistorialCuentasClienteService, HistorialCuentasCobroService y HistorialCuentasLoadService para encapsular filtrado/estadísticas, operaciones de cliente, resolución del pedido a cobrar y carga inicial con validación de caja. Además se compactó parte del estado modal del buscador/detalle. Pendiente: adelgazar más la navegación y el estado visual restante del ViewModel.
[x] Paso 4. Introducir cache de lectura corta para catálogos, cuentas abiertas, pedidos recientes y estado de mesas: completado con SessionCacheService y servicios específicos para catálogo POS, cuenta abierta por mesa, resumen de cuenta, pedido actual por mesa/detalle, pedido reciente por cuenta y listado de mesas. Además se añadieron invalidaciones finas al crear pedidos, cambiar su estado, cobrar/cerrar cuentas, liberar mesas y ejecutar CRUD de mesas.
[ ] Paso 5. Reemplazar ScrollView externos en pantallas con CollectionView y restaurar virtualización real.
	Avance parcial: MapaPage ya no envuelve la lista principal en un ScrollView vertical externo; el contenido superior se movió al Header de la CollectionView y la leyenda al Footer, restaurando la virtualización real de la lista de grupos/comedores sin cambiar el ViewModel. PagoPage siguió el mismo patrón: el ScrollView externo se sustituyó por una CollectionView principal con Header/Footer, eliminando la lista embebida en scroll y dejando el detalle del pedido como lista base desplazable. PedidoDetallePage también se migró a una única CollectionView principal: Detalles quedó como lista virtualizada base y el historial pasó a BindableLayout dentro del footer para evitar anidar otra CollectionView vertical.
[ ] Paso 6. Reducir recargas en OnAppearing: pasar a invalidación selectiva, refresh manual o refresh por evento.
	Avance parcial: PosPage dejó de forzar CargarDatosAsync en cada OnAppearing y ahora usa una carga condicional con ventana mínima en PosViewModel, alineándose con el patrón ya aplicado en Pago e HistorialCuentas para evitar recargas completas al volver rápidamente a la pantalla. El mismo patrón se aplicó en MesaDetallePage y TurnoPage mediante carga condicional en sus ViewModels para evitar refetch completo al reentrar en pocos segundos. Mapa y Pedidos ya dependen del LiveRefreshCoordinator, que mantiene su propia ventana mínima antes del refresh inicial al reaparecer.
[ ] Paso 7. Simplificar ApiService y decidir arquitectura de servicios: fachada única o servicios por dominio inyectables.
	Paso completado: el desacoplamiento ya cubre tambien las vistas operativas y permitio retirar por completo la fachada ApiService del proyecto MAUI. HistorialCuentasLoadService ya depende directamente de CajaApiService y CuentaApiService, HistorialCuentasCobroService usa PedidoApiService de forma directa, ProductosViewModel consume ProductoApiService, ComedoresMesasViewModel utiliza MesaApiService junto con MesaStateService, ClientesViewModel trabaja con ClienteApiService, PosClientService e HistorialCuentasClienteService combinan ClienteApiService y CuentaApiService segun corresponda, se creó ReporteApiService para extraer la consulta de ventas del día, PerfilViewModel cambia contraseña a traves de UsuarioApiService y el flujo de login/conectividad se movió a servicios directos: LoginViewModel usa UsuarioApiService, CajaApiService y ServerConnectionService, mientras ConnectivityService verifica salud del backend con ServerConnectionService. Ademas, PosCatalogService consume ProductoApiService, MesaStateService usa MesaApiService, PosDataService obtiene pedidos desde PedidoApiService, OrderWorkflowService combina PedidoApiService y CuentaApiService, PagoComprobanteService sube comprobantes mediante PagoApiService, MesaDetailWorkflowService consulta pedidos con PedidoApiService, PagoWorkflowService combina CuentaApiService y PagoApiService, PosOrderWorkflowService trabaja con PedidoApiService y ProductoApiService, y tanto PedidosViewModel como MapaViewModel consumen ya PedidoApiService. Con este corte, TurnoWorkflowService combina CajaApiService, CuentaApiService, PagoApiService y ReporteApiService, AdminDashboardService usa ReporteApiService, CajaApiService y MesaApiService, los consumidores de aplicación quedaron movidos a servicios por dominio registrados en MauiProgram con HttpClient configurado dinámicamente desde SettingsService y ApiService fue eliminado del proyecto sin afectar la compilación.
[x] Paso 8. Consolidar reglas pedido-cuenta en un solo workflow de dominio: completado con PedidoCuentaWorkflowService como unica politica compartida para resolver o crear la cuenta abierta de una mesa/cliente, asociar el pedido a la cuenta y reutilizar esa regla tanto desde OrderWorkflowService como desde PagoWorkflowService.
[x] Paso 9. Limpiar código muerto y módulos huérfanos confirmados, incluyendo backup XAML, estilos no usados y propiedades obsoletas: completado con la eliminación del backup PosPage.xaml.bak ya ausente, la retirada de propiedades sin bindings confirmados en PosViewModel y TurnoViewModel como BienvenidaTexto, FotoTexto, MesaInfoTexto y EstadoCajaEsVerde, la depuración de estilos globales sin uso en Styles.xaml, la limpieza de aliases/colores redundantes sin referencias reales en Colors.xaml y el borrado físico de los módulos administrativos huérfanos Admin, Clientes, Productos y ComedoresMesas junto con el servicio residual AdminDashboardService, manteniendo la compilación correcta.
[x] Paso 10. Ejecutar fase final de microoptimización de bindings, imágenes y colecciones: completado con la preservación de colecciones observables en HistorialCuentasViewModel, MapaViewModel y NotificacionesViewModel; la compactación de derivados simples en PagoViewModel, PedidoDetalleViewModel, MesaDetalleViewModel, TurnoViewModel y PosViewModel; la reducción de notificaciones manuales y resets inconsistentes en POS; la mejora del borrador por mesa para evitar arrastre de observaciones/foto; el uso de un helper único de reemplazo en sitio para varias cargas base y listas filtradas; la incorporación de cache explícito y feedback de carga para imágenes en PosPage; y la mejora del contexto visual en listas activas de MapaPage y TurnoPage mediante KeepScrollOffset y alturas menos rígidas. Además, Turno quedó conectado a un desglose real de ventas por método desde el backend y la UI ahora aclara cuándo un $0.00 corresponde a ausencia real de movimientos. Con ello se reduce rerender innecesario, se estabiliza mejor el estado visual y se cierra la fase de microoptimizaciones rentables sin mezclarla con refactors mayores aún abiertos.

## TIEMPO ESTIMADO

1. Estabilización de refresco, STOMP y polling: 6 a 10 horas.
2. Refactor de POS: 12 a 18 horas.
3. Refactor de Pago e HistorialCuentas: 10 a 16 horas.
4. Cache, simplificación de servicios y limpieza: 8 a 14 horas.
5. Ajuste XAML y virtualización: 6 a 10 horas.
6. Validación integral en dispositivo: 6 a 8 horas.

Estimación total razonable: 48 a 76 horas efectivas.

## IMPACTO ESPERADO

Inicio: mejora estimada de 15% a 25% en carga percibida de pantallas pesadas.

Navegación: mejora estimada de 25% a 40% al reducir reconstrucción y recargas completas.

Memoria: mejora estimada de 15% a 30% si se elimina acumulación de callbacks y se reduce recreación de páginas/listas.

Red: mejora estimada de 30% a 50% si se introducen cache corto y consultas específicas por caso de uso.

UX: mejora clara en fluidez de listas, menor parpadeo y menos esperas tras acciones simples.

## ARCHIVOS EXACTOS A MODIFICAR

1. Núcleo de navegación y composición
ChozaMaui/MauiProgram.cs
ChozaMaui/Services/NavigationService.cs
ChozaMaui/Views/AppShell.xaml
ChozaMaui/Views/AppShell.xaml.cs
ChozaMaui/Views/AppShellCajero.xaml
ChozaMaui/Views/AppRoutes.cs

2. Refresco, memoria y conectividad
ChozaMaui/Services/StompWebSocketService.cs
ChozaMaui/Services/ConnectivityService.cs
ChozaMaui/Services/NotificationService.cs

3. Servicios y workflows
ChozaMaui/Services/ApiService.cs
ChozaMaui/Services/OrderWorkflowService.cs
ChozaMaui/Services/PendingOrderService.cs
ChozaMaui/Services/PagoApiService.cs
ChozaMaui/Services/PedidoApiService.cs
ChozaMaui/Services/CuentaApiService.cs

4. ViewModels prioritarios
ChozaMaui/ViewModels/PosViewModel.cs
ChozaMaui/ViewModels/PagoViewModel.cs
ChozaMaui/ViewModels/PedidosViewModel.cs
ChozaMaui/ViewModels/MapaViewModel.cs
ChozaMaui/ViewModels/HistorialCuentasViewModel.cs

5. XAML con sobrecarga o virtualización degradada
ChozaMaui/Views/PosPage.xaml
ChozaMaui/Views/PagoPage.xaml
ChozaMaui/Views/PedidosPage.xaml
ChozaMaui/Views/MapaPage.xaml
ChozaMaui/Views/PedidoDetallePage.xaml
ChozaMaui/Views/TurnoPage.xaml
ChozaMaui/Views/AdminPage.xaml

6. Code-behind con carga automática
ChozaMaui/Views/PosPage.xaml.cs
ChozaMaui/Views/PedidosPage.xaml.cs
ChozaMaui/Views/MapaPage.xaml.cs
ChozaMaui/Views/PagoPage.xaml.cs
ChozaMaui/Views/HistorialCuentasPage.xaml.cs

7. Limpieza
ChozaMaui/Views/PosPage.xaml.bak
ChozaMaui/Resources/Styles/Colors.xaml
ChozaMaui/Resources/Styles/Styles.xaml

## RESULTADO ESPERADO

Inicio: menos descargas masivas al entrar a POS, Pago e Historial.

Navegación: transiciones más rápidas entre tabs y menos esperas al volver a una pantalla ya visitada.

Memoria: menor retención de callbacks, menos recreación de listas y menos presión de GC.

Red: menos consultas globales para resolver datos puntuales y menor tráfico periódico duplicado.

UX: scroll más fluido, menos parpadeos, menos estados inconsistentes y feedback más rápido tras cobrar, entregar o refrescar.