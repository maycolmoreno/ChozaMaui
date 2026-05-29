# Pruebas De Concurrencia PostgreSQL

Fecha: 28 de mayo de 2026

## Objetivo

Validar en base real que PostgreSQL y la logica de backend bloquean los riesgos criticos:

- doble cobro de una misma cuenta;
- doble apertura de caja;
- doble cuenta abierta para una misma mesa.

Estas pruebas deben ejecutarse en base de prueba o copia de datos reales, nunca directamente en produccion.

## Preparacion

1. Levantar `pisip` contra PostgreSQL.
2. Aplicar migraciones hasta `V6__constraints_caja_cuenta_indexes.sql`.
3. Confirmar que el preflight no reporta duplicados:

```powershell
psql "$env:DATABASE_URL" -f pisip/src/main/resources/db/preflight/pre_v6_constraints_check.sql
```

4. Tener dos terminales abiertas con `psql` contra la misma base:

```powershell
psql "$env:DATABASE_URL"
```

## 1. Doble Caja Abierta

Esta prueba valida el indice parcial `uq_caja_turno_abierta`.

### Terminal A

```sql
BEGIN;

INSERT INTO caja_turno (
    fecha_apertura,
    monto_inicial,
    estado,
    usuario_apertura
) VALUES (
    NOW(),
    10,
    'ABIERTA',
    'concurrencia_a'
);

-- Dejar la transaccion abierta hasta ejecutar Terminal B.
```

### Terminal B

```sql
BEGIN;

INSERT INTO caja_turno (
    fecha_apertura,
    monto_inicial,
    estado,
    usuario_apertura
) VALUES (
    NOW(),
    20,
    'ABIERTA',
    'concurrencia_b'
);
```

Resultado esperado:

- Terminal B debe quedar bloqueada esperando o fallar cuando Terminal A confirme.

### Terminal A

```sql
COMMIT;
```

### Terminal B

Resultado esperado despues del `COMMIT` de A:

```text
ERROR: duplicate key value violates unique constraint "uq_caja_turno_abierta"
```

Limpiar datos:

```sql
ROLLBACK;

UPDATE caja_turno
SET estado = 'CERRADA',
    fecha_cierre = NOW(),
    usuario_cierre = 'prueba_concurrencia',
    monto_esperado_cierre = monto_inicial,
    monto_declarado_cierre = monto_inicial,
    diferencia = 0
WHERE estado = 'ABIERTA'
  AND usuario_apertura = 'concurrencia_a';
```

## 2. Doble Cuenta Abierta Por Mesa

Esta prueba valida el indice parcial `uq_cuenta_abierta_mesa`.

Antes de iniciar, elegir una mesa real:

```sql
SELECT idmesa, numero FROM mesa ORDER BY idmesa LIMIT 5;
```

Usar el mismo `idmesa` en ambas terminales.

En cada terminal, definir la variable de `psql`:

```sql
\set idmesa 1
```

### Terminal A

```sql
BEGIN;

INSERT INTO cuenta (
    fecha_apertura,
    estado,
    total,
    fk_mesa
) VALUES (
    NOW(),
    'ABIERTA',
    0,
    :idmesa
);

-- Dejar la transaccion abierta hasta ejecutar Terminal B.
```

### Terminal B

```sql
BEGIN;

INSERT INTO cuenta (
    fecha_apertura,
    estado,
    total,
    fk_mesa
) VALUES (
    NOW(),
    'ABIERTA',
    0,
    :idmesa
);
```

### Terminal A

```sql
COMMIT;
```

### Terminal B

Resultado esperado:

```text
ERROR: duplicate key value violates unique constraint "uq_cuenta_abierta_mesa"
```

Limpiar datos:

```sql
ROLLBACK;

UPDATE cuenta
SET estado = 'CANCELADA',
    fecha_cierre = NOW()
WHERE estado = 'ABIERTA'
  AND total = 0
  AND fk_mesa = :idmesa;
```

## 3. Doble Cobro De Cuenta

Esta prueba valida que el pago usa bloqueo pesimista sobre cuenta.

Preparar una cuenta abierta con total mayor a 0 y una caja abierta:

```sql
SELECT idcuenta, total
FROM cuenta
WHERE estado = 'ABIERTA'
  AND total > 0
ORDER BY idcuenta DESC
LIMIT 5;

SELECT idcaja
FROM caja_turno
WHERE estado = 'ABIERTA'
ORDER BY idcaja DESC
LIMIT 1;
```

Usar el endpoint real de API desde dos clientes al mismo tiempo. Ejemplo conceptual:

```http
POST /api/pagos/cuenta/{idcuenta}
Authorization: Bearer <token_cajero>
Content-Type: application/json

{
  "monto": 100.00,
  "metodo": "EFECTIVO",
  "usuario": "cajero"
}
```

Resultado esperado:

- Un pago debe registrarse.
- El segundo pago debe esperar el bloqueo y luego fallar si excede el saldo pendiente o si la cuenta ya quedo pagada.
- No deben quedar pagos duplicados que superen el total de la cuenta.

Verificacion:

```sql
\set idcuenta 1

SELECT
    c.idcuenta,
    c.total,
    c.estado,
    COALESCE(SUM(p.monto), 0) AS total_pagado,
    c.total - COALESCE(SUM(p.monto), 0) AS saldo
FROM cuenta c
LEFT JOIN pago p ON p.fk_cuenta = c.idcuenta
WHERE c.idcuenta = :idcuenta
GROUP BY c.idcuenta, c.total, c.estado;
```

El `total_pagado` nunca debe ser mayor que `total`.

## Criterio De Aprobacion

- No se pueden crear dos cajas `ABIERTA`.
- No se pueden crear dos cuentas `ABIERTA` para la misma mesa.
- Dos cobros simultaneos no pueden superar el total de la cuenta.
- La API responde con mensaje claro cuando PostgreSQL rechaza por integridad.

## Si Falla

- Si hay doble caja o doble cuenta, revisar si la migracion V6 fue aplicada.
- Si el doble cobro supera el total, revisar que `PagoUseCaseImpl` use `buscarPorIdParaActualizar`.
- Si la API devuelve error generico, revisar `GlobalExceptionHandler`.
