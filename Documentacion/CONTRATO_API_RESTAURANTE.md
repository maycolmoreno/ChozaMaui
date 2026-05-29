# Contrato API Restaurante La Choza

Fecha: 28 de mayo de 2026

## 1. Alcance

Este documento resume el contrato actual de la API `pisip` para web Thymeleaf y app .NET MAUI.

El sistema no es autoservicio. El cliente no consume la API directamente. Los roles operativos son:

- `ADMIN`
- `CAMARERO`
- `COCINA`
- `CAJERO`

La autoridad final de permisos es el backend Spring Boot. MAUI y Thymeleaf solo deben ocultar o mostrar opciones, pero nunca asumir permisos distintos.

## 2. Autenticacion

Endpoints publicos:

| Metodo | Ruta | Uso |
|---|---|---|
| `POST` | `/api/usuarios/login` | Login y emision de token |
| `GET` | `/api/usuarios/existe-alguno` | Verifica si existe usuario inicial |
| `POST` | `/api/usuarios/setup-admin` | Configuracion inicial de admin |
| `GET` | `/actuator/health` | Salud de API |

El resto de endpoints requiere token JWT.

## 3. Formato De Error

La API devuelve errores JSON uniformes:

```json
{
  "codigo": "ACCESS_DENIED",
  "mensaje": "No tiene permisos para acceder a este recurso.",
  "timestamp": "2026-05-28T23:00:00",
  "path": "/api/caja/abierta"
}
```

Codigos principales:

| HTTP | codigo | Significado |
|---|---|---|
| `400` | `BAD_REQUEST` / `VALIDATION_ERROR` | Datos invalidos |
| `401` | `UNAUTHORIZED` | Token ausente o invalido |
| `403` | `ACCESS_DENIED` | Rol sin permiso |
| `404` | `NOT_FOUND` | Registro no encontrado |
| `409` | `BUSINESS_ERROR` / `DATA_INTEGRITY` | Regla de negocio o restriccion BD |
| `503` | `DROPBOX_UNAVAILABLE` | Dropbox no disponible |

Mensajes especiales de integridad:

| Restriccion | Mensaje |
|---|---|
| `uq_caja_turno_abierta` | Ya existe una caja abierta. Cierre la caja actual antes de abrir otra. |
| `uq_cuenta_abierta_mesa` | La mesa ya tiene una cuenta abierta. Use la cuenta existente. |

## 4. Estados

### Pedido

| Estado backend | Significado visual |
|---|---|
| `PENDIENTE` | Pedido creado, aun no enviado o pendiente |
| `EN_COCINA` | Pedido enviado a cocina / en preparacion |
| `EN_BAR` | Pedido enviado a bar |
| `LISTO_PARA_ENTREGA` | Cocina lo marco listo |
| `COMPLETADO` | Entregado al cliente |
| `CANCELADO` | Cancelado |

Nota: `COMPLETADO` significa entregado, no pagado. El cobro vive en `Cuenta`.

### Cuenta

| Estado backend | Significado |
|---|---|
| `ABIERTA` | Cuenta activa de mesa |
| `PAGADA` | Cuenta cobrada |
| `ANULADA` | Cuenta anulada |

### Caja

| Estado backend | Significado |
|---|---|
| `ABIERTA` | Turno de caja activo |
| `CERRADA` | Turno de caja cerrado |

### Pago

Metodos validos:

- `EFECTIVO`
- `TARJETA`
- `TRANSFERENCIA`
- `OTRO`

## 5. Flujo Principal Restaurante

1. `CAMARERO` consulta mesas: `GET /api/mesas`.
2. `CAMARERO` crea pedido con cuenta: `POST /api/pedidos/con-cuenta`.
3. `CAMARERO` agrega/edita detalles mientras el pedido es editable.
4. `CAMARERO` envia a cocina: `PATCH /api/pedidos/{id}/confirmar`.
5. `COCINA` marca preparando: `PATCH /api/pedidos/{id}/preparando`.
6. `COCINA` marca listo: `PATCH /api/pedidos/{id}/listo`.
7. `CAMARERO` entrega: `PATCH /api/pedidos/{id}/entregado`.
8. `CAJERO` consulta cuenta: `GET /api/cuentas/{id}` o `GET /api/cuentas/mesa/{idMesa}/abierta`.
9. `CAJERO` cobra: `POST /api/cuentas/{idCuenta}/pagos`.
10. Backend deja cuenta pagada y libera mesa cuando corresponde.

## 6. Pedidos

Base: `/api/pedidos`

| Metodo | Ruta | Roles | Uso |
|---|---|---|---|
| `GET` | `/api/pedidos` | Todos | Listar pedidos |
| `GET` | `/api/pedidos/{id}` | Todos | Obtener pedido |
| `GET` | `/api/pedidos/cuenta/{idCuenta}/reciente` | Todos | Ultimo pedido de una cuenta |
| `GET` | `/api/pedidos/paginado` | Todos | Listado con filtros |
| `POST` | `/api/pedidos` | `ADMIN`, `CAMARERO` | Crear pedido |
| `POST` | `/api/pedidos/con-cuenta` | `ADMIN`, `CAMARERO` | Crear pedido y resolver cuenta |
| `PUT` | `/api/pedidos/{id}` | `ADMIN`, `CAMARERO` | Actualizar pedido |
| `PATCH` | `/api/pedidos/{id}/estado` | Segun estado destino | Compatibilidad |
| `PATCH` | `/api/pedidos/{id}/confirmar` | `ADMIN`, `CAMARERO` | Enviar a cocina |
| `PATCH` | `/api/pedidos/{id}/preparando` | `ADMIN`, `COCINA` | Marcar en preparacion |
| `PATCH` | `/api/pedidos/{id}/listo` | `ADMIN`, `COCINA` | Marcar listo |
| `PATCH` | `/api/pedidos/{id}/entregado` | `ADMIN`, `CAMARERO` | Marcar entregado |
| `PATCH` | `/api/pedidos/{id}/cancelar` | `ADMIN` | Cancelar pedido |
| `DELETE` | `/api/pedidos/{id}` | `ADMIN`, `CAMARERO` | Eliminar segun reglas del caso de uso |

Filtros de paginado:

| Query | Ejemplo |
|---|---|
| `estado` | `PENDIENTE`, `EN_COCINA`, `LISTO_PARA_ENTREGA`, `COMPLETADO`, `CANCELADO`, `TODOS` |
| `q` | texto libre |
| `fechaDesde` | `2026-05-01` |
| `fechaHasta` | `2026-05-28` |
| `page` | `0` |
| `size` | `10` |

Reglas relevantes:

- Backend ignora el precio enviado por cliente al crear detalle.
- El precio historico se toma desde `Producto.precio`.
- No se debe enviar a cocina un pedido sin productos.
- No se debe modificar libremente un pedido pagado o cerrado.
- `CAJERO` puede consultar pedidos, pero no crearlos ni marcar cocina/listo/entregado.

## 7. Detalles De Pedido

Base: `/api/pedidos/{idPedido}/detalles`

| Metodo | Ruta | Roles efectivos | Uso |
|---|---|---|---|
| `GET` | `/api/pedidos/{idPedido}/detalles` | Todos | Listar detalles |
| `GET` | `/api/pedidos/{idPedido}/detalles/{idDetalle}` | Todos | Obtener detalle |
| `POST` | `/api/pedidos/{idPedido}/detalles` | `ADMIN`, `CAMARERO` | Agregar producto |
| `PUT` | `/api/pedidos/{idPedido}/detalles/{idDetalle}` | `ADMIN`, `CAMARERO` | Actualizar detalle |
| `DELETE` | `/api/pedidos/{idPedido}/detalles/{idDetalle}` | `ADMIN`, `CAMARERO` | Eliminar detalle |

Roles efectivos vienen de `SeguridadConfig`, porque el controlador no define `@PreAuthorize` propio.

Reglas relevantes:

- Producto debe estar activo.
- Stock debe ser suficiente.
- Precio se recalcula en backend.

## 8. Cuentas

Base: `/api/cuentas`

| Metodo | Ruta | Roles | Uso |
|---|---|---|---|
| `GET` | `/api/cuentas` | `ADMIN`, `CAMARERO`, `CAJERO` | Listar cuentas |
| `GET` | `/api/cuentas/abiertas` | `ADMIN`, `CAMARERO`, `CAJERO` | Listar abiertas |
| `GET` | `/api/cuentas/{id}` | `ADMIN`, `CAMARERO`, `CAJERO` | Obtener cuenta |
| `GET` | `/api/cuentas/mesa/{idMesa}/abierta` | `ADMIN`, `CAMARERO`, `CAJERO` | Cuenta activa de mesa |
| `POST` | `/api/cuentas` | `ADMIN`, `CAMARERO`, `CAJERO` | Crear cuenta |
| `POST` | `/api/cuentas/{idCuenta}/pedidos/{idPedido}` | `ADMIN`, `CAMARERO`, `CAJERO` | Agregar pedido a cuenta |
| `PATCH` | `/api/cuentas/{id}/estado` | `ADMIN`, `CAJERO` | Cambiar estado de cuenta |
| `PATCH` | `/api/cuentas/{id}/cliente` | `ADMIN`, `CAJERO` | Asignar cliente |

Reglas relevantes:

- Una mesa solo puede tener una cuenta `ABIERTA`.
- Un pedido no puede asociarse a una cuenta de otra mesa.
- La cuenta es la unidad de cobro.

## 9. Pagos Y Comprobantes

Base: `/api/cuentas/{idCuenta}/pagos`

| Metodo | Ruta | Roles | Uso |
|---|---|---|---|
| `POST` | `/api/cuentas/{idCuenta}/pagos` | `ADMIN`, `CAJERO` | Registrar pago |
| `POST` | `/api/cuentas/{idCuenta}/pagos/con-comprobante` | `ADMIN`, `CAJERO` | Registrar pago con imagen |
| `GET` | `/api/cuentas/{idCuenta}/pagos` | `ADMIN`, `CAJERO` | Listar pagos de cuenta |
| `GET` | `/api/cuentas/{idCuenta}/pagos/resumen` | `ADMIN`, `CAJERO` | Total pagado y saldo |
| `POST` | `/api/cuentas/{idCuenta}/pagos/{idPago}/comprobante` | `ADMIN`, `CAJERO` | Adjuntar comprobante |
| `GET` | `/api/cuentas/{idCuenta}/pagos/{idPago}/comprobante` | `ADMIN`, `CAJERO` | Ver comprobante |
| `GET` | `/api/cuentas/{idCuenta}/pagos/dropbox/estado` | `ADMIN`, `CAJERO` | Validar Dropbox |
| `DELETE` | `/api/cuentas/{idCuenta}/pagos/{idPago}/comprobante` | `ADMIN` | Eliminar comprobante |

Reglas relevantes:

- Debe existir caja abierta.
- El monto debe ser mayor a `0`.
- El metodo debe estar en la lista permitida.
- El pago no puede exceder el saldo pendiente.
- La cuenta se bloquea con `PESSIMISTIC_WRITE` durante el pago.
- Si el pago completa el total, la cuenta pasa a `PAGADA` y la mesa queda libre.

## 10. Caja

Base: `/api/caja`

| Metodo | Ruta | Roles | Uso |
|---|---|---|---|
| `POST` | `/api/caja/apertura` | `ADMIN`, `CAJERO` | Abrir caja |
| `GET` | `/api/caja/abierta` | `ADMIN`, `CAJERO` | Caja abierta actual |
| `POST` | `/api/caja/cierre` | `ADMIN`, `CAJERO` | Cerrar caja |
| `GET` | `/api/caja` | `ADMIN`, `CAJERO` | Listar turnos |
| `GET` | `/api/caja/{idcaja}/pagos` | `ADMIN`, `CAJERO` | Pagos del turno |

Reglas relevantes:

- Solo puede existir una caja `ABIERTA`.
- El monto inicial y declarado no pueden ser negativos.
- Cierre calcula monto esperado con pagos del turno.

## 11. Productos Y Categorias

### Productos

Base: `/api/productos`

| Metodo | Ruta | Roles | Uso |
|---|---|---|---|
| `GET` | `/api/productos` | Todos | Listar productos |
| `GET` | `/api/productos/{id}` | Todos | Obtener producto |
| `GET` | `/api/productos/categoria/{idCategoria}` | Todos | Productos por categoria |
| `GET` | `/api/productos/activos` | Todos | Productos activos |
| `POST` | `/api/productos` | `ADMIN` | Crear producto |
| `PUT` | `/api/productos/{id}` | `ADMIN` | Actualizar producto |
| `DELETE` | `/api/productos/{id}` | `ADMIN` | Baja logica |

### Categorias

Base: `/api/categorias`

| Metodo | Ruta | Roles | Uso |
|---|---|---|---|
| `GET` | `/api/categorias` | Todos | Listar |
| `GET` | `/api/categorias/activas` | Todos | Listar activas |
| `GET` | `/api/categorias/{id}` | Todos | Obtener |
| `POST` | `/api/categorias` | `ADMIN` | Crear |
| `PUT` | `/api/categorias/{id}` | `ADMIN` | Actualizar |
| `DELETE` | `/api/categorias/{id}` | `ADMIN` | Eliminar/desactivar |

Reglas relevantes:

- No borrar fisicamente productos historicos.
- Producto eliminado queda con `estado=false`.
- Pedidos historicos conservan `pedido_detalle.precio_unitario`.

## 12. Mesas, Comedores Y Clientes

### Mesas

Base: `/api/mesas`

| Metodo | Ruta | Roles |
|---|---|---|
| `GET` | `/api/mesas`, `/api/mesas/{id}`, `/api/mesas/disponibles`, `/api/mesas/ocupadas` | `ADMIN`, `CAMARERO`, `CAJERO` |
| `POST` | `/api/mesas` | `ADMIN` |
| `PUT` | `/api/mesas/{id}` | `ADMIN` |
| `DELETE` | `/api/mesas/{id}` | `ADMIN` |

MAUI no debe liberar mesa con `PUT /api/mesas/{id}`. Esa accion queda en backend al pagar/cerrar cuenta.

### Comedores

Base: `/api/comedores`

Todos los endpoints son solo `ADMIN`.

### Clientes

Base: `/api/clientes`

| Metodo | Roles |
|---|---|
| `GET`, `POST` | `ADMIN`, `CAMARERO`, `CAJERO` |
| `PUT`, `DELETE` | `ADMIN` |

## 13. Usuarios Y Reportes

### Usuarios

Base: `/api/usuarios`

| Metodo | Ruta | Roles |
|---|---|---|
| `POST` | `/api/usuarios/login` | Publico |
| `GET` | `/api/usuarios/existe-alguno` | Publico |
| `POST` | `/api/usuarios/setup-admin` | Publico inicial |
| `POST` | `/api/usuarios/cambiar-password` | Autenticado |
| `GET` | `/api/usuarios/por-username/{username}` | Autenticado |
| CRUD restante | `/api/usuarios/**` | `ADMIN` |

### Reportes

Base: `/api/reportes`

Solo `ADMIN`.

## 14. Reglas Para Clientes MAUI/Web

- Mostrar el `mensaje` del error API si existe.
- No usar `EnsureSuccessStatusCode` sin leer el body de error.
- No enviar ni confiar en precios de detalle.
- No inventar estados visuales como valor backend. Si se muestra "Entregado", mapearlo a `COMPLETADO`.
- No liberar mesas desde MAUI.
- No permitir que `CAJERO` cree pedidos desde mesas.
- No permitir que `COCINA` cobre o gestione caja.
- Refrescar cuenta/caja despues de cada pago o cierre.

## 15. Pendientes Del Contrato

- Generar OpenAPI/Swagger formal si se usara para integraciones externas.
- Validar este contrato con pruebas manuales end-to-end.
- Revisar si `CAJERO` debe conservar permiso de `POST /api/cuentas` y `POST /api/cuentas/{idCuenta}/pedidos/{idPedido}` o si debe quedar solo en consulta/cobro.
- Documentar ejemplos JSON completos por DTO si el equipo lo necesita para QA.
