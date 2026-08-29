-- =====================================================================
-- ESTUDIO JURÍDICO JYM — Dashboard (ASP.NET Core MVC)
-- Script único con el esquema COMPLETO y FINAL de la base de datos
-- (ya incluye los ajustes: clientes sin sexo/estado civil/teléfono)
-- =====================================================================
-- Uso: crea la base de datos "estudio_juridico_jym" en PostgreSQL y
-- ejecuta este script completo una sola vez desde pgAdmin (Query Tool)
-- o psql. Crea todas las tablas, en el orden correcto, con los roles y
-- tipos de proceso iniciales ya cargados.
-- =====================================================================

-- ---------------------------------------------------------------------
-- Etapa 1: roles, usuarios, usuarios_roles
-- ---------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS roles (
    id              BIGSERIAL PRIMARY KEY,
    nombre          VARCHAR(50) NOT NULL UNIQUE,
    descripcion     VARCHAR(255),
    estado          BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS usuarios (
    id                     BIGSERIAL PRIMARY KEY,
    nombres                VARCHAR(100) NOT NULL,
    apellidos              VARCHAR(100) NOT NULL,
    dni                    VARCHAR(20) NOT NULL UNIQUE,
    correo                 VARCHAR(150) NOT NULL UNIQUE,
    telefono               VARCHAR(20),
    password               VARCHAR(255) NOT NULL,
    estado                 BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_creacion         TIMESTAMP,
    fecha_actualizacion    TIMESTAMP
);

CREATE TABLE IF NOT EXISTS usuarios_roles (
    usuario_id BIGINT NOT NULL REFERENCES usuarios(id),
    rol_id     BIGINT NOT NULL REFERENCES roles(id),
    PRIMARY KEY (usuario_id, rol_id)
);

INSERT INTO roles (nombre, descripcion, estado) VALUES
    ('ABOGADO', 'Abogado del estudio', TRUE),
    ('ASESOR_LEGAL', 'Asesor legal del estudio', TRUE)
ON CONFLICT (nombre) DO NOTHING;

-- ---------------------------------------------------------------------
-- Etapa 3: clientes (version final, simplificada -- sin sexo, estado
-- civil, telefono, ni nacionalidad/direccion/distrito/provincia/
-- departamento/ocupacion/empresa)
-- ---------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS clientes (
    id                     BIGSERIAL PRIMARY KEY,
    nombres                VARCHAR(100) NOT NULL,
    apellidos              VARCHAR(100) NOT NULL,
    dni                    VARCHAR(20) NOT NULL UNIQUE,
    fecha_nacimiento       DATE,
    whatsapp               VARCHAR(20),
    correo                 VARCHAR(150),
    observaciones          TEXT,
    usuario_registro_id    BIGINT NOT NULL REFERENCES usuarios(id),
    estado                 BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_registro         TIMESTAMP,
    fecha_actualizacion    TIMESTAMP
);

-- ---------------------------------------------------------------------
-- Etapa 4: tipos_proceso, procesos
-- ---------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS tipos_proceso (
    id              BIGSERIAL PRIMARY KEY,
    nombre          VARCHAR(100) NOT NULL UNIQUE,
    descripcion     VARCHAR(255),
    estado          BOOLEAN NOT NULL DEFAULT TRUE
);

INSERT INTO tipos_proceso (nombre, descripcion, estado) VALUES
    ('Civil', 'Procesos de materia civil', TRUE),
    ('Penal', 'Procesos de materia penal', TRUE),
    ('Laboral', 'Procesos de materia laboral', TRUE),
    ('Familiar', 'Procesos de materia familiar', TRUE),
    ('Administrativo', 'Procesos de materia administrativa', TRUE),
    ('Comercial', 'Procesos de materia comercial', TRUE),
    ('Constitucional', 'Procesos de materia constitucional', TRUE),
    ('Otro', 'Otro tipo de proceso', TRUE)
ON CONFLICT (nombre) DO NOTHING;

CREATE TABLE IF NOT EXISTS procesos (
    id                      BIGSERIAL PRIMARY KEY,
    cliente_id              BIGINT NOT NULL REFERENCES clientes(id),
    tipo_proceso_id         BIGINT NOT NULL REFERENCES tipos_proceso(id),
    materia                 VARCHAR(150) NOT NULL,
    descripcion             TEXT,
    numero_expediente       VARCHAR(100),
    entidad_relacionada     VARCHAR(200),
    juzgado_fiscalia        VARCHAR(200),
    distrito_judicial       VARCHAR(100),
    fecha_inicio            DATE,
    estado                  VARCHAR(20) NOT NULL DEFAULT 'NUEVO'
                             CHECK (estado IN ('NUEVO','EN_PROCESO','EN_ESPERA','FINALIZADO','ARCHIVADO')),
    prioridad               VARCHAR(20) NOT NULL DEFAULT 'MEDIA'
                             CHECK (prioridad IN ('BAJA','MEDIA','ALTA','URGENTE')),
    abogado_responsable_id  BIGINT REFERENCES usuarios(id),
    asesor_responsable_id   BIGINT REFERENCES usuarios(id),
    observaciones           TEXT,
    usuario_registro_id     BIGINT NOT NULL REFERENCES usuarios(id),
    fecha_registro          TIMESTAMP,
    fecha_actualizacion     TIMESTAMP
);

-- ---------------------------------------------------------------------
-- Etapa 5: pagos, cuotas, abonos_cuota
-- (numero_cuotas queda fijo en 3 en la aplicación: 50% / 25% / 25%)
-- ---------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS pagos (
    id                        BIGSERIAL PRIMARY KEY,
    cliente_id                BIGINT NOT NULL REFERENCES clientes(id),
    proceso_id                BIGINT REFERENCES procesos(id),
    concepto                  VARCHAR(255) NOT NULL,
    monto_total                NUMERIC(10,2) NOT NULL,
    forma_pago                VARCHAR(20) NOT NULL
                               CHECK (forma_pago IN ('YAPE','PLIN','TRANSFERENCIA','EFECTIVO')),
    numero_cuotas              INTEGER NOT NULL DEFAULT 3,
    estado                    VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE'
                               CHECK (estado IN ('PENDIENTE','CONFIRMADO','OBSERVADO','ANULADO')),
    fecha_pago                DATE NOT NULL,
    observaciones             TEXT,
    usuario_registro_id       BIGINT NOT NULL REFERENCES usuarios(id),
    usuario_confirmacion_id   BIGINT REFERENCES usuarios(id),
    fecha_confirmacion        TIMESTAMP,
    fecha_registro            TIMESTAMP,
    fecha_actualizacion       TIMESTAMP
);

CREATE TABLE IF NOT EXISTS cuotas (
    id                  BIGSERIAL PRIMARY KEY,
    pago_id             BIGINT NOT NULL REFERENCES pagos(id) ON DELETE CASCADE,
    numero_cuota        INTEGER NOT NULL,
    monto               NUMERIC(10,2) NOT NULL,
    monto_pagado        NUMERIC(10,2) NOT NULL DEFAULT 0,
    fecha_vencimiento   DATE,
    fecha_pago          DATE,
    estado              VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE'
                        CHECK (estado IN ('PENDIENTE','PARCIAL','PAGADA','OBSERVADA','ANULADA')),
    forma_pago          VARCHAR(20)
                        CHECK (forma_pago IN ('YAPE','PLIN','TRANSFERENCIA','EFECTIVO')),
    observaciones       TEXT
);

CREATE TABLE IF NOT EXISTS abonos_cuota (
    id                     BIGSERIAL PRIMARY KEY,
    cuota_id               BIGINT NOT NULL REFERENCES cuotas(id) ON DELETE CASCADE,
    monto                  NUMERIC(10,2) NOT NULL,
    fecha_abono            DATE NOT NULL,
    forma_pago             VARCHAR(20)
                           CHECK (forma_pago IN ('YAPE','PLIN','TRANSFERENCIA','EFECTIVO')),
    observaciones          TEXT,
    usuario_registro_id    BIGINT NOT NULL REFERENCES usuarios(id),
    fecha_registro         TIMESTAMP NOT NULL
);

-- ---------------------------------------------------------------------
-- Etapa 6: agenda
-- ---------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS agenda (
    id                     BIGSERIAL PRIMARY KEY,
    titulo                 VARCHAR(200) NOT NULL,
    cliente_id             BIGINT REFERENCES clientes(id),
    proceso_id             BIGINT REFERENCES procesos(id),
    tipo                   VARCHAR(20) NOT NULL
                           CHECK (tipo IN ('CITA','AUDIENCIA','REUNION','CONSULTA')),
    fecha                  DATE NOT NULL,
    hora                   TIME NOT NULL,
    duracion               INTEGER,
    lugar                  VARCHAR(255),
    responsable_id         BIGINT NOT NULL REFERENCES usuarios(id),
    estado                 VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE'
                           CHECK (estado IN ('PENDIENTE','CONFIRMADA','REALIZADA','CANCELADA')),
    observaciones          TEXT,
    usuario_registro_id    BIGINT NOT NULL REFERENCES usuarios(id)
);

-- ---------------------------------------------------------------------
-- Etapa 7: mensajes, notificaciones
-- ---------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS mensajes (
    id                  BIGSERIAL PRIMARY KEY,
    remitente_id        BIGINT NOT NULL REFERENCES usuarios(id),
    destinatario_id     BIGINT NOT NULL REFERENCES usuarios(id),
    asunto              VARCHAR(200),
    contenido           TEXT NOT NULL,
    leido               BOOLEAN NOT NULL DEFAULT FALSE,
    fecha_lectura       TIMESTAMP,
    fecha_envio         TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS notificaciones (
    id                  BIGSERIAL PRIMARY KEY,
    usuario_id          BIGINT NOT NULL REFERENCES usuarios(id),
    titulo              VARCHAR(200) NOT NULL,
    mensaje             TEXT NOT NULL,
    tipo                VARCHAR(50),
    referencia_id       BIGINT,
    leida               BOOLEAN NOT NULL DEFAULT FALSE,
    fecha_lectura       TIMESTAMP,
    fecha_creacion      TIMESTAMP NOT NULL
);

-- ---------------------------------------------------------------------
-- Etapa 8: registro_actividades (auditoría, solo lectura desde la app)
-- ---------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS registro_actividades (
    id              BIGSERIAL PRIMARY KEY,
    usuario_id      BIGINT NOT NULL REFERENCES usuarios(id),
    accion          VARCHAR(100) NOT NULL,
    modulo          VARCHAR(100) NOT NULL,
    registro_id     BIGINT,
    descripcion     TEXT,
    fecha_hora      TIMESTAMP NOT NULL
);

-- =====================================================================
-- Fin del script. Todas las tablas, roles iniciales (ABOGADO,
-- ASESOR_LEGAL) y tipos de proceso precargados quedan listos.
-- Recuerda ajustar la contraseña de PostgreSQL en appsettings.json.
-- =====================================================================
