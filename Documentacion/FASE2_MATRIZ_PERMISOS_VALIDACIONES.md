Fase 2

Matriz de permisos actuales y validaciones funcionales propuestas

Fecha

28 de mayo de 2026

Objetivo

Identificar donde se decide hoy cada permiso y cada validacion critica del flujo operativo, distinguiendo entre enforcement backend real, restricciones web, reglas locales en MAUI y comportamientos inconsistentes entre canales.

Preguntas de la fase y respuesta ejecutiva

1. Donde se decide que el camarero puede crear pedidos.

Se decide en backend. La API REST permite crear pedidos a ADMIN, CAMARERO y CAJERO. La web tambien permite acceso a pedidos para CAMARERO, ADMIN y CAJERO. MAUI ademas agrega reglas propias de visibilidad segun pantalla.

2. Donde se decide que el cajero no puede crearlos.

No existe evidencia de una restriccion backend que prohiba a CAJERO crear pedidos. Al contrario, la API REST lo permite expresamente. La idea de que el cajero no debe crear pedidos aparece como decision funcional parcial dentro de MAUI, no como politica canonica del sistema.

3. Si la restriccion vive en backend, en UI o en ambos.

Hoy vive en ambos, pero de forma desigual. El backend si protege varias capacidades criticas. La UI de MAUI agrega reglas extras, negaciones por rol y ocultamiento de botones que no siempre coinciden con backend.

4. Si los permisos se expresan por rol fijo o por capacidad funcional.

Hoy se expresan principalmente por rol fijo. La propuesta es pasar a capacidades funcionales para que la UI consuma una politica clara sin reescribir condiciones en cada pantalla.

5. Si las validaciones criticas fallan correctamente cuando se llama directo a API.

En permisos, la API si tiene puntos fuertes de enforcement. En validaciones funcionales, varias siguen existiendo tambien en cliente, especialmente alrededor de cobro, cuenta y flujo de pedido. Eso indica que hay enforcement backend parcial pero no suficientemente concentrado en casos de uso compuestos.

Resumen ejecutivo

La Fase 2 confirma que el sistema ya tiene una base de seguridad backend valida, pero todavia no tiene una politica funcional unificada entre web, API y MAUI.

Los hallazgos dominantes son estos:

1. Web y API usan seguridad por rol bien definida, pero no por capacidades de negocio.
2. MAUI duplica permisos y validaciones en ViewModels y servicios locales.
3. Varias reglas cliente usan negaciones amplias como no ser CAMARERO o no ser COCINA, en vez de capacidades explicitas.
4. La API permite a CAJERO crear y confirmar pedidos, pero MAUI presenta una experiencia ambigua o contradictoria para ese rol.
5. El cobro en MAUI depende de validaciones fuertes en cliente sobre monto recibido y comprobante, lo que deja demasiada logica operativa fuera del backend.

Modelo actual de autenticacion y enforcement

Web Thymeleaf

1. Usa autenticacion basada en sesion HTTP.
2. La seguridad se define por rutas en SecurityConfig.
3. La sesion web es stateful y mantiene control de acceso por request.

API REST

1. Usa JWT y politica stateless.
2. La seguridad combina reglas por ruta y anotaciones PreAuthorize en controladores.
3. Las transiciones semanticas de pedido tienen control fino en backend.

MAUI

1. Guarda token, usuario y rol en SecureStorage mediante SessionService.
2. Decide shell, navegacion y varias acciones a partir del rol guardado localmente.
3. Repite parte de la politica funcional en ViewModels y servicios.

Matriz de permisos actuales

Regla funcional: entrar al modulo pedidos

Backend web

1. Web permite pedidos a CAMARERO, ADMIN y CAJERO.

Backend API

1. API permite GET de pedidos a ADMIN, CAMARERO, COCINA y CAJERO.

MAUI

1. CAJERO tiene tab Pedidos en su shell.
2. CAMARERO tiene acceso a Pedidos por shell general.

Estado

Ya reforzado en backend, pero con semantica funcional difusa en MAUI para CAJERO.

Regla funcional: crear pedido

Backend web

1. La web permite acceso operativo a pedidos para CAMARERO, ADMIN y CAJERO.

Backend API

1. PedidoControlador.crear permite ADMIN, CAMARERO y CAJERO.

MAUI

1. PosViewModel permite que CAJERO use EnviarPedidoAsync.
2. Para CAJERO, la app decide localmente que el pedido salga directo a EN_COCINA.
3. La accion Enviar a cocina explicita en POS solo se muestra para ADMIN o CAMARERO.

Estado

Inconsistente.

Lectura tecnica

El backend no niega a CAJERO crear pedidos. La UI movil redefine el comportamiento segun rol y genera una politica distinta a la del backend.

Regla funcional: confirmar o enviar pedido a cocina

Backend web

1. Web permite cambios de estado POS a CAMARERO, ADMIN, CAJERO y COCINA en rutas de POS.

Backend API

1. PedidoControlador.confirmar permite ADMIN, CAMARERO y CAJERO.
2. PedidoControlador.preparando permite ADMIN y COCINA.
3. El endpoint generico de cambio de estado revalida por estado solicitado.

MAUI

1. PosViewModel usa PuedeEnviarACocina solo para ADMIN y CAMARERO.
2. MapaViewModel usa una regla mas amplia: pedido pendiente y no ser COCINA.
3. PedidoDetalleViewModel tambien usa una regla amplia: estado pendiente y no ser COCINA.

Estado

Inconsistente y duplicado.

Lectura tecnica

La API ya expresa una politica clara. MAUI introduce tres versiones distintas de la misma regla.

Regla funcional: marcar pedido como listo

Backend web

1. Cocina opera este cambio en su flujo propio.

Backend API

1. PedidoControlador.listo permite ADMIN y COCINA.

MAUI

1. PosViewModel permite ADMIN y COCINA.
2. PedidoDetalleViewModel permite ADMIN y COCINA.

Estado

Mayormente alineado.

Lectura tecnica

Aqui la regla backend y la UI coinciden bastante bien, aunque sigue expresada como if de rol en cliente y no como capacidad reutilizable.

Regla funcional: entregar pedido

Backend web

1. La web lo resuelve desde el flujo operativo de pedidos.

Backend API

1. PedidoControlador.entregado permite ADMIN y CAMARERO.

MAUI

1. PedidosViewModel.EntregarRapidoAsync no hace chequeo de rol local antes de invocar cambio a COMPLETADO.
2. PedidoDetalleViewModel expone PuedeEntregarCliente solo por estado, no por rol.
3. PosViewModel controla entrega por estado del pedido, no por rol.

Estado

Inconsistente y riesgoso en UI.

Lectura tecnica

El backend protege la transicion, pero la UI deja ver o intentar acciones a perfiles que no deberian tenerlas. El enforcement real queda del lado API, pero la experiencia queda incorrecta.

Regla funcional: cancelar pedido

Backend web

1. No se reviso un flujo web detallado de cancelacion en esta fase, pero no aparece como capacidad amplia para todos.

Backend API

1. PedidoControlador.cancelar permite solo ADMIN.

MAUI

1. PedidoDetalleViewModel expone PuedeCancelarPedido solo por estado, sin control de rol.
2. CancelarPedidoAsync termina llamando a cambio generico de estado con CANCELADO.

Estado

Contradictorio.

Lectura tecnica

La API ya define una regla fuerte y MAUI no la replica correctamente en UI.

Regla funcional: listar cuentas

Backend web

1. Web permite cuentas a CAMARERO, ADMIN y CAJERO.

Backend API

1. CuentaControlador permite listar a ADMIN, CAMARERO y CAJERO.

MAUI

1. HistorialCuentasLoadService exige caja abierta cuando el rol es CAJERO antes de continuar.

Estado

Alineado con una condicion operativa adicional en cliente.

Lectura tecnica

La restriccion de caja abierta no esta modelada como autorizacion backend sobre listar cuentas, sino como prerequisito operativo en MAUI.

Regla funcional: cobrar cuenta

Backend web

1. Caja es de ADMIN y CAJERO.

Backend API

1. CuentaControlador.cambiarEstado permite ADMIN y CAJERO para cobrar o cerrar cuenta.

MAUI

1. HistorialCuentasViewModel define PuedeCobrarCuenta como EsCajero y cuenta abierta.
2. MapaViewModel define PuedeCobrar como mesa pendiente de pago y no ser CAMARERO.
3. La vista del mapa ademas invierte la visibilidad de la accion cobrar.

Estado

Inconsistente.

Lectura tecnica

La politica correcta deberia ser capacidad positiva de cobro, no una negacion amplia de rol. La inversion visual en el mapa agrava el problema.

Regla funcional: gestionar caja o turno

Backend web

1. Caja es solo ADMIN y CAJERO.

Backend API

1. La ruta de caja esta restringida a ADMIN y CAJERO.

MAUI

1. TurnoViewModel usa PuedeGestionarCaja para ADMIN o CAJERO.
2. HistorialCuentasLoadService redirige a turnos cuando CAJERO no tiene caja abierta.

Estado

Alineado.

Lectura tecnica

Esta es una de las areas mas coherentes entre backend y movil.

Regla funcional: asignar cliente a cuenta

Backend web

1. La operacion existe en el flujo web de POS y cuentas.

Backend API

1. CuentaControlador.asignarCliente permite ADMIN y CAJERO.

MAUI

1. HistorialCuentasViewModel expone buscador y alta rapida para cuentas.
2. La UI no expresa explicitamente la capacidad por politica funcional, sino por ruta de acceso de cajero.

Estado

Alineado en backend, con expresion difusa en UI.

Regla funcional: agregar pedido a cuenta

Backend web

1. La web lo hace como paso propio del flujo de pedido.

Backend API

1. CuentaControlador.agregarPedido permite ADMIN, CAMARERO y CAJERO.

MAUI

1. OrderWorkflowService intenta asociar el pedido a cuenta despues de crearlo.
2. Si recibe Forbidden, lo oculta y deja continuar el flujo.

Estado

Riesgoso e inconsistente.

Lectura tecnica

La app asume que algunos perfiles podrian crear pedido pero no mutar cuenta y silencia ese problema. Eso debilita la trazabilidad del permiso real y complica el diagnostico funcional.

Matriz de validaciones actuales

Validacion: autenticacion del usuario

1. Web la hace por sesion HTTP.
2. API la hace por JWT y filtros de seguridad.
3. MAUI depende del token almacenado en SecureStorage.

Estado

Centralizada correctamente en backend y transporte.

Validacion: acceso por rol a endpoint

1. API usa reglas por ruta y PreAuthorize.
2. Web usa reglas por ruta.
3. MAUI solo anticipa u oculta acciones.

Estado

Centralizada en backend, aunque la UI la replica de forma imperfecta.

Validacion: monto de cobro

1. PagoValidationService valida monto, faltante y suficiencia de efectivo en cliente.
2. La fase auditada no muestra un caso de uso backend equivalente como fuente unica de verdad para ese control previo.

Estado

Demasiado cargada en UI.

Validacion: comprobante de transferencia

1. MAUI exige foto de comprobante antes de cobrar por transferencia.
2. Esa regla se expresa en cliente mediante PagoValidationService y PagoViewModel.

Estado

Demasiado cargada en UI.

Validacion: caja abierta para operar cobros

1. MAUI la valida en HistorialCuentasLoadService antes de dejar operar a CAJERO.
2. No se observó en esta fase una politica backend equivalente para impedir toda operacion dependiente de caja desde una capacidad funcional comun.

Estado

Parcial y distribuida.

Validacion: permiso de transicion de estado del pedido

1. API si la refuerza por anotaciones y validacion del estado solicitado.
2. MAUI la replica con mensajes y condiciones locales.

Estado

Bien reforzada en backend, pero duplicada en UI.

Donde vive hoy cada tipo de regla

Reglas bien ancladas en backend

1. Autenticacion.
2. Autorizacion por rol de rutas y endpoints.
3. Crear pedido a nivel base.
4. Transiciones semanticas confirmar, listo, entregado y cancelar.
5. Operaciones de caja para ADMIN y CAJERO.

Reglas todavia demasiado distribuidas

1. Quien puede ver o intentar cobrar desde ciertas pantallas.
2. Quien puede enviar a cocina segun pagina.
3. Si el cajero debe o no crear pedidos desde movil.
4. Validacion previa de cobro por metodo de pago.
5. Requisito operativo de caja abierta para ciertos flujos.
6. Asociacion pedido a cuenta como parte del flujo normal.

Permisos funcionales propuestos

En lugar de seguir modelando la UI con if por rol, la politica deberia expresarse con capacidades funcionales reutilizables:

1. pedido.crear
2. pedido.confirmar
3. pedido.marcar-listo
4. pedido.entregar
5. pedido.cancelar
6. cuenta.abrir
7. cuenta.asociar-pedido
8. cuenta.asignar-cliente
9. cuenta.cobrar
10. caja.abrir
11. caja.cerrar
12. caja.operar

Mapeo funcional propuesto por rol

CAMARERO

1. pedido.crear
2. pedido.confirmar
3. pedido.entregar
4. cuenta.abrir
5. cuenta.asociar-pedido

CAJERO

1. cuenta.cobrar
2. cuenta.asignar-cliente
3. caja.abrir
4. caja.cerrar
5. caja.operar
6. segun decision de negocio pendiente: pedido.crear y pedido.confirmar o solo pedido.consultar

COCINA

1. pedido.marcar-listo
2. pedido.preparar

ADMIN

1. todas las capacidades anteriores

Conclusion critica sobre CAJERO

La auditoria no encuentra hoy una fuente de verdad unica que diga que CAJERO no puede crear pedidos. Si esa es la decision funcional deseada para la app movil, debe formalizarse en backend como capacidad funcional o restriccion de caso de uso. Mientras no ocurra, seguir ocultandolo solo en MAUI generara contradicciones y errores de flujo.

Backlog derivado de Fase 2

1. Definir si CAJERO puede o no crear pedidos como politica de negocio oficial.
2. Crear un servicio de capacidades en backend que MAUI pueda consumir o derivar de forma uniforme.
3. Sustituir condiciones negativas de rol en MAUI por capacidades positivas.
4. Mover validaciones criticas de cobro a backend o al menos duplicarlas con enforcement server-side obligatorio.
5. Corregir las vistas que muestran acciones solo por estado y no por capacidad, especialmente entregar, cancelar y cobrar.
6. Eliminar el swallow silencioso de Forbidden en la asociacion pedido-cuenta o al menos registrarlo como incidente funcional.

Veredicto de cierre

La Fase 2 deja una conclusion firme: el sistema ya tiene enforcement backend suficiente para varias reglas de acceso, pero no tiene todavia una politica funcional unificada. El mayor problema no es ausencia de seguridad, sino divergencia entre lo que backend permite, lo que web asume y lo que MAUI oculta, reinterpreta o valida localmente.