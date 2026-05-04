# FASE 2: INICIACIÓN - Metodología Mobile-D
## Proyecto: Choza POS - Sistema de Punto de Venta Móvil

> **Estado**: ✅ COMPLETADA | **Fecha**: Mayo 2026 | **Versión**: 2.0

---

## 1. PREPARACIÓN DEL PROYECTO

### 1.1 Configuración del Entorno de Desarrollo

#### Herramientas y Software Instalado
- **IDE**: Visual Studio Community 2026 (v18.5.1)
- **Framework**: .NET 10
- **SDK**: .NET MAUI para desarrollo móvil multiplataforma
- **Control de Versiones**: Git + GitHub
- **Gestión de Dependencias**: NuGet Package Manager

#### Estructura del Workspace
```
C:\Users\mmorenos\Desktop\version final 3\Proyecto\
├── ChozaMaui\                    # Aplicación móvil .NET MAUI
│   ├── ChozaMaui.csproj
│   ├── MauiProgram.cs
│   ├── Models\
│   ├── Views\                    # 14 páginas XAML
│   ├── ViewModels\               # 14 ViewModels
│   ├── Services\                 # 5 servicios
│   ├── Converters\
│   ├── Resources\
│   └── Platforms\
├── pisip\                        # Backend REST principal (Spring Boot 3.5.10, Java 17)
├── consumochoza\                 # Backend de consumo/reportes (Spring Boot 3.5.10, Java 17)
└── Documentacion\
```

#### Dependencias del Proyecto
```xml
- Microsoft.Maui.Controls (net10.0-android)
- Microsoft.Maui.Controls.Maps (integración de mapas Google)
- Microsoft.Extensions.Logging.Debug (v9.0.4)
- CommunityToolkit.Mvvm (v8.4.0)  -- Fuente MVVM: ObservableObject, RelayCommand, ObservableProperty
- Microsoft.Extensions.Http           -- IHttpClientFactory + DelegatingHandler
```

#### Configuración de Plataforma Objetivo
- **Target Framework**: net10.0-android
- **SDK Mínimo**: Android API 24 (Android 7.0)
- **Application ID**: com.lachozag4.chozamaui
- **Versión**: 1.0

---

## 2. PLANIFICACIÓN INICIAL

### 2.1 Metodología de Desarrollo

#### Enfoque Ágil con Mobile-D
- **Iteraciones cortas**: Sprints de 1-2 semanas
- **Entregas incrementales**: Funcionalidades operativas al final de cada iteración
- **Feedback continuo**: Validación con stakeholders
- **Adaptación flexible**: Ajustes basados en retroalimentación

#### Roles del Equipo
- **Product Owner**: Propietario del restaurante
- **Scrum Master/Lead Developer**: Coordinador técnico
- **Developers**: Equipo de desarrollo (2-3 personas)
- **Testers**: Meseros del restaurante (usuarios finales)

### 2.2 Cronograma de Iteraciones

#### Sprint 0 - Preparación (1 semana) ✅ COMPLETADO
- Configuración del entorno de desarrollo
- Creación del proyecto base .NET MAUI
- Configuración de repositorio Git
- Definición de arquitectura inicial
- Conexión a backends Spring Boot (pisip, consumochoza)

#### Sprint 1 - Autenticación y Navegación (2 semanas) ✅ COMPLETADO
**Objetivo**: Sistema de autenticación y dos Shells de navegación por rol

**Entregables**:
- Sistema de login con validación y JWT
- Sesión persistente con SecureStorage
- AppShell (rol Mesero) con tabs: POS, Pedidos, Mapa, Perfil
- AppShellCajero (rol Cajero/Administrador) con tabs: Turno, HistorialCuentas, Comedores/Mesas, Clientes, Productos, Admin, Perfil
- AuthHandler para inyección automática de JWT en todas las peticiones

**User Stories completadas**:
- US-001: Inicio de sesión con redirección por rol
- US-002: Sesión persistente entre cierres de app
- US-003: Navegación diferenciada por Shell según rol

#### Sprint 2 - Módulo POS y Pedidos (2 semanas) ✅ COMPLETADO
**Objetivo**: Toma de pedidos y visualización completa

**Entregables**:
- POS funcional: catálogo por categorías, carrito, asignación de mesa y cliente
- Listado de pedidos con filtros por estado
- Detalle de pedido con productos y totales
- Detalle de mesa con pedidos activos

**User Stories completadas**:
- US-004: Catálogo de productos con filtro por categoría
- US-005: Agregar/quitar productos del carrito con cantidades
- US-006: Asignar mesa y cliente (opcional) al pedido
- US-007: Enviar pedido a cocina y confirmar creación

#### Sprint 3 - Pagos y Cuentas (2 semanas) ✅ COMPLETADO
**Objetivo**: Módulo completo de cobro y cuentas

**Entregables**:
- PagoPage: registro de pago con método (Efectivo/Tarjeta/Transferencia)
- Cálculo automático de saldo pendiente y cierre de cuenta
- HistorialCuentasPage con filtros y estadísticas rápidas
- Generación de recibo PDF por pedido (ReceiptPdfService, Android nativo)

**User Stories completadas**:
- US-008: Registrar pago parcial o total desde la cuenta del pedido
- US-009: Consultar historial de cuentas con filtro por estado y fecha
- US-010: Generar y compartir recibo PDF del consumo

#### Sprint 4 - Panel Admin y Gestión (1 semana) ✅ COMPLETADO
**Objetivo**: Funcionalidades de administración y control

**Entregables**:
- AdminPage: KPIs del día con carga paralela (Task.WhenAll)
- TurnoPage: apertura y cierre de turno de caja
- ComedoresMesasPage: CRUD de comedores y mesas
- ClientesPage: CRUD de clientes con búsqueda
- ProductosPage: CRUD de productos con imagen y categoría

**User Stories completadas**:
- US-011: Panel admin con ventas, pedidos del día y top productos
- US-012: Abrir y cerrar turno de caja con montos
- US-013: Gestionar comedores y mesas del restaurante
- US-014: Gestionar catálogo completo de productos

#### Sprint 5 - Perfil, Mapa y Estabilización (1 semana) ✅ COMPLETADO
**Objetivo**: Completar funcionalidades complementarias y corregir bugs

**Entregables**:
- PerfilPage con edición y cambio de contraseña
- MapaPage con mapa interactivo y pin de ubicación
- Converters de estado a color, moneda y fechas
- Optimizaciones de UI/UX y correción de bugs reportados

#### Sprint 3 - Historial y Detalle (1 semana) ✅ COMPLETADO
**Objetivo**: Consulta de pedidos existentes

**Entregables**:
- Listado de pedidos con filtros por estado (PENDIENTE, EN_PROCESO, LISTO, ENTREGADO, CANCELADO)
- Detalle completo de pedido con productos, cantidades y totales
- Colores por estado para retroalimentación visual instantánea
- Polling automático cada 30 segundos para actualización en tiempo real

**User Stories completadas**:
- US-008: Historial de pedidos con filtros
- US-009: Detalle completo de pedido específico
- US-010: Filtrado de pedidos por estado con colores

#### Sprint 4 - Funcionalidades Complementarias (1 semana) ✅ COMPLETADO
**Objetivo**: Perfil, mapa y logout

**Entregables**:
- Perfil de usuario con edición y cambio de contraseña
- Integración de mapa con ubicación del restaurante
- Cierre de sesión seguro (limpia SecureStorage)

**User Stories completadas**:
- US-011: Ver y editar perfil de usuario
- US-012: Ver ubicación del restaurante en mapa interactivo
- US-013: Cierre de sesión seguro

#### Sprint 5 - Pruebas y Pulido (1 semana) ✅ COMPLETADO
**Objetivo**: Estabilización, mejoras finales y documentación

**Entregables**:
- Correción de bugs reportados en pruebas con usuarios reales
- Optimizaciones de rendimiento (carga paralela con Task.WhenAll)
- Mejoras de UI/UX (colores de estado, feedback visual, toasts)
- Documentación de usuario y técnica completa

---

## 3. ARQUITECTURA DEL SISTEMA

### 3.1 Patrón Arquitectónico: MVVM (Model-View-ViewModel)

#### Ventajas para el Proyecto
- ✅ Separación clara de responsabilidades
- ✅ Testabilidad mejorada
- ✅ Reutilización de código
- ✅ Soporte nativo de .NET MAUI
- ✅ Data binding bidireccional

#### Capas del Sistema

```
┌─────────────────────────────────────────────────────────┐
│                        VIEW LAYER                        │
│  (XAML Pages - UI/UX - Interacción con usuario)        │
├─────────────────────────────────────────────────────────┤
│  - LoginPage.xaml                                       │
│  - PosPage.xaml (Punto de Venta)                       │
│  - PedidosPage.xaml (Listado)                          │
│  - PedidoDetallePage.xaml                              │
│  - MapaPage.xaml                                        │
│  - PerfilPage.xaml                                      │
│  - AppShell.xaml (Navegación)                          │
└─────────────────────────────────────────────────────────┘
                          ↕ Data Binding
┌─────────────────────────────────────────────────────────┐
│                    VIEWMODEL LAYER                       │
│  (Lógica de Presentación - Commands - Properties)      │
├─────────────────────────────────────────────────────────┤
│  - LoginViewModel                                       │
│  - PosViewModel                                         │
│  - PedidosViewModel                                     │
│  - PedidoDetalleViewModel                              │
│  - MapaViewModel                                        │
│  - PerfilViewModel                                      │
└─────────────────────────────────────────────────────────┘
                          ↕ Consume
┌─────────────────────────────────────────────────────────┐
│                     SERVICE LAYER                        │
│  (Lógica de Negocio - Comunicación API)                │
├─────────────────────────────────────────────────────────┤
│  - ApiService (HTTP Client, REST API calls)            │
│  - SessionService (Gestión de sesión y tokens)         │
└─────────────────────────────────────────────────────────┘
                          ↕ Define
┌─────────────────────────────────────────────────────────┐
│                      MODEL LAYER                         │
│  (Entidades de Datos - DTOs)                           │
├─────────────────────────────────────────────────────────┤
│  - LoginRequest / LoginResponse                         │
│  - ProductoResponse                                     │
│  - CategoriaResponse                                    │
│  - PedidoRequest / PedidoResponse                      │
│  - PedidoDetalleRequest / PedidoDetalleResponse        │
│  - MesaResponse                                         │
│  - UsuarioResponse / ClienteResponse                   │
└─────────────────────────────────────────────────────────┘
```

### 3.2 Componentes Principales

#### 3.2.1 Models (Modelos de Datos)
**Responsabilidad**: Representar las entidades del dominio

**Clases Implementadas**:
- `LoginRequest`, `LoginResponse`: Autenticación
- `ProductoResponse`, `CategoriaResponse`: Catálogo
- `PedidoRequest`, `PedidoResponse`: Gestión de pedidos
- `PedidoDetalleRequest`, `PedidoDetalleResponse`: Items del pedido
- `MesaResponse`: Mesas del restaurante
- `UsuarioResponse`, `ClienteResponse`: Información de usuarios y clientes

**Características**:
- Propiedades con valores por defecto
- Propiedades calculadas (ej: `Subtotal`)
- Nullable reference types habilitados

#### 3.2.2 Services (Servicios)
**Responsabilidad**: Lógica de negocio y comunicación externa

**ApiService**:
- Comunicación con API REST backend (pisip + consumochoza)
- Manejo de peticiones HTTP (GET, POST, PUT, DELETE) vía `IHttpClientFactory`
- Serialización/deserialización JSON con `System.Net.Http.Json`
- Timeout global de 15 segundos
- URL base configurable: `http://10.0.2.2:8081` (emulador) ↔ `http://localhost:8081` (Windows/iOS)

**AuthHandler** (DelegatingHandler):
- Inyecta automáticamente el token JWT en el header `Authorization: Bearer <token>` de cada petición
- Elimina la necesidad de aplicar el token manualmente en ApiService

**SessionService**:
- Almacena token JWT y datos del usuario en `SecureStorage`
- Expone `Username`, `Rol`, `IsAuthenticated` para toda la app
- Métodos: `SaveSession()`, `ClearSession()`, `GetToken()`

**INavigationService / NavigationService**:
- Abstrae la navegación entre páginas vía Shell routing
- Registrado como Singleton para acceso desde cualquier ViewModel

**ReceiptPdfService**:
- Genera recibos PDF nativos en Android usando `Android.Graphics.Pdf`
- Incluye: nombre del restaurante, mesero, lista de productos, totales
- Guarda en caché del dispositivo y retorna ruta para compartir

#### 3.2.3 ViewModels (Modelos de Vista)
**Responsabilidad**: Lógica de presentación y binding

**14 ViewModels implementados** (todos extienden `ObservableObject` de CommunityToolkit.Mvvm):

1. **LoginViewModel**: Validación de credenciales, comando Login, redirección por rol
2. **PosViewModel**: Catálogo con filtro por categoría, carrito, mesa, cliente y creación de pedido
3. **PedidosViewModel**: Listado con filtros de estado, polling automático cada 30 s
4. **PedidoDetalleViewModel**: Detalle de pedido, cambio de estado, generación de PDF
5. **MesaDetalleViewModel**: Pedidos activos de una mesa, navegación a pago
6. **PagoViewModel**: Registro de pago, cálculo de saldo, cierre de cuenta
7. **HistorialCuentasViewModel**: Cuentas con filtros (estado/fecha/cliente), estadísticas rápidas
8. **TurnoViewModel**: Apertura y cierre de turno con montos
9. **AdminViewModel**: KPIs del día (Task.WhenAll), top productos, estado de mesas
10. **ClientesViewModel**: CRUD de clientes con búsqueda en tiempo real
11. **ProductosViewModel**: CRUD de productos con imagen y categoría
12. **ComedoresMesasViewModel**: CRUD de comedores y mesas con filtro por comedor
13. **MapaViewModel**: Carga de mapa y pin de ubicación del restaurante
14. **PerfilViewModel**: Edición de perfil y cambio de contraseña

**Características Comunes**:
- `ObservableObject` base, source-generation con `[ObservableProperty]` y `[RelayCommand]`
- `ObservableCollection<T>` para listas enlazadas a la UI
- `IsBusy` y `Mensaje` en todos los ViewModels para feedback visual

#### 3.2.4 Views (Vistas)
**Responsabilidad**: Interfaz de usuario

**14 páginas XAML + 2 Shells implementados**:

**Estructura de Navegación Dual**:
```
LoginPage (página inicial, sin Shell)
│
├── AppShell (Rol: Mesero)
│   ├── PosPage          (Tab: Pedido)
│   ├── PedidosPage      (Tab: Historial)
│   │   └── → PedidoDetallePage (modal)
│   │       └── → PagoPage (modal)
│   ├── MapaPage         (Tab: Mapa)
│   └── PerfilPage       (Tab: Perfil)
│
└── AppShellCajero (Rol: Cajero / Administrador)
    ├── TurnoPage            (Tab: Turno)
    ├── HistorialCuentasPage (Tab: Cuentas)
    ├── ComedoresMesasPage   (Tab: Comedores/Mesas)
    │   └── → MesaDetallePage (modal)
    ├── ClientesPage         (Tab: Clientes)
    ├── ProductosPage        (Tab: Productos)
    ├── AdminPage            (Tab: Dashboard)
    └── PerfilPage           (Tab: Perfil)
```

**Características de UI**:
- Diseño responsivo para diferentes tamaños de pantalla Android
- Tema con colores corporativos (#1a1a2e fondo, naranjas para acciones)
- Colores de estado para pedidos y cuentas (verde/amarillo/rojo)
- Toasts de confirmación e indicadores de carga

#### 3.2.5 Converters (Convertidores)
**Responsabilidad**: Transformación de datos para UI

**Implementados**:
- Conversión de estados a colores
- Formateo de fechas
- Formateo de moneda
- Conversiones booleanas

---

## 4. DISEÑO DE DATOS

### 4.1 Modelo de Dominio

```
┌─────────────────┐         ┌─────────────────┐
│    Usuario      │         │    Cliente      │
├─────────────────┤         ├─────────────────┤
│ Idusuario       │         │ Idcliente       │
│ Username        │         │ NombreCompleto  │
│ NombreCompleto  │         │                 │
│ Rol             │         └─────────────────┘
│ Estado          │                  │
└─────────────────┘                  │ 1
        │ 1                           │
        │                             │
        │ crea                        │ asociado
        │                             │
        ↓ *                           ↓ *
┌─────────────────┐         ┌─────────────────┐
│    Pedido       │────────▶│      Mesa       │
├─────────────────┤    *:1  ├─────────────────┤
│ Idpedido        │         │ Idmesa          │
│ Fecha           │         │ Numero          │
│ Estado          │         │ Capacidad       │
│ Observaciones   │         │ Estado          │
│ Total           │         │ Idcomedor       │
│ IdUsuario       │         │ NombreComedor   │
│ IdMesa          │         └─────────────────┘
│ IdCliente       │
└─────────────────┘
        │ 1
        │ contiene
        ↓ *
┌─────────────────┐         ┌─────────────────┐
│ PedidoDetalle   │────────▶│   Producto      │
├─────────────────┤    *:1  ├─────────────────┤
│Idpedidodetalle  │         │ Idproducto      │
│ Cantidad        │         │ Nombre          │
│ PrecioUnitario  │         │ Precio          │
│ Subtotal        │         │ StockActual     │
│ IdProducto      │         │ Descripcion     │
└─────────────────┘         │ ImagenUrl       │
                            │ Estado          │
                            │ CategoriaId     │
                            └─────────────────┘
                                    │ *
                                    │ pertenece a
                                    ↓ 1
                            ┌─────────────────┐
                            │   Categoria     │
                            ├─────────────────┤
                            │ Idcategoria     │
                            │ Nombre          │
                            │ Descripcion     │
                            │ Estado          │
                            └─────────────────┘
```

### 4.2 Flujo de Datos

#### Flujo de Autenticación
```
1. Usuario ingresa credenciales → LoginViewModel
2. LoginViewModel valida → ApiService.Login()
3. ApiService envía POST /api/auth/login → Backend
4. Backend responde Token + Info Usuario
5. SessionService almacena token y datos
6. Navegación a AppShell (tabs)
```

#### Flujo de Creación de Pedido
```
1. Mesero navega a PosPage
2. PosViewModel carga productos → ApiService.GetProductos()
3. Mesero selecciona productos → Se agregan a carrito (ObservableCollection)
4. Mesero selecciona mesa → ApiService.GetMesas()
5. Mesero confirma pedido
6. PosViewModel construye PedidoRequest
7. ApiService.CrearPedido() → POST /api/pedidos
8. Backend procesa y responde con ID de pedido
9. Confirmación al usuario + Limpiar carrito
```

#### Flujo de Consulta de Pedidos
```
1. Usuario navega a PedidosPage
2. PedidosViewModel carga pedidos → ApiService.GetPedidos()
3. Backend responde con lista filtrada
4. UI renderiza lista con estados
5. Usuario selecciona pedido
6. Navegación a PedidoDetallePage
7. PedidoDetalleViewModel carga detalle → ApiService.GetPedidoDetalle(id)
8. UI muestra productos, totales, información completa
```

---

## 5. CONFIGURACIÓN TÉCNICA DETALLADA

### 5.1 Configuración del Proyecto (.csproj)

```xml
<PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>net10.0-android</TargetFrameworks>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <ApplicationTitle>Choza POS</ApplicationTitle>
    <ApplicationId>com.lachozag4.chozamaui</ApplicationId>
    <ApplicationVersion>1</ApplicationVersion>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>

    <SupportedOSPlatformVersion>24.0</SupportedOSPlatformVersion>
</PropertyGroup>
```

### 5.2 Inyección de Dependencias (MauiProgram.cs)

```csharp
// Servicios Singleton (compartidos en toda la app)
builder.Services.AddSingleton<SessionService>();
builder.Services.AddSingleton<INavigationService, NavigationService>();
builder.Services.AddSingleton<ReceiptPdfService>();
builder.Services.AddSingleton<AppShell>();
builder.Services.AddSingleton<AppShellCajero>();

// AuthHandler + HttpClient tipado con timeout 15 s
builder.Services.AddTransient<AuthHandler>();
builder.Services.AddHttpClient<ApiService>(c => {
    c.BaseAddress = new Uri(ApiService.BaseUrl);
    c.Timeout = TimeSpan.FromSeconds(15);
}).AddHttpMessageHandler<AuthHandler>();

// ViewModels Transient (nueva instancia en cada navegación)
builder.Services.AddTransient<LoginViewModel>();
builder.Services.AddTransient<PosViewModel>();
builder.Services.AddTransient<PedidosViewModel>();
builder.Services.AddTransient<PedidoDetalleViewModel>();
builder.Services.AddTransient<MesaDetalleViewModel>();
builder.Services.AddTransient<PagoViewModel>();
builder.Services.AddTransient<MapaViewModel>();
builder.Services.AddTransient<PerfilViewModel>();
builder.Services.AddTransient<TurnoViewModel>();
builder.Services.AddTransient<AdminViewModel>();
builder.Services.AddTransient<ClientesViewModel>();
builder.Services.AddTransient<ProductosViewModel>();
builder.Services.AddTransient<ComedoresMesasViewModel>();
builder.Services.AddTransient<HistorialCuentasViewModel>();

// Pages Transient (se crean en cada navegación)
builder.Services.AddTransient<LoginPage>(); /* ... 13 páginas más */
```

**Decisiones Arquitectónicas**:
- **Singleton**: Servicios que mantienen estado global (sesión, navegación, PDF) y Shells
- **Transient**: ViewModels y Pages para re-creación limpia en cada navegación
- **DelegatingHandler**: `AuthHandler` inyecta JWT automáticamente en toda petición HTTP

### 5.3 Recursos de la Aplicación

#### Iconos y Assets
```
Resources\
├── AppIcon\
│   ├── appicon.svg
│   └── appiconfg.svg (foreground)
├── Splash\
│   └── splash.svg
└── Raw\
    └── [archivos estáticos]
```

#### Tema de Colores
- **Color Primario**: #1a1a2e (Azul oscuro)
- **Color Secundario**: [Definir según branding]
- **Color de Acento**: [Definir según branding]

---

## 6. ESTRATEGIA DE TESTING

### 6.1 Tipos de Pruebas

#### Pruebas Unitarias
**Alcance**: ViewModels y Services
**Framework**: xUnit / NUnit
**Cobertura Objetivo**: 70%

**Casos de Prueba Prioritarios**:
- Validación de credenciales en LoginViewModel
- Cálculo de totales en PosViewModel
- Filtrado de pedidos en PedidosViewModel
- Manejo de tokens en SessionService
- Construcción de requests en ApiService

#### Pruebas de Integración
**Alcance**: Comunicación con API
**Herramientas**: Moq (para mocking), HttpClient mocks

**Escenarios**:
- Flujo completo de autenticación
- Creación de pedido end-to-end
- Manejo de errores de red
- Timeout de sesión

#### Pruebas de UI
**Alcance**: Flujos de usuario completos
**Método**: Pruebas manuales con checklist
**Dispositivos de Prueba**: Mínimo 3 dispositivos Android con diferentes versiones

**Casos de Prueba**:
- Login exitoso y fallido
- Creación de pedido completo
- Navegación entre secciones
- Comportamiento de la app sin conexión
- Rotación de pantalla

### 6.2 Estrategia de QA

#### Pruebas Alpha
- **Responsable**: Equipo de desarrollo
- **Duración**: Durante cada sprint
- **Enfoque**: Funcionalidad y casos de borde

#### Pruebas Beta
- **Responsables**: 2-3 meseros del restaurante
- **Duración**: 1 semana antes del lanzamiento
- **Enfoque**: Usabilidad y casos de uso reales

#### Métricas de Calidad
- 0 bugs críticos antes de producción
- < 5 bugs menores aceptables
- Feedback de usuarios beta > 4/5 estrellas

---

## 7. GESTIÓN DE CONFIGURACIÓN

### 7.1 Control de Versiones

#### Estrategia de Branching (Git Flow simplificado)
```
main (producción)
  ↑
  └── develop (integración)
        ↑
        ├── feature/login
        ├── feature/pedidos
        ├── feature/historial
        └── hotfix/bug-critrico
```

#### Convención de Commits
```
feat: Nueva funcionalidad
fix: Corrección de bug
refactor: Refactorización sin cambio funcional
docs: Documentación
style: Formato, sin cambio de código
test: Pruebas
```

### 7.2 Gestión de Configuraciones

#### Ambientes
1. **Desarrollo**: URL de API local o de desarrollo
2. **Staging**: URL de API de pruebas
3. **Producción**: URL de API productiva

#### Archivo de Configuración (appsettings.json - conceptual)
```json
{
  "ApiBaseUrl": "https://api.lachoza.com",
  "Timeout": 30,
  "EnableLogging": true,
  "CacheExpiration": 300
}
```

---

## 8. PLAN DE CAPACITACIÓN

### 8.1 Capacitación Técnica (Equipo de Desarrollo)

#### Temas
- Arquitectura MVVM en .NET MAUI
- CommunityToolkit.Mvvm avanzado
- Pruebas unitarias en proyectos MAUI
- Debugging en dispositivos Android

#### Duración: 1 semana (paralela al Sprint 0)

### 8.2 Capacitación de Usuarios Finales

#### Sesión 1: Introducción (1 hora)
- Presentación de la aplicación
- Beneficios del nuevo sistema
- Navegación básica

#### Sesión 2: Operación (2 horas)
- Inicio de sesión
- Toma de pedidos paso a paso
- Asignación de mesas
- Consulta de historial

#### Sesión 3: Práctica (1 hora)
- Ejercicios prácticos con datos de prueba
- Resolución de dudas
- Casos especiales

#### Materiales de Apoyo
- Manual de usuario en PDF
- Videos tutoriales cortos (2-3 min)
- Cheat sheet impreso

---

## 9. MÉTRICAS DE ÉXITO

### 9.1 Métricas Técnicas

| Métrica | Objetivo | Medición |
|---------|----------|----------|
| Cobertura de código | > 70% | Herramientas de análisis |
| Bugs en producción | < 3 por mes | Sistema de tracking |
| Tiempo de carga inicial | < 3 segundos | Profiling |
| Crashes | < 1% de sesiones | Analytics |

### 9.2 Métricas de Negocio

| Métrica | Línea Base | Objetivo | Medición |
|---------|------------|----------|----------|
| Tiempo de toma de pedido | 5 min | 3.5 min | Cronómetro manual |
| Errores de pedido | 8% | < 2% | Registros de cocina |
| Satisfacción de meseros | N/A | > 4/5 | Encuesta |
| Adopción del sistema | 0% | 100% en 1 mes | Uso vs papel |

### 9.3 Métricas de Usuario

| Métrica | Objetivo | Herramienta |
|---------|----------|-------------|
| Tasa de login exitoso | > 95% | Logs de aplicación |
| Sesiones diarias | 1 por mesero | Analytics |
| Pedidos creados por sesión | > 5 | Base de datos |
| Tiempo promedio de uso | > 3 horas/turno | Analytics |

---

## 10. RIESGOS Y MITIGACIÓN (Actualizado)

### Riesgos Técnicos Identificados en Iniciación

| Riesgo | Impacto | Probabilidad | Mitigación Implementada |
|--------|---------|--------------|-------------------------|
| Curva de aprendizaje de MAUI | Medio | Media | Capacitación inicial, documentación interna |
| Dependencias de terceros con bugs | Alto | Baja | Uso de versiones estables, alternativas identificadas |
| Problemas de rendimiento en dispositivos antiguos | Alto | Media | Testing en dispositivos target, optimización preventiva |
| Compatibilidad de Maps en todos los dispositivos | Medio | Media | Feature flag, graceful degradation |

---

## 11. ENTREGABLES DE LA FASE DE INICIACIÓN

### ✅ Documentación Completada
1. [X] Arquitectura del sistema definida
2. [X] Modelo de datos diseñado
3. [X] Plan de iteraciones establecido
4. [X] Entorno de desarrollo configurado
5. [X] Estructura de proyecto creada

### ✅ Infraestructura Técnica
1. [X] Proyecto .NET MAUI inicializado
2. [X] Dependencias instaladas y configuradas
3. [X] Inyección de dependencias implementada
4. [X] Estructura de carpetas organizada
5. [X] Control de versiones configurado

### ✅ Código Base Inicial
1. [X] Modelos de datos creados (Models.cs)
2. [X] Servicios base implementados (ApiService, SessionService)
3. [X] ViewModels principales esqueletizados
4. [X] Views XAML básicas creadas
5. [X] Navegación con AppShell configurada

### ✅ Recursos de Diseño
1. [X] Icono de aplicación configurado
2. [X] Splash screen definida
3. [X] Colores corporativos establecidos

---

## 12. TRANSICIÓN A FASE 3: PRODUCCIÓN

### Criterios de Salida (Exit Criteria) ✅
- [X] Arquitectura aprobada por el equipo técnico
- [X] Entorno de desarrollo operativo
- [X] Plan de sprints definido con user stories
- [X] Equipo capacitado en tecnologías clave
- [X] Código base inicial funcionando (compilable)
- [X] Estructura de navegación implementada

### Próximos Pasos Inmediatos (Sprint 1)
1. **Implementar funcionalidad de Login completa**
   - Integración real con API
   - Validación de credenciales
   - Manejo de errores
   - Navegación post-autenticación

2. **Completar SessionService**
   - Persistencia de token
   - Manejo de expiración
   - Logout

3. **Refinar UI de LoginPage**
   - Diseño visual según mockups
   - Feedback de carga
   - Animaciones

4. **Preparar PosPage para siguiente sprint**
   - Estructura base de UI
   - Integración con PosViewModel
   - Binding de datos inicial

---

## 13. LECCIONES APRENDIDAS

### Decisiones Técnicas Clave

#### ✅ Uso de CommunityToolkit.Mvvm
**Justificación**: Reduce boilerplate significativamente, acelera desarrollo
**Resultado**: Implementación más limpia y mantenible

#### ✅ Arquitectura MVVM estricta
**Justificación**: Facilita testing y separación de responsabilidades
**Resultado**: Código más testeable y escalable

#### ✅ Inyección de Dependencias desde el inicio
**Justificación**: Permite flexibilidad y facilita pruebas
**Resultado**: Acoplamiento reducido entre componentes

#### ⚠️ Foco inicial solo en Android
**Justificación**: Reducir complejidad inicial, todos los dispositivos objetivo son Android
**Consideración futura**: Evaluar expansión a iOS si es necesario

### Mejoras Identificadas
1. **Documentación**: Mantener documentación inline actualizada
2. **Testing**: Comenzar con pruebas unitarias desde Sprint 1
3. **Code Review**: Implementar revisiones cruzadas obligatorias
4. **CI/CD**: Configurar pipeline de integración continua

---

## 14. CONCLUSIONES

### Estado del Proyecto ✅ LISTO PARA PRODUCCIÓN (FASE 3)

La Fase de Iniciación ha sido **completada exitosamente**. Se han establecido las bases técnicas y organizacionales necesarias para comenzar el desarrollo iterativo.

### Fortalezas Identificadas
- ✅ Arquitectura sólida y escalable
- ✅ Stack tecnológico moderno y apropiado
- ✅ Equipo capacitado y comprometido
- ✅ Requisitos claros y priorizados
- ✅ Plan de sprints realista

### Áreas de Atención
- ⚠️ Mantener comunicación constante con stakeholders
- ⚠️ Monitorear riesgos técnicos durante el desarrollo
- ⚠️ Asegurar disponibilidad de dispositivos de prueba
- ⚠️ Validar continuamente con usuarios finales

### Siguiente Hito
**FASE 3 - PRODUCCIÓN: Sprint 1**  
Fecha de inicio: Inmediata  
Duración: 2 semanas  
Objetivo: Sistema de autenticación operativo

---

**Documento elaborado por**: Equipo de Desarrollo Choza POS  
**Fecha**: 2024  
**Versión**: 1.0  
**Estado**: Aprobado - Listo para Fase de Producción  
**Próxima Revisión**: Al finalizar Sprint 1
