# FASE 1: EXPLORACIÓN - Metodología Mobile-D
## Proyecto: Choza POS - Sistema de Punto de Venta Móvil

> **Estado**: ✅ COMPLETADA | **Fecha**: Mayo 2026 | **Versión**: 2.0

---

## 1. IDENTIFICACIÓN DE LOS STAKEHOLDERS

### Stakeholders Primarios
- **Propietario del Restaurante "La Choza"**: Tomador de decisiones final, interesado en incrementar eficiencia operativa y reducir errores. Tiene acceso al panel administrativo con KPIs y reportes de ventas.
- **Meseros/Personal de Servicio**: Usuarios principales del módulo POS; toman pedidos, gestionan mesas y visualizan el estado de las órdenes en tiempo real.
- **Chef/Personal de Cocina**: Receptores de los pedidos; requieren información clara, oportuna y cambio de estado de pedido (PENDIENTE → EN_PROCESO → LISTO → ENTREGADO).
- **Cajeros**: Procesan pagos (Efectivo, Tarjeta, Transferencia), abren/cierran turnos de caja y gestionan el historial de cuentas.

### Stakeholders Secundarios
- **Clientes del Restaurante**: Beneficiarios indirectos de un servicio más rápido y preciso.
- **Administrador de Sistemas**: Responsable del mantenimiento y soporte técnico.
- **Equipo de Desarrollo**: Responsables de la implementación y evolución del sistema.

---

## 2. ESTABLECIMIENTO DEL PROYECTO

### 2.1 Contexto del Negocio
El restaurante "La Choza" busca modernizar su sistema de toma de pedidos, eliminando el uso de papel y mejorando la comunicación entre el personal de servicio y cocina. La solución debe ser móvil para permitir que los meseros tomen pedidos directamente en las mesas.

### 2.2 Objetivos del Proyecto

#### Objetivos de Negocio
1. **Reducir tiempos de atención**: Disminuir en un 30% el tiempo desde la toma del pedido hasta su registro en cocina.
2. **Minimizar errores**: Reducir errores de transcripción de pedidos en un 80%.
3. **Mejorar experiencia del cliente**: Aumentar la satisfacción del cliente mediante un servicio más eficiente.
4. **Optimizar gestión de mesas**: Mejorar el control del estado y disponibilidad de mesas.

#### Objetivos Técnicos
1. Desarrollar aplicación móvil nativa para Android usando .NET MAUI.
2. Implementar arquitectura MVVM para mantenibilidad del código.
3. Integrar con API REST existente para gestión de datos.
4. Garantizar funcionamiento offline con sincronización posterior.

### 2.3 Alcance del Proyecto

#### Funcionalidades Incluidas (Scope In) — Implementadas ✅
- ✅ Autenticación de usuarios con roles diferenciados (Mesero, Cajero, Administrador) mediante JWT
- ✅ Punto de Venta (POS): selección de productos, carrito, asignación de mesa y cliente
- ✅ Gestión completa de pedidos (crear, visualizar, modificar, cambiar estado)
- ✅ Catálogo de productos por categorías con gestión CRUD (módulo Administrador)
- ✅ Asignación de pedidos a mesas con soporte multi-comedor
- ✅ Módulo de Pagos: Efectivo, Tarjeta y Transferencia con registro de cuentas
- ✅ Historial de cuentas con filtros por estado, fecha y búsqueda de cliente
- ✅ Panel Administrativo: KPIs del día (ventas, tickets, top 5 productos, estado de mesas)
- ✅ Control de Turnos de Caja (apertura/cierre con monto inicial/final)
- ✅ Gestión de Comedores y Mesas (CRUD completo)
- ✅ Gestión de Clientes (CRUD con búsqueda por nombre y cédula)
- ✅ Generación de Recibos PDF por pedido (Android nativo)
- ✅ Dos Shells de navegación: AppShell (Mesero) y AppShellCajero (Cajero/Administrador)
- ✅ Perfil de usuario (visualización y edición)
- ✅ Geolocalización del restaurante en mapa interactivo

#### Funcionalidades Excluidas (Scope Out)
- ❌ Procesamiento de pagos en línea (pasarela externa)
- ❌ Sistema de inventario automático con alertas de stock
- ❌ Gestión de empleados y nómina
- ❌ Reservaciones de mesas
- ❌ Soporte iOS (sólo Android API 24+)

### 2.4 Restricciones y Limitaciones

#### Restricciones Técnicas
- **Plataforma**: Solo Android (SDK 24.0 o superior)
- **Framework**: .NET MAUI con .NET 10, C# 13
- **Conectividad**: Requiere conexión a internet/red local para operación completa
- **Hardware**: Dispositivos con GPS para funcionalidad de mapas
- **Backend**: Dos proyectos Spring Boot 3.5.10 (Java 17): `pisip` (API REST principal) y `consumochoza` (módulo de consumo/reportes)
- **Base de Datos**: PostgreSQL (accedida exclusivamente por los backends)

#### Restricciones de Negocio
- **Presupuesto**: Limitado, uso de herramientas open-source cuando sea posible
- **Tiempo**: Desarrollo iterativo con entregables semanales
- **Personal**: Equipo de desarrollo reducido

#### Restricciones de Seguridad
- Autenticación mediante tokens JWT
- Comunicación segura con API (HTTPS)
- No almacenar contraseñas en texto plano

---

## 3. ANÁLISIS DE LA VIABILIDAD

### 3.1 Viabilidad Técnica ✅ FACTIBLE

#### Recursos Tecnológicos Disponibles
- **Framework**: .NET MAUI - Maduro y con soporte oficial de Microsoft
- **Lenguaje**: C# - Equipo con experiencia
- **Herramientas**: Visual Studio Community 2026 (18.5.1)
- **Bibliotecas**:
  - CommunityToolkit.MVVM (8.4.0) - Para implementación de patrones
  - Microsoft.Maui.Controls.Maps - Para funcionalidad de geolocalización

#### Capacidades del Equipo
- Conocimiento sólido en C# y .NET
- Experiencia con arquitectura MVVM
- Familiaridad con desarrollo móvil multiplataforma
- Capacidad para integración con APIs REST

#### Evaluación: **ALTA VIABILIDAD TÉCNICA**
El stack tecnológico es apropiado y el equipo cuenta con las capacidades necesarias.

### 3.2 Viabilidad Económica ✅ FACTIBLE

#### Costos de Desarrollo
- **Software**: $0 (uso de herramientas gratuitas)
- **Hardware**: Dispositivos Android para pruebas (ya disponibles)
- **Licencias**: No requeridas para fase MVP
- **Hosting**: API backend ya existente

#### Retorno de Inversión Estimado
- Reducción de errores = Menos desperdicio de alimentos
- Mayor rotación de mesas = Incremento en ventas
- Reducción de papel = Ahorro en material de oficina
- ROI estimado: 6 a 8 semanas

#### Evaluación: **ALTA VIABILIDAD ECONÓMICA**
Bajo costo de implementación con beneficios tangibles a corto plazo.

### 3.3 Viabilidad Operacional ✅ FACTIBLE

#### Adopción por Usuarios
- **Facilidad de uso**: Interfaz intuitiva diseñada para personal con nivel técnico básico
- **Capacitación**: Requerida pero mínima (2-3 horas)
- **Resistencia al cambio**: Moderada, mitigable mediante demostración de beneficios

#### Integración con Procesos Actuales
- No requiere cambios drásticos en el flujo de trabajo
- Mejora procesos existentes sin reemplazarlos completamente
- Permite transición gradual (uso paralelo inicial)

#### Soporte y Mantenimiento
- Documentación técnica completa
- Capacidad interna para correcciones menores
- Plan de actualización continua

#### Evaluación: **ALTA VIABILIDAD OPERACIONAL**
El sistema se integra naturalmente con los procesos actuales del restaurante.

### 3.4 Viabilidad Legal y Regulatoria ✅ FACTIBLE

#### Cumplimiento Normativo
- **Protección de datos**: Cumplimiento con leyes de privacidad locales
- **Seguridad de información**: Implementación de mejores prácticas
- **Propiedad intelectual**: Uso de software con licencias apropiadas

#### Riesgos Legales
- Bajo riesgo, no maneja información financiera sensible directamente
- No procesa pagos con tarjeta (fuera de alcance)

#### Evaluación: **ALTA VIABILIDAD LEGAL**
No existen barreras legales significativas para la implementación.

---

## 4. DEFINICIÓN DE REQUISITOS INICIALES

### 4.1 Requisitos Funcionales

#### RF-01: Autenticación de Usuarios con Roles
- **Prioridad**: Alta | **Estado**: ✅ Implementado
- **Descripción**: El sistema debe permitir el inicio de sesión con usuario y contraseña con redirección diferenciada por rol (Mesero → AppShell, Cajero/Admin → AppShellCajero)
- **Criterio de Aceptación**: Usuario se autentica, recibe JWT y es redirigido al Shell correspondiente a su rol

#### RF-02: Punto de Venta (POS)
- **Prioridad**: Alta | **Estado**: ✅ Implementado
- **Descripción**: Crear pedidos seleccionando productos del catálogo, asignando mesa y cliente opcional
- **Criterio de Aceptación**: Mesero puede tomar pedidos completos con carrito, cantidades, mesa y enviarlos al backend

#### RF-03: Catálogo de Productos
- **Prioridad**: Alta | **Estado**: ✅ Implementado
- **Descripción**: Visualizar y gestionar productos organizados por categorías (CRUD para administradores)
- **Criterio de Aceptación**: Productos se muestran con nombre, precio, descripción, imagen y estado; el administrador puede crear/editar/deshabilitar productos

#### RF-04: Gestión de Comedores y Mesas
- **Prioridad**: Alta | **Estado**: ✅ Implementado
- **Descripción**: CRUD completo de comedores y mesas con filtros por comedor y control de estado (Libre/Ocupada)
- **Criterio de Aceptación**: Administrador gestiona comedores y mesas; cada pedido tiene mesa con número y comedor identificados

#### RF-05: Historial de Pedidos
- **Prioridad**: Media | **Estado**: ✅ Implementado
- **Descripción**: Consultar pedidos con filtros por estado (PENDIENTE, EN_PROCESO, LISTO, ENTREGADO, CANCELADO)
- **Criterio de Aceptación**: Usuario ve listado con estado visual codificado por color y puede acceder al detalle

#### RF-06: Módulo de Pagos y Cuentas
- **Prioridad**: Alta | **Estado**: ✅ Implementado
- **Descripción**: Registrar pagos (Efectivo, Tarjeta, Transferencia) asociados a cuentas por pedido; soporta pagos parciales
- **Criterio de Aceptación**: Cajero procesa pago, el sistema calcula saldo pendiente y cierra cuenta al completar

#### RF-07: Historial de Cuentas
- **Prioridad**: Media | **Estado**: ✅ Implementado
- **Descripción**: Consultar cuentas con filtros por estado (ABIERTA/CERRADA/ANULADA), fecha y búsqueda de cliente; muestra estadísticas rápidas
- **Criterio de Aceptación**: Cajero/Admin consulta cuentas con totales facturados y puede expandir detalle de cada cuenta

#### RF-08: Panel Administrativo
- **Prioridad**: Alta | **Estado**: ✅ Implementado
- **Descripción**: Dashboard con KPIs del día: total de ventas, número de pedidos, ticket promedio, top 5 productos, estado de mesas y turno activo
- **Criterio de Aceptación**: Administrador visualiza métricas en tiempo real con carga paralela de datos

#### RF-09: Control de Turnos de Caja
- **Prioridad**: Alta | **Estado**: ✅ Implementado
- **Descripción**: Apertura y cierre de turno de caja con registro de monto inicial y final
- **Criterio de Aceptación**: Cajero abre turno con monto inicial; al cerrar registra monto final y el sistema almacena el turno

#### RF-10: Gestión de Clientes
- **Prioridad**: Media | **Estado**: ✅ Implementado
- **Descripción**: CRUD de clientes con búsqueda por nombre y cédula; registro rápido desde módulo de pagos
- **Criterio de Aceptación**: Usuario busca o crea clientes en tiempo real; se asocian a pedidos y cuentas

#### RF-11: Generación de Recibos PDF
- **Prioridad**: Media | **Estado**: ✅ Implementado
- **Descripción**: Generar y compartir recibo de consumo en formato PDF por pedido (Android nativo)
- **Criterio de Aceptación**: El recibo incluye detalle de productos, totales, mesero y número de pedido; se guarda en caché y se puede compartir

#### RF-12: Perfil de Usuario
- **Prioridad**: Baja | **Estado**: ✅ Implementado
- **Descripción**: Visualizar y editar información del usuario; cambiar contraseña
- **Criterio de Aceptación**: Usuario edita sus datos y puede cambiar contraseña con validación

#### RF-13: Geolocalización
- **Prioridad**: Baja | **Estado**: ✅ Implementado
- **Descripción**: Mostrar ubicación del restaurante en mapa interactivo
- **Criterio de Aceptación**: Mapa muestra pin con la ubicación del establecimiento

### 4.2 Requisitos No Funcionales

#### RNF-01: Rendimiento
- Tiempo de respuesta < 2 segundos para operaciones locales
- Carga de catálogo completo < 5 segundos

#### RNF-02: Usabilidad
- Interfaz intuitiva, navegable sin capacitación extensa
- Diseño responsive para diferentes tamaños de pantalla Android

#### RNF-03: Seguridad
- Autenticación mediante JWT tokens
- Comunicación cifrada con API (HTTPS)
- Sesión expira después de inactividad

#### RNF-04: Confiabilidad
- Disponibilidad del 95% durante horario de servicio
- Manejo gracioso de errores de red

#### RNF-05: Mantenibilidad
- Código siguiendo arquitectura MVVM
- Documentación técnica completa
- Separación clara de responsabilidades

#### RNF-06: Compatibilidad
- Android API 24 (Android 7.0) o superior
- .NET 10 target framework

---

## 5. RIESGOS IDENTIFICADOS

### Riesgos Técnicos

| ID | Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|---|
| RT-01 | Problemas de conectividad con API | Alta | Alto | Implementar caché local y cola de sincronización |
| RT-02 | Incompatibilidad con dispositivos antiguos | Media | Medio | Definir dispositivos mínimos soportados |
| RT-03 | Rendimiento lento en listados grandes | Baja | Medio | Implementar paginación y virtualización |

### Riesgos Operacionales

| ID | Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| RO-01 | Resistencia al cambio del personal | Alta | Alto | Capacitación y período de adaptación |
| RO-02 | Falta de dispositivos suficientes | Media | Alto | Planificar adquisición gradual |
| RO-03 | Personal olvida credenciales | Media | Bajo | Sistema de recuperación de contraseñas |

### Riesgos de Negocio

| ID | Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| RN-01 | Cambios frecuentes en menú | Alta | Medio | Sistema flexible de actualización de productos |
| RN-02 | Expansión a nuevas ubicaciones | Baja | Medio | Arquitectura multitenancy desde el inicio |

---

## 6. PLAN DE COMUNICACIÓN

### Reuniones del Proyecto
- **Daily Standup**: Breve sincronización diaria del equipo (15 min)
- **Demo Semanal**: Presentación de avances a stakeholders
- **Retrospectiva Quincenal**: Evaluación de proceso y mejoras

### Canales de Comunicación
- **Slack/Teams**: Comunicación diaria del equipo
- **Email**: Comunicaciones formales con stakeholders
- **GitHub**: Gestión de código y issues técnicos

### Reportes
- **Reporte Semanal**: Avance, bloqueadores, próximos pasos
- **Reporte de Riesgos**: Actualización mensual de matriz de riesgos

---

## 7. DECISIÓN FINAL

✅ **PROYECTO APROBADO PARA CONTINUAR A FASE DE INICIACIÓN**

### Justificación
- **Alta viabilidad** en todos los aspectos evaluados (técnica, económica, operacional, legal)
- **Beneficios claros** para el negocio y usuarios finales
- **Riesgos identificados** son manejables con las mitigaciones propuestas
- **Requisitos bien definidos** permiten avanzar a planificación detallada
- **Stack tecnológico apropiado** para los objetivos del proyecto

### Próximos Pasos
1. Avanzar a **Fase 2: Iniciación**
2. Elaborar arquitectura detallada del sistema
3. Definir plan de iteraciones y entregables
4. Preparar entorno de desarrollo y pruebas

---

**Documento elaborado por**: Equipo de Desarrollo Choza POS  
**Fecha**: Mayo 2026  
**Versión**: 2.0 (actualizado con alcance final implementado)  
**Estado**: ✅ Fase Completada — Todos los requisitos identificados fueron implementados exitosamente
