# CHOZA POS — Aplicación Móvil para la Gestión de Pedidos en Restaurante

---

## Descripción del Proyecto

**Choza POS** es una aplicación móvil desarrollada con el framework **.NET MAUI** (Multi-platform App UI), orientada a modernizar y digitalizar la operación interna del restaurante *La Choza*. El sistema reemplaza el proceso manual basado en papel para la toma, seguimiento y gestión de pedidos, estableciendo una comunicación en tiempo real entre el personal de salón, cocina y administración.

La aplicación implementa el patrón arquitectónico **MVVM** (Model-View-ViewModel) y se integra con un servidor REST mediante peticiones HTTP con autenticación por tokens **JWT**. El sistema contempla tres roles de usuario —Mesero, Chef y Administrador—, cada uno con acceso a las funcionalidades correspondientes a su responsabilidad dentro del restaurante.

Entre sus módulos principales se encuentran: el punto de venta (POS) para la captura de pedidos con asignación de mesas y clientes; el monitor de pedidos con actualización automática; la gestión de comedores y mesas; el módulo de pagos y cuentas con soporte para métodos múltiples; y un panel administrativo con reportes, control de turnos y estadísticas del día. La aplicación está dirigida a dispositivos Android (API 24 en adelante) y fue diseñada siguiendo principios de usabilidad orientados a entornos de ritmo acelerado como lo es la operación de un restaurante.

---

## Objetivos del Proyecto

1. **Digitalizar** el proceso de toma y seguimiento de pedidos del restaurante, eliminando el uso de comandas en papel y reduciendo el margen de error en las órdenes de un 8 % a menos del 2 %.

2. **Desarrollar** una aplicación móvil multiplataforma bajo el patrón MVVM que integre los módulos de punto de venta, gestión de mesas, control de pagos y panel administrativo en una sola interfaz unificada.

3. **Implementar** un sistema de comunicación en tiempo real entre el personal de salón y cocina mediante polling automático y actualización de estados de pedido (PENDIENTE, EN_PROCESO, LISTO, ENTREGADO, CANCELADO), disminuyendo el tiempo promedio de atención al cliente en un 30 %.

4. **Gestionar** de forma segura las sesiones de usuario mediante autenticación con tokens JWT y almacenamiento en `SecureStorage`, garantizando el control de acceso diferenciado por roles (Mesero, Chef, Administrador).

5. **Generar** reportes y estadísticas operativas diarias —ventas totales, productos más solicitados, disponibilidad de mesas y estado de turnos de caja— que sirvan como herramienta de toma de decisiones para la administración del restaurante.

---

## Requerimientos

### Requerimientos Funcionales

| ID   | Descripción |
|------|-------------|
| RF-01 | El sistema debe permitir el inicio de sesión de usuarios mediante credenciales (usuario y contraseña) con autenticación JWT. |
| RF-02 | El sistema debe permitir registrar nuevos pedidos asociando productos del catálogo, una mesa y un cliente opcional. |
| RF-03 | El sistema debe mostrar el estado actualizado de todos los pedidos activos con filtrado por estado, mesa o identificador. |
| RF-04 | El sistema debe permitir cambiar el estado de un pedido (PENDIENTE → EN_PROCESO → LISTO → ENTREGADO). |
| RF-05 | El sistema debe gestionar la disponibilidad de mesas por comedor (Libre / Ocupada) y permitir su cambio manual. |
| RF-06 | El sistema debe permitir crear cuentas por mesa, registrar pagos parciales o totales y cerrar la cuenta al saldarse. |
| RF-07 | El sistema debe soportar al menos tres métodos de pago: Efectivo, Tarjeta y Transferencia bancaria. |
| RF-08 | El sistema debe proveer un panel administrativo con KPIs diarios: total de ventas, top 5 productos y estadísticas de pedidos. |
| RF-09 | El sistema debe permitir la apertura y cierre de turno de caja por parte del administrador. |
| RF-10 | El sistema debe permitir al usuario editar su perfil y cerrar sesión de forma segura. |

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
- Cámara trasera disponible (requerida para captura de fotos en pedidos).
- Conexión a red local o internet para comunicarse con el servidor API REST.

#### Software de Desarrollo
- **IDE**: Visual Studio 2022 con carga de trabajo .NET MAUI instalada.
- **SDK**: .NET 10 SDK.
- **Emulador / Dispositivo**: Android Emulator (API 24+) o dispositivo físico Android.
- **Control de versiones**: Git (estrategia Git Flow recomendada).

#### Software del Servidor (Backend)
- Servidor REST disponible en red accesible desde el dispositivo (configurado en `http://10.0.2.2:8081` para el emulador Android).
- Soporte de autenticación JWT en todos los endpoints protegidos.
- Base de datos relacional compatible con el modelo de entidades definido (Productos, Pedidos, Mesas, Cuentas, Pagos, Usuarios).

#### Dependencias NuGet del Proyecto
- `Microsoft.Maui.Controls`
- `Microsoft.Maui.Controls.Maps`
- `CommunityToolkit.Mvvm` v8.4.0
- `Microsoft.Extensions.Logging.Debug` v9.0.4
