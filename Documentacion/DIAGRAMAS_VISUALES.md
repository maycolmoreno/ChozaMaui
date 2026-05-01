# DIAGRAMAS Y VISUALIZACIONES - Mobile-D
## Proyecto: Choza POS

---

## 1. FASES DE MOBILE-D EN EL PROYECTO

```
┌──────────────────────────────────────────────────────────────────────┐
│                    METODOLOGÍA MOBILE-D                               │
│                    Aplicada a Choza POS                               │
└──────────────────────────────────────────────────────────────────────┘

┌─────────────┐   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐
│   FASE 1    │   │   FASE 2    │   │   FASE 3    │   │   FASE 4    │   │   FASE 5    │
│ EXPLORACIÓN │──▶│ INICIACIÓN  │──▶│ PRODUCCIÓN  │──▶│ ESTABILIZA- │──▶│ PRUEBAS     │
│             │   │             │   │             │   │    CIÓN     │   │ SISTEMA     │
└─────────────┘   └─────────────┘   └─────────────┘   └─────────────┘   └─────────────┘
      ✅               ✅               ⏳ SIGUIENTE          PENDIENTE         PENDIENTE
  (Completada)     (Completada)      Sprint 1-5            Sprint 6          Sprint 7

ENTREGABLES:      ENTREGABLES:      ENTREGABLES:       ENTREGABLES:      ENTREGABLES:
• Análisis        • Arquitectura    • Funcionalidad    • Optimización    • App lista
  de viabilidad     definida          completa           de rendimiento    producción
• Requisitos      • Entorno         • Código           • Corrección      • Documentación
  identificados     configurado       testeable          de bugs           usuario
• Riesgos         • Plan de         • UI/UX            • Refactoring     • Despliegue
  analizados        sprints           implementada       código            en tiendas
• Stakeholders    • Código base     • Integración      • Testing         • Capacitación
  identificados     inicial           completa           exhaustivo        completa
```

---

## 2. ARQUITECTURA MVVM DEL SISTEMA

```
┌────────────────────────────────────────────────────────────────────────────┐
│                          ARQUITECTURA MVVM                                  │
│                        ChozaMaui Application                                │
└────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────┐
│ CAPA DE PRESENTACIÓN (VIEW)                                                │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  LoginPage   │  │   PosPage    │  │ PedidosPage  │  │  MapaPage    │  │
│  │   (XAML)     │  │   (XAML)     │  │   (XAML)     │  │   (XAML)     │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘  │
│                                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                     │
│  │PerfilPage    │  │PedidoDetalle │  │  AppShell    │                     │
│  │   (XAML)     │  │Page (XAML)   │  │ (Navigation) │                     │
│  └──────────────┘  └──────────────┘  └──────────────┘                     │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
                                    ⬇️ Data Binding
┌────────────────────────────────────────────────────────────────────────────┐
│ CAPA DE LÓGICA DE PRESENTACIÓN (VIEWMODEL)                                │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌────────────────────┐  ┌────────────────────┐  ┌──────────────────────┐│
│  │  LoginViewModel    │  │   PosViewModel     │  │ PedidosViewModel     ││
│  │                    │  │                    │  │                      ││
│  │ • Username         │  │ • Productos        │  │ • ListaPedidos       ││
│  │ • Password         │  │ • Categorias       │  │ • FiltroEstado       ││
│  │ • LoginCommand     │  │ • CarritoPedido    │  │ • CargarCommand      ││
│  │ • IsLoading        │  │ • MesaSeleccionada │  │ • DetalleCommand     ││
│  └────────────────────┘  │ • CrearPedido      │  └──────────────────────┘│
│                           │ • AgregarProducto  │                           │
│                           └────────────────────┘                           │
│  ┌────────────────────┐  ┌────────────────────┐  ┌──────────────────────┐│
│  │  MapaViewModel     │  │ PerfilViewModel    │  │PedidoDetalleViewModel││
│  │                    │  │                    │  │                      ││
│  │ • Ubicacion        │  │ • Usuario          │  │ • Pedido             ││
│  │ • MostrarMapa      │  │ • EditarPerfil     │  │ • Detalles           ││
│  └────────────────────┘  │ • LogoutCommand    │  │ • Total              ││
│                           └────────────────────┘  └──────────────────────┘│
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
                                    ⬇️ Consume
┌────────────────────────────────────────────────────────────────────────────┐
│ CAPA DE SERVICIOS (BUSINESS LOGIC)                                        │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                         ApiService                                   │  │
│  │                                                                      │  │
│  │ • Login(username, password): LoginResponse                          │  │
│  │ • GetProductos(): List<ProductoResponse>                            │  │
│  │ • GetCategorias(): List<CategoriaResponse>                          │  │
│  │ • GetMesas(): List<MesaResponse>                                    │  │
│  │ • CrearPedido(pedidoRequest): PedidoResponse                        │  │
│  │ • GetPedidos(filtro): List<PedidoResponse>                          │  │
│  │ • GetPedidoDetalle(id): PedidoDetalleResponse                       │  │
│  │ • ActualizarUsuario(usuario): UsuarioResponse                       │  │
│  │                                                                      │  │
│  │ [HttpClient, JSON Serialization, Error Handling]                    │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                      SessionService                                  │  │
│  │                                                                      │  │
│  │ • Token: string                                                      │  │
│  │ • CurrentUser: LoginResponse                                         │  │
│  │ • IsAuthenticated: bool                                              │  │
│  │ • SaveSession(token, user)                                           │  │
│  │ • ClearSession()                                                     │  │
│  │ • GetToken(): string                                                 │  │
│  │                                                                      │  │
│  │ [Secure Storage, Session Management]                                 │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
                                    ⬇️ Utiliza
┌────────────────────────────────────────────────────────────────────────────┐
│ CAPA DE DATOS (MODELS)                                                    │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐             │
│  │ LoginRequest    │ │ LoginResponse   │ │ ProductoResponse│             │
│  ├─────────────────┤ ├─────────────────┤ ├─────────────────┤             │
│  │ • Username      │ │ • Token         │ │ • Idproducto    │             │
│  │ • Password      │ │ • Idusuario     │ │ • Nombre        │             │
│  └─────────────────┘ │ • Username      │ │ • Precio        │             │
│                       │ • NombreCompleto│ │ • Stock         │             │
│  ┌─────────────────┐ │ • Rol           │ │ • Descripcion   │             │
│  │ PedidoRequest   │ └─────────────────┘ │ • ImagenUrl     │             │
│  ├─────────────────┤                      │ • CategoriaId   │             │
│  │ • Fecha         │ ┌─────────────────┐ └─────────────────┘             │
│  │ • Observaciones │ │ PedidoResponse  │                                  │
│  │ • IdUsuario     │ ├─────────────────┤ ┌─────────────────┐             │
│  │ • IdMesa        │ │ • Idpedido      │ │ MesaResponse    │             │
│  │ • IdCliente     │ │ • Fecha         │ ├─────────────────┤             │
│  │ • Detalles[]    │ │ • Estado        │ │ • Idmesa        │             │
│  └─────────────────┘ │ • Total         │ │ • Numero        │             │
│                       │ • Observaciones │ │ • Capacidad     │             │
│  ┌─────────────────┐ └─────────────────┘ │ • Estado        │             │
│  │PedidoDetalle    │                      │ • NombreComedor │             │
│  │   Request       │ ┌─────────────────┐ └─────────────────┘             │
│  ├─────────────────┤ │PedidoDetalle    │                                  │
│  │ • IdProducto    │ │   Response      │ ┌─────────────────┐             │
│  │ • Cantidad      │ ├─────────────────┤ │ CategoriaRes    │             │
│  │ • PrecioUnit    │ │ • Id            │ ├─────────────────┤             │
│  │ • Subtotal      │ │ • Producto      │ │ • Idcategoria   │             │
│  └─────────────────┘ │ • Cantidad      │ │ • Nombre        │             │
│                       │ • PrecioUnit    │ │ • Descripcion   │             │
│                       │ • Subtotal      │ │ • Estado        │             │
│                       └─────────────────┘ └─────────────────┘             │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. FLUJO DE NAVEGACIÓN DE LA APLICACIÓN

```
┌──────────────────────────────────────────────────────────────────────────┐
│                      FLUJO DE NAVEGACIÓN                                  │
└──────────────────────────────────────────────────────────────────────────┘

                          ┌─────────────────┐
                          │   APLICACIÓN    │
                          │     INICIA      │
                          └────────┬────────┘
                                   │
                                   ▼
                          ┌─────────────────┐
                          │   LoginPage     │◀──────────┐
                          │                 │           │
                          │ • Usuario       │           │
                          │ • Contraseña    │           │
                          │ [Iniciar Sesión]│           │
                          └────────┬────────┘           │
                                   │                    │
                            Login exitoso               │
                                   │                    │
                                   ▼                    │
                          ┌─────────────────┐          │
                          │    AppShell     │          │
                          │  (Navegación)   │          │
                          └────────┬────────┘          │
                                   │                    │
                    ┌──────────────┼──────────────┬─────┴─────┐
                    │              │              │            │
                    ▼              ▼              ▼            ▼
          ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
          │   PosPage   │ │ PedidosPage │ │  MapaPage   │ │ PerfilPage  │
          │   (Tab 1)   │ │   (Tab 2)   │ │   (Tab 3)   │ │   (Tab 4)   │
          └─────────────┘ └──────┬──────┘ └─────────────┘ └──────┬──────┘
          │                      │                                │
          │ Flujo de             │ Seleccionar                    │ Logout
          │ Creación             │ Pedido                         │
          │ de Pedido            │                                │
          │                      ▼                                ▼
          ▼              ┌─────────────────┐            ┌─────────────────┐
  ┌──────────────┐      │ PedidoDetalle   │            │   Cierra sesión │
  │1. Seleccionar│      │     Page        │            │   Vuelve a      │
  │   Productos  │      │   (Modal)       │            │   LoginPage     │
  └──────┬───────┘      │                 │            └─────────────────┘
         │              │ • Lista items   │
         ▼              │ • Cantidades    │
  ┌──────────────┐      │ • Precios       │
  │2. Seleccionar│      │ • Total         │
  │     Mesa     │      │ • Estado        │
  └──────┬───────┘      └─────────────────┘
         │
         ▼
  ┌──────────────┐
  │3. Confirmar  │
  │    Pedido    │
  └──────┬───────┘
         │
         ▼
  ┌──────────────┐
  │ Pedido creado│
  │ Vuelve a Pos │
  │  o Historial │
  └──────────────┘
```

---

## 4. FLUJO DE DATOS - CREACIÓN DE PEDIDO

```
┌──────────────────────────────────────────────────────────────────────────┐
│              FLUJO DE DATOS: CREACIÓN DE PEDIDO                           │
└──────────────────────────────────────────────────────────────────────────┘

 [MESERO]
    │
    │ 1. Agrega productos al carrito
    │
    ▼
┌─────────────────┐
│   PosPage       │
│   (View)        │
└────────┬────────┘
         │ 2. User clicks "Crear Pedido"
         │
         ▼
┌─────────────────┐
│   PosViewModel  │
│                 │
│  3. Construye   │────────┐
│  PedidoRequest  │        │
└────────┬────────┘        │ PedidoRequest {
         │                 │   Fecha: DateTime.Now
         │                 │   IdUsuario: 5
         │                 │   IdMesa: 12
         │                 │   IdCliente: 1
         │                 │   Detalles: [
         │                 │     { IdProducto: 3, Cantidad: 2, Precio: 15.50 },
         │                 │     { IdProducto: 7, Cantidad: 1, Precio: 8.00 }
         │                 │   ]
         │                 │ }
         │                 │
         ▼                 ◀─────────┘
┌─────────────────┐
│   ApiService    │
│                 │
│ 4. Envía HTTP   │
│    POST         │
└────────┬────────┘
         │ POST /api/pedidos
         │ Headers: { Authorization: Bearer <token> }
         │ Body: { JSON del PedidoRequest }
         │
         ▼
    ╔═══════════╗
    ║  BACKEND  ║
    ║  API REST ║
    ╚═════╤═════╝
          │ 5. Procesa pedido
          │    Valida datos
          │    Guarda en BD
          │    Genera ID
          │
          ▼
    ╔═══════════╗
    ║  DATABASE ║
    ╚═════╤═════╝
          │
          │ 6. Retorna respuesta
          ▼
┌─────────────────┐
│   ApiService    │────────┐
│                 │        │ PedidoResponse {
│ 7. Deserializa  │        │   Idpedido: 145
│    respuesta    │        │   Fecha: "2024-01-15T14:30:00"
└────────┬────────┘        │   Estado: "Pendiente"
         │                 │   Total: 39.00
         │                 │   Observaciones: null
         │                 │ }
         ▼                 ◀─────────┘
┌─────────────────┐
│   PosViewModel  │
│                 │
│ 8. Actualiza UI │
│    Limpia       │
│    carrito      │
└────────┬────────┘
         │ 9. Notifica éxito
         │
         ▼
┌─────────────────┐
│   PosPage       │
│   (View)        │
│                 │
│ "Pedido #145    │
│  creado         │
│  exitosamente"  │
└─────────────────┘
         │
         ▼
    [MESERO]
   Ve confirmación
```

---

## 5. CRONOGRAMA DE SPRINTS

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        CRONOGRAMA DE DESARROLLO                           │
│                           7 Semanas - 5 Sprints                           │
└──────────────────────────────────────────────────────────────────────────┘

Semana 1-2       │ ████████████████████████ │ Sprint 1: Autenticación
                 │                          │ • Login funcional
                 │                          │ • Gestión de sesión
                 │                          │ • Navegación Shell
                 │                          │
Semana 3-4       │ ████████████████████████ │ Sprint 2: Gestión Pedidos
                 │                          │ • Catálogo productos
                 │                          │ • Carrito de compras
                 │                          │ • Asignación mesas
                 │                          │ • Creación pedido
                 │                          │
Semana 5         │ ████████████             │ Sprint 3: Historial
                 │                          │ • Listado pedidos
                 │                          │ • Filtros por estado
                 │                          │ • Detalle de pedido
                 │                          │
Semana 6         │ ████████████             │ Sprint 4: Complementarios
                 │                          │ • Perfil de usuario
                 │                          │ • Mapa / Ubicación
                 │                          │ • Logout
                 │                          │
Semana 7         │ ████████████             │ Sprint 5: Estabilización
                 │                          │ • Corrección de bugs
                 │                          │ • Optimizaciones
                 │                          │ • Pruebas finales
                 │                          │
                 └──────────────────────────┘

Leyenda:
████ = Desarrollo activo
▓▓▓▓ = Testing
░░░░ = Documentación
```

---

## 6. MODELO DE RELACIONES DE ENTIDADES

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    MODELO DE RELACIONES DE DATOS                          │
└──────────────────────────────────────────────────────────────────────────┘

                            1               *
┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│   Usuario    │───────│    Pedido    │───────│     Mesa     │
├──────────────┤ crea  ├──────────────┤ asig  ├──────────────┤
│• Idusuario   │       │• Idpedido    │       │• Idmesa      │
│• Username    │       │• Fecha       │       │• Numero      │
│• Nombre      │       │• Estado      │       │• Capacidad   │
│• Rol         │       │• Total       │       │• Estado      │
│• Estado      │       │• Observ.     │       │• Idcomedor   │
└──────────────┘       │• IdUsuario   │       └──────────────┘
                       │• IdMesa      │
                       │• IdCliente   │
                       └──────┬───────┘
                              │
                              │ 1
                              │
                              │ contiene
                              │
                              │ *
                              │
                    ┌─────────▼─────────┐
                    │  PedidoDetalle    │
                    ├───────────────────┤
                    │• Iddetalle        │
                    │• Cantidad         │
                    │• PrecioUnitario   │
                    │• Subtotal         │
                    │• IdPedido         │
                    │• IdProducto       │────┐
                    └───────────────────┘    │ *
                                             │
                                             │
                                             │ pertenece a
                                             │
                                             │ 1
                                             │
                    ┌────────────────────────▼
                    │     Producto           │
                    ├────────────────────────┤
                    │• Idproducto            │
                    │• Nombre                │
                    │• Precio                │
                    │• StockActual           │
                    │• Descripcion           │
                    │• ImagenUrl             │
                    │• Estado                │
                    │• CategoriaId           │────┐
                    └────────────────────────┘    │ *
                                                  │
                                                  │ pertenece a
                                                  │
                                                  │ 1
                                                  │
                    ┌─────────────────────────────▼
                    │     Categoria               │
                    ├─────────────────────────────┤
                    │• Idcategoria                │
                    │• Nombre                     │
                    │• Descripcion                │
                    │• Estado                     │
                    └─────────────────────────────┘


┌──────────────┐       1               *       ┌──────────────┐
│   Cliente    │─────────────────────────────▶ │   Pedido     │
├──────────────┤         asociado con          └──────────────┘
│• Idcliente   │
│• Nombre      │
└──────────────┘
```

---

## 7. STACK TECNOLÓGICO

```
┌──────────────────────────────────────────────────────────────────────────┐
│                         STACK TECNOLÓGICO                                 │
└──────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│                      FRONTEND (Aplicación Móvil)               │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────────────────────────────────────────┐     │
│  │             .NET MAUI Framework                      │     │
│  │                 (.NET 10)                            │     │
│  └──────────────────────────────────────────────────────┘     │
│                          │                                     │
│         ┌────────────────┼────────────────┐                   │
│         │                │                │                   │
│         ▼                ▼                ▼                   │
│  ┌──────────┐   ┌──────────────┐  ┌─────────────┐           │
│  │   XAML   │   │ CommunityTK  │  │ .NET MAUI   │           │
│  │   UI     │   │    MVVM      │  │    Maps     │           │
│  └──────────┘   │   (8.4.0)    │  └─────────────┘           │
│                  └──────────────┘                             │
│                                                                │
│  Lenguaje: C# 13                                              │
│  Target: Android API 24+                                      │
│  IDE: Visual Studio 2026 (v18.5.1)                           │
│                                                                │
└────────────────────────────────────────────────────────────────┘
                              │
                              │ HTTPS / REST API
                              │ JSON (Request/Response)
                              │ JWT Token Authentication
                              │
                              ▼
┌────────────────────────────────────────────────────────────────┐
│                      BACKEND (API REST)                        │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Endpoints:                                                     │
│  • POST   /api/auth/login                                      │
│  • GET    /api/productos                                       │
│  • GET    /api/categorias                                      │
│  • GET    /api/mesas                                           │
│  • POST   /api/pedidos                                         │
│  • GET    /api/pedidos                                         │
│  • GET    /api/pedidos/{id}                                    │
│  • PUT    /api/usuarios/{id}                                   │
│                                                                 │
│  Autenticación: JWT Bearer Tokens                              │
│  Formato: JSON                                                  │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────┐
│                        BASE DE DATOS                           │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Tablas:                                                        │
│  • Usuarios                                                     │
│  • Clientes                                                     │
│  • Productos                                                    │
│  • Categorias                                                   │
│  • Mesas                                                        │
│  • Pedidos                                                      │
│  • PedidosDetalles                                             │
│                                                                 │
└────────────────────────────────────────────────────────────────┘


┌────────────────────────────────────────────────────────────────┐
│                    HERRAMIENTAS DE DESARROLLO                  │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  • Git (Control de versiones)                                  │
│  • GitHub (Repositorio remoto)                                 │
│  • NuGet (Gestión de paquetes)                                 │
│  • PowerShell (Terminal)                                       │
│  • Android Emulator / Dispositivos físicos (Testing)           │
│  • Postman (Pruebas de API)                                    │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
```

---

## 8. MÉTRICAS DE ÉXITO DEL PROYECTO

```
┌──────────────────────────────────────────────────────────────────────────┐
│                         MÉTRICAS DE ÉXITO                                 │
└──────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  TIEMPO DE TOMA DE PEDIDO                                   │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ANTES:  ████████████████████  5.0 min                      │
│                                                              │
│  META:   ████████████▓▓        3.5 min  ⬅ Objetivo         │
│                                                              │
│  Mejora esperada: -30%                                       │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  TASA DE ERRORES EN PEDIDOS                                 │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ANTES:  ████████  8%                                       │
│                                                              │
│  META:   ██        2%  ⬅ Objetivo                          │
│                                                              │
│  Mejora esperada: -75%                                       │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  SATISFACCIÓN DE MESEROS (Escala 1-5)                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ANTES:  N/A (Proceso manual)                               │
│                                                              │
│  META:   ████████████████████  4.0/5.0  ⬅ Objetivo         │
│                                                              │
│  Medición: Encuesta post-implementación                      │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  ADOPCIÓN DEL SISTEMA                                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Mes 1:  ██████████████        50%                          │
│  Mes 2:  ███████████████████   75%                          │
│  Mes 3:  ████████████████████  100%  ⬅ Objetivo            │
│                                                              │
│  Meta: 100% de meseros usando la app en 1 mes               │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  MÉTRICAS TÉCNICAS                                           │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Cobertura de código:         ████████████████  > 70%       │
│  Tiempo de carga inicial:     ██████            < 3 seg     │
│  Tasa de crashes:             █                 < 1%        │
│  Disponibilidad del sistema:  ███████████████   95%         │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 9. MAPA DE RIESGOS

```
┌──────────────────────────────────────────────────────────────────────────┐
│                      MATRIZ DE RIESGOS                                    │
│                  (Probabilidad vs Impacto)                                │
└──────────────────────────────────────────────────────────────────────────┘

         ALTA │                     │ [RT-01]            │
              │                     │ Problemas de       │
              │                     │ conectividad       │
PROBABILIDAD  │                     │ API                │
              │                     │                    │
              ├─────────────────────┼────────────────────┤
        MEDIA │ [RO-03]             │ [RT-02]            │ [RO-01]
              │ Olvido de           │ Incompatibilidad   │ Resistencia
              │ credenciales        │ dispositivos       │ al cambio
              │                     │ antiguos           │
              ├─────────────────────┼────────────────────┤
         BAJA │                     │ [RT-03]            │
              │                     │ Rendimiento        │
              │                     │ en listados        │
              │                     │ grandes            │
              └─────────────────────┴────────────────────┘
                    BAJO              MEDIO              ALTO
                                   IMPACTO

Código de Colores:
[RT-XX] = Riesgo Técnico
[RO-XX] = Riesgo Operacional
[RN-XX] = Riesgo de Negocio

Acciones:
• Rojo (Alta prob + Alto impacto): Mitigación inmediata prioritaria
• Amarillo (Media prob o impacto): Monitoreo continuo + Plan de contingencia
• Verde (Baja prob + Bajo impacto): Seguimiento pasivo
```

---

## 10. PROCESO DE DESARROLLO ÁGIL

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    PROCESO ÁGIL CON MOBILE-D                              │
└──────────────────────────────────────────────────────────────────────────┘

┌──────────────────── SPRINT (1-2 semanas) ─────────────────────┐
│                                                                 │
│  DÍA 1: PLANIFICACIÓN                                          │
│  ┌─────────────────────────────────────────────────────┐      │
│  │ • Selección de User Stories del Backlog             │      │
│  │ • Estimación de esfuerzo                             │      │
│  │ • Asignación de tareas                               │      │
│  │ • Definición de "Definition of Done"                 │      │
│  └─────────────────────────────────────────────────────┘      │
│                           │                                     │
│                           ▼                                     │
│  DÍAS 2-9: DESARROLLO                                          │
│  ┌─────────────────────────────────────────────────────┐      │
│  │ Daily Standup (15 min)                              │      │
│  │ ┌───────────────────────────────────────────────┐   │      │
│  │ │ ¿Qué hice ayer?                               │   │      │
│  │ │ ¿Qué haré hoy?                                │   │      │
│  │ │ ¿Tengo algún bloqueador?                      │   │      │
│  │ └───────────────────────────────────────────────┘   │      │
│  │                                                      │      │
│  │ Desarrollo ─▶ Code Review ─▶ Testing               │      │
│  │      │              │              │                 │      │
│  │      └──────────────┴──────────────┘                │      │
│  │              Iteración continua                      │      │
│  └─────────────────────────────────────────────────────┘      │
│                           │                                     │
│                           ▼                                     │
│  DÍA 10: REVISIÓN (Demo)                                       │
│  ┌─────────────────────────────────────────────────────┐      │
│  │ • Demostración a stakeholders                        │      │
│  │ • Feedback sobre funcionalidades                     │      │
│  │ • Ajustes al Product Backlog                         │      │
│  └─────────────────────────────────────────────────────┘      │
│                           │                                     │
│                           ▼                                     │
│  DÍA 10: RETROSPECTIVA                                         │
│  ┌─────────────────────────────────────────────────────┐      │
│  │ • ¿Qué salió bien?                                   │      │
│  │ • ¿Qué se puede mejorar?                             │      │
│  │ • Acciones de mejora para siguiente sprint          │      │
│  └─────────────────────────────────────────────────────┘      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                           │
                           ▼
                  SIGUIENTE SPRINT
```

---

**FIN DE LOS DIAGRAMAS**

Estos diagramas visuales complementan la documentación detallada de las Fases 1 y 2 de Mobile-D para el proyecto Choza POS.
