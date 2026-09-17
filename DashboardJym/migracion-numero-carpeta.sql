-- =====================================================================
-- DashboardJym — Agrega el número de carpeta (manual) a procesos
-- EJECUTAR UNA SOLA VEZ sobre la base de datos PostgreSQL del sistema.
-- =====================================================================

ALTER TABLE procesos
    ADD COLUMN IF NOT EXISTS numero_carpeta VARCHAR(100);

-- Verificación: debe mostrar la nueva columna.
-- SELECT column_name FROM information_schema.columns WHERE table_name = 'procesos' AND column_name = 'numero_carpeta';
