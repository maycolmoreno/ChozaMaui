# Estado Actual De Estabilizacion La Choza

Fecha de actualizacion: 28 de mayo de 2026

## 1. Resumen ejecutivo

El proyecto avanzo de una etapa de diagnostico a una etapa de estabilizacion tecnica real. Ya se corrigieron puntos criticos de permisos, caja, pagos, precios historicos, manejo de errores y experiencia MAUI para CAJERO/CAMARERO.

El estado general actual es: **regular en mejora controlada**.

No se considera todavia listo para produccion sin una validacion manual completa con backend, web y MAUI ejecutandose juntos, pero los riesgos mas peligrosos del flujo restaurante ya fueron atacados: permisos por rol, doble cobro, doble caja abierta, precio manipulado desde cliente y errores 403 mal presentados.

## 2. Completado

### 2.1 Seguridad y permisos por rol

Se corrigio la matriz de permisos en backend web y API.

Archivos principales:

- `consumochoza/src/main/java/com/choza/consumochoza/config/SecurityConfig.java`
- `pisip/src/main/java/com/lachozag4/pisip/infraestructura/configuracion/SeguridadConfig.java`
- `pisip/src/main/java/com/lachozag4/pisip/presentacion/controladores/PedidoControlador.java`
- `ChozaMaui/Services/RoleCapabilityService.cs`
- `ChozaMaui/Views/AppShellCajero.xaml`

Cambios aplicados:

- Administracion de productos, categorias, usuarios, reportes, mesas, comedor y clientes quedo restringida a `ADMIN` en web.
- Cocina quedo limitada a `COCINA` y `ADMIN`.
- Caja quedo limitada a `CAJERO` y `ADMIN`.
- Cuentas quedo limitada a `CAJERO` y `ADMIN` en la parte web sensible.
- API de pedidos ahora diferencia acciones por rol:
  - `CAMARERO` y `ADMIN`: crear, confirmar, enviar y entregar pedidos.
  - `COCINA` y `ADMIN`: marcar preparando/listo.
  - `CAJERO`: consultar pedidos, pero no crear ni manipular cocina.
  - `ADMIN`: cancelar e intervenir.
- En MAUI el rol `CAJERO` ya no muestra flujo de mesas para crear pedidos.

Pruebas agregadas:

- `pisip/src/test/java/com/lachozag4/pisip/presentacion/controladores/RoleAuthorizationWebMvcTest.java`

Casos cubiertos:

- `CAJERO` no crea pedidos.
- `CAJERO` no marca pedido listo.
- `COCINA` no cobra.
- `CAMARERO` no cancela pedidos.
- `CAMARERO` puede crear pedidos.
- `CAMARERO` puede entregar pedidos.
- `COCINA` puede marcar pedido listo.
- `CAJERO` puede registrar pagos.
- `CAJERO` puede abrir caja.
- `ADMIN` puede cancelar pedidos.
- `CAMARERO` no puede crear productos.
- `CAJERO` no puede crear categorias.
- `COCINA` no puede crear usuarios.
- `ADMIN` puede crear productos.
- `ADMIN` puede crear categorias.
- `ADMIN` puede crear usuarios.
- `COCINA` no puede abrir caja.
- `COCINA` no puede cerrar caja.
- `CAJERO` no puede actualizar usuarios.
- `CAMARERO` no puede actualizar categorias.
- `CAMARERO` no puede eliminar productos.
- `ADMIN` puede actualizar productos.
- `ADMIN` puede eliminar usuarios.

Refuerzo aplicado:

- `CategoriaControlador` ahora tiene `@PreAuthorize` por metodo:
  - lecturas: todos los roles operativos;
  - escrituras: solo `ADMIN`.
- `UsuarioControlador` ahora tiene `@PreAuthorize` por metodo:
  - login/setup/existe siguen como endpoints publicos controlados por `SeguridadConfig`;
  - cambio de password y consulta por username requieren autenticacion;
  - CRUD administrativo requiere `ADMIN`.

### 2.2 Secretos y configuracion sensible

Archivos principales:

- `ChozaMaui/appsettings.json`
- `pisip/src/main/resources/application.properties`

Cambios aplicados:

- Se retiro el token largo de Dropbox del `appsettings.json` de MAUI.
- Se retiro el token largo de Dropbox que quedaba como valor por defecto en `pisip/src/main/resources/application.properties`.
- La configuracion de PostgreSQL y Dropbox en `pisip` quedo preparada para variables de entorno.
- Dropbox sigue funcionando segun validacion funcional indicada por el usuario.
- Se documento despliegue y variables de entorno en `Documentacion/DESPLIEGUE_VARIABLES_ENTORNO.md`.

Pendiente asociado:

- Rotar/revocar secretos que ya hayan estado expuestos.
- Configurar variables reales fuera del repositorio en cada ambiente.

### 2.3 Precio seguro e historico de pedido

Archivos principales:

- `pisip/src/main/java/com/lachozag4/pisip/aplicacion/mapeadores/PedidoRequestMapper.java`
- `pisip/src/main/java/com/lachozag4/pisip/aplicacion/casosuso/impl/PedidoDetalleUseCaseImpl.java`

Cambios aplicados:

- El backend ya no confia en `precioUnitario` enviado desde MAUI o web.
- Al crear detalle de pedido se usa siempre el precio real de `Producto`.
- `pedido_detalle.precio_unitario` se mantiene como precio historico congelado.

Prueba agregada:

- `pisip/src/test/java/com/lachozag4/pisip/aplicacion/mapeadores/PedidoRequestMapperTest.java`

### 2.4 Doble cobro y consistencia de pagos

Archivos principales:

- `pisip/src/main/java/com/lachozag4/pisip/dominio/repositorios/ICuentaRepositorio.java`
- `pisip/src/main/java/com/lachozag4/pisip/infraestructura/adaptadores/repositorios/CuentaRepositorioImpl.java`
- `pisip/src/main/java/com/lachozag4/pisip/infraestructura/persistencia/repositorios/CuentaJpaRepository.java`
- `pisip/src/main/java/com/lachozag4/pisip/aplicacion/casosuso/impl/PagoUseCaseImpl.java`

Cambios aplicados:

- Se agrego lectura de cuenta con bloqueo pesimista (`PESSIMISTIC_WRITE`) para registrar pagos.
- La validacion de saldo pendiente ocurre dentro de la transaccion bloqueada.
- Esto reduce el riesgo de doble cobro en concurrencia.

Prueba actualizada:

- `pisip/src/test/java/com/lachozag4/pisip/aplicacion/casosuso/impl/PagoUseCaseImplTest.java`

### 2.5 Doble caja abierta y cuenta abierta por mesa

Archivo principal:

- `pisip/src/main/resources/db/migration/V6__constraints_caja_cuenta_indexes.sql`

Cambios aplicados:

- Indice unico parcial para impedir mas de una caja `ABIERTA`.
- Indice unico parcial para impedir mas de una cuenta `ABIERTA` por mesa.
- Indices de apoyo para consultas de pedido, cuenta y pago.

Archivos de logica:

- `pisip/src/main/java/com/lachozag4/pisip/aplicacion/casosuso/impl/CuentaUseCaseImpl.java`
- `pisip/src/main/java/com/lachozag4/pisip/aplicacion/casosuso/impl/PedidoUseCaseImpl.java`

Cambios aplicados:

- Una mesa no puede abrir varias cuentas activas.
- Un pedido se resuelve contra la cuenta abierta de su mesa.
- Se valida que la mesa del pedido coincida con la mesa de la cuenta.

Pruebas agregadas:

- `pisip/src/test/java/com/lachozag4/pisip/aplicacion/casosuso/impl/CajaUseCaseImplTest.java`
- `pisip/src/test/java/com/lachozag4/pisip/aplicacion/casosuso/impl/CuentaUseCaseImplTest.java`

Casos cubiertos:

- No se abre una caja si ya existe una caja abierta.
- No se crea una cuenta si la mesa ya tiene cuenta abierta.
- No se agrega a una cuenta un pedido que pertenece a otra mesa.

### 2.6 Comprobantes y Dropbox

Archivo principal:

- `pisip/src/main/java/com/lachozag4/pisip/aplicacion/servicios/ComprobanteService.java`

Cambios aplicados:

- Se reconstruyo el servicio de comprobantes.
- Valida imagen, tamano maximo, content type y extension.
- Evita comprobante duplicado para un mismo pago.
- Sube archivo a Dropbox, obtiene URL publica y guarda metadata.
- Permite consultar, obtener y eliminar comprobantes.

Controlador:

- `pisip/src/main/java/com/lachozag4/pisip/presentacion/controladores/PagoControlador.java`

Cambios aplicados:

- Registro de pago con comprobante.
- Endpoints para subir, consultar y eliminar comprobante.
- Endpoint de estado de Dropbox.

### 2.7 Errores API uniformes

Archivo principal:

- `pisip/src/main/java/com/lachozag4/pisip/presentacion/excepciones/GlobalExceptionHandler.java`

Cambios aplicados:

- Respuestas JSON uniformes con:
  - `codigo`
  - `mensaje`
  - `timestamp`
  - `path`
- Manejo de errores de negocio, validacion, integridad, Dropbox y acceso denegado.
- `AccessDeniedException` y `AuthorizationDeniedException` devuelven 403 con `ACCESS_DENIED`.
- Los conflictos de integridad de caja/cuenta ahora tienen mensajes especificos:
  - `uq_caja_turno_abierta`: "Ya existe una caja abierta. Cierre la caja actual antes de abrir otra."
  - `uq_cuenta_abierta_mesa`: "La mesa ya tiene una cuenta abierta. Use la cuenta existente."

Prueba agregada:

- `pisip/src/test/java/com/lachozag4/pisip/presentacion/excepciones/GlobalExceptionHandlerTest.java`

Casos cubiertos:

- Conflicto de caja abierta devuelve 409, `DATA_INTEGRITY` y mensaje claro.
- Conflicto de cuenta abierta por mesa devuelve 409, `DATA_INTEGRITY` y mensaje claro.

MAUI:

- `ChozaMaui/Services/ApiErrorHelper.cs`

Servicios ajustados:

- `ClienteApiService`
- `MesaApiService`
- `ProductoApiService`
- `ReporteApiService`
- `UsuarioApiService`
- `PagoApiService`
- `PedidoApiService`
- `CuentaApiService`
- `CajaApiService`

Resultado:

- MAUI ya no depende de `EnsureSuccessStatusCode` sin mensaje util.
- Los errores 403/400/500 se muestran con mensajes de API cuando existen.

### 2.8 Limpieza de Turno, Caja y Pagos en MAUI

Backend:

- `GET /api/caja/{idcaja}/pagos`

Archivos principales:

- `pisip/src/main/java/com/lachozag4/pisip/presentacion/controladores/CajaControlador.java`
- `pisip/src/main/java/com/lachozag4/pisip/aplicacion/casosuso/impl/CajaUseCaseImpl.java`
- `pisip/src/main/java/com/lachozag4/pisip/infraestructura/adaptadores/repositorios/PagoRepositorioImpl.java`

MAUI:

- `ChozaMaui/Services/CajaApiService.cs`
- `ChozaMaui/Services/TurnoWorkflowService.cs`
- `ChozaMaui/ViewModels/TurnoViewModel.cs`
- `ChozaMaui/Views/TurnoPage.xaml`

Cambios aplicados:

- Turno ya no carga reportes administrativos para `CAJERO`.
- Turno muestra solo:
  - caja abierta/cerrada;
  - usuario/rol;
  - monto inicial;
  - pagos del turno;
  - acciones abrir/cerrar caja.
- Se elimino la seccion de registrar pago desde Turno.
- El historial de pagos del turno se carga desde la caja real.

### 2.9 Flujos MAUI corregidos

Correcciones aplicadas:

- `CAJERO` con caja abierta ya no debe ver "Sin caja" por error de permisos.
- Pedidos ahora carga inicialmente y considera pedidos activos aunque no sean del dia.
- "Entregar al cliente" ya no intenta hacer `PUT /api/mesas` restringido a admin.
- Pago ya no intenta liberar mesa directamente desde MAUI.
- La mesa se refresca en cache/estado visual, pero la liberacion real queda en backend al cerrar cuenta.

### 2.10 Estados centralizados en MAUI

Archivo agregado:

- `ChozaMaui/Models/DomainConstants.cs`

Constantes creadas:

- `PedidoEstados`
- `CuentaEstados`
- `MetodosPago`

Archivos ajustados:

- `ChozaMaui/Models/Models.cs`
- `ChozaMaui/Services/PedidoPresentationService.cs`
- `ChozaMaui/Services/PosOrderWorkflowService.cs`
- `ChozaMaui/ViewModels/PedidoDetalleViewModel.cs`
- `ChozaMaui/ViewModels/PedidosViewModel.cs`
- `ChozaMaui/ViewModels/PosViewModel.cs`
- `ChozaMaui/ViewModels/PagoViewModel.cs`
- `ChozaMaui/ViewModels/TurnoViewModel.cs`
- `ChozaMaui/ViewModels/HistorialCuentasViewModel.cs`
- `ChozaMaui/Services/HistorialCuentasPresentationService.cs`
- `ChozaMaui/ViewModels/MapaViewModel.cs`
- `ChozaMaui/Services/MesaDetailWorkflowService.cs`
- `ChozaMaui/Services/NotificationService.cs`
- `ChozaMaui/Services/PedidoApiService.cs`

Resultado:

- Se redujo el uso de strings sueltos para estados de pedido/cuenta/metodo de pago.

### 2.11 Manejo de errores en Thymeleaf

Archivos agregados:

- `consumochoza/src/main/java/com/choza/consumochoza/service/ApiClientException.java`
- `consumochoza/src/main/java/com/choza/consumochoza/service/impl/ApiErrorUtil.java`

Servicios ajustados:

- `PedidoServiceImpl`
- `CuentaServiceImpl`
- `ProductoServiceImpl`
- `CajaServiceImpl`
- `PagoServiceImpl`

Controladores ajustados:

- `CajaControlador`
- `CocinaControlador`
- `ProductoControlador`
- `CuentasControlador`
- `PedidosControlador`
- `DashboardControlador`

Resultado:

- Las vistas web ya no deberian mostrar "sin datos" silenciosamente cuando falla la API.
- Ahora pueden mostrar `mensajeError` con causa real.

### 2.12 Proteccion de productos historicos

Archivo principal:

- `pisip/src/main/java/com/lachozag4/pisip/aplicacion/casosuso/impl/ProductoUseCaseImpl.java`

Estado validado:

- La eliminacion de producto no borra fisicamente el registro.
- El producto se marca con `estado=false`.
- Los pedidos historicos conservan la referencia al producto y el precio historico en `pedido_detalle.precio_unitario`.

Prueba agregada:

- `pisip/src/test/java/com/lachozag4/pisip/aplicacion/casosuso/impl/ProductoUseCaseImplTest.java`

Caso cubierto:

- `eliminarDesactivaProductoSinBorrarloFisicamenteParaMantenerHistorial`

### 2.13 Stock y disponibilidad en pedidos

Archivos principales:

- `pisip/src/main/java/com/lachozag4/pisip/aplicacion/casosuso/impl/PedidoUseCaseImpl.java`
- `pisip/src/main/java/com/lachozag4/pisip/aplicacion/casosuso/impl/PedidoDetalleUseCaseImpl.java`
- `pisip/src/main/java/com/lachozag4/pisip/aplicacion/servicios/GestionStockServicioImpl.java`

Cambios aplicados:

- Al crear un pedido completo se valida primero que todos los productos esten activos.
- Luego se valida y descuenta stock.
- Si un producto esta dado de baja, el pedido no se guarda y no descuenta stock.
- En detalles individuales ya existia validacion de producto activo y stock.

Prueba agregada:

- `pisip/src/test/java/com/lachozag4/pisip/aplicacion/casosuso/impl/PedidoUseCaseImplTest.java`

Caso cubierto:

- `crearRechazaProductoInactivoAntesDeDescontarStockOGuardarPedido`

### 2.14 Contrato API restaurante

Archivo agregado:

- `Documentacion/CONTRATO_API_RESTAURANTE.md`

Contenido documentado:

- autenticacion y endpoints publicos;
- formato uniforme de errores;
- estados de pedido, cuenta, caja y metodos de pago;
- flujo principal restaurante;
- endpoints de pedidos, detalles, cuentas, pagos, comprobantes, caja, productos, categorias, mesas, comedores, clientes, usuarios y reportes;
- matriz de roles por endpoint;
- reglas concretas para MAUI/web.

Pendiente asociado:

- Validar el contrato con prueba manual end-to-end.
- Decidir si `CAJERO` debe conservar permisos de crear/agregar cuentas o quedar limitado a consulta/cobro.

## 3. Validacion ejecutada

Comandos ejecutados correctamente:

```powershell
dotnet build ChozaMaui\ChozaMaui.csproj --no-restore
```

Resultado: compilacion MAUI correcta.

```powershell
mvn test
```

Resultado: pruebas de `pisip` correctas.

Incluye las pruebas nuevas de baja logica de producto historico y rechazo de producto inactivo al crear pedido.
Tambien incluye la prueba nueva de mensajes claros para conflictos de integridad de caja/cuenta.

```powershell
mvn -DskipTests compile
```

Resultado: compilacion backend/API correcta.

```powershell
mvn -DskipTests compile
```

Resultado: compilacion web `consumochoza` correcta.

Nota tecnica:

- El wrapper Maven `.mvnw.cmd` presento fallos intermitentes de arranque.
- Se valido usando Maven directo desde la instalacion local.
- En pruebas aparece un stacktrace esperado de Dropbox en un caso que simula error de API, pero la suite termina correctamente.

## 4. Pendiente urgente

### 4.1 Validacion manual end-to-end

Falta ejecutar el flujo completo con API, web y MAUI levantados al mismo tiempo:

1. `CAMARERO` selecciona mesa.
2. Crea o abre pedido activo.
3. Agrega productos.
4. Envia pedido a cocina.
5. `COCINA` cambia a preparando/listo.
6. `CAMARERO` entrega.
7. `CAJERO` cobra con caja abierta.
8. Cuenta queda pagada.
9. Mesa queda libre.

### 4.2 Revisar migracion contra base de datos real

Antes de aplicar `V6__constraints_caja_cuenta_indexes.sql` en una base existente hay que revisar si ya existen:

- dos cajas `ABIERTA`;
- dos cuentas `ABIERTA` para la misma mesa.

Si existen duplicados, la migracion fallara y se debe limpiar la data primero.

Script de preflight agregado:

- `pisip/src/main/resources/db/preflight/pre_v6_constraints_check.sql`

Uso recomendado contra base de prueba o copia de datos reales:

```powershell
psql "$env:DATABASE_URL" -f pisip/src/main/resources/db/preflight/pre_v6_constraints_check.sql
```

El script no modifica datos. Reporta:

- cajas `ABIERTA` duplicadas;
- mesas con mas de una cuenta `ABIERTA`;
- pedidos asociados a una cuenta de otra mesa;
- resumen final con contadores que deben quedar en `0` antes de aplicar V6.

### 4.3 Rotacion de secretos expuestos

Aunque Dropbox sigue funcionando, falta confirmar en produccion:

- token anterior revocado;
- refresh token correcto;
- variables de entorno configuradas;
- credenciales fuera del repositorio.

### 4.4 Pruebas de concurrencia reales

Ya existen validaciones unitarias de caso de uso para evitar duplicados y reglas cruzadas. Falta probar con escenarios concurrentes reales sobre base de datos:

- dos cajeros cobrando la misma cuenta;
- dos aperturas de caja simultaneas;
- dos intentos de abrir cuenta para la misma mesa.

Guia operativa agregada:

- `Documentacion/PRUEBAS_CONCURRENCIA_POSTGRES.md`

La guia define pasos con dos sesiones `psql` para caja/cuenta y verificacion SQL para doble cobro por API. Debe ejecutarse sobre una base de prueba o copia de datos reales.

## 5. Pendiente importante

### 5.1 Pulir UX web por rol

Validar manualmente en Thymeleaf:

- menus ocultos por rol;
- botones de acciones segun permisos;
- errores visibles cuando falla API;
- rutas protegidas aunque el usuario escriba URL directa.

### 5.2 Pulir UX MAUI por rol

Validar en dispositivo/emulador:

- `CAMARERO`: Mesas, Pedidos, Avisos, Perfil.
- `CAJERO`: Turno, Cuentas, Pedidos, Avisos, Perfil.
- Turno sin informacion administrativa innecesaria.
- Pedidos con filtros claros: activos, listos, entregados, cancelados, todos.
- Pago mostrando mensajes reales de API.

### 5.3 Documentar contrato API

Estado: completado como contrato operativo inicial en `Documentacion/CONTRATO_API_RESTAURANTE.md`.

Queda pendiente, solo si se necesita para QA formal, agregar ejemplos JSON completos por DTO para:

- pedidos;
- cuentas;
- pagos;
- caja;
- comprobantes;
- errores uniformes;
- permisos por rol.

## 6. Pendiente opcional

- Reducir ruido de logs en pruebas de Dropbox simuladas.
- Crear tablero dedicado de cocina en MAUI si se decide soportar `COCINA` en movil.
- Exponer endpoint de capacidades por usuario si a futuro se necesitan permisos dinamicos.
- Refactorizar controladores web grandes en controladores mas pequenos por caso de uso.
- Centralizar estados tambien en backend/web si quedan strings repetidos.

## 7. Riesgos actuales

| Riesgo | Estado | Mitigacion actual | Pendiente |
|---|---|---|---|
| Acceso indebido por rol | Mitigado en API principal | Seguridad web/API y pruebas de rol | Validar UX y rutas Thymeleaf manualmente |
| Doble cobro | Reducido | Bloqueo pesimista de cuenta | Prueba concurrente real |
| Doble caja abierta | Reducido | Indice unico parcial, mensaje 409 claro y preflight SQL | Ejecutar preflight en datos reales |
| Cuenta duplicada por mesa | Reducido | Indice unico parcial, validacion, mensaje 409 claro y preflight SQL | Ejecutar preflight en datos reales |
| Precio manipulado desde cliente | Mitigado | Backend usa precio de producto | Revisar todos los flujos de detalle |
| Producto historico borrado | Mitigado | Baja logica y prueba unitaria | Validar mensajes web/MAUI al dar de baja |
| Venta de producto inactivo | Mitigado | Validacion backend antes de descontar stock | Validar mensaje visual en web/MAUI |
| Error 403 confuso en MAUI | Reducido | `ApiErrorHelper` y handler JSON | Validacion manual completa |
| Menu MAUI desalineado | Reducido | Shell cajero ajustada | Prueba UX final por rol |
| Secretos expuestos historicamente | Pendiente | Configuracion externalizada | Rotar credenciales |

## 8. Orden recomendado para continuar

1. Levantar backend `pisip`, web `consumochoza` y MAUI.
2. Ejecutar flujo end-to-end de restaurante con usuarios reales por rol.
3. Aplicar/verificar migracion `V6` en base de prueba con datos reales.
4. Ejecutar la guia `Documentacion/PRUEBAS_CONCURRENCIA_POSTGRES.md`.
5. Validar manualmente UX de Turno, Cuentas, Pedidos y Pago.
6. Validar mensajes visuales para producto inactivo/sin stock en web y MAUI.
7. Configurar variables de entorno reales fuera del repositorio.
8. Preparar checklist de presentacion/produccion.

## 9. Checklist actual

- [x] Permisos principales corregidos en web.
- [x] Permisos principales corregidos en API.
- [x] CAJERO removido del flujo movil de crear pedido desde mesas.
- [x] Precio de pedido protegido en backend.
- [x] Bloqueo de cuenta al registrar pago.
- [x] Restriccion de una caja abierta.
- [x] Restriccion de una cuenta abierta por mesa.
- [x] Pruebas unitarias anti-duplicado para caja/cuenta/mesa.
- [x] Errores API uniformes.
- [x] Mensajes claros para conflictos de caja/cuenta en base de datos.
- [x] Script preflight para validar datos antes de migracion V6.
- [x] Guia de pruebas de concurrencia PostgreSQL preparada.
- [x] Contrato API restaurante documentado.
- [x] Documentacion de despliegue/variables de entorno actualizada.
- [x] Token Dropbox removido de configuracion versionada principal.
- [x] MAUI consume mensajes de error de API.
- [x] Turno MAUI simplificado para caja/pagos.
- [x] Web deja de esconder fallos de API como listas vacias.
- [x] Productos historicos protegidos con baja logica.
- [x] Pedido completo rechaza productos inactivos antes de descontar stock.
- [x] Compilacion MAUI correcta.
- [x] Compilacion backend/API correcta.
- [x] Compilacion web correcta.
- [x] Pruebas backend actuales correctas.
- [x] Pruebas positivas/negativas de permisos para pedidos, pagos y caja.
- [x] Pruebas positivas/negativas de permisos para productos, categorias y usuarios.
- [x] Pruebas de bloqueo para actualizacion/eliminacion administrativa sensible.
- [ ] Validacion manual end-to-end completa.
- [ ] Pruebas concurrentes reales ejecutadas.
- [ ] Preflight V6 ejecutado contra base con datos reales.
- [ ] Migracion V6 aplicada en base con datos reales.
- [ ] Secretos rotados y documentados.
- [ ] Validacion visual de mensajes de stock/disponibilidad en web y MAUI.
- [ ] Variables reales configuradas fuera del repositorio.
- [ ] Checklist final de produccion/presentacion cerrado.

## 10. Nota sobre documentos anteriores

Los documentos `FASE4_ESTABILIZACION.md` y `FASE5_PRUEBAS_SISTEMA.md` mantienen valor como entregables academicos/metodologicos. Este documento representa el estado tecnico actualizado despues de las correcciones recientes y debe usarse como fuente principal para decidir el siguiente trabajo.
