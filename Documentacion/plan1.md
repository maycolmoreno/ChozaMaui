Plan de auditoria y centralizacion de logica de negocio

Objetivo general

Centralizar la logica de negocio del sistema en backend para que web Thymeleaf, API REST y app .NET MAUI ejecuten exactamente las mismas reglas funcionales, permisos, validaciones y flujos operativos.

Resultado esperado

Al terminar este trabajo debe existir:

1. Un diagnostico completo.
2. Una matriz de diferencias entre Thymeleaf, API REST y MAUI.
3. Una lista priorizada de huecos funcionales y tecnicos.
4. Una propuesta de arquitectura con backend como fuente de verdad.
5. Un plan de refactorizacion por fases antes de tocar codigo.

Principios de trabajo

1. No mover logica de negocio a UI.
2. No duplicar reglas entre web, API y MAUI.
3. Todo permiso debe expresarse como capacidad funcional, no como condicion hardcodeada en pantalla.
4. Toda validacion critica debe vivir en backend.
5. MAUI debe consumir flujos completos, no reconstruir reglas de negocio localmente.
6. Thymeleaf y API deben apoyarse en los mismos servicios de aplicacion.

Alcance funcional prioritario

1. Pedidos.
2. Mesas.
3. Cuentas.
4. Pagos.
5. Clientes.
6. Roles y permisos.
7. Flujo de cocina.

Pregunta central a responder

Que partes de la logica que hoy funcionan en Thymeleaf no estan correctamente expuestas, reutilizadas o consumidas por la API REST y por MAUI.

Definicion de exito

Se considerara exitoso el plan cuando, para cada flujo critico, exista evidencia de que:

1. La regla de negocio vive en backend.
2. El flujo esta disponible mediante servicio reutilizable.
3. Existe endpoint suficiente para el cliente que lo necesite.
4. MAUI no reimplementa la logica central.
5. Los permisos son consistentes entre web y movil.
6. Las validaciones criticas no dependen de la UI.
7. El flujo completo puede trazarse de controlador a servicio, DTO, repositorio y pantalla consumidora.

Artefactos obligatorios de la auditoria

La auditoria debe producir estos entregables:

1. Inventario de modulos y responsables funcionales.
2. Matriz de trazabilidad por caso de uso.
3. Catalogo de endpoints y sus huecos.
4. Catalogo de permisos actuales y permisos funcionales propuestos.
5. Lista de logica duplicada en MAUI.
6. Lista de logica acoplada a Thymeleaf.
7. Lista de DTOs faltantes o inconsistentes.
8. Lista de pantallas, botones, acciones y navegaciones incompletas en MAUI.
9. Lista de riesgos tecnicos y operativos.
10. Roadmap de refactorizacion por fases.

Orden de ejecucion

Fase 0. Preparacion y delimitacion

Objetivo:
Definir el mapa real del sistema antes de comparar comportamientos.

Tareas:

1. Identificar que modulo contiene hoy la logica canonica del negocio.
2. Separar claramente:
  a. Web Thymeleaf.
  b. API REST.
  c. App MAUI.
3. Identificar controladores, servicios, DTOs, repositorios y mecanismos de autenticacion por modulo.
4. Identificar si existe uso de sesion HTTP, cache de sesion o estado local en MAUI.
5. Confirmar actores funcionales reales:
  a. camarero;
  b. cajero;
  c. cocina;
  d. administrador;
  e. otros roles existentes.

Entregable:
Mapa del sistema y delimitacion de responsabilidades actuales.

Criterio de cierre:
Debe quedar claro donde vive hoy cada flujo critico y que modulo manda sobre cada regla.

Fase 1. Auditoria del flujo de pedidos de punta a punta

Objetivo:
Usar pedidos como flujo maestro para descubrir huecos estructurales.

Caso principal:
Crear pedido desde MAUI comparado contra flujo web que ya funciona.

Subflujo obligatorio a revisar:

1. Seleccionar mesa.
2. Validar mesa libre.
3. Abrir cuenta.
4. Registrar o asociar cliente.
5. Agregar productos.
6. Calcular importes y estados.
7. Confirmar pedido.
8. Enviar a cocina.
9. Refrescar estado en interfaces consumidoras.

Analizar por cada paso:

1. Controlador Thymeleaf implicado.
2. Servicio backend implicado.
3. Repositorio y entidad implicados.
4. Endpoint REST equivalente o faltante.
5. DTO de entrada y salida existente o faltante.
6. ViewModel MAUI implicado.
7. Pagina MAUI implicada.
8. Accion visual existente o faltante.
9. Validacion backend y validacion UI.
10. Permiso requerido.

Entregable:
Matriz de trazabilidad del flujo de pedidos con diferencias exactas entre web, API y MAUI.

Criterio de cierre:
Cada paso del flujo debe estar clasificado como:

1. Ya centralizado.
2. Solo existe en Thymeleaf.
3. Existe en backend pero no expuesto por API.
4. Existe en API pero no consumido por MAUI.
5. Reimplementado en MAUI.
6. Incompleto o inconsistente.

Fase 2. Auditoria de permisos y validaciones

Objetivo:
Eliminar decisiones de negocio basadas en UI, rol hardcodeado o flujo informal.

Preguntas a resolver:

1. Donde se decide que el camarero puede crear pedidos.
2. Donde se decide que el cajero no puede crearlos.
3. Si la restriccion vive en backend, en UI o en ambos.
4. Si los permisos se expresan por rol fijo o por capacidad funcional.
5. Si las validaciones criticas fallan correctamente cuando se llama directo a API.

Analizar:

1. Anotaciones de seguridad.
2. Filtros o interceptores.
3. Uso de sesion HTTP.
4. Condiciones en controladores.
5. Condiciones en servicios.
6. Condiciones en ViewModels o Pages MAUI.
7. Botones ocultos por rol en UI.

Entregable:
Matriz de permisos actuales versus permisos funcionales propuestos.

Criterio de cierre:
Toda regla critica debe tener un punto de enforcement backend identificable.

Fase 3. Auditoria de mesas, cuentas, clientes, pagos y cocina

Objetivo:
Extender el mismo metodo del flujo de pedidos al resto de dominios prioritarios.

Dominios:

1. Mesas.
2. Cuentas.
3. Clientes.
4. Pagos.
5. Cocina.

Para cada dominio revisar:

1. Casos de uso soportados en web.
2. Casos de uso soportados en API.
3. Casos de uso soportados en MAUI.
4. Botones y acciones disponibles por actor.
5. Navegacion y pantallas necesarias.
6. Estados de negocio y transiciones.
7. Validaciones y mensajes de error.
8. Datos requeridos por DTO.
9. Dependencias con otros modulos.

Entregable:
Tabla de cobertura funcional por dominio con huecos priorizados.

Criterio de cierre:
Cada dominio debe tener lista clara de faltantes clasificados por backend, API, DTO, UI, navegacion o permiso.

Fase 4. Consolidacion de hallazgos

Objetivo:
Transformar observaciones sueltas en diagnostico tecnico util para refactorizar.

Clasificar cada hallazgo como uno de estos tipos:

1. Logica de negocio acoplada a Thymeleaf.
2. Logica faltante en API REST.
3. Logica duplicada en MAUI.
4. Permiso rigido por rol.
5. Endpoint faltante.
6. DTO faltante o inconsistente.
7. Pantalla incompleta.
8. Boton o accion faltante.
9. Navegacion truncada.
10. Validacion inexistente o mal ubicada.
11. Sincronizacion inconsistente entre modulos.

Entregable:
Backlog priorizado con severidad, impacto y accion recomendada.

Priorizacion:

1. Bloquea operacion.
2. Permite errores de negocio.
3. Rompe consistencia entre canales.
4. Afecta seguridad o permisos.
5. Afecta UX pero no la integridad funcional.

Fase 5. Arquitectura objetivo

Objetivo:
Definir como debe quedar el sistema antes de tocar implementacion.

Arquitectura propuesta:

1. Backend como fuente unica de verdad.
2. Servicios de aplicacion reutilizables para web y API.
3. Controladores Thymeleaf delgados sin reglas de negocio propias.
4. Controladores REST delgados apoyados en los mismos servicios.
5. DTOs definidos por caso de uso y no por conveniencia de pantalla.
6. Permisos funcionales centralizados.
7. MAUI como consumidor de flujos backend.
8. UI responsable solo de presentacion, captura de datos, navegacion y feedback.

Que debe vivir en backend:

1. Reglas de apertura de cuenta.
2. Validacion de mesa disponible.
3. Creacion y actualizacion de pedido.
4. Transiciones de estado.
5. Reglas de cocina.
6. Reglas de pago y cierre.
7. Permisos funcionales.
8. Validaciones criticas.
9. Consistencia transaccional.

Que puede vivir en UI:

1. Navegacion.
2. Orden visual.
3. Confirmaciones de usuario.
4. Mensajes y feedback.
5. Estados temporales de formulario.
6. Cache local no autoritativa.

Entregable:
Documento de arquitectura objetivo con principios, capas y responsabilidades.

Fase 6. Plan de refactorizacion por fases

Objetivo:
Definir la secuencia segura de cambios despues de la auditoria.

Fase de implementacion A. Backend comun

1. Extraer o consolidar servicios de aplicacion compartidos.
2. Mover reglas de negocio que sigan en controladores web.
3. Consolidar validaciones de negocio.
4. Consolidar permisos funcionales.

Fase de implementacion B. API REST completa

1. Exponer endpoints faltantes por caso de uso.
2. Normalizar contratos DTO.
3. Dejar respuestas consistentes para MAUI.

Fase de implementacion C. Ajuste de MAUI

1. Eliminar logica duplicada.
2. Consumir nuevos endpoints.
3. Completar navegacion y pantallas faltantes.
4. Habilitar acciones segun permisos funcionales reales.

Fase de implementacion D. Alineacion de web

1. Hacer que Thymeleaf use los mismos servicios centralizados.
2. Eliminar condiciones especiales fuera de servicios.
3. Homogeneizar validaciones y estados.

Fase de implementacion E. Validacion integral

1. Comparar flujos web y MAUI contra los mismos casos de uso.
2. Verificar permisos por actor.
3. Verificar estados y efectos colaterales.
4. Verificar que API y UI no permitan saltar reglas.

Formato obligatorio de hallazgos

Cada hallazgo debe documentarse asi:

1. Dominio.
2. Caso de uso.
3. Flujo web actual.
4. Flujo API actual.
5. Flujo MAUI actual.
6. Diferencia exacta.
7. Riesgo.
8. Causa probable.
9. Archivo o archivos implicados.
10. Accion recomendada.
11. Prioridad.

Lista minima de archivos a inspeccionar

Backend Java:

1. Controladores Thymeleaf.
2. Controladores REST.
3. Services.
4. DTOs.
5. Repositorios.
6. Configuracion de seguridad.
7. Entidades y estados de dominio.

MAUI:

1. ViewModels.
2. Views.
3. Servicios de navegacion.
4. Servicios de pedidos, mesas, cuentas, pagos y clientes.
5. Coordinadores de flujo.
6. Validaciones locales.
7. Reglas visibles por boton o comando.

Preguntas que la auditoria debe contestar sin ambiguedad

1. Que logica de negocio sigue atrapada en Thymeleaf.
2. Que logica backend no esta expuesta por API.
3. Que flujo existe en web pero no en MAUI.
4. Que botones o acciones faltan en MAUI.
5. Que interfaces o pantallas estan incompletas.
6. Que permisos estan hardcodeados.
7. Que validaciones dependen de la UI.
8. Que DTOs hacen falta.
9. Que endpoints hacen falta.
10. Que reglas deben centralizarse primero.

Plan inmediato de ejecucion

Paso 1:
Mapear el flujo de crear pedido en web Thymeleaf extremo a extremo.

Paso 2:
Mapear el flujo REST equivalente.

Paso 3:
Mapear el flujo MAUI equivalente.

Paso 4:
Comparar diferencias exactas paso por paso.

Paso 5:
Auditar permisos del caso camarero versus cajero.

Paso 6:
Documentar huecos de endpoint, DTO, UI, boton, permiso y validacion.

Paso 7:
Extender el mismo metodo a mesas, cuentas, pagos, clientes y cocina.

Paso 8:
Redactar diagnostico final y backlog de refactor.

Regla de control

No implementar cambios hasta terminar las fases de auditoria, consolidacion de hallazgos y arquitectura objetivo.
