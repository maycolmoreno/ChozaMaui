Fase 1. Paso 1

Mapeo del flujo web Thymeleaf para crear pedido

Fecha

28 de mayo de 2026

Objetivo

Trazar el flujo de crear pedido en la web desde la pantalla Thymeleaf hasta los servicios y endpoints backend que ejecutan cada paso del caso de uso.

Resumen ejecutivo

El flujo web de crear pedido no es un simple submit de formulario. La vista Thymeleaf de POS arma y valida una parte importante del proceso en cliente, mientras que PedidosControlador orquesta varias operaciones en secuencia:

1. valida mesa, cliente y productos;
2. valida stock preliminar;
3. crea uno o dos pedidos segun si hay mezcla cocina y bar;
4. abre o reutiliza cuenta;
5. asocia cada pedido creado a la cuenta;
6. opcionalmente registra pago al inicio.

La ejecucion real del negocio no queda en una sola llamada backend compuesta. La web compone el flujo mediante varias llamadas separadas a la API.

Entrada principal del flujo

Pantalla de entrada:

1. Vista Thymeleaf Pedido/POS.
2. Ruta web GET /pedidos/nuevo.
3. Controlador de entrada PedidosControlador.mostrarFormularioNuevo.

Funcion de inicializacion del modelo

El GET /pedidos/nuevo crea un PedidoDTO vacio y llama a prepararModeloPos. Esa rutina carga al modelo todos los datos necesarios para operar el POS web:

1. usuario logueado;
2. usuarios activos;
3. todas las mesas;
4. mesas disponibles;
5. comedores activos;
6. mesas agrupadas por comedor;
7. clientes;
8. productos activos;
9. categorias activas;
10. cuentas abiertas;
11. ids de mesas ocupadas.

Esto confirma que la web precarga gran parte del contexto de operacion antes del submit.

Formulario principal

La vista POS envia un formulario POST a /pedidos/guardar con estos datos base:

1. idpedido.
2. idUsuario.
3. idMesa.
4. idCliente.
5. idCuentaSeleccionada.
6. modoCuenta.
7. modoCobro.
8. metodoPagoInicio.
9. referenciaPagoInicio.
10. observaciones.
11. detalles[i].idProducto.
12. detalles[i].cantidad.
13. detalles[i].precioUnitario.

Construccion del pedido en cliente

La vista construye el pedido usando JavaScript local:

1. el carrito se maneja en memoria en la variable carrito;
2. al renderizar, el JS genera hidden inputs detalles[i].*;
3. las notas por producto no viajan como detalle estructurado;
4. las notas de items se concatenan en observacionesGlobal antes del submit;
5. el total visible se calcula en cliente para UX.

Esto implica que la estructura enviada al backend ya llega transformada por la UI web.

Subflujo 1. Seleccionar mesa

Implementacion web:

1. el usuario selecciona una mesa en el plano del restaurante;
2. seleccionarMesa actualiza el hidden idMesa;
3. la UI marca la mesa seleccionada y habilita el panel de cliente;
4. la UI recarga cuentas abiertas y consulta pedidos activos de la mesa.

Datos y validaciones usadas:

1. la disponibilidad visual se basa en mesasOcupadasIds;
2. mesasOcupadasIds mezcla dos fuentes:
   a. mesas con estado false;
   b. mesas con cuenta abierta;
3. la validacion de submit exige que idMesa no sea 0.

Llamadas implicadas:

1. GET inicial a mesaService.listarTodas.
2. GET inicial a mesaService.listarDisponibles.
3. AJAX GET /api/pos/cuentas/abiertas.
4. AJAX GET /api/pos/mesas/{idMesa}/pedidos-activos.

Lectura tecnica:

La web no se limita a confiar en una sola validacion backend de mesa libre. Ya toma decisiones previas con estado de mesa, cuentas abiertas y pedidos activos.

Subflujo 2. Seleccionar o registrar cliente

Implementacion web:

1. el usuario puede buscar cliente existente;
2. el usuario puede crear cliente rapido desde modal;
3. al seleccionar, el JS actualiza el hidden idCliente.

Busqueda de cliente:

1. AJAX GET /api/pos/clientes/buscar?termino=...;
2. si falla, usa fallback local con clientes precargados en el modelo.

Creacion rapida de cliente:

1. validacion local de nombre, cedula y telefono;
2. AJAX POST /api/pos/clientes/crear-rapido;
3. tras crear, la UI selecciona automaticamente el cliente retornado.

Lectura tecnica:

Aqui ya existe logica duplicada entre servidor y cliente potencialmente peligrosa:

1. validacion de cedula en JS;
2. validacion de telefono en JS;
3. deteccion local de cliente duplicado usando clientesData.

Subflujo 3. Seleccionar cuenta

Implementacion web:

1. la vista maneja dos modos: EXISTENTE o NUEVA;
2. al seleccionar mesa, la UI consulta cuentas abiertas y muestra las de esa mesa;
3. si hay cuenta abierta, por defecto intenta usar una existente;
4. si no hay, fuerza nueva cuenta.

Llamadas implicadas:

1. AJAX GET /api/pos/cuentas/abiertas;
2. en submit, si idCuentaSeleccionada existe, el controlador intenta obtenerla;
3. si no existe o no es valida, el controlador busca una cuenta abierta por mesa y cliente;
4. si tampoco existe, el controlador crea una nueva.

Lectura tecnica:

La decision final sobre cuenta no la resuelve una sola operacion backend atomica. La resuelve PedidosControlador con logica propia de seleccion y fallback.

Subflujo 4. Agregar productos

Implementacion web:

1. el catalogo se carga con productos activos;
2. el JS controla stock visual y bloqueo de incrementos;
3. el carrito permite notas por item;
4. el submit exige al menos un producto.

Llamadas implicadas:

1. GET inicial productoService.listarActivos;
2. AJAX GET /api/pos/productos/activos para refrescar stock.

Validaciones locales:

1. no deja agregar si stock es 0;
2. no deja subir cantidad sobre stock actual;
3. no deja enviar si algun item excede stock;
4. concatena notas al campo observaciones.

Lectura tecnica:

La web hace una prevalidacion fuerte de stock y composicion del pedido antes de llegar al backend.

Subflujo 5. Guardado del pedido

Punto de entrada:

1. POST /pedidos/guardar.
2. Handler PedidosControlador.guardar.

Validaciones servidor web previas a la API:

1. mesa obligatoria;
2. cliente obligatorio;
3. al menos un detalle;
4. validacion preliminar de stock en pedidos nuevos.

Preparacion:

1. si fecha no viene, asigna LocalDateTime.now;
2. si es pedido nuevo, prepara lista pedidosAProcesar.

Particion cocina y bar:

1. obtiene categorias y detecta la categoria BAR;
2. separa detalles en detallesBar y detallesCocina;
3. si el pedido es mixto, crea dos PedidoDTO clonados;
4. si no es mixto, procesa uno solo.

Creacion efectiva:

1. por cada PedidoDTO a procesar llama pedidoService.crear;
2. eso termina en POST /pedidos del backend REST.

Lectura tecnica:

La web decide dividir un pedido mixto en dos pedidos distintos antes de enviarlo a backend. Esa es una regla de flujo importante y hoy esta fuera del backend canonico, al menos desde lo observado en este paso.

Subflujo 6. Apertura o reutilizacion de cuenta

Despues de crear el o los pedidos, PedidosControlador ejecuta esta secuencia:

1. determina si modoCuenta es NUEVA o EXISTENTE;
2. si hay idCuentaSeleccionada, intenta obtener esa cuenta;
3. valida que la cuenta seleccionada siga ABIERTA;
4. si no sirve, busca una cuenta abierta con la misma mesa y cliente;
5. si no encuentra, crea una nueva cuenta.

Servicios usados:

1. cuentaService.obtenerPorId.
2. cuentaService.listarAbiertas.
3. cuentaService.crearCuenta.

Endpoints implicados:

1. GET /cuentas/{id}.
2. GET /cuentas/abiertas.
3. POST /cuentas.

Lectura tecnica:

La apertura o reutilizacion de cuenta esta orquestada en la web, no encapsulada en un caso de uso backend unico tipo crearPedidoConCuenta.

Subflujo 7. Asociacion pedido-cuenta

Una vez resuelta la cuenta:

1. por cada pedido creado se llama cuentaService.agregarPedido;
2. eso ejecuta POST /cuentas/{idCuenta}/pedidos/{idPedido}.

Consecuencia:

Crear pedido y asociarlo a cuenta no es una unica transaccion visible desde la web. Son operaciones separadas y secuenciales.

Subflujo 8. Pago al inicio

Si modoCobro es AL_INICIO:

1. el controlador suma el total de los pedidos creados;
2. toma username del Authentication actual;
3. usa metodo y referencia ingresados en UI;
4. llama pagoService.registrarPago.

Endpoint implicado:

1. POST /cuentas/{idCuenta}/pagos.

Lectura tecnica:

El pago al inicio se resuelve como un paso posterior a crear pedido y cuenta, no como una operacion de negocio compuesta en backend.

Subflujos AJAX auxiliares del POS web

Durante la operacion del POS, la web tambien consume estos endpoints propios:

1. GET /api/pos/clientes/buscar.
2. POST /api/pos/clientes/crear-rapido.
3. GET /api/pos/mesas/{idMesa}/pedidos-activos.
4. GET /api/pos/cuentas/abiertas.
5. GET /api/pos/productos/activos.
6. GET /api/pos/ventas-hoy.
7. PATCH /api/pos/pedidos/{id}/estado.

Observacion importante:

Estos endpoints no son la API canonica de pisip sino endpoints REST auxiliares expuestos desde consumochoza para soportar el POS web.

Mapa de delegacion servicio web -> endpoint backend

Pedidos:

1. pedidoService.listarTodos -> GET /pedidos.
2. pedidoService.listarConFiltros -> GET /pedidos/paginado.
3. pedidoService.obtenerPorId -> GET /pedidos/{id}.
4. pedidoService.crear -> POST /pedidos.
5. pedidoService.actualizar -> PUT /pedidos/{id}.
6. pedidoService.cambiarEstado -> PATCH /pedidos/{id}/estado.

Cuentas:

1. cuentaService.listarAbiertas -> GET /cuentas/abiertas.
2. cuentaService.obtenerPorId -> GET /cuentas/{id}.
3. cuentaService.crearCuenta -> POST /cuentas.
4. cuentaService.agregarPedido -> POST /cuentas/{idCuenta}/pedidos/{idPedido}.

Clientes:

1. clienteService.listarTodos -> GET /clientes.
2. clienteService.crear -> POST /clientes.

Mesas:

1. mesaService.listarTodas -> GET /mesas.
2. mesaService.listarDisponibles -> GET /mesas/disponibles.

Productos:

1. productoService.listarActivos -> GET /productos/activos.

Usuarios:

1. usuarioService.buscarPorUsername -> GET /usuarios/por-username/{username}.
2. usuarioService.listarTodos -> GET /usuarios.

Pagos:

1. pagoService.registrarPago -> POST /cuentas/{idCuenta}/pagos.

Reglas y comportamiento que hoy viven en la web

1. decidir que una mesa esta ocupada combinando estado de mesa y existencia de cuenta abierta;
2. precargar y filtrar clientes localmente como fallback;
3. validar formato de cedula y telefono en cliente;
4. detectar duplicado de cliente en cliente usando clientesData;
5. validar stock preliminar en JS y nuevamente en controlador web;
6. concatenar notas de items al campo observaciones;
7. dividir pedido mixto en dos pedidos separados por categoria BAR;
8. decidir si se reutiliza o crea cuenta;
9. secuenciar crear pedido, crear cuenta, asociar pedido y cobrar.

Hallazgos tempranos para la auditoria

Hallazgo 1. La web compone el caso de uso completo

El flujo crear pedido no esta encapsulado en una unica operacion backend. La web hace orquestacion importante.

Hallazgo 2. Existe una API POS intermedia en consumochoza

La vista no consume solo servicios MVC. Tambien depende de endpoints /api/pos/** propios de consumochoza, lo que agrega otra capa de comportamiento entre Thymeleaf y pisip.

Hallazgo 3. La logica de pedido mixto parece acoplada a la web

La separacion cocina versus bar se hace dentro de PedidosControlador antes de llamar a pedidoService.crear.

Hallazgo 4. Crear pedido con cuenta no es atomico

Hoy se ejecuta como varias llamadas secuenciales. Eso puede dejar inconsistencias si una etapa falla despues de otra.

Hallazgo 5. La validacion de stock esta duplicada

Existe validacion en JS, validacion previa en controlador web y probablemente validacion posterior en backend API.

Hallazgo 6. Crear cliente rapido depende de validaciones locales

La UI ya decide formato y duplicidad aparente antes del POST.

Riesgos directos detectados en este paso

1. divergencia entre web y MAUI si MAUI no replica exactamente la secuencia crear pedido -> cuenta -> pago;
2. inconsistencias transaccionales si falla la asociacion a cuenta o el pago tras haber creado el pedido;
3. imposibilidad de afirmar que el backend es la unica fuente de verdad mientras el split bar/cocina y la resolucion de cuenta sigan en la web;
4. duplicacion de reglas de validacion entre cliente JS, controlador web y API.

Archivos trazados en este paso

1. consumochoza/src/main/java/com/choza/consumochoza/controlador/PedidosControlador.java
2. consumochoza/src/main/resources/templates/Pedido/POS.html
3. consumochoza/src/main/java/com/choza/consumochoza/controlador/PosApiControlador.java
4. consumochoza/src/main/java/com/choza/consumochoza/service/impl/PedidoServiceImpl.java
5. consumochoza/src/main/java/com/choza/consumochoza/service/impl/CuentaServiceImpl.java
6. consumochoza/src/main/java/com/choza/consumochoza/service/impl/PagoServiceImpl.java
7. consumochoza/src/main/java/com/choza/consumochoza/service/impl/ClienteServiceImpl.java
8. consumochoza/src/main/java/com/choza/consumochoza/service/impl/MesaServiceImpl.java
9. consumochoza/src/main/java/com/choza/consumochoza/service/impl/ProductoServiceImpl.java
10. consumochoza/src/main/java/com/choza/consumochoza/service/impl/UsuarioServiceImpl.java
11. consumochoza/src/main/java/com/choza/consumochoza/modelo/dto/PedidoDTO.java
12. consumochoza/src/main/java/com/choza/consumochoza/modelo/dto/PedidoDetalleDTO.java

Salida del paso 1

Queda mapeado el flujo web Thymeleaf de crear pedido de punta a punta. El siguiente paso de la Fase 1 es trazar el flujo equivalente en la API REST y comprobar si existe una operacion backend que realmente concentre estas reglas o si la web esta cubriendo huecos funcionales del backend.