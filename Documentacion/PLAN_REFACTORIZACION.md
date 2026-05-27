# PLAN DE REFACTORIZACIÓN — LA CHOZA
> Generado tras auditoría técnica completa. 22/05/2026.
> Marcar ✅ cada ítem al completarlo.

---

## FASE 1 — CRÍTICA (bugs y seguridad antes de producción)

### F1-1 — Bug: mesa no se libera al pagar desde MAUI
**Archivo:** `pisip/.../aplicacion/casosuso/impl/PagoUseCaseImpl.java`
**Problema:** `registrarPago` marca la cuenta como PAGADA pero nunca libera la mesa.
La app MAUI llama `CerrarCuentaAsync` justo después, pero `CuentaUseCaseImpl.cambiarEstado`
lanza BusinessException "ya está cerrada" → mesa queda ocupada para siempre.

**Solución:** Inyectar `IMesaRepositorio` en `PagoUseCaseImpl` y liberar la mesa
al mismo tiempo que se cierra la cuenta.

```java
// 1. Agregar dependencia en el constructor
private final IMesaRepositorio mesaRepositorio;

public PagoUseCaseImpl(IPagoRepositorio pagoRepositorio,
                       ICuentaRepositorio cuentaRepositorio,
                       IUsuarioRepositorio usuarioRepositorio,
                       ICajaTurnoRepositorio cajaTurnoRepositorio,
                       IMesaRepositorio mesaRepositorio) {  // ← agregar
    this.pagoRepositorio       = pagoRepositorio;
    this.cuentaRepositorio     = cuentaRepositorio;
    this.usuarioRepositorio    = usuarioRepositorio;
    this.cajaTurnoRepositorio  = cajaTurnoRepositorio;
    this.mesaRepositorio       = mesaRepositorio;         // ← agregar
}

// 2. En el bloque que marca la cuenta como PAGADA (buscar "ESTADO_PAGADA")
//    agregar DESPUÉS de cuentaRepositorio.actualizar(pagada):
if (cuenta.getFkMesa() != null) {
    mesaRepositorio.buscarPorId(cuenta.getFkMesa().getIdmesa())
        .ifPresent(m -> mesaRepositorio.actualizar(m.conEstado(true)));
}
```

**También actualizar** `ConfiguracionGeneral.java` para pasar `mesaRepositorio`
al construir `PagoUseCaseImpl`:
```java
// Buscar el @Bean de pagoUseCase y agregar el argumento:
@Bean IPagoUseCase pagoUseCase() {
    return new PagoUseCaseImpl(
        pagoRepositorio(), cuentaRepositorio(),
        usuarioRepositorio(), cajaTurnoRepositorio(),
        mesaRepositorio()   // ← agregar
    );
}
```

**Estado:** ✅

---

### F1-2 — Bug: contadores de cuentas siempre 0 en MAUI
**Archivo:** `ChozaMaui/ViewModels/HistorialCuentasViewModel.cs`
**Problema:** `RecalcularEstadisticas` filtra `c.Estado == "CERRADA"` pero el backend
devuelve `"PAGADA"` para cuentas cobradas → `CuentasCerradas` y `TotalFacturado` = 0 siempre.

**Cambio exacto** (buscar el método `RecalcularEstadisticas`):
```csharp
// ANTES:
CuentasCerradas = _todas.Count(c => c.Estado == "CERRADA");
TotalFacturado  = _todas.Where(c => c.Estado == "CERRADA").Sum(c => c.Total);

// DESPUÉS:
CuentasCerradas = _todas.Count(c => c.Estado == "PAGADA");
TotalFacturado  = _todas.Where(c => c.Estado == "PAGADA").Sum(c => c.Total);
```

**Estado:** ✅

---

### F1-3 — Seguridad: CORS abierto a todos los orígenes
**Archivo:** `pisip/.../infraestructura/seguridad/SeguridadConfig.java`
**Problema:** Hay tanto `setAllowedOrigins(origins)` como `setAllowedOriginPatterns(List.of("*"))`.
El patrón `*` sobreescribe la lista específica → CORS efectivamente sin restricciones.

**Cambio exacto** (eliminar la línea del patrón wildcard):
```java
// ELIMINAR esta línea:
config.setAllowedOriginPatterns(List.of("*"));

// DEJAR solo:
config.setAllowedOrigins(origins);
// (y asegurarse de que `origins` en application.properties tiene los valores correctos)
```

**Estado:** ✅

---

### F1-4 — Seguridad: Swagger público en producción
**Archivo:** `pisip/.../infraestructura/seguridad/SeguridadConfig.java`
**Problema:** `/swagger-ui/**` y `/v3/api-docs/**` son accesibles sin autenticación,
exponiendo toda la superficie de la API.

**Cambio exacto:**
```java
// ANTES:
.requestMatchers("/swagger-ui/**", "/v3/api-docs/**").permitAll()

// DESPUÉS (solo ADMIN puede ver Swagger):
.requestMatchers("/swagger-ui/**", "/v3/api-docs/**").hasRole("ADMIN")
```
> Nota: si en desarrollo necesitas acceso sin login, usa un perfil Spring (`@Profile("dev")`).

**Estado:** ✅

---

### F1-5 — Seguridad: GlobalExceptionHandler expone detalles internos
**Archivo:** `pisip/.../presentacion/excepciones/GlobalExceptionHandler.java`
**Problema:** El handler de excepciones genéricas devuelve `ex.getMessage()` al cliente
(puede revelar nombres de tablas, queries SQL, stack traces).

**Cambio exacto** (buscar el handler de `Exception.class`):
```java
// ANTES:
return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
    .body("Error interno del servidor: " + ex.getMessage());

// DESPUÉS:
log.error("Error interno no controlado", ex);  // log privado
return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
    .body("Error interno del servidor. Contacte al administrador.");
```
> Asegurarse de que la clase tiene `private static final Logger log = LoggerFactory.getLogger(...)`.

**Estado:** ✅

---

### F1-6 — Seguridad: JWT secret con fallback predecible
**Archivo:** `pisip/src/main/resources/application.properties`
**Problema:** `jwt.secret=${JWT_SECRET:change-this-secret-with-at-least-32-characters}`
Si la variable de entorno `JWT_SECRET` no está definida, el sistema arranca con un secreto conocido.

**Cambio exacto:**
```properties
# ANTES:
jwt.secret=${JWT_SECRET:change-this-secret-with-at-least-32-characters}

# DESPUÉS (sin fallback — falla explícitamente si no está configurado):
jwt.secret=${JWT_SECRET}
```
> En el entorno de desarrollo, definir `JWT_SECRET` en el archivo `.env` o en la configuración
> de run de IntelliJ/VS Code. NUNCA commitear el valor real.

**Estado:** ✅

---

## FASE 2 — IMPORTANTE (deuda técnica de alto impacto)

### F2-1 — N+1: `listarTodos()` en cada cambio de estado de pedido
**Archivo:** `pisip/.../aplicacion/casosuso/impl/PedidoUseCaseImpl.java` +
            `pisip/.../dominio/repositorios/IPedidoRepositorio.java` +
            `pisip/.../infraestructura/repositorios/IPedidoJpaRepositorio.java` +
            `pisip/.../infraestructura/persistencia/adaptadores/PedidoRepositorioImpl.java`

**Problema:** En `cambiarEstado`, se llama `repositorio.listarTodos()` solo para
comprobar si una mesa tiene otros pedidos activos. Con miles de pedidos históricos,
carga todo en memoria.

**Paso 1 — Agregar método al puerto de dominio:**
```java
// IPedidoRepositorio.java — agregar:
boolean existePedidoActivoPorMesa(int idMesa, int excluirIdPedido);
```

**Paso 2 — Agregar query al repositorio JPA:**
```java
// IPedidoJpaRepositorio.java — agregar:
@Query("""
    SELECT CASE WHEN COUNT(p) > 0 THEN true ELSE false END
    FROM PedidoJpa p
    WHERE p.fkMesa.idmesa = :idMesa
      AND p.idpedido <> :excluirId
      AND p.estado NOT IN ('COMPLETADO','CANCELADO')
    """)
boolean existePedidoActivoPorMesaExcluyendo(
    @Param("idMesa") int idMesa,
    @Param("excluirId") int excluirId);
```

**Paso 3 — Implementar en el adaptador:**
```java
// PedidoRepositorioImpl.java — agregar:
@Override
public boolean existePedidoActivoPorMesa(int idMesa, int excluirIdPedido) {
    return jpaRepository.existePedidoActivoPorMesaExcluyendo(idMesa, excluirIdPedido);
}
```

**Paso 4 — Usar en el use case (reemplazar el `listarTodos()`):**
```java
// PedidoUseCaseImpl.cambiarEstado — ANTES:
boolean tieneOtros = repositorio.listarTodos().stream()
    .anyMatch(p -> p.getFkMesa() != null
        && p.getFkMesa().getIdmesa() == idMesa
        && !p.esEstadoFinal()
        && p.getIdpedido() != id);

// DESPUÉS:
boolean tieneOtros = repositorio.existePedidoActivoPorMesa(idMesa, id);
```

**Estado:** ✅

---

### F2-2 — Lógica de negocio en ReporteControlador
**Archivo:** `pisip/.../presentacion/controladores/ReporteControlador.java`
**Problema:** El controlador filtra pedidos, calcula totales, agrupa por producto
y ordena. Además llama `pedidoUseCase.listar()` → carga TODA la tabla.

**Paso 1 — Crear el puerto:**
```java
// Crear: pisip/.../dominio/casosuso/IReporteUseCase.java
package com.lachozag4.pisip.dominio.casosuso;
import com.lachozag4.pisip.presentacion.dto.response.ReporteVentasDiaDTO;
import java.time.LocalDate;

public interface IReporteUseCase {
    ReporteVentasDiaDTO obtenerVentasDia(LocalDate fecha);
}
```

**Paso 2 — Mover la lógica a la implementación:**
```java
// Crear: pisip/.../aplicacion/casosuso/impl/ReporteUseCaseImpl.java
// Copiar TODO el contenido actual del método del controlador,
// reemplazando @Autowired por constructor injection.
// Retornar el DTO ya construido.
```

**Paso 3 — Simplificar el controlador:**
```java
// ReporteControlador.java — método DESPUÉS del refactor:
@GetMapping("/ventas-dia")
public ResponseEntity<ReporteVentasDiaDTO> ventasDia(
        @RequestParam(required = false)
        @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate fecha) {
    LocalDate fechaConsulta = fecha != null ? fecha : LocalDate.now();
    return ResponseEntity.ok(reporteUseCase.obtenerVentasDia(fechaConsulta));
}
```

**Paso 4 — Registrar en ConfiguracionGeneral:**
```java
@Bean IReporteUseCase reporteUseCase() {
    return new ReporteUseCaseImpl(pedidoRepositorio());
}
```

**Estado:** ✅

---

### F2-3 — `findPedidoActivoByMesa` devuelve Optional pero puede haber múltiples
**Archivo:** `pisip/.../infraestructura/repositorios/IPedidoJpaRepositorio.java`
**Problema:** La query puede retornar más de un pedido activo por mesa. Con `Optional`,
Hibernate lanzará `IncorrectResultSizeDataAccessException` en ese caso.

**Cambio exacto:**
```java
// ANTES:
@Query("...")
Optional<PedidoJpa> findPedidoActivoByMesa(@Param("idMesa") int idMesa);

// DESPUÉS:
@Query("...")
List<PedidoJpa> findPedidosActivosByMesa(@Param("idMesa") int idMesa);
```
> Actualizar todos los lugares que llamen a este método para usar `stream().findFirst()`
> o manejar la lista correctamente.

**Estado:** ✅

---

### F2-4 — `@Data` en entidades JPA
**Archivos:** `PedidoJpa.java`, `CuentaJpa.java`, `PedidoDetalleJpa.java` y demás entidades JPA.
**Problema:** Lombok `@Data` genera `equals/hashCode/toString` con TODAS las relaciones,
causando `LazyInitializationException` o `StackOverflowError` con relaciones bidireccionales.

**Cambio por cada entidad JPA:**
```java
// ANTES:
@Data
@NoArgsConstructor
@Entity
public class PedidoJpa { ... }

// DESPUÉS:
@Getter
@Setter
@NoArgsConstructor
@Entity
public class PedidoJpa {
    // ... campos ...

    // Agregar manualmente al final de la clase:
    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (!(o instanceof PedidoJpa that)) return false;
        return idpedido != 0 && idpedido == that.idpedido;
    }

    @Override
    public int hashCode() { return getClass().hashCode(); }

    @Override
    public String toString() { return "PedidoJpa(id=" + idpedido + ")"; }
}
```

**Estado:** ✅

---

### F2-5 — Setters en entidades de dominio rompen inmutabilidad
**Archivos:** `Pedido.java`, `Cuenta.java`, `Producto.java`
**Problema:** Las entidades tienen campos `final` (inmutabilidad) pero también
setters públicos (`setFkUsuario`, `setFkMesa`, `setFkCliente`, `setFkCategoria`).
El use case `CuentaUseCaseImpl.asignarCliente` llama `cuenta.setFkCliente(cliente)`
en lugar de crear una nueva instancia.

**Paso 1 — Agregar método `wither` en Cuenta:**
```java
// Cuenta.java — agregar:
public Cuenta conCliente(Cliente cliente) {
    return new Cuenta(this.idcuenta, this.estado, this.total,
                      this.fechaApertura, this.fechaCierre,
                      this.fkMesa, cliente);  // cliente nuevo
}
```

**Paso 2 — Actualizar `CuentaUseCaseImpl.asignarCliente`:**
```java
// ANTES:
cuenta.setFkCliente(cliente);
return repositorio.actualizar(cuenta);

// DESPUÉS:
Cuenta actualizada = cuenta.conCliente(cliente);
return repositorio.actualizar(actualizada);
```

**Paso 3 (opcional):** Eliminar los setters de `Pedido` y `Cuenta` una vez que
todos los llamadores usen withers. Buscar todos los `setFk*` en el proyecto.

**Estado:** ✅

---

## FASE 3 — OPTIMIZACIÓN (calidad y escalabilidad)

### F3-1 — Introducir Flyway para migraciones de base de datos
**Archivo:** `pisip/pom.xml` + `application.properties` + crear carpeta `db/migration`
**Problema:** `spring.jpa.hibernate.ddl-auto=update` no es seguro en producción.

**Paso 1 — Agregar dependencia en pom.xml:**
```xml
<dependency>
    <groupId>org.flywaydb</groupId>
    <artifactId>flyway-core</artifactId>
</dependency>
<dependency>
    <groupId>org.flywaydb</groupId>
    <artifactId>flyway-database-postgresql</artifactId>
</dependency>
```

**Paso 2 — Cambiar application.properties:**
```properties
# ANTES:
spring.jpa.hibernate.ddl-auto=update

# DESPUÉS:
spring.jpa.hibernate.ddl-auto=validate
spring.flyway.enabled=true
spring.flyway.locations=classpath:db/migration
```

**Paso 3 — Crear el script inicial:**
Generar `src/main/resources/db/migration/V1__schema_inicial.sql` con el DDL
actual de la base de datos (exportar desde pgAdmin o con `pg_dump --schema-only`).

**Estado:** ✅

---

### F3-2 — N+1 en `listarTodos` por relaciones lazy de PedidoJpa
**Archivo:** `pisip/.../infraestructura/repositorios/IPedidoJpaRepositorio.java`
**Problema:** `findAll()` no hace JOIN de las relaciones. Cuando se accede a
`pedido.getDetalles()` por cada pedido, Hibernate lanza N queries adicionales.

**Cambio exacto — agregar query con JOIN FETCH:**
```java
// IPedidoJpaRepositorio.java — agregar:
@Query("""
    SELECT DISTINCT p FROM PedidoJpa p
    LEFT JOIN FETCH p.detalles d
    LEFT JOIN FETCH d.fkProducto
    LEFT JOIN FETCH p.fkMesa
    LEFT JOIN FETCH p.fkUsuario
    LEFT JOIN FETCH p.fkCliente
    """)
List<PedidoJpa> findAllWithRelations();
```

Luego en `PedidoRepositorioImpl.listarTodos()`:
```java
@Override
public List<Pedido> listarTodos() {
    return jpaRepository.findAllWithRelations().stream()
        .map(mapeador::toDomain)
        .toList();
}
```

**Estado:** ✅

---

### F3-3 — Dividir `ConfiguracionGeneral.java` (God class)
**Archivo:** `pisip/.../infraestructura/configuracion/ConfiguracionGeneral.java`
**Problema:** 212 líneas, 60+ imports, crea todos los beans de todas las capas.

**Opción A (recomendada):** Marcar las implementaciones con `@Service` y eliminar
la configuración manual. Spring resolverá las inyecciones automáticamente.
```java
// Agregar a cada UseCase Impl:
@Service  ← añadir
@RequiredArgsConstructor
public class PedidoUseCaseImpl implements IPedidoUseCase { ... }

// Agregar a cada RepositorioImpl:
@Repository  ← añadir
@RequiredArgsConstructor
public class PedidoRepositorioImpl implements IPedidoRepositorio { ... }
```
Luego eliminar `ConfiguracionGeneral.java` o dejarlo solo para beans de terceros.

**Opción B:** Dividir en clases por dominio:
```java
@Configuration class PedidoBeans { ... }
@Configuration class CuentaBeans { ... }
@Configuration class PagoBeans  { ... }
// etc.
```

**Estado:** ✅

---

### F3-4 — Dividir `ApiService.cs` (MAUI - God Service)
**Archivo:** `ChozaMaui/Services/ApiService.cs` (~350 líneas)
**Problema:** Maneja auth, mesas, categorías, productos, pedidos, caja, comedores,
cuentas, pagos, clientes y reportes en una sola clase.

**Refactor sugerido — crear por dominio:**
```
ChozaMaui/Services/
    ApiService.cs           ← solo configuración base (BaseUrl, opciones JSON)
    PedidoApiService.cs     ← GetPedidosAsync, CrearPedidoAsync, CambiarEstado...
    CuentaApiService.cs     ← GetCuentas, CrearCuenta, AgregarPedido, Cerrar...
    PagoApiService.cs       ← RegistrarPagoAsync
    ProductoApiService.cs   ← Categorías + Productos CRUD
    MesaApiService.cs       ← Mesas + Comedores CRUD
    CajaApiService.cs       ← Apertura, cierre, estado
    ClienteApiService.cs    ← CRUD clientes
```
> Cada servicio recibe `HttpClient` del mismo `IHttpClientFactory` con nombre "ApiService".

**Estado:** ✅

---

### F3-5 — Eliminar `DatosInicialesConfig` vacío (Thymeleaf)
**Archivo:** `consumochoza/.../config/DatosInicialesConfig.java`
**Problema:** Clase `@Component` que implementa `CommandLineRunner` con cuerpo vacío.
Bean innecesario en el contexto de Spring.

**Acción:** Eliminar el archivo completo si ya no se usa para nada.

**Estado:** ✅

---

### F3-6 — Reactivar o eliminar `@PreAuthorize` comentados
**Archivo:** `pisip/.../presentacion/controladores/PedidoControlador.java`
**Problema:** Todas las anotaciones `@PreAuthorize` están comentadas. La seguridad
solo depende de los patrones URL de `SeguridadConfig`. Si se añade un endpoint nuevo
y se olvida el patrón, queda desprotegido.

**Opciones:**
1. Reactivar `@PreAuthorize` en cada método (requiere `@EnableMethodSecurity` en config)
2. Si se decide no usarlos, eliminar los comentarios para no crear confusión

```java
// Para activar, agregar en SeguridadConfig:
@EnableMethodSecurity(prePostEnabled = true)
public class SeguridadConfig { ... }

// Y descomentar en cada método:
@PreAuthorize("hasAnyRole('ADMIN','CAMARERO')")
@GetMapping
public ResponseEntity<...> listar() { ... }
```

**Estado:** ✅

---

### F3-7 — `getEstado()` en Producto viola convención JavaBeans
**Archivo:** `pisip/.../dominio/entidades/Producto.java`
**Problema:** `private final boolean estado` tiene `getEstado()` cuando la convención
para `boolean` es `isEstado()`. Algunos frameworks no serializan correctamente.

**Cambio:**
```java
// ANTES:
public boolean getEstado() { return estado; }

// DESPUÉS:
public boolean isEstado() { return estado; }
// Mantener getEstado() como @Deprecated si hay código que lo llame,
// o hacer el reemplazo global y actualizar todos los llamadores.
```

**Estado:** ✅

---

### F3-8 — Endpoint duplicado PATCH + PUT para /estado
**Archivos:** `PedidoControlador.java`, `CuentaControlador.java`
**Problema:** Cada controlador tiene tanto `@PatchMapping("/{id}/estado")`
como `@PutMapping("/{id}/estado")` para la misma acción.

**Acción:** Eliminar el `@PutMapping` duplicado en ambos controladores y
actualizar los clientes que usaban PUT para usar PATCH.
Verificar en `ApiService.cs` que `CambiarEstadoPedidoAsync` ya usa `PatchAsJsonAsync` ✓.

**Estado:** ✅

---

### F3-9 — Migrar estados de pedido y cuenta a `enum`
**Archivos:** `Pedido.java`, `Cuenta.java` y sus JPA correspondientes
**Problema:** Los estados son constantes `String` y se comparan con `.equals()`.
Agregar un nuevo estado requiere buscar todas las comparaciones en el código.

**Paso 1 — Crear enums en el dominio:**
```java
// Crear: dominio/enums/EstadoPedido.java
public enum EstadoPedido {
    PENDIENTE, EN_COCINA, EN_BAR, LISTO_PARA_ENTREGA, COMPLETADO, CANCELADO
}

// Crear: dominio/enums/EstadoCuenta.java
public enum EstadoCuenta { ABIERTA, PAGADA, ANULADA }
```

**Paso 2 — Actualizar entidades JPA:**
```java
@Enumerated(EnumType.STRING)
@Column(name = "estado")
private EstadoPedido estado;
```

**Paso 3 — Migrar constantes String por el enum en uso cases y controladores.**
> Este cambio es amplio. Hacer en branch separado con tests.

**Estado:** ✅

---

### F3-10 — IVA hardcodeado en MAUI
**Archivo:** `ChozaMaui/ViewModels/PagoViewModel.cs`
**Problema:** `private const double IvaPct = 0.12;` — la tasa impositiva
está fija en la capa de presentación.

**Solución:** El backend debería incluir el IVA calculado en la respuesta de pedido,
o exponer un endpoint de configuración. En el MAUI, leer el valor desde la respuesta
de la API en lugar de calcularlo localmente.

**Estado:** ✅

---

## RESUMEN DE PRIORIDADES

| Fase | Items | Esfuerzo estimado |
|------|-------|-------------------|
| FASE 1 — Crítica  | F1-1 a F1-6 | 2-4 horas |
| FASE 2 — Importante | F2-1 a F2-5 | 1-2 días  |
| FASE 3 — Optimización | F3-1 a F3-10 | 3-5 días  |

**Orden sugerido de ejecución:**
1. F1-2 (bug visual MAUI) — 5 minutos, máximo impacto visible
2. F1-1 (bug mesa no se libera) — más crítico funcionalmente
3. F1-3, F1-4, F1-5, F1-6 (seguridad) — antes de cualquier despliegue
4. F2-1 (N+1 en cambio de estado) — el más impactante en rendimiento
5. F2-2 (lógica en ReporteControlador)
6. F2-3, F2-4, F2-5 (calidad de código)
7. FASE 3 completa según tiempo disponible
