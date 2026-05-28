Fase 1. Paso 2

Matriz de trazabilidad del flujo de pedidos entre web, API y MAUI

Fecha

28 de mayo de 2026

Objetivo

Comparar el flujo maestro de pedidos entre ConsumoChoza web, la API REST de pisip y ChozaMaui para clasificar que partes ya estan centralizadas, que partes existen solo en web y que partes MAUI consume o reimplementa localmente.

Hipotesis de trabajo validada

El backend expone piezas importantes del flujo de pedidos, pero no el caso de uso completo crear pedido con cuenta, cliente y transicion operacional como una sola operacion. Por eso la web orquesta el flujo en su controlador y MAUI reconstruye parte de la logica mediante servicios propios.

Resumen ejecutivo

El flujo de pedidos todavia no esta centralizado de punta a punta. El estado actual queda asi:

1. La API si centraliza la creacion basica de pedido y varias transiciones de estado.
2. La web sigue orquestando la resolucion real del flujo operativo completo.
3. MAUI consume endpoints validos, pero recompone localmente resolucion de cuenta, carga de contexto POS, seleccion de cliente y parte de las reglas de accion.
4. No existe evidencia de un endpoint backend unico tipo crearPedidoConCuenta para reemplazar la orquestacion web y movil.

Criterio de clasificacion usado

1. Ya centralizado.
2. Solo existe en Thymeleaf.
3. Existe en backend pero no expuesto por API.
4. Existe en API pero no consumido por MAUI.
5. Reimplementado en MAUI.
6. Incompleto o inconsistente.

Matriz de trazabilidad

Paso 1. Seleccionar mesa

Web

1. La vista POS precarga mesas, mesas disponibles y mesas ocupadas.
2. La seleccion de mesa dispara consulta de cuentas abiertas y pedidos activos.

API

1. Existen GET /api/mesas y GET /api/mesas/disponibles.
2. El PedidoRequestMapper resuelve la mesa por id al crear el pedido.
3. No aparece en esta revision un endpoint REST semantico que garantice reservar o validar mesa libre dentro del mismo caso de uso de pedido.

MAUI

1. PosDataService carga mesas y pedidos en paralelo para armar la vista del POS.
2. MapaViewModel y PosDataService construyen el estado visual de la mesa en cliente con listas de pedidos activos.

Clasificacion

Incompleto o inconsistente.

Lectura tecnica

La consulta de mesa existe en API, pero la decision operacional de si la mesa esta libre o puede continuar flujo no esta encapsulada en un caso de uso unico. Web y MAUI reconstruyen esa lectura con datos agregados.

Paso 2. Validar mesa libre

Web

1. Usa mesasOcupadasIds, cuentas abiertas y pedidos activos para prevalidar la mesa antes del submit.
2. La validacion visible nace en la UI web.

API

1. Hay listados de mesas disponibles y ocupadas.
2. No se observo un endpoint especifico para validar disponibilidad de mesa al momento de crear el pedido como parte de una operacion atomica.

MAUI

1. Infiere el estado operativo a partir de mesas mas pedidos activos cargados desde cliente.
2. No se ve una validacion atomica separada antes de submit; la app depende de la foto actual del estado.

Clasificacion

Incompleto o inconsistente.

Lectura tecnica

El backend ofrece informacion, pero no una proteccion de flujo equivalente a crear pedido validando disponibilidad de mesa en la misma operacion.

Paso 3. Abrir o resolver cuenta

Web

1. PedidosControlador decide si usa cuenta existente o nueva.
2. Si falla la cuenta elegida, busca otra abierta por mesa y cliente.
3. Si tampoco existe, crea una nueva.

API

1. Existen GET /api/cuentas/abiertas, GET /api/cuentas/{id}, GET /api/cuentas/mesa/{idMesa}/abierta y POST /api/cuentas.
2. Existen piezas para resolver la cuenta, pero no un endpoint unificado que cree el pedido con la cuenta ya resuelta.

MAUI

1. PedidoCuentaWorkflowService reproduce la misma estrategia: usa cuenta actual, luego cuentas abiertas por mesa y cliente, y si no encuentra crea una nueva.
2. OrderWorkflowService llama esa resolucion despues de crear el pedido.

Clasificacion

Reimplementado en MAUI.

Lectura tecnica

Este es uno de los puntos mas claros de duplicacion. La API expone piezas sueltas y MAUI recompone localmente un caso de uso que deberia vivir centralizado en backend.

Paso 4. Registrar o asociar cliente

Web

1. Permite buscar cliente y crearlo rapido desde POS.
2. Tiene validaciones de UI y fallback local con clientes precargados.

API

1. Existen GET /api/clientes y POST /api/clientes.
2. No se detecto un endpoint especializado de busqueda rapida o creacion rapida para POS equivalente a los auxiliares web revisados.

MAUI

1. PosClientService obtiene todos los clientes y arma la seleccion con ActionSheet local.
2. La creacion rapida de cliente se reconstruye en cliente mediante prompts y POST generico.

Clasificacion

Reimplementado en MAUI.

Lectura tecnica

La capacidad base existe en API, pero la experiencia POS de seleccion rapida y alta rapida no esta modelada como caso de uso especifico. Tanto web como MAUI agregan comportamiento local.

Paso 5. Agregar productos

Web

1. Precarga catalogo, categorias y stock.
2. Arma el carrito y hidden inputs en JavaScript.
3. Hace prevalidacion fuerte de stock y cantidad.

API

1. La creacion de pedido recibe PedidoRequestDTO con detalles.
2. El mapper de request resuelve mesa y cliente y convierte detalles a dominio.
3. Hay evidencia de servicios de gestion de stock en backend, lo que indica validacion de negocio del lado servidor al crear pedido.

MAUI

1. PosDataService carga categorias y productos activos.
2. PosViewModel maneja carrito y bloquea agregar si stock actual es 0 o si se supera stock visible.

Clasificacion

Ya centralizado, pero con prevalidacion duplicada en UI.

Lectura tecnica

El backend parece tener enforcement real de stock, pero tanto web como MAUI siguen duplicando validaciones para UX. Eso es aceptable si la regla critica queda efectivamente en servidor.

Paso 6. Calcular importes y estado inicial

Web

1. Calcula total visible en cliente.
2. El controlador decide particion de pedidos y luego suma totales para pasos posteriores.

API

1. PedidoControlador crea el pedido via POST /api/pedidos.
2. El request mapper fija estado inicial PENDIENTE al construir el agregado.
3. El response mapper calcula totales del pedido en DTO de salida.

MAUI

1. PosViewModel calcula total del carrito para UX local.
2. OrderWorkflowService puede crear y luego mover estado con otra llamada.
3. La app decide estado destino segun flujo local antes o despues del submit.

Clasificacion

Incompleto o inconsistente.

Lectura tecnica

El estado inicial base esta centralizado en API, pero la logica operacional de cuando un pedido queda pendiente, enviado o necesita otra transicion sigue repartida entre clientes.

Paso 7. Confirmar pedido

Web

1. El POST /pedidos/guardar orquesta validaciones y llamadas sucesivas.
2. La confirmacion funcional no sale como un solo caso de uso backend compuesto.

API

1. Existe POST /api/pedidos para crear.
2. Existe PATCH /api/pedidos/{id}/confirmar para enviar a cocina y notificar.

MAUI

1. OrderWorkflowService crea el pedido y opcionalmente cambia estado con una segunda llamada.
2. PosViewModel apoya ese flujo mediante servicios intermedios, no mediante un endpoint unico de confirmacion completa con cuenta.

Clasificacion

Existe en API pero no consumido por MAUI de forma completa.

Lectura tecnica

La API ya tiene transicion semantica confirmar, pero MAUI todavia opera con secuencia crear mas cambiar estado mas resolver cuenta. El flujo funcional sigue fragmentado.

Paso 8. Enviar a cocina

Web

1. La web compone el paso despues de crear el pedido dentro de su propio flujo POS.

API

1. Existe PATCH /api/pedidos/{id}/confirmar para ADMIN, CAMARERO y CAJERO.
2. Tambien existe PATCH /api/pedidos/{id}/estado como endpoint de compatibilidad.

MAUI

1. MapaViewModel envia a cocina con CambiarEstadoPedidoAsync y no con el endpoint semantico confirmar.
2. PosViewModel usa reglas locales de rol para decidir si mostrar la accion.

Clasificacion

Existe en API pero no consumido por MAUI de forma coherente.

Lectura tecnica

La API ya expresa una accion de negocio concreta, pero MAUI sigue apoyandose en cambio generico de estado y reglas locales de visibilidad.

Paso 9. Refrescar estado en interfaces consumidoras

Web

1. Reconsulta datos auxiliares del POS y listas ligadas a mesa, cuentas y pedidos activos.

API

1. PedidoControlador emite notificaciones en confirmar, preparando, listo, entregado y cancelar.

MAUI

1. MapaViewModel consume notificaciones y polling por topics operativos.
2. PosDataService y MapaViewModel tambien reconstruyen estado visual a partir de pedidos activos.

Clasificacion

Ya centralizado, pero con presentacion local reconstruida.

Lectura tecnica

La API ya provee el canal de cambio de estado, pero la composicion de la vista sigue ocurriendo en cliente, lo que es normal mientras no se convierta en logica de negocio.

Huecos estructurales encontrados en Fase 1

1. No existe evidencia de un caso de uso backend unico para crear pedido con resolucion de cuenta y asociacion transaccional.
2. Web y MAUI resuelven cuenta con la misma estrategia en cliente o capa de aplicacion local.
3. MAUI reimplementa seleccion y alta rapida de cliente con prompts y listados completos, no con un flujo POS especifico de backend.
4. MAUI usa endpoints genericos de cambio de estado donde la API ya ofrece transiciones semanticas.
5. La validacion operacional de mesa libre sigue siendo una composicion de datos, no un contrato backend atomico de pedido.
6. El flujo completo sigue siendo crear pedido, luego resolver cuenta, luego agregar pedido a cuenta, luego cambiar estado, en vez de una sola operacion de negocio.

DTOs y contratos observados

Ya existentes

1. PedidoRequestDTO y PedidoResponseDTO.
2. CuentaRequestDTO y CuentaResponseDTO.
3. ClienteRequestDTO y ClienteResponseDTO.
4. MesaResponseDTO.
5. CambiarEstadoRequestDTO.

Faltantes o insuficientes para el flujo maestro

1. DTO de crear pedido con cuenta resuelta dentro de la misma operacion.
2. DTO orientado a POS para cliente rapido o busqueda rapida.
3. DTO o endpoint que resuelva disponibilidad operativa de mesa dentro del submit de pedido.
4. DTO de respuesta compuesto para que MAUI no tenga que recomponer pedido, cuenta y estado posterior con varias llamadas.

Clasificacion final del flujo maestro

1. Ya centralizado: creacion basica de pedido, DTOs base de pedido, notificaciones por cambio de estado.
2. Solo existe en Thymeleaf: la orquestacion detallada del flujo POS web y la particion previa del pedido mixto observada en el controlador web.
3. Existe en API pero no consumido por MAUI: confirmar pedido como endpoint semantico y varias piezas de cuenta que MAUI recompone localmente.
4. Reimplementado en MAUI: resolucion de cuenta, seleccion y alta rapida de cliente, composicion de estado visual para operar el POS.
5. Incompleto o inconsistente: validacion de mesa libre como contrato operativo, confirmacion transaccional de pedido con cuenta y uso coherente de transiciones semanticas.

Conclusion de cierre de Fase 1

La Fase 1 confirma que pedidos sigue siendo el mejor flujo para detectar el problema estructural del sistema. El backend ya tiene piezas valiosas, pero todavia no manda sobre el flujo completo. La web opera como ensamblador principal y MAUI como segundo ensamblador parcial. Mientras no exista un caso de uso backend compuesto para crear y confirmar pedido con cuenta, cliente, validaciones criticas y estado resultante, la divergencia entre canales va a seguir creciendo.

Siguiente paso recomendado

Entrar en Fase 2 para mapear permisos y validaciones sobre los mismos puntos de ruptura detectados aqui: crear pedido, confirmar, agregar a cuenta, cobrar y mostrar u ocultar acciones por rol.