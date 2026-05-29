# Despliegue Y Variables De Entorno

Fecha: 28 de mayo de 2026

## 1. Alcance

Esta guia documenta la configuracion necesaria para ejecutar La Choza con:

- API backend `pisip` en Spring Boot;
- web Thymeleaf `consumochoza`;
- app movil .NET MAUI `ChozaMaui`;
- PostgreSQL;
- Dropbox para comprobantes.

No guardar secretos reales en archivos versionados.

## 2. API `pisip`

Archivo de referencia:

- `pisip/src/main/resources/application.properties`

Puerto por defecto:

- `8081`

### Variables requeridas

| Variable | Requerida | Ejemplo | Descripcion |
|---|---|---|---|
| `SERVER_PORT` | No | `8081` | Puerto de la API |
| `DB_URL` | Si | `jdbc:postgresql://localhost:5432/PisipApi2` | URL JDBC PostgreSQL |
| `DB_USER` | Si | `postgres` | Usuario BD |
| `DB_PASSWORD` | Si | `********` | Password BD |
| `JWT_SECRET` | Si | valor largo aleatorio | Firma de tokens JWT |
| `JWT_EXPIRATION` | No | `86400000` | Duracion token en ms |
| `CORS_ALLOWED_ORIGINS` | Si en despliegue | `http://localhost:8085` | Origenes permitidos para web |
| `DROPBOX_APP_KEY` | Si | `app_key` | App key OAuth Dropbox |
| `DROPBOX_APP_SECRET` | Si | `app_secret` | App secret OAuth Dropbox |
| `DROPBOX_REFRESH_TOKEN` | Si | `refresh_token` | Refresh token Dropbox |
| `DROPBOX_FOLDER_ROOT` | No | `/LaChoza/Comprobantes` | Carpeta raiz de comprobantes |

### Ejemplo PowerShell local

```powershell
$env:SERVER_PORT="8081"
$env:DB_URL="jdbc:postgresql://localhost:5432/PisipApi2"
$env:DB_USER="postgres"
$env:DB_PASSWORD="<password_local>"
$env:JWT_SECRET="<minimo_32_caracteres_aleatorios>"
$env:CORS_ALLOWED_ORIGINS="http://localhost:8085"
$env:DROPBOX_APP_KEY="<dropbox_app_key>"
$env:DROPBOX_APP_SECRET="<dropbox_app_secret>"
$env:DROPBOX_REFRESH_TOKEN="<dropbox_refresh_token>"
$env:DROPBOX_FOLDER_ROOT="/LaChoza/Comprobantes"
```

Arranque:

```powershell
cd pisip
mvn spring-boot:run
```

Validacion:

```powershell
curl http://localhost:8081/actuator/health
```

Debe responder `UP`.

## 3. Web `consumochoza`

Archivo de referencia:

- `consumochoza/src/main/resources/application.properties`

Puerto por defecto:

- `8085`

### Variables

| Variable | Requerida | Ejemplo | Descripcion |
|---|---|---|---|
| `SERVER_PORT` | No | `8085` | Puerto web |
| `API_BASE_URL` | Si | `http://localhost:8081/api` | URL base de la API |

Ejemplo PowerShell:

```powershell
$env:SERVER_PORT="8085"
$env:API_BASE_URL="http://localhost:8081/api"
```

Arranque:

```powershell
cd consumochoza
mvn spring-boot:run
```

Validacion:

```powershell
curl http://localhost:8085/actuator/health
```

## 4. App MAUI `ChozaMaui`

La app movil no debe llevar secretos de Dropbox. El archivo:

- `ChozaMaui/appsettings.json`

debe permanecer sin token real:

```json
{
  "Dropbox": {
    "AccessToken": ""
  }
}
```

La URL del backend se configura desde la pantalla de login y se guarda en `Preferences`.

Valores actuales por defecto en `SettingsService`:

| Campo | Valor |
|---|---|
| Host | `192.168.0.168` |
| Puerto | `8081` |

Para emulador Android normalmente puede usarse:

| Entorno | Host API |
|---|---|
| Android Emulator hacia localhost PC | `10.0.2.2` |
| Dispositivo fisico en red local | IP del PC/servidor |
| Produccion | Dominio o IP del servidor |

La app construye la URL como:

```text
http://{host}:{port}
```

## 5. PostgreSQL Y Migraciones

La API usa Flyway:

- `spring.flyway.enabled=true`
- migraciones en `classpath:db/migration`
- `spring.jpa.hibernate.ddl-auto=validate`

Antes de aplicar `V6__constraints_caja_cuenta_indexes.sql` sobre una base existente, ejecutar:

```powershell
psql "$env:DATABASE_URL" -f pisip/src/main/resources/db/preflight/pre_v6_constraints_check.sql
```

Los contadores finales deben estar en `0` para:

- cajas abiertas sobrantes;
- mesas con cuentas abiertas duplicadas;
- pedidos con cuenta de otra mesa.

## 6. Orden De Arranque Local

1. PostgreSQL.
2. API `pisip` en `8081`.
3. Web `consumochoza` en `8085`.
4. MAUI apuntando a host/puerto de API.

Validar primero API:

```powershell
curl http://localhost:8081/actuator/health
```

Luego web:

```powershell
curl http://localhost:8085/actuator/health
```

## 7. Seguridad De Secretos

Estado actual recomendado:

- No dejar `DROPBOX_ACCESS_TOKEN` ni tokens largos en archivos `.properties` o `.json`.
- Usar `DROPBOX_APP_KEY`, `DROPBOX_APP_SECRET` y `DROPBOX_REFRESH_TOKEN`.
- Rotar cualquier token que haya sido expuesto en el repositorio.
- No commitear passwords reales de PostgreSQL.
- Usar `JWT_SECRET` largo, aleatorio y distinto por ambiente.

Checklist:

- [ ] Token Dropbox expuesto anteriormente revocado.
- [ ] Refresh token nuevo configurado fuera del repo.
- [ ] Password DB fuera del repo.
- [ ] `JWT_SECRET` fuera del repo.
- [ ] `CORS_ALLOWED_ORIGINS` restringido a dominios reales.
- [ ] `appsettings.json` de MAUI sin secretos.

## 8. Validacion Funcional Minima

Despues de configurar variables y levantar servicios:

1. Login web con `ADMIN`.
2. Login MAUI con `CAMARERO`.
3. `CAJERO` consulta `GET /api/caja/abierta`.
4. `CAMARERO` crea pedido desde mesa.
5. `COCINA` marca listo.
6. `CAMARERO` entrega.
7. `CAJERO` registra pago.
8. Verificar comprobante Dropbox si aplica.

## 9. Problemas Comunes

| Sintoma | Causa probable | Accion |
|---|---|---|
| API no arranca por Dropbox | Faltan `DROPBOX_APP_KEY`, `DROPBOX_APP_SECRET` o `DROPBOX_REFRESH_TOKEN` | Configurar variables reales |
| Web muestra 401/403 | Token ausente, expirado o rol incorrecto | Reautenticar y validar rol |
| MAUI no conecta | Host/puerto incorrecto | Revisar `SettingsService` desde login |
| Migracion V6 falla | Ya existen duplicados | Ejecutar preflight y limpiar datos |
| CORS bloquea web | `CORS_ALLOWED_ORIGINS` incorrecto | Agregar origen web real |
