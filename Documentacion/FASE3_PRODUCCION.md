# FASE 3: PRODUCCIÓN - Metodología Mobile-D
## Proyecto: Choza POS - Sistema de Punto de Venta Móvil

> **Estado**: ✅ COMPLETADA | **Fecha**: Mayo 2026 | **Versión**: 1.0

---

## 1. OBJETIVO DE LA FASE

La Fase de Producción es el núcleo del desarrollo en Mobile-D. Comprende la implementación iterativa e incremental de todas las funcionalidades planificadas, organizadas en sprints cortos con entregables funcionales al final de cada ciclo.

**Meta de la fase**: Tener toda la funcionalidad implementada, compilando correctamente y con integración completa con los backends REST (pisip + consumochoza).

---

## 2. SPRINTS DE PRODUCCIÓN

### Sprint 1: Autenticación y Navegación Dual ✅ COMPLETADO
**Duración**: 2 semanas | **Objetivo**: Login funcional con redirección por rol

#### Cambios Implementados

**Módulo de Autenticación:**
- `LoginPage.xaml` / `LoginViewModel`: formulario de credenciales con validación, loading state e indicador de error
- `LoginViewModel.LoginCommand`: llamada a `ApiService.LoginAsync()`, almacenamiento de sesión en `SessionService` y redirección diferenciada
  - Rol `MESERO` → `AppShell`
  - Rol `CAJERO` / `ADMINISTRADOR` → `AppShellCajero`

**Servicios de Infraestructura:**
- `SessionService`: almacena `Token`, `Username`, `Rol`, `NombreCompleto` en `SecureStorage`
- `AuthHandler` (DelegatingHandler): inyecta `Authorization: Bearer <token>` en cada petición HTTP automáticamente
- `IHttpClientFactory` configurado en `MauiProgram.cs` con timeout de 15 segundos

**Shells de Navegación:**
- `AppShell.xaml`: TabBar para Mesero (POS, Pedidos, Mapa, Perfil)
- `AppShellCajero.xaml`: TabBar para Cajero/Admin (Turno, Cuentas, Comedores/Mesas, Clientes, Productos, Dashboard, Perfil)

#### Verificación
- ✅ Login exitoso con credenciales válidas
- ✅ Mensaje de error con credenciales inválidas
- ✅ Redirección correcta por rol
- ✅ Sesión persistente entre reinicios de app
- ✅ JWT incluido automáticamente en todas las peticiones

---

### Sprint 2: Módulo POS (Punto de Venta) ✅ COMPLETADO
**Duración**: 2 semanas | **Objetivo**: Toma de pedidos completa

#### Cambios Implementados

**PosPage / PosViewModel:**
- Carga paralela de productos y categorías al entrar a la página
- Filtro de productos por categoría (selector horizontal)
- Carrito de pedido: agregar, incrementar, decrementar y eliminar ítems
- Cálculo de total en tiempo real con `ObservableCollection`
- Selector de mesa (mesas disponibles desde API)
- Selector de cliente opcional (búsqueda desde `ClientesPage`)
- Observaciones del pedido
- `CrearPedidoCommand`: construye `PedidoRequest` y hace POST a `/api/pedidos`
- Confirmación visual y limpieza del carrito tras creación exitosa

**PedidosPage / PedidosViewModel:**
- Listado de todos los pedidos activos
- Filtros por estado: TODOS, PENDIENTE, EN_PROCESO, LISTO, ENTREGADO, CANCELADO
- Tarjetas con código de color por estado
- Navegación a `PedidoDetallePage` al seleccionar un pedido
- Polling automático cada 30 segundos para actualizaciones en tiempo real

**PedidoDetallePage / PedidoDetalleViewModel:**
- Visualización completa del pedido: productos, cantidades, precios unitarios, subtotales y total
- Información de mesa, comedor, mesero y fecha
- Cambio de estado del pedido
- Botón "Generar PDF" → `ReceiptPdfService.GenerarReciboPedidoAsync()`
- Navegación a `PagoPage` para iniciar cobro

#### Verificación
- ✅ Catálogo carga con categorías filtrables
- ✅ Carrito refleja totales en tiempo real
- ✅ Pedido creado con éxito y visible en listado
- ✅ Estados de pedido con colores diferenciados
- ✅ PDF generado y compartible desde Android

---

### Sprint 3: Módulo de Pagos y Cuentas ✅ COMPLETADO
**Duración**: 2 semanas | **Objetivo**: Cobro completo con cuentas

#### Cambios Implementados

**PagoPage / PagoViewModel:**
- Recibe `PedidoResponse` por `QueryProperty` (navegación Shell)
- Muestra resumen del pedido: productos, total, mesa
- Formulario de pago: monto, método (`EFECTIVO` / `TARJETA` / `TRANSFERENCIA`), referencia opcional
- Cálculo de saldo pendiente tras cada pago parcial
- `RegistrarPagoCommand`: POST a `/api/cuentas/{id}/pagos`
- Cierre automático de cuenta cuando `Saldo ≤ 0`
- Propiedades calculadas: `TotalPedido`, `TotalPagado`, `Saldo`, `PagadoCompleto`

**HistorialCuentasPage / HistorialCuentasViewModel:**
- Lista completa de cuentas del restaurante
- Filtros: estado (ABIERTA/CERRADA/ANULADA), rango de fechas y búsqueda de texto
- Estadísticas rápidas: total de cuentas, abiertas, cerradas y total facturado
- Detalle expandible por cuenta
- Buscador de cliente con registro rápido de nuevo cliente desde la misma página
- Validación de cédula con expresión regular

**ReceiptPdfService:**
- Generación de PDF con `Android.Graphics.Pdf` (nativo, sin dependencias externas)
- Diseño tipográfico profesional: encabezado "La Choza", detalle de ítems, línea divisoria, total en naranja
- Página de 420×720 px optimizada para impresoras térmicas
- Archivo guardado en `FileSystem.CacheDirectory` con nombre único por timestamp

#### Verificación
- ✅ Pago parcial reduce saldo correctamente
- ✅ Cuenta se cierra al saldar completamente
- ✅ Historial filtra por estado y fechas
- ✅ PDF generado tiene formato correcto y es compartible

---

### Sprint 4: Panel Administrativo y Gestión ✅ COMPLETADO
**Duración**: 1 semana | **Objetivo**: Módulos de administración completos

#### Cambios Implementados

**AdminPage / AdminViewModel:**
- KPIs del día cargados con `Task.WhenAll` (3 llamadas paralelas: reporte, turno, mesas)
- Métricas mostradas: Total Ventas, Número de Pedidos, Ticket Promedio, Total Productos
- Lista Top 5 productos más vendidos con cantidades
- Estado de mesas: disponibles, ocupadas, total
- Pedidos del día con estado de turno activo
- Selector de fecha para consultar reportes históricos

**TurnoPage / TurnoViewModel:**
- Verificación del turno activo al cargar la página
- Formulario de apertura: monto inicial (validación decimal robusta con `CultureInfo.InvariantCulture`)
- Formulario de cierre: monto final registrado
- Estado visual: turno abierto (verde) vs. sin turno (rojo)
- Validación de monto negativo con mensaje de error

**ComedoresMesasPage / ComedoresMesasViewModel:**
- Pestañas: Comedores / Mesas
- CRUD completo de comedores: nombre, descripción, estado
- CRUD completo de mesas: número, capacidad, comedor asignado, estado
- Filtro de mesas por comedor seleccionado
- Formularios inline dentro de la misma página (sin modales externos)

**ClientesPage / ClientesViewModel:**
- Lista con búsqueda en tiempo real (nombre o cédula)
- CRUD completo: nombre, cédula, teléfono, dirección, email, estado
- Formulario inline con validación

**ProductosPage / ProductosViewModel:**
- Lista de productos con imagen, precio y categoría
- CRUD completo: nombre, precio, stock, descripción, URL de imagen, categoría, estado
- Filtro por categoría

#### Verificación
- ✅ KPIs se cargan en paralelo sin bloquear la UI
- ✅ Turno se abre y cierra con montos registrados
- ✅ CRUD de comedores, mesas, clientes y productos funcional
- ✅ Filtros y búsquedas responden en tiempo real

---

### Sprint 5: Perfil, Mapa y Complementarios ✅ COMPLETADO
**Duración**: 1 semana | **Objetivo**: Módulos complementarios y cierre de funcionalidad

#### Cambios Implementados

**PerfilPage / PerfilViewModel:**
- Visualización de datos del usuario logueado (nombre, username, rol)
- Formulario de edición de perfil con PUT a `/api/usuarios/{id}`
- Módulo de cambio de contraseña: contraseña actual + nueva + confirmación
- Validación de coincidencia de contraseñas
- `CerrarSesionCommand`: limpia `SessionService` y navega de vuelta a `LoginPage`

**MapaPage / MapaViewModel:**
- Integración con `.NET MAUI Maps` (Google Maps en Android)
- Pin fijo en la ubicación del restaurante "La Choza"
- Cámara centrada automáticamente en el pin al cargar

**MesaDetallePage / MesaDetalleViewModel:**
- Vista de todos los pedidos activos de una mesa específica
- Acceso rápido a `PagoPage` desde el pedido activo
- Navegación desde `ComedoresMesasPage`

**Converters.cs:**
- `EstadoPedidoColorConverter`: mapea estados de pedido a colores RGBA
- `EstadoCuentaColorConverter`: mapea estados de cuenta a colores
- `MonedaConverter`: formatea decimales como moneda (S/ 0.00)
- `FechaHoraConverter`: formatea `DateTime` a cadenas legibles
- `BoolToVisibilityConverter` y variantes

#### Verificación
- ✅ Edición de perfil se persiste correctamente
- ✅ Cambio de contraseña funcional con validación
- ✅ Mapa muestra pin en ubicación correcta
- ✅ MesaDetalle muestra pedidos activos de la mesa seleccionada

---

## 3. INTEGRACIÓN CON BACKENDS

### 3.1 Backend pisip (API REST principal)
- **Framework**: Spring Boot 3.5.10 | **Java**: 17 | **BD**: PostgreSQL
- **URL**: `http://10.0.2.2:8081` (emulador Android) / `http://localhost:8081` (Windows)
- **Autenticación**: JWT Bearer Token

**Endpoints integrados:**

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/usuarios/login` | Autenticación |
| POST | `/api/usuarios/cambiar-password` | Cambio de contraseña |
| GET | `/api/mesas` | Todas las mesas |
| GET | `/api/mesas/disponibles` | Mesas disponibles |
| GET | `/api/categorias` | Categorías de productos |
| GET | `/api/productos` | Todos los productos |
| POST/PUT/DELETE | `/api/productos/{id}` | CRUD productos |
| GET | `/api/comedores` | Lista de comedores |
| POST/PUT/DELETE | `/api/comedores/{id}` | CRUD comedores |
| POST/PUT/DELETE | `/api/mesas/{id}` | CRUD mesas |
| GET | `/api/clientes` | Lista de clientes |
| POST/PUT | `/api/clientes/{id}` | CRUD clientes |
| POST | `/api/pedidos` | Crear pedido |
| GET | `/api/pedidos` | Listar pedidos |
| GET | `/api/pedidos/{id}` | Detalle de pedido |
| PUT | `/api/pedidos/{id}/estado` | Cambiar estado de pedido |
| GET/POST | `/api/cuentas` | Cuentas |
| POST | `/api/cuentas/{id}/pagos` | Registrar pago |
| POST/PUT | `/api/caja/abrir` | Abrir turno de caja |
| GET | `/api/caja/activa` | Turno activo |
| PUT | `/api/caja/cerrar` | Cerrar turno |
| GET | `/api/reportes/ventas-dia` | Reporte de ventas del día |

### 3.2 Patrón de Comunicación
```
ViewModel
   │ llama a método async (ej: _api.CrearPedidoAsync())
   │
   ▼
ApiService (HttpClient con AuthHandler)
   │ inyecta JWT automáticamente
   │ POST/GET/PUT/DELETE + JSON body
   │
   ▼
Backend REST (pisip / consumochoza)
   │ valida JWT, procesa, responde JSON
   │
   ▼
ApiService deserializa respuesta → retorna DTO al ViewModel
   │
   ▼
ViewModel actualiza ObservableProperties → UI se actualiza por binding
```

---

## 4. DECISIONES TÉCNICAS TOMADAS EN PRODUCCIÓN

| Decisión | Alternativa Descartada | Justificación |
|----------|----------------------|---------------|
| `DelegatingHandler` para JWT | Aplicar token manualmente en cada llamada | Elimina código repetitivo y garantiza consistencia |
| `Task.WhenAll` en AdminViewModel | Llamadas secuenciales | Reduce tiempo de carga de KPIs de ~3s a ~1s |
| `AppShell` + `AppShellCajero` | Shell único con páginas ocultas | Separación limpia de roles sin lógica condicional en UI |
| PDF nativo con `Android.Graphics.Pdf` | Librería NuGet externa | Cero dependencias adicionales, control total del diseño |
| `IHttpClientFactory` tipado | `new HttpClient()` | Gestión correcta del ciclo de vida del socket, evita agotamiento |
| Polling cada 30 s en PedidosViewModel | WebSocket / SignalR | Simplifica el backend; suficiente para el caso de uso |

---

## 5. ESTADO DE COBERTURA DE REQUISITOS

| Req. | Descripción | Estado |
|------|-------------|--------|
| RF-01 | Autenticación con roles y JWT | ✅ Completado |
| RF-02 | Punto de Venta (POS) | ✅ Completado |
| RF-03 | Catálogo de Productos (CRUD) | ✅ Completado |
| RF-04 | Gestión de Comedores y Mesas (CRUD) | ✅ Completado |
| RF-05 | Historial de Pedidos con filtros | ✅ Completado |
| RF-06 | Módulo de Pagos y Cuentas | ✅ Completado |
| RF-07 | Historial de Cuentas | ✅ Completado |
| RF-08 | Panel Administrativo con KPIs | ✅ Completado |
| RF-09 | Control de Turnos de Caja | ✅ Completado |
| RF-10 | Gestión de Clientes (CRUD) | ✅ Completado |
| RF-11 | Generación de Recibos PDF | ✅ Completado |
| RF-12 | Perfil de Usuario | ✅ Completado |
| RF-13 | Geolocalización en Mapa | ✅ Completado |

**Cobertura total: 13/13 requisitos funcionales implementados (100%)**

---

**Documento elaborado por**: Equipo de Desarrollo Choza POS  
**Fecha**: Mayo 2026  
**Versión**: 1.0  
**Estado**: ✅ Fase Completada — Todos los módulos en producción
