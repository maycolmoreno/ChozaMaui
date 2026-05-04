# CHOZA POS — Aplicación Móvil para la Gestión de Pedidos en Restaurante

> **Estado del proyecto**: ✅ Completado | **Fecha**: Mayo 2026 | **Metodología**: Mobile-D

---

## Descripción del Proyecto

**Choza POS** es una aplicación móvil desarrollada con el framework **.NET MAUI** (Multi-platform App UI), orientada a modernizar y digitalizar la operación interna del restaurante *La Choza*. El sistema reemplaza el proceso manual basado en papel para la toma, seguimiento y gestión de pedidos, estableciendo una comunicación ágil entre el personal de salón, cocina, caja y administración.

La aplicación implementa el patrón arquitectónico **MVVM** (Model-View-ViewModel) y se integra con dos backends REST mediante peticiones HTTP con autenticación por tokens **JWT**: el proyecto **pisip** (API REST principal en Spring Boot 3.5.10 / Java 17) y el proyecto **consumochoza** (módulo de consumo y reportes). El sistema contempla tres roles de usuario —Mesero, Cajero y Administrador—, cada uno con su propio Shell de navegación y acceso a las funcionalidades correspondientes.

Entre sus módulos principales se encuentran: el punto de venta (POS) para la captura de pedidos con asignación de mesas y clientes; el monitor de pedidos con actualización automática por polling; la gestión completa de comedores y mesas con CRUD; el módulo de pagos y cuentas con soporte para métodos múltiples y pagos parciales; el historial de cuentas con filtros avanzados; el panel administrativo con KPIs, reportes de ventas y control de turnos de caja; la gestión de clientes y productos; y la generación de recibos PDF nativos en Android. La aplicación está dirigida a dispositivos Android (API 24 en adelante) y fue diseñada siguiendo principios de usabilidad orientados a entornos de ritmo acelerado como lo es la operación de un restaurante.

---

## Objetivos del Proyecto

1. **Digitalizar** el proceso de toma y seguimiento de pedidos del restaurante, eliminando el uso de comandas en papel y reduciendo el margen de error en las órdenes de un 8 % a menos del 2 %.

2. **Desarrollar** una aplicación móvil multiplataforma bajo el patrón MVVM que integre los módulos de punto de venta, gestión de mesas y comedores, control de pagos y cuentas, administración de clientes y productos, y panel administrativo en una sola interfaz unificada con navegación diferenciada por rol.

3. **Implementar** un sistema de comunicación en tiempo real entre el personal de salón y cocina mediante polling automático (30 segundos) y actualización de estados de pedido (PENDIENTE, EN_PROCESO, LISTO, ENTREGADO, CANCELADO), disminuyendo el tiempo promedio de atención al cliente en un 30 %.

4. **Gestionar** de forma segura las sesiones de usuario mediante autenticación con tokens JWT, inyección automática del token vía `AuthHandler` (DelegatingHandler) y almacenamiento en `SecureStorage`, garantizando el control de acceso diferenciado por roles (Mesero → AppShell, Cajero/Administrador → AppShellCajero).

5. **Generar** reportes y estadísticas operativas diarias —ventas totales, ticket promedio, productos más solicitados, disponibilidad de mesas y estado de turnos de caja— que sirvan como herramienta de toma de decisiones para la administración del restaurante.

6. **Automatizar** la generación de recibos de consumo en formato PDF de manera nativa en Android, integrando el detalle del pedido, totales y datos del mesero, para ser compartidos con el cliente o impresos.

---

## Requerimientos

### Requerimientos Funcionales

| ID    | Descripción | Estado |
|-------|-------------|--------|
| RF-01 | El sistema debe permitir el inicio de sesión mediante credenciales con autenticación JWT y redirección diferenciada por rol (Mesero / Cajero-Admin). | ✅ |
| RF-02 | El sistema debe permitir registrar nuevos pedidos asociando productos del catálogo, una mesa y un cliente opcional desde el módulo POS. | ✅ |
| RF-03 | El sistema debe mostrar el estado actualizado de todos los pedidos activos con filtrado por estado mediante polling automático cada 30 segundos. | ✅ |
| RF-04 | El sistema debe permitir cambiar el estado de un pedido (PENDIENTE → EN_PROCESO → LISTO → ENTREGADO → CANCELADO). | ✅ |
| RF-05 | El sistema debe gestionar la disponibilidad de mesas por comedor (Libre / Ocupada) con CRUD completo de comedores y mesas. | ✅ |
| RF-06 | El sistema debe permitir crear cuentas por mesa, registrar pagos parciales o totales y cerrar la cuenta al saldarse. | ✅ |
| RF-07 | El sistema debe soportar al menos tres métodos de pago: Efectivo, Tarjeta y Transferencia bancaria, con registro de referencia opcional. | ✅ |
| RF-08 | El sistema debe proveer un panel administrativo con KPIs diarios: total de ventas, ticket promedio, top 5 productos, estado de mesas y turno activo. | ✅ |
| RF-09 | El sistema debe permitir la apertura y cierre de turno de caja con registro de monto inicial y final. | ✅ |
| RF-10 | El sistema debe permitir al usuario editar su perfil, cambiar su contraseña y cerrar sesión de forma segura limpiando el `SecureStorage`. | ✅ |
| RF-11 | El sistema debe proveer un módulo de historial de cuentas con filtros por estado, rango de fechas y búsqueda de cliente, mostrando estadísticas rápidas. | ✅ |
| RF-12 | El sistema debe permitir la gestión completa (CRUD) de clientes con búsqueda en tiempo real por nombre y cédula, y registro rápido desde el módulo de pagos. | ✅ |
| RF-13 | El sistema debe permitir la gestión completa (CRUD) del catálogo de productos con imagen, precio, categoría y estado. | ✅ |
| RF-14 | El sistema debe generar y permitir compartir un recibo de consumo en formato PDF por pedido, con detalle de ítems, totales y datos del mesero. | ✅ |
| RF-15 | El sistema debe mostrar la ubicación del restaurante en un mapa interactivo con pin georreferenciado. | ✅ |

### Requerimientos No Funcionales

| ID   | Categoría | Descripción |
|------|-----------|-------------|
| RNF-01 | Rendimiento | La aplicación debe responder a interacciones del usuario en menos de 2 segundos bajo condiciones de red estándar. |
| RNF-02 | Disponibilidad | El sistema de actualización automática de pedidos debe ejecutar el polling cada 30 segundos sin intervención del usuario. |
| RNF-03 | Seguridad | Las credenciales de sesión deben almacenarse en el `SecureStorage` del dispositivo; el token JWT debe incluirse en cada petición autenticada. |
| RNF-04 | Usabilidad | La interfaz debe ser operable con una sola mano en entornos de ritmo acelerado, con retroalimentación visual inmediata (colores por estado, toasts de confirmación). |
| RNF-05 | Compatibilidad | La aplicación debe funcionar en dispositivos Android con API Level 24 (Android 7.0) o superior. |
| RNF-06 | Mantenibilidad | El código debe seguir el patrón MVVM con inyección de dependencias, facilitando la adición de nuevas funcionalidades sin modificar capas existentes. |

### Requerimientos de Hardware y Software

#### Hardware
- Dispositivo móvil Android con versión 7.0 (API 24) o superior.
- Conexión a red local o internet para comunicarse con el servidor API REST.
- GPS (requerido para la funcionalidad de mapa interactivo).

#### Software de Desarrollo
- **IDE**: Visual Studio Community 2026 (v18.5.1) con carga de trabajo .NET MAUI instalada.
- **SDK**: .NET 10 SDK con workload Android.
- **Emulador / Dispositivo**: Android Emulator (API 24+) o dispositivo físico Android.
- **Control de versiones**: Git.

#### Software del Servidor (Backend)
- **Backend principal (pisip)**: Spring Boot 3.5.10 / Java 17 / PostgreSQL. API REST accesible en `http://10.0.2.2:8081` desde el emulador Android o `http://localhost:8081` desde Windows.
- **Backend de reportes (consumochoza)**: Spring Boot 3.5.10 / Java 17. Provee endpoints de reportes, reporte de ventas del día y operaciones de consumo.
- Soporte de autenticación JWT en todos los endpoints protegidos.
- Base de datos PostgreSQL con tablas: Usuarios, Clientes, Productos, Categorías, Comedores, Mesas, Pedidos, PedidosDetalles, Cuentas, Pagos, CajaTurnos.

#### Dependencias NuGet del Proyecto
- `Microsoft.Maui.Controls` (net10.0-android)
- `Microsoft.Maui.Controls.Maps` (Google Maps)
- `CommunityToolkit.Mvvm` v8.4.0
- `Microsoft.Extensions.Logging.Debug` v9.0.4
- `Microsoft.Extensions.Http` (IHttpClientFactory)
