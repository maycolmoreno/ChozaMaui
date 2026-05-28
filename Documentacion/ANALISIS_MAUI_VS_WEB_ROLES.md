Auditoria funcional MAUI vs ConsumoChoza

Fecha

28 de mayo de 2026

Objetivo

Determinar si el problema principal actual exige un sistema dinamico de permisos o si primero debe corregirse la alineacion funcional entre ConsumoChoza, la API REST y ChozaMaui.

Conclusion ejecutiva

No parece necesario introducir todavia un sistema dinamico de permisos. El problema dominante hoy no es la falta de un motor complejo de autorizacion, sino la desalineacion entre:

1. lo que web y API permiten por rol;
2. lo que MAUI muestra en menus y shells;
3. lo que MAUI habilita o bloquea mediante logica local dispersa;
4. la cantidad de reglas funcionales que siguen viviendo en cliente.

Con los cuatro roles actuales ADMIN, CAMARERO, CAJERO y COCINA se puede cubrir el sistema actual si primero se corrige:

1. navegacion y shells por rol;
2. visibilidad de botones y comandos;
3. reglas locales inconsistentes en ViewModels;
4. flujos incompletos en pedidos, cobro, cocina y usuarios;
5. centralizacion de reglas de negocio en backend.

Diagnostico principal

Diagnostico 1. Los roles actuales son suficientes para el estado actual del negocio

La API REST ya expresa una matriz de permisos bastante clara:

1. ADMIN administra todo.
2. CAMARERO opera pedidos, cuentas, mesas y clientes.
3. CAJERO opera caja, cobros, cuentas y parte de pedidos.
4. COCINA opera estados de preparacion y consulta pedidos.

El problema no es que falten roles. El problema es que MAUI no replica de forma consistente las capacidades, menus y restricciones de esos mismos roles.

Diagnostico 2. MAUI usa solo dos shells, no un menu dinamico real

ChozaMaui no tiene un sistema de menu dinamico basado en capacidades. Tiene una seleccion binaria de shell:

1. CAJERO usa AppShellCajero.
2. Todos los demas usan AppShell.

Eso significa que:

1. ADMIN, CAMARERO y COCINA comparten el mismo menu base aunque no tengan las mismas funciones.
2. La visibilidad fina no se resuelve por menu sino por comandos y propiedades dispersas en ViewModels.
3. El sistema no esta roto por falta de permisos dinamicos sino por una arquitectura de UI demasiado gruesa para diferenciar roles operativos.

Diagnostico 3. MAUI duplica reglas de rol y de negocio en demasiados puntos

Se detecta logica de rol en:

1. NavigationService.
2. PerfilViewModel.
3. MapaViewModel.
4. PosViewModel.
5. PedidosViewModel.
6. PedidoDetalleViewModel.
7. MesaDetalleViewModel.
8. TurnoViewModel.
9. HistorialCuentasViewModel.
10. HistorialCuentasLoadService.
11. OrderWorkflowService.

El efecto practico es este:

1. la UI no se gobierna desde una politica unica;
2. aparecen inconsistencias entre pantalla y pantalla;
3. una misma accion puede estar permitida en una vista y bloqueada en otra;
4. la diferencia entre permiso visible y permiso real depende del archivo, no del sistema.

Diagnostico 4. Parte del problema esta en MAUI, pero otra parte esta en la arquitectura web/API actual

ConsumoChoza sigue resolviendo parte de la orquestacion del negocio en la capa web. En el flujo de pedidos ya quedo evidenciado que la web compone pasos como:

1. validacion previa de stock;
2. separacion bar y cocina;
3. resolucion de cuenta;
4. asociacion pedido-cuenta;
5. pago al inicio.

Mientras eso siga asi, MAUI siempre tendera a quedar por detras de la web aunque tenga el mismo rol.

Comparacion por rol

ADMIN en web/API

1. Gestiona usuarios, categorias, productos, mesas, clientes, caja, cuentas, pedidos y cocina.
2. Puede ver y modificar entidades de configuracion.
3. Puede intervenir en estados de pedido y caja.
4. Puede acceder a reportes.

ADMIN en MAUI

1. Usa AppShell general con Mesas, Pedidos, Avisos y Perfil.
2. Puede enviar a cocina desde POS.
3. Puede marcar listo junto con cocina.
4. Puede cerrar mesa desde MesaDetalle.
5. Puede cobrar por varias rutas de UI.
6. No tiene un modulo visible de usuarios.
7. No tiene UI propia de reportes.
8. Comparte shell con camarero y cocina, sin separacion funcional completa.

Diferencia clave:

ADMIN en backend es claramente superusuario. ADMIN en MAUI es un operador extendido, no un verdadero administrador funcional completo.

CAMARERO en web/API

1. Accede a pedidos, cuentas, clientes, mesas y POS.
2. Puede crear pedidos.
3. Puede asociar pedidos a cuenta.
4. Puede consultar clientes y crear clientes.
5. Puede operar flujo de sala.

CAMARERO en MAUI

1. Comparte AppShell con ADMIN y COCINA.
2. Puede operar mesas y pedidos.
3. Puede enviar a cocina desde PosViewModel.
4. No puede cobrar en varias rutas, lo cual es coherente con el objetivo general.
5. Tiene acceso a tabs que tambien ven cocina y admin, aunque no necesariamente a las mismas acciones.

Diferencia clave:

El menu base es aceptable, pero la logica de acciones depende de propiedades locales y no de una politica central. Eso vuelve fragil el comportamiento.

CAJERO en web/API

1. Tiene acceso a caja, cuentas y cobros.
2. Puede abrir y cerrar caja.
3. Puede operar cuentas y pagos.
4. En API tambien puede consultar y crear pedidos, y cambiar ciertos estados.
5. En web accede a pedidos, cuentas, caja y POS.

CAJERO en MAUI

1. Usa AppShellCajero con Cuentas, Pedidos, Avisos y Perfil.
2. Tiene el mejor aislamiento de menu de toda la app.
3. Puede abrir y cerrar turno.
4. Puede cobrar cuentas y registrar pagos.
5. Puede entrar a pedidos.
6. El flujo lo redirige a turnos cuando necesita caja abierta.

Diferencia clave:

Es el rol mejor modelado en MAUI a nivel de shell. Aun asi, hay reglas de pago y cobro metidas en cliente y acciones de pedidos que necesitan limpieza.

COCINA en web/API

1. Accede a cocina y a consulta de productos.
2. Puede preparar y marcar pedidos listos.
3. No deberia operar caja ni cobros.
4. No deberia operar administracion de usuarios.

COCINA en MAUI

1. Comparte AppShell con ADMIN y CAMARERO.
2. Entra por Pedidos, no por una shell especifica de cocina.
3. Puede marcar listo en varias pantallas.
4. No deberia enviar pedidos a cocina, pero algunas reglas cliente usan negacion de rol en vez de permiso positivo.
5. Puede terminar viendo rutas de cobro segun la pantalla, aunque backend probablemente las niegue.

Diferencia clave:

COCINA es el rol peor representado visualmente. No tiene shell o menu especializado y varias restricciones dependen de if locales dispersos.

Comparacion por modulo

Modulo Pedidos

Web:

1. Es el modulo mas completo.
2. Tiene POS web funcional, lista, detalle, cocina y cambios de estado.
3. La web compone parte importante del flujo.

API:

1. Tiene endpoints para listar, crear, actualizar y cambiar estado.
2. Tiene rutas semanticas como confirmar, listo, entregado y cancelar.
3. Tiene control de acceso por rol.

MAUI:

1. Tiene Mapa, POS, lista de pedidos y detalle.
2. Hay multiples caminos para cambiar estados.
3. Hay inconsistencia entre vistas para decidir quien puede enviar a cocina o cobrar.
4. Parte del flujo de pedido y cuenta sigue siendo secuencial en cliente.

Problemas detectados:

1. PuedeEnviarACocina no se expresa igual en Mapa, POS y Detalle.
2. Hay decisiones de estado ligadas al rol del cliente.
3. El flujo crear pedido sigue siendo de varias llamadas, no un caso de uso atomico.

Modulo Mesas

Web:

1. Gestion completa, especialmente para admin.
2. Relacion fuerte con cuentas y pedidos.

API:

1. Consulta de mesas para admin, camarero y cajero.
2. Escritura solo admin.

MAUI:

1. Mapa es la entrada principal para admin, camarero y parte de cocina.
2. MesaDetalle permite ver pedidos y cerrar mesa.
3. Cerrar mesa queda limitado a admin en cliente.

Problemas detectados:

1. El modelo de mesa sigue muy acoplado al flujo de pedido activo.
2. La accion cobrar se muestra por una regla local basada en no ser camarero, lo que es demasiado amplia.

Modulo Clientes

Web:

1. Busqueda y creacion rapida funcional desde POS.
2. Gestion completa para admin.

API:

1. GET y POST para admin, camarero y cajero.
2. PUT y DELETE solo admin.

MAUI:

1. Puede seleccionar y crear cliente rapido desde flujo POS.
2. No tiene un modulo visible de clientes como area independiente.
3. No replica una gestion administrativa completa.

Problemas detectados:

1. Cliente existe solo como subflujo del pedido o cuenta, no como modulo consistente.
2. Parte de la validacion de cliente sigue en cliente.

Modulo Caja

Web:

1. Caja separada y coherente para admin y cajero.
2. Integrada al flujo operativo real.

API:

1. Apertura, caja abierta, cierre y listado solo admin y cajero.

MAUI:

1. TurnoViewModel y HistorialCuentasViewModel cubren el caso principal.
2. Shell separada para cajero funciona bien.
3. Los reportes del dia se reutilizan dentro del dashboard de turno.

Problemas detectados:

1. Las validaciones de cobro, cambio y comprobante estan muy cargadas en cliente.
2. No todo el flujo de caja esta expresado como capacidad backend reutilizable.

Modulo Cocina

Web:

1. Tiene rutas y KDS mas claras.
2. Rol cocina esta mejor representado como espacio propio.

API:

1. Tiene confirmaciones y cambios de estado semanticos.
2. Seguridad por rol para cocina y admin.

MAUI:

1. Cocina entra por Pedidos del shell general.
2. No tiene shell dedicada ni tablero claramente especializado.
3. Se apoya en filtros y botones dentro de las mismas pantallas compartidas con otros roles.

Problemas detectados:

1. Cocina esta funcionalmente soportada, pero pobremente modelada en UI.
2. Comparte demasiado con otros roles.

Modulo Usuarios

Web:

1. Admin dispone de CRUD y configuracion.
2. Cambio de contraseña y setup inicial integrados.

API:

1. Login publico.
2. Cambio de contraseña autenticado.
3. CRUD de usuarios para admin.

MAUI:

1. Login y cambio de contraseña.
2. No hay CRUD de usuarios.
3. No hay listado ni administracion visible.

Problemas detectados:

1. El modulo usuarios en MAUI esta incompleto si se pretende soporte administrativo real.
2. Si no se busca administracion completa en movil, esto debe definirse expresamente como fuera de alcance y no como deuda ambigua.

Menus, pantallas y navegacion

Menus actuales en MAUI

AppShell:

1. Mesas.
2. Pedidos.
3. Avisos.
4. Perfil.

AppShellCajero:

1. Cuentas.
2. Pedidos.
3. Avisos.
4. Perfil.

Evaluacion:

1. No existe menu dinamico por capacidades.
2. Solo existe conmutacion entre dos shells fijas.
3. Eso alcanza para distinguir cajero del resto, pero no para diferenciar bien admin, camarero y cocina.

Pantallas MAUI presentes

1. LoginPage.
2. MapaPage.
3. PosPage.
4. PedidosPage.
5. PedidoDetallePage.
6. MesaDetallePage.
7. HistorialCuentasPage.
8. PagoPage.
9. TurnoPage.
10. NotificacionesPage.
11. PerfilPage.

Pantallas faltantes o incompletas frente a web

1. Administracion de usuarios.
2. Reportes como modulo visible.
3. Configuracion de productos, categorias, mesas y clientes como modulo administrativo completo.
4. Un espacio de cocina verdaderamente separado.

Botones y acciones con problemas

Problema 1. Cobrar se habilita de forma demasiado amplia

MapaViewModel usa una regla tipo no camarero para habilitar cobro. Eso incluye roles que no deberian cobrar solo por descarte.

Problema 2. Enviar a cocina no usa una politica consistente

PosViewModel usa permiso positivo admin o camarero.
MapaViewModel y PedidoDetalleViewModel usan bloqueo por no cocina o variantes similares. Eso no es equivalente.

Problema 3. Ir a pagar desde detalle tambien esta abierto de forma amplia

PedidoDetalleViewModel permite navegar a pago a cualquiera que no sea camarero. De nuevo, la regla esta modelada por exclusion y no por capacidad real.

Problema 4. Turno y cuentas estan bien aislados, pero dependen de navegacion secundaria

HistorialCuentas redirige a turnos si hace falta caja abierta. Eso es correcto funcionalmente, pero la coordinacion esta repartida entre ViewModel y servicio, no desde una politica central.

Problema 5. Menu de cocina insuficiente

COCINA comparte la misma shell con roles de sala. Falta una experiencia mas cercana al KDS web.

Lado backend y necesidad de permisos dinamicos

Lo que ya existe y alcanza por ahora

1. Seguridad por rol en pisip.
2. Endpoints semanticos de pedidos.
3. Restricciones claras para caja, usuarios, clientes, cuentas y pedidos.
4. Roles bien definidos para el negocio actual.

Lo que falta antes de pensar en permisos dinamicos

1. Un mapa unico de capacidades de UI por rol.
2. Quitar ifs de rol dispersos de los ViewModels.
3. Mover reglas de negocio de cliente a backend o a una capa de orquestacion controlada.
4. Decidir que modulos realmente deben existir en movil y cuales solo en web.

Determinacion solicitada

1. Los roles actuales son suficientes.

Si el negocio actual sigue siendo ADMIN, CAMARERO, CAJERO y COCINA, no hay evidencia de que haga falta introducir nuevos roles o un modelo dinamico para resolver el problema inmediato.

2. No parece necesario un sistema dinamico de permisos como primer paso.

Primero hay que alinear MAUI con las capacidades ya existentes. De otro modo, un sistema dinamico solo agregara complejidad sobre una base inconsistente.

3. Lo que esta mal implementado hoy en MAUI.

1. Menus insuficientemente diferenciados entre admin, camarero y cocina.
2. Reglas de rol duplicadas y contradictorias entre pantallas.
3. Acciones habilitadas por descarte de rol en lugar de capacidad positiva.
4. Modulos administrativos faltantes o ambiguos.
5. Logica funcional y validaciones comerciales en cliente.
6. Flujos repartidos entre varias capas cliente en vez de un caso de uso estable.

4. Lo que falta replicar desde ConsumoChoza.

1. Mejor representacion del rol cocina.
2. Cobertura funcional administrativa de usuarios y otros modulos si se desea en movil.
3. Flujos completos donde la web hoy resuelve mejor la secuencia operativa.
4. Consistencia en visibilidad de acciones segun rol.

5. Lo que debe corregirse primero.

1. Crear una matriz unica rol -> menus -> pantallas -> acciones.
2. Centralizar capacidades de UI en un PermissionService o CapabilityService local simple.
3. Reemplazar comparaciones directas de rol en ViewModels por capacidades declarativas.
4. Corregir acciones mal habilitadas, especialmente cobro y enviar a cocina.
5. Definir que modulos administrativos no van a existir en MAUI para no tratarlos como errores abiertos.
6. Mover reglas como split bar/cocina, resolucion de cuenta y validaciones criticas al backend.

6. Arquitectura recomendada para evolucionar sin romper lo actual.

Fase objetivo recomendada:

1. Backend como fuente de verdad para permisos funcionales y reglas de negocio.
2. MAUI con un servicio local de capacidades basado en los roles actuales.
3. ViewModels sin comparar strings de rol directamente.
4. Endpoints semanticos en backend para transiciones y casos compuestos.
5. Si a futuro aparecen combinaciones nuevas de privilegios o roles custom, entonces si introducir permisos dinamicos descargados desde backend.

Recomendacion arquitectonica

Recomendacion 1. No saltar aun a permisos dinamicos

Implementarlos ahora encubriria problemas de modelado actual y aumentaria la superficie de errores.

Recomendacion 2. Introducir un mapa de capacidades, no un sistema complejo

Algo como:

1. ViewMesa.
2. CreatePedido.
3. SendPedidoCocina.
4. MarkPedidoReady.
5. CobrarCuenta.
6. ManageCaja.
7. ManageUsuarios.

Y luego mapear esos permisos a los roles actuales desde una sola capa.

Recomendacion 3. Centralizar primero en backend los casos compuestos

Especialmente:

1. crear pedido con resolucion de cuenta;
2. crear pedido y enviar a cocina;
3. cobrar cuenta con validaciones de metodo y comprobante.

Recomendacion 4. Tratar MAUI como consumidor fino

La UI debe decidir navegacion y presentacion. El backend debe decidir las reglas finales de negocio.

Plan de correccion por fases

Fase A. Alineacion funcional MAUI

1. Inventariar acciones por rol y pantalla.
2. Corregir menus y rutas visibles.
3. Corregir botones mal habilitados.
4. Unificar reglas de rol en un servicio de capacidades.

Fase B. Limpieza de logica cliente

1. Sacar validaciones comerciales de los ViewModels.
2. Reducir decisiones de negocio en cliente.
3. Unificar transiciones de pedido con los endpoints semanticos de pisip.

Fase C. Cierre de brechas funcionales

1. Definir si usuarios y reportes deben existir en movil.
2. Si deben existir, implementarlos.
3. Si no deben existir, documentarlo y excluirlos del alcance movil.

Fase D. Centralizacion backend

1. Mover casos compuestos hoy orquestados por web o cliente.
2. Reducir diferencias entre Thymeleaf y MAUI.
3. Exponer capacidades o metadatos de accion si luego se necesitan.

Fase E. Evolucion opcional a permisos dinamicos

Solo si despues de estabilizar:

1. aparecen permisos especiales dentro de un mismo rol;
2. un mismo usuario puede tener combinaciones variables;
3. la administracion exige cambiar capacidades sin desplegar app.

Archivos clave revisados

Backend web/API:

1. consumochoza/src/main/java/com/choza/consumochoza/config/SecurityConfig.java
2. consumochoza/src/main/java/com/choza/consumochoza/controlador/PedidosControlador.java
3. consumochoza/src/main/java/com/choza/consumochoza/controlador/PosApiControlador.java
4. pisip/src/main/java/com/lachozag4/pisip/infraestructura/configuracion/SeguridadConfig.java
5. pisip/src/main/java/com/lachozag4/pisip/presentacion/controladores/PedidoControlador.java
6. pisip/src/main/java/com/lachozag4/pisip/presentacion/controladores/CajaControlador.java
7. pisip/src/main/java/com/lachozag4/pisip/presentacion/controladores/UsuarioControlador.java

MAUI:

1. ChozaMaui/Views/AppShell.xaml
2. ChozaMaui/Views/AppShellCajero.xaml
3. ChozaMaui/Views/AppRoutes.cs
4. ChozaMaui/Services/NavigationService.cs
5. ChozaMaui/ViewModels/PerfilViewModel.cs
6. ChozaMaui/ViewModels/MapaViewModel.cs
7. ChozaMaui/ViewModels/PosViewModel.cs
8. ChozaMaui/ViewModels/PedidosViewModel.cs
9. ChozaMaui/ViewModels/PedidoDetalleViewModel.cs
10. ChozaMaui/ViewModels/MesaDetalleViewModel.cs
11. ChozaMaui/ViewModels/TurnoViewModel.cs
12. ChozaMaui/ViewModels/HistorialCuentasViewModel.cs
13. ChozaMaui/Services/OrderWorkflowService.cs
14. ChozaMaui/Services/PedidoApiService.cs
15. ChozaMaui/Services/UsuarioApiService.cs
