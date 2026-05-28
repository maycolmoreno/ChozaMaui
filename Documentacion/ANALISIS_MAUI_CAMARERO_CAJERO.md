Auditoria focal MAUI vs Web/API para CAMARERO y CAJERO

Fecha

28 de mayo de 2026

Objetivo

Determinar si para la app MAUI bastan los roles actuales CAMARERO y CAJERO, que diferencias funcionales existen frente a la web y la API, que modulos estan incompletos, que botones o menus estan mal alineados y que debe corregirse antes de pensar en permisos dinamicos.

Conclusion ejecutiva

Los roles actuales CAMARERO y CAJERO son suficientes para la siguiente etapa del proyecto. El problema principal no es de permisos dinamicos sino de alineacion funcional.

La app MAUI todavia no replica de forma consistente el comportamiento que ya existe en web y API para estos dos roles. Los fallos dominantes son:

1. menus de CAJERO incompletos para operacion diaria;
2. visibilidad de acciones basada en reglas locales dispersas;
3. errores de UI donde una accion se muestra u oculta al reves;
4. flujo de pedido y cuenta todavia orquestado desde cliente en varios pasos;
5. validaciones de cobro y decisiones de estado duplicadas en MAUI;
6. backend y web aun concentran el flujo correcto, mientras MAUI implementa una version parcial.

No hace falta introducir todavia un sistema de permisos dinamicos. Primero hay que dejar estable y coherente el contrato operativo para CAMARERO y CAJERO.

Respuesta corta a la pregunta central

1. Si, los roles actuales bastan.
2. No, el problema actual no exige permisos dinamicos.
3. Si, MAUI necesita correcciones funcionales y de arquitectura antes que un motor nuevo de autorizacion.
4. Si, varias reglas deben centralizarse en backend para evitar que la app vuelva a desviarse de la web.

Diagnostico por rol

CAMARERO

Comportamiento esperado segun web/API

1. Debe operar mesas, clientes, cuentas y pedidos desde el flujo POS.
2. Debe poder crear pedidos y continuar pedidos abiertos.
3. Debe poder asociar cliente y cuenta durante el flujo del pedido.
4. Debe enviar a cocina cuando corresponde.
5. No debe ser el actor principal del cobro.

Estado actual en MAUI

1. Tiene una shell razonable para sala: Mesas, Pedidos, Avisos y Perfil.
2. El flujo de mesas y POS existe, pero no esta alineado del todo con la web.
3. La logica de envio a cocina depende de propiedades locales y no de una politica unica.
4. Comparte shell base con roles que ya no se usaran en MAUI, lo que ensucia decisiones y condiciones.

Diagnostico CAMARERO

1. El menu de CAMARERO no es el principal problema; el problema es la consistencia del flujo.
2. La web sigue siendo la referencia funcional mas completa para crear pedido, separar productos, resolver cuenta y avanzar el estado.
3. MAUI implementa el flujo con demasiada logica local, lo que la vuelve fragil cuando cambia una regla de negocio.

CAJERO

Comportamiento esperado segun web/API

1. Debe abrir y cerrar caja o turno.
2. Debe listar cuentas abiertas y cobradas.
3. Debe cobrar una cuenta con reglas claras por metodo de pago.
4. Debe poder navegar rapidamente entre cuentas pendientes, detalle y cobro.
5. Puede consultar pedidos y cuentas, pero su foco operativo es caja y cobro.

Estado actual en MAUI

1. Tiene shell propia, lo cual es correcto como base.
2. Puede entrar a cuentas, pedidos, avisos y perfil.
3. Tiene redireccion a turnos cuando no existe caja abierta.
4. El flujo de cobro existe, pero depende mucho de validacion local y navegacion indirecta.
5. Le faltan accesos principales de caja en el shell y en la navegacion operativa.

Diagnostico CAJERO

1. Es el rol con mas problemas funcionales visibles en MAUI.
2. El shell actual no refleja bien que caja/turno es su modulo principal.
3. Parte de la operacion existe, pero esta repartida entre HistorialCuentas, Turno y Pago sin una entrada clara y estable.
4. Hay evidencias de botones y tarjetas presentes pero no completamente conectados a un flujo real de caja.

Diferencias concretas entre web y app

1. En web, CAJERO tiene el modulo caja como area explicita; en MAUI, la shell de cajero no expone Turno o Caja como pestana principal.
2. En web, el flujo de pedido correcto esta centralizado en el POS web; en MAUI, POS, mapa y detalle reparten reglas de estado.
3. En web, CAMARERO y CAJERO usan pantallas separadas por modulo; en MAUI, parte del comportamiento depende de banderas locales en ViewModels.
4. En web, la caja es una unidad operacional visible; en MAUI, la caja aparece mas como requisito previo o pantalla secundaria.
5. En web, el POS integra cliente, cuenta y pedido como flujo continuo; en MAUI, la asociacion pedido-cuenta todavia presenta huecos y acoplamiento a respuestas parciales del backend.

Modulos incompletos o mal alineados en MAUI

Modulo caja/turno de CAJERO

1. El shell de cajero no prioriza caja como modulo central.
2. Turno existe, pero no esta representado como entrada primaria del rol.
3. En la pantalla de turno hay tarjetas visuales como Arqueo, Movimientos y Clientes sin un flujo operativo equivalente al peso que la web les da.

Modulo cobros/cuentas

1. HistorialCuentas depende de que exista caja abierta y redirige a turnos, pero la navegacion queda fragmentada.
2. El boton COBRAR aparece por estado de cuenta, no por una politica de capacidad centralizada.
3. Las reglas de pago viven con demasiado detalle en la app.

Modulo pedido para CAMARERO

1. Crear pedido existe, pero no esta respaldado por un caso de uso atomico equivalente al flujo correcto de la web.
2. El avance de estado aparece repartido entre mapa, POS, detalle y listado.
3. La logica de quien puede enviar a cocina o cobrar no esta unificada.

Botones y menus faltantes o mal resueltos

1. CAJERO necesita una entrada principal visible a Turno o Caja desde su shell, no solo llegar por redireccion.
2. La accion Cobrar cuenta en el mapa presenta una inversion de visibilidad: se enlaza con un inversor sobre PuedeCobrar, por lo que visualmente puede aparecer cuando no deberia y ocultarse cuando si deberia.
3. El shell de CAJERO da visibilidad a Pedidos, pero no deja claro si ese modulo es consulta, apoyo operativo o creacion completa.
4. Turno muestra tarjetas de Arqueo, Movimientos y Clientes, pero en el estado auditado no se aprecia la misma completitud funcional que si existe en web.
5. La app no presenta una navegacion unificada de caja similar a la web para apertura, seguimiento, cobro y cierre.

Errores de flujo detectados

1. La accion de cobrar en el mapa depende de una regla amplia basada en no ser CAMARERO, cuando deberia depender de una capacidad positiva y centralizada.
2. En el mapa, la visibilidad de la accion de cobrar parece invertida por uso de InvertBoolConverter sobre PuedeCobrar.
3. El flujo de cobro se valida fuertemente en cliente, incluyendo monto recibido, faltante y comprobante, lo que deja reglas de negocio fuera del backend.
4. HistorialCuentas carga todas las cuentas y solo despues aplica filtros locales; eso sirve para UI, pero no consolida el caso de uso real de cajero.
5. El flujo de pedido sigue dependiendo de varias llamadas y decisiones cliente, en lugar de un endpoint de orquestacion unico.
6. La transicion entre cuenta abierta, pedido asociado y cobro no esta modelada en MAUI con la misma solidez que en web.

Problemas de arquitectura

1. La app usa reglas de rol dispersas en NavigationService, MapaViewModel, PosViewModel, PedidoDetalleViewModel, PedidosViewModel, HistorialCuentasViewModel y servicios auxiliares.
2. Varias decisiones usan negaciones de rol como no ser CAMARERO o no ser COCINA en vez de capacidades explicitas.
3. La UI decide demasiado sobre estados, cobro y transiciones.
4. La web sigue orquestando mejor el flujo funcional, lo que indica que el backend aun no expresa todos los casos de uso en endpoints semanticos y atomicos.
5. Mientras CAMARERO y CAJERO dependan de reglas locales en MAUI, cualquier cambio de negocio obligara a tocar varias pantallas y varios ViewModels.

Que debe centralizarse en backend

1. Crear pedido completo con resolucion de cuenta y asociacion pedido-cuenta como una sola operacion de negocio.
2. Reglas para permitir envio a cocina, cobro, entrega y cancelacion segun estado y rol.
3. Reglas de cobro por metodo de pago, especialmente transferencia y cierre de cuenta.
4. Estado operacional de caja abierta para habilitar o bloquear acciones de cajero.
5. Capacidades efectivas por rol para que la app consuma un contrato simple y no replique condiciones.

Fases recomendadas de correccion

Fase 1. Corregir navegacion y shell de CAJERO

1. Hacer de Turno o Caja una entrada principal visible en AppShellCajero.
2. Definir si Pedidos para CAJERO sera lectura, apoyo operativo o flujo completo.
3. Reordenar la navegacion para que cuentas, turno y cobro formen una ruta operativa clara.

Fase 2. Corregir errores de visibilidad y botones

1. Corregir la visibilidad de Cobrar cuenta en el mapa.
2. Sustituir reglas negativas de rol por propiedades de capacidad positivas.
3. Revisar todas las acciones de enviar a cocina, entregar, cobrar y cerrar mesa para que dependan del mismo criterio.

Fase 3. Unificar casos de uso de pedido y cobro

1. Reducir la cantidad de decisiones hechas en ViewModels.
2. Llevar al backend la orquestacion de pedido con cuenta.
3. Llevar al backend la validacion final de cobro y cierre de cuenta.

Fase 4. Limpiar MAUI para solo dos perfiles reales

1. Eliminar condiciones residuales pensadas para ADMIN y COCINA dentro de la experiencia MAUI.
2. Dejar a CAMARERO orientado a sala y a CAJERO orientado a caja.
3. Mantener el backend con la matriz completa de roles, pero simplificar la UI movil a esos dos perfiles.

Fase 5. Evaluar permisos dinamicos solo si aparece una necesidad real

Solo despues de estabilizar CAMARERO y CAJERO tendria sentido evaluar permisos dinamicos. Eso seria util si el negocio necesita escenarios como:

1. cajeros senior con acciones extra;
2. meseros con restricciones por sede o turno;
3. activacion o desactivacion remota de modulos sin nueva version de app;
4. menus parametrizables por sucursal.

Si esa necesidad no existe todavia, introducir permisos dinamicos ahora solo agregaria complejidad.

Prioridades inmediatas

1. Corregir el shell de CAJERO y dar acceso principal a caja/turno.
2. Corregir la visibilidad de Cobrar cuenta en el mapa.
3. Unificar reglas de capacidad para CAMARERO y CAJERO en un solo servicio o politica.
4. Diseñar endpoints backend mas atomicos para pedido con cuenta y cobro.
5. Revalidar la experiencia completa de CAMARERO y CAJERO contra la web antes de abrir nuevas fases.

Veredicto final

Para MAUI no hace falta crear ahora un sistema dinamico de permisos. Con CAMARERO y CAJERO alcanza, siempre que:

1. la app movil se simplifique realmente a esos dos perfiles;
2. se corrijan menus, botones y flujos incompletos;
3. la logica operativa se mueva progresivamente al backend;
4. la web se use como referencia funcional hasta cerrar la brecha.

El problema actual no es falta de flexibilidad en autorizacion. El problema es falta de consistencia funcional entre cliente movil, web y backend.