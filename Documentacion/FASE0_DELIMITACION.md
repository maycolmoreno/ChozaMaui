Fase 0. Delimitacion y mapa actual del sistema

Fecha

28 de mayo de 2026

Objetivo de esta fase

Identificar que modulo cumple hoy cada responsabilidad, como se autentican los clientes, donde se mantiene el estado de sesion y cual es el backend que parece actuar como fuente operativa de verdad.

Conclusion ejecutiva

El sistema actual esta dividido en tres piezas con responsabilidades distintas:

1. consumochoza funciona como aplicacion web Thymeleaf con seguridad basada en sesion HTTP.
2. pisip funciona como API REST principal con JWT stateless, seguridad por roles y capas mas cercanas a una arquitectura de aplicacion.
3. ChozaMaui funciona como cliente movil Android que consume la API por HTTP y WebSocket, guardando sesion localmente.

La evidencia revisada apunta a que pisip es hoy el backend operativo mas cercano a la fuente de verdad de negocio para pedidos, cuentas, clientes, mesas, pagos y usuarios. consumochoza no parece ser el backend canonico del dominio, sino un cliente web enriquecido que consume la API de pisip y ademas contiene logica de presentacion y de orquestacion funcional propia. MAUI consume pisip directamente, pero mantiene decisiones de rol y visibilidad en cliente.

Estado de la delimitacion

1. Delimitacion de modulos: confirmada.
2. Mecanismo de autenticacion por modulo: confirmado.
3. Estado de sesion web y movil: confirmado.
4. Catalogo inicial de roles: confirmado.
5. Fuente canonica definitiva de toda la logica: probable pero aun debe validarse por caso de uso en la Fase 1.

Mapa de modulos

Modulo 1. consumochoza

Rol actual:

1. Aplicacion web Spring Boot con Thymeleaf.
2. Mantiene login web propio con Spring Security.
3. Usa sesion HTTP para guardar el JWT que obtiene desde la API.
4. Consume endpoints de backend mediante WebClient.
5. Contiene controladores MVC y un controlador REST propio para POS web.

Evidencia principal:

1. pom con Thymeleaf, Security, JPA y WebFlux para WebClient.
2. SecurityConfig define rutas web por rol y manejo de sesion.
3. ApiAuthenticationProvider autentica contra la API y guarda JWT en HttpSession.
4. WebClientConfig inyecta el JWT de sesion en cada request saliente.
5. PedidoServiceImpl consume /pedidos via WebClient, no implementa logica de persistencia local en el servicio inspeccionado.

Controladores detectados:

1. AuthControlador.
2. DashboardControlador.
3. UsuarioControlador.
4. ClienteControlador.
5. CategoriaControlador.
6. ComedorControlador.
7. CocinaControlador.
8. ProductoControlador.
9. MenuDigitalControlador.
10. CuentasControlador.
11. CuentaControlador.
12. CajaControlador.
13. PedidosControlador.
14. MesaControlador.
15. ReportesControlador.
16. PosApiControlador.

Servicios detectados:

1. IUsuarioService y UsuarioServiceImpl.
2. IReporteService y ReporteServiceImpl.
3. IProductoService y ProductoServiceImpl.
4. IPedidoService y PedidoServiceImpl.
5. IPagoService y PagoServiceImpl.
6. ICuentaService y CuentaServiceImpl.
7. IClienteService y ClienteServiceImpl.
8. IComedorService y ComedorServiceImpl.
9. ICategoriaService y CategoriaServiceImpl.
10. IMesaService y MesaServiceImpl.
11. ICajaService y CajaServiceImpl.

Lectura arquitectonica inicial:

consumochoza mezcla tres responsabilidades:

1. web MVC y renderizado Thymeleaf;
2. seguridad web basada en sesion;
3. composicion de flujo y logica de presentacion cercana al caso de uso.

Ejemplo confirmado:

En PedidosControlador se observan decisiones de vista y flujo como agrupacion kanban, determinacion de boton Cobrar por mesa y cliente, y preparacion del modelo POS. Esto no es persistencia, pero tampoco es solo rendering trivial. Hay riesgo de que parte del comportamiento funcional real este acoplado a la web.

Modulo 2. pisip

Rol actual:

1. API REST principal del sistema.
2. Seguridad JWT stateless.
3. Seguridad HTTP por ruta y seguridad fina por metodo con PreAuthorize.
4. Estructura por capas: presentacion, aplicacion, dominio e infraestructura.
5. Repositorios JPA reales para entidades del negocio.

Evidencia principal:

1. pom con Web, Data JPA, Validation, Security, WebSocket, Flyway y OpenAPI.
2. SeguridadConfig usa SessionCreationPolicy.STATELESS.
3. Roles centraliza expresiones reutilizables para permisos.
4. PedidoControlador consume IPedidoUseCase y mapeadores request/response.
5. Existen repositorios JPA para usuarios, productos, pedidos, detalle, pagos, mesas, cuentas, comedor, clientes, categorias y caja.

Controladores REST detectados:

1. UsuarioControlador.
2. CajaControlador.
3. ReporteControlador.
4. ProductoControlador.
5. PedidoDetalleControlador.
6. PedidoControlador.
7. CategoriaControlador.
8. PagoControlador.
9. MesaControlador.
10. ComedorControlador.
11. ClienteControlador.
12. CuentaControlador.

Repositorios JPA detectados:

1. IUsuarioJpaRepositorio.
2. IProductoJpaRepositorio.
3. IPedidoJpaRepositorio.
4. IPedidoDetalleJpaRepositorio.
5. IPagoJpaRepositorio.
6. IMesaJpaRepositorio.
7. ICuentaJpaRepositorio.
8. IComprobantePagoJpaRepositorio.
9. IComedorJpaRepositorio.
10. IClienteJpaRepositorio.
11. ICategoriaJpaRepositorio.
12. ICajaTurnoJpaRepositorio.

Lectura arquitectonica inicial:

pisip es el modulo mas cercano al backend canonico. La presencia de use cases, mapeadores, repositorios JPA, JWT y controles por metodo indica que aqui deberian vivir las reglas operativas y las capacidades funcionales compartidas por web y MAUI.

Modulo 3. ChozaMaui

Rol actual:

1. Cliente movil .NET MAUI Android.
2. Consume la API por HttpClient con Bearer token.
3. Guarda sesion en SecureStorage.
4. Decide navegacion inicial segun rol.
5. Mantiene coordinadores y workflow services en cliente.
6. Consume WebSocket STOMP para refresco en tiempo real.

Evidencia principal:

1. SessionService guarda token, userId, username, nombre y rol en SecureStorage.
2. AuthHandler inyecta el Bearer token en cada request.
3. NavigationService selecciona shell distinta para CAJERO y shell general para otros roles.
4. MauiProgram registra multiples servicios de flujo como PosOrderWorkflowService, PagoWorkflowService, TurnoWorkflowService y MesaDetailWorkflowService.
5. MapaViewModel, PosViewModel, PedidosViewModel, PedidoDetalleViewModel, TurnoViewModel e HistorialCuentasViewModel contienen logica visible condicionada por rol.

Servicios cliente relevantes detectados:

1. SessionService.
2. AuthHandler.
3. NavigationService.
4. PedidoApiService.
5. CuentaApiService.
6. PagoApiService.
7. ProductoApiService.
8. MesaApiService.
9. ClienteApiService.
10. UsuarioApiService.
11. CajaApiService.
12. ReporteApiService.
13. PosOrderWorkflowService.
14. PagoWorkflowService.
15. TurnoWorkflowService.
16. MesaDetailWorkflowService.
17. LiveRefreshCoordinator.
18. StompWebSocketService.

Lectura arquitectonica inicial:

MAUI no es un cliente pasivo. Tiene una capa propia de orquestacion y varias decisiones funcionales visibles por rol. Esto no prueba aun duplicacion indebida, pero si confirma que parte del flujo esta distribuido entre backend y cliente movil.

Autenticacion y sesion por modulo

consumochoza

1. Login via Spring Security web.
2. Credenciales delegadas a la API de pisip.
3. JWT recibido desde pisip y guardado en HttpSession.
4. WebClient agrega Authorization Bearer en llamadas salientes usando la sesion.

pisip

1. Login REST publico en /api/usuarios/login.
2. JWT validado por filtro.
3. Sin sesion HTTP.
4. Seguridad por rutas y por metodos.

ChozaMaui

1. Login via UsuarioApiService.
2. JWT guardado en SecureStorage.
3. AuthHandler agrega Authorization Bearer.
4. La app decide shell y pantallas iniciales segun rol.

Roles confirmados

Roles operativos detectados en backend:

1. ADMIN.
2. CAMARERO.
3. COCINA.
4. CAJERO.

Observaciones:

1. En pisip los roles estan centralizados en una clase de constantes reutilizable.
2. En MAUI siguen apareciendo comparaciones directas de strings de rol en ViewModels y navegacion.
3. En consumochoza la autorizacion principal sigue estando definida por rutas web y roles.

Riesgos identificados en Fase 0

Riesgo 1. consumochoza contiene logica funcional de composicion de flujo

Aunque los servicios inspeccionados delegan a la API, la web sigue tomando decisiones de comportamiento en controladores MVC y en su controlador POS propio. Eso anticipa riesgo de divergencia entre web y movil.

Riesgo 2. MAUI decide capacidad funcional usando rol en cliente

Se detectan condiciones como permitir cobrar o enviar a cocina basadas en _session.Rol. Eso puede desalinearse de backend si cambian permisos o transiciones.

Riesgo 3. Doble capa de permisos

Hay permisos en pisip, permisos de rutas en consumochoza y visibilidad por rol en MAUI. Esto hace probable que existan diferencias entre lo que la UI deja intentar y lo que el backend realmente acepta.

Riesgo 4. Backend canonico probable, pero no aun probado por flujo completo

La estructura de pisip lo posiciona como fuente de verdad, pero la Fase 1 debe verificar si todos los pasos del flujo de crear pedido estan realmente soportados por endpoints y DTOs suficientes.

Riesgo 5. Web enriquecida y movil enriquecido

Si ambos clientes contienen orquestacion relevante, la migracion a backend como fuente unica de verdad exigira limpiar logica en dos frentes, no solo en MAUI.

Hipotesis de trabajo para la Fase 1

1. pisip es el backend canonico para pedidos, cuentas, clientes, mesas, pagos y permisos.
2. consumochoza funciona como cliente web que aun conserva reglas de presentacion y partes del flujo operativo.
3. MAUI consume pisip pero mantiene logica de permisos y visibilidad por rol en cliente.

Chequeos ya confirmados

1. consumochoza delega autenticacion a pisip y propaga JWT por sesion.
2. PedidoServiceImpl de consumochoza consume endpoints REST de pisip.
3. pisip posee los repositorios JPA del dominio principal.
4. MAUI guarda sesion local y decide navegacion segun rol.
5. MAUI contiene reglas visibles por rol en ViewModels.

Pendientes que pasan a la Fase 1

1. Verificar si crear pedido desde web, API y MAUI usan exactamente el mismo flujo real.
2. Verificar si abrir cuenta y asociar cliente esta centralizado en backend o distribuido.
3. Verificar si el cajero puede crear pedido via API aunque la UI lo oculte.
4. Verificar si faltan endpoints o DTOs para cubrir el flujo movil completo.
5. Verificar si PosApiControlador en consumochoza duplica endpoints ya existentes en pisip.

Archivos base usados en esta delimitacion

consumochoza:

1. consumochoza/pom.xml
2. consumochoza/src/main/java/com/choza/consumochoza/config/SecurityConfig.java
3. consumochoza/src/main/java/com/choza/consumochoza/config/ApiAuthenticationProvider.java
4. consumochoza/src/main/java/com/choza/consumochoza/config/WebClientConfig.java
5. consumochoza/src/main/java/com/choza/consumochoza/controlador/PedidosControlador.java
6. consumochoza/src/main/java/com/choza/consumochoza/controlador/PosApiControlador.java
7. consumochoza/src/main/java/com/choza/consumochoza/service/impl/PedidoServiceImpl.java

pisip:

1. pisip/pom.xml
2. pisip/src/main/java/com/lachozag4/pisip/infraestructura/configuracion/SeguridadConfig.java
3. pisip/src/main/java/com/lachozag4/pisip/infraestructura/seguridad/Roles.java
4. pisip/src/main/java/com/lachozag4/pisip/presentacion/controladores/PedidoControlador.java

ChozaMaui:

1. ChozaMaui/ChozaMaui.csproj
2. ChozaMaui/MauiProgram.cs
3. ChozaMaui/Services/SessionService.cs
4. ChozaMaui/Services/NavigationService.cs
5. ChozaMaui/Services/SettingsService.cs
6. ChozaMaui/ViewModels/LoginViewModel.cs
7. ChozaMaui/ViewModels/MapaViewModel.cs
8. ChozaMaui/ViewModels/PosViewModel.cs
9. ChozaMaui/Views/AppShell.xaml.cs

Salida de la Fase 0

La Fase 0 queda suficientemente avanzada para iniciar la Fase 1 con foco en el flujo crear pedido. No se recomienda saltar todavia a mesas, cuentas o pagos sin antes cerrar la trazabilidad completa del pedido entre web, API y MAUI.