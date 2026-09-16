-- =====================================================================
-- DashboardJym — Tabla de solicitudes de cambio (aprobación del abogado)
-- EJECUTAR UNA SOLA VEZ sobre la base de datos PostgreSQL del sistema.
-- Sin esta tabla, un asesor legal NO puede guardar cambios: la app
-- intenta insertar la solicitud y falla porque la tabla no existe.
-- =====================================================================

CREATE TABLE IF NOT EXISTS solicitudes_cambio (
    id               BIGSERIAL PRIMARY KEY,
    modulo           VARCHAR(50)  NOT NULL,
    registro_id      BIGINT       NOT NULL,
    tipo_accion      VARCHAR(20)  NOT NULL,
    datos_json       TEXT,
    resumen_cambios  TEXT,
    descripcion      VARCHAR(300),
    solicitante_id   BIGINT       NOT NULL REFERENCES usuarios (id),
    estado           VARCHAR(20)  NOT NULL DEFAULT 'PENDIENTE',
    revisor_id       BIGINT       REFERENCES usuarios (id),
    motivo_rechazo   VARCHAR(300),
    fecha_solicitud  TIMESTAMP    NOT NULL DEFAULT NOW(),
    fecha_revision   TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_solicitudes_estado
    ON solicitudes_cambio (estado, fecha_solicitud DESC);

-- Verificación: debe devolver una fila.
-- SELECT to_regclass('public.solicitudes_cambio');
