-- ============================================================
--  SISTEMA DE ASISTENCIA - TALMA SERVICIOS AEROPORTUARIOS
--  Versión 2.0 | Abril 2026
--  Script de migración y estructura completa
-- ============================================================

USE [SistemaAsistencia]
GO

-- ============================================================
-- 0. LIMPIEZA ORDENADA (respeta FK)
-- ============================================================
IF OBJECT_ID('dbo.amonestaciones',       'U') IS NOT NULL DROP TABLE dbo.amonestaciones;
IF OBJECT_ID('dbo.asistencias',          'U') IS NOT NULL DROP TABLE dbo.asistencias;
IF OBJECT_ID('dbo.dias_descanso',        'U') IS NOT NULL DROP TABLE dbo.dias_descanso;
IF OBJECT_ID('dbo.horarios',             'U') IS NOT NULL DROP TABLE dbo.horarios;
IF OBJECT_ID('dbo.plantillas_turno',     'U') IS NOT NULL DROP TABLE dbo.plantillas_turno;
IF OBJECT_ID('dbo.trabajadores',         'U') IS NOT NULL DROP TABLE dbo.trabajadores;
IF OBJECT_ID('dbo.cargos',              'U') IS NOT NULL DROP TABLE dbo.cargos;
IF OBJECT_ID('dbo.areas',              'U') IS NOT NULL DROP TABLE dbo.areas;
IF OBJECT_ID('dbo.administradores',      'U') IS NOT NULL DROP TABLE dbo.administradores;
GO

-- ============================================================
-- 1. ÁREAS  (Rampa, CCO, Cabina, Mantenimiento, etc.)
-- ============================================================
CREATE TABLE dbo.areas (
    id          INT           IDENTITY(1,1) NOT NULL,
    nombre      VARCHAR(80)   NOT NULL,
    descripcion VARCHAR(200)  NULL,
    activo      BIT           NOT NULL DEFAULT 1,
    CONSTRAINT PK_areas PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_areas_nombre UNIQUE (nombre)
);
GO

-- ============================================================
-- 2. CARGOS  (Auxiliar, Operador, Cabinero, Supervisor, etc.)
-- ============================================================
CREATE TABLE dbo.cargos (
    id          INT           IDENTITY(1,1) NOT NULL,
    nombre      VARCHAR(80)   NOT NULL,
    descripcion VARCHAR(200)  NULL,
    activo      BIT           NOT NULL DEFAULT 1,
    CONSTRAINT PK_cargos PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_cargos_nombre UNIQUE (nombre)
);
GO

-- ============================================================
-- 3. TRABAJADORES
-- ============================================================
CREATE TABLE dbo.trabajadores (
    id               INT           IDENTITY(1,1) NOT NULL,
    dni              VARCHAR(8)    NOT NULL,
    nombres          VARCHAR(60)   NOT NULL,
    apellidos        VARCHAR(60)   NOT NULL,
    correo           VARCHAR(100)  NULL,
    telefono         VARCHAR(12)   NULL,
    foto_url         VARCHAR(300)  NULL,
    id_area          INT           NULL,   -- FK ? areas
    id_cargo         INT           NULL,   -- FK ? cargos
    estado           BIT           NOT NULL DEFAULT 1,
    fecha_registro   DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_trabajadores PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_trabajadores_dni UNIQUE (dni),
    CONSTRAINT FK_trabajadores_area  FOREIGN KEY (id_area)  REFERENCES dbo.areas (id),
    CONSTRAINT FK_trabajadores_cargo FOREIGN KEY (id_cargo) REFERENCES dbo.cargos (id)
);
-- Índice para búsqueda por DNI (kiosco lo usa constantemente)
CREATE NONCLUSTERED INDEX IX_trabajadores_dni ON dbo.trabajadores (dni) WHERE estado = 1;
GO

-- ============================================================
-- 4. ADMINISTRADORES
-- ============================================================
CREATE TABLE dbo.administradores (
    id            INT           IDENTITY(1,1) NOT NULL,
    username      VARCHAR(50)   NOT NULL,
    password_hash VARCHAR(200)  NOT NULL,
    nombre        VARCHAR(100)  NULL,
    estado        BIT           NOT NULL DEFAULT 1,
    CONSTRAINT PK_administradores PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_administradores_username UNIQUE (username)
);
GO

-- ============================================================
-- 5. PLANTILLAS DE TURNO
--    Permite definir patrones reutilizables:
--    "8h L-V", "12h 2x2", "8h 5x1x1", etc.
--    El admin asigna una plantilla a un trabajador via horarios.
-- ============================================================
CREATE TABLE dbo.plantillas_turno (
    id                  INT           IDENTITY(1,1) NOT NULL,
    nombre              VARCHAR(80)   NOT NULL,       -- "Turno 8h Lun-Vie", "12h 2-2"
    descripcion         VARCHAR(200)  NULL,
    horas_turno         TINYINT       NOT NULL,        -- 8 o 12
    -- Patrón de días: cuántos días trabaja, cuántos descansa (ciclo)
    dias_trabajo_ciclo  TINYINT       NOT NULL,        -- ej. 5 (trabaja 5)
    dias_descanso_ciclo TINYINT       NOT NULL,        -- ej. 2 (descansa 2)
    -- Días fijos de la semana (solo cuando el patrón es semanal fijo)
    -- Guardado como "L,M,X,J,V" o NULL si es ciclo rotativo
    dias_semana_fijos   VARCHAR(20)   NULL,
    activo              BIT           NOT NULL DEFAULT 1,
    CONSTRAINT PK_plantillas_turno PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_plantillas_nombre UNIQUE (nombre)
);
GO

-- ============================================================
-- 6. HORARIOS
--    Un trabajador puede tener varios horarios en el tiempo.
--    Solo uno estará activo en cada fecha.
--    El admin puede modificar:
--      - Solo un día (excepcion en asistencias.horario_override)
--      - Desde una fecha (fecha_inicio del nuevo horario)
--      - Todo (cierra el anterior con fecha_fin y abre uno nuevo)
-- ============================================================
CREATE TABLE dbo.horarios (
    id                  INT           IDENTITY(1,1) NOT NULL,
    id_trabajador       INT           NOT NULL,
    id_plantilla        INT           NULL,            -- referencia visual, no obligatoria
    hora_entrada        TIME(0)       NOT NULL,
    hora_salida         TIME(0)       NOT NULL,
    tolerancia_minutos  TINYINT       NOT NULL DEFAULT 5,
    -- Días de trabajo: "L,M,X,J,V" | "L,M,X,J,V,S" | NULL = ciclo rotativo
    dias_trabajo        VARCHAR(20)   NULL,
    -- Para turnos rotativos: el ciclo completo
    dias_trabajo_ciclo  TINYINT       NULL,
    dias_descanso_ciclo TINYINT       NULL,
    -- Vigencia
    fecha_inicio        DATE          NOT NULL,
    fecha_fin           DATE          NULL,            -- NULL = vigente
    -- Auditoría
    creado_por          INT           NULL,            -- id admin
    creado_en           DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_horarios PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_horarios_trabajador  FOREIGN KEY (id_trabajador) REFERENCES dbo.trabajadores (id),
    CONSTRAINT FK_horarios_plantilla   FOREIGN KEY (id_plantilla)  REFERENCES dbo.plantillas_turno (id)
);
-- Índice clave: buscar horario vigente de un trabajador en una fecha
CREATE NONCLUSTERED INDEX IX_horarios_trabajador_fecha
    ON dbo.horarios (id_trabajador, fecha_inicio, fecha_fin)
    INCLUDE (hora_entrada, hora_salida, tolerancia_minutos, dias_trabajo);
GO

-- ============================================================
-- 7. ASISTENCIAS
--    Una fila por trabajador por día laboral.
--    Estados de entrada: PUNTUAL | A_TIEMPO | TARDANZA | FALTA | SIN_HORARIO
--    Estados de salida:  REGISTRADA | SALIDA_ANTICIPADA | SALIDA_NO_REGISTRADA | PENDIENTE
-- ============================================================
CREATE TABLE dbo.asistencias (
    id                      INT           IDENTITY(1,1) NOT NULL,
    id_trabajador           INT           NOT NULL,
    fecha                   DATE          NOT NULL,
    -- Marcaciones reales
    hora_entrada            DATETIME      NULL,
    hora_salida             DATETIME      NULL,
    -- Estados calculados
    estado_entrada          VARCHAR(25)   NOT NULL DEFAULT 'SIN_REGISTRO',
    estado_salida           VARCHAR(25)   NOT NULL DEFAULT 'PENDIENTE',
    -- Minutos de deuda acumulada (tardanza + salida anticipada)
    minutos_tardanza        SMALLINT      NOT NULL DEFAULT 0,
    minutos_salida_anticipada SMALLINT    NOT NULL DEFAULT 0,
    -- Override de horario para ese día específico (admin lo puede cambiar)
    hora_entrada_programada TIME(0)       NULL,   -- si NULL, se lee del horario vigente
    hora_salida_programada  TIME(0)       NULL,
    -- Corrección manual del admin
    corregido_por_admin     BIT           NOT NULL DEFAULT 0,
    id_admin_corrector      INT           NULL,
    fecha_correccion        DATETIME      NULL,
    observacion             VARCHAR(300)  NULL,
    CONSTRAINT PK_asistencias PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_asistencias_trabajador_fecha UNIQUE (id_trabajador, fecha),
    CONSTRAINT FK_asistencias_trabajador FOREIGN KEY (id_trabajador) REFERENCES dbo.trabajadores (id),
    CONSTRAINT CK_estado_entrada CHECK (estado_entrada IN (
        'PUNTUAL','A_TIEMPO','TARDANZA','FALTA','SIN_HORARIO','SIN_REGISTRO')),
    CONSTRAINT CK_estado_salida  CHECK (estado_salida  IN (
        'REGISTRADA','SALIDA_ANTICIPADA','SALIDA_NO_REGISTRADA','PENDIENTE'))
);
-- Índice principal: consultas por trabajador + mes (calendario)
CREATE NONCLUSTERED INDEX IX_asistencias_trabajador_fecha
    ON dbo.asistencias (id_trabajador, fecha)
    INCLUDE (hora_entrada, hora_salida, estado_entrada, estado_salida,
             minutos_tardanza, minutos_salida_anticipada);
-- Índice para consultas del día (kiosco + reporte admin)
CREATE NONCLUSTERED INDEX IX_asistencias_fecha
    ON dbo.asistencias (fecha)
    INCLUDE (id_trabajador, estado_entrada, estado_salida);
GO

-- ============================================================
-- 8. DÍAS DE DESCANSO
--    El admin registra descansos individuales o por rango.
--    Motivo: PROGRAMADO | COMPENSATORIO | FERIADO | HORAS_EXTRAS
-- ============================================================
CREATE TABLE dbo.dias_descanso (
    id            INT           IDENTITY(1,1) NOT NULL,
    id_trabajador INT           NOT NULL,
    fecha         DATE          NOT NULL,
    motivo        VARCHAR(20)   NOT NULL DEFAULT 'PROGRAMADO',
    observacion   VARCHAR(200)  NULL,
    creado_por    INT           NULL,
    creado_en     DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_dias_descanso PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_dias_descanso_trabajador_fecha UNIQUE (id_trabajador, fecha),
    CONSTRAINT FK_dias_descanso_trabajador FOREIGN KEY (id_trabajador) REFERENCES dbo.trabajadores (id),
    CONSTRAINT CK_dias_descanso_motivo CHECK (motivo IN (
        'PROGRAMADO','COMPENSATORIO','FERIADO','HORAS_EXTRAS'))
);
CREATE NONCLUSTERED INDEX IX_dias_descanso_trabajador_fecha
    ON dbo.dias_descanso (id_trabajador, fecha);
GO

-- ============================================================
-- 9. AMONESTACIONES
--    El admin genera amonestaciones (1er aviso, suspensión, etc.)
--    Se envía correo automáticamente al registrar.
-- ============================================================
CREATE TABLE dbo.amonestaciones (
    id              INT           IDENTITY(1,1) NOT NULL,
    id_trabajador   INT           NOT NULL,
    tipo            VARCHAR(20)   NOT NULL,   -- AVISO_ESCRITO | SUSPENSION_1D | SUSPENSION_2D
    motivo          VARCHAR(300)  NOT NULL,
    fecha_emision   DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    dias_suspension TINYINT       NOT NULL DEFAULT 0,
    correo_enviado  BIT           NOT NULL DEFAULT 0,
    fecha_correo    DATETIME      NULL,
    creado_por      INT           NOT NULL,   -- id admin
    creado_en       DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_amonestaciones PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_amonestaciones_trabajador FOREIGN KEY (id_trabajador) REFERENCES dbo.trabajadores (id),
    CONSTRAINT CK_amonestaciones_tipo CHECK (tipo IN (
        'AVISO_ESCRITO','SUSPENSION_1D','SUSPENSION_2D'))
);
CREATE NONCLUSTERED INDEX IX_amonestaciones_trabajador
    ON dbo.amonestaciones (id_trabajador, fecha_emision);
GO

-- ============================================================
-- 10. STORED PROCEDURE: Cerrar días con salida pendiente
--     Se ejecuta automáticamente al inicio del día siguiente
--     (o lo llama el backend en un job/background service).
--     Marca como SALIDA_NO_REGISTRADA los registros del día
--     anterior que aún tienen estado_salida = 'PENDIENTE'.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_cerrar_dia_anterior
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ayer DATE = CAST(DATEADD(DAY, -1, GETDATE()) AS DATE);

    UPDATE dbo.asistencias
    SET    estado_salida = 'SALIDA_NO_REGISTRADA',
           observacion   = ISNULL(observacion + ' | ', '') +
                           'Salida no registrada - cierre automático'
    WHERE  fecha        = @ayer
      AND  estado_salida = 'PENDIENTE'
      AND  hora_entrada IS NOT NULL;   -- solo si llegó (no fue falta)

    -- Retorna cuántos registros afectó (útil para logging)
    SELECT @@ROWCOUNT AS registros_cerrados;
END;
GO

-- ============================================================
-- 11. STORED PROCEDURE: Calendario mensual de un trabajador
--     Devuelve todos los días del mes con su estado calculado.
--     Una sola consulta, sin N+1 queries.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_calendario_trabajador
    @id_trabajador INT,
    @mes           TINYINT,
    @anio          SMALLINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @primer_dia DATE = DATEFROMPARTS(@anio, @mes, 1);
    DECLARE @ultimo_dia DATE  = EOMONTH(@primer_dia);
    DECLARE @hoy        DATE  = CAST(GETDATE() AS DATE);

    -- Horario vigente del trabajador en ese mes
    DECLARE @h_entrada         TIME(0),
            @h_salida          TIME(0),
            @tolerancia        TINYINT,
            @dias_trabajo      VARCHAR(20),
            @dias_trab_ciclo   TINYINT,
            @dias_desc_ciclo   TINYINT,
            @horario_inicio    DATE;

    SELECT TOP 1
        @h_entrada       = hora_entrada,
        @h_salida        = hora_salida,
        @tolerancia      = tolerancia_minutos,
        @dias_trabajo    = dias_trabajo,
        @dias_trab_ciclo = dias_trabajo_ciclo,
        @dias_desc_ciclo = dias_descanso_ciclo,
        @horario_inicio  = fecha_inicio
    FROM dbo.horarios
    WHERE id_trabajador = @id_trabajador
      AND fecha_inicio  <= @ultimo_dia
      AND (fecha_fin IS NULL OR fecha_fin >= @primer_dia)
    ORDER BY fecha_inicio DESC;

    -- Datos del trabajador (nombre, cargo, área)
    SELECT
        t.id,
        t.nombres + ' ' + t.apellidos AS nombre_completo,
        t.dni,
        t.foto_url,
        c.nombre  AS cargo,
        a.nombre  AS area
    FROM dbo.trabajadores t
    LEFT JOIN dbo.cargos c ON c.id = t.id_cargo
    LEFT JOIN dbo.areas  a ON a.id = t.id_area
    WHERE t.id = @id_trabajador;

    -- Asistencias y descansos del mes (una sola consulta de cada tabla)
    SELECT
        fecha,
        hora_entrada,
        hora_salida,
        estado_entrada,
        estado_salida,
        minutos_tardanza,
        minutos_salida_anticipada,
        hora_entrada_programada,
        hora_salida_programada,
        corregido_por_admin,
        observacion
    FROM dbo.asistencias
    WHERE id_trabajador = @id_trabajador
      AND fecha BETWEEN @primer_dia AND @ultimo_dia;

    SELECT fecha, motivo
    FROM dbo.dias_descanso
    WHERE id_trabajador = @id_trabajador
      AND fecha BETWEEN @primer_dia AND @ultimo_dia;

    -- Horario de referencia para el frontend
    SELECT
        @h_entrada      AS hora_entrada_horario,
        @h_salida       AS hora_salida_horario,
        @tolerancia     AS tolerancia_minutos,
        @dias_trabajo   AS dias_trabajo,
        @dias_trab_ciclo AS dias_trabajo_ciclo,
        @dias_desc_ciclo AS dias_descanso_ciclo,
        @horario_inicio  AS horario_desde;
END;
GO

-- ============================================================
-- 12. STORED PROCEDURE: Semana actual de un trabajador
--     Usado en el kiosco al ingresar DNI (rápido y liviano).
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_semana_trabajador
    @id_trabajador INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @hoy    DATE = CAST(GETDATE() AS DATE);
    -- Lunes de la semana actual
    DECLARE @lunes  DATE = DATEADD(DAY, -(DATEPART(WEEKDAY, @hoy) + 5) % 7, @hoy);
    DECLARE @sabado DATE = DATEADD(DAY, 5, @lunes);

    SELECT
        a.fecha,
        CONVERT(VARCHAR(5), a.hora_entrada, 108) AS hora_entrada,
        CONVERT(VARCHAR(5), a.hora_salida,  108) AS hora_salida,
        a.estado_entrada,
        a.estado_salida,
        a.minutos_tardanza,
        a.minutos_salida_anticipada
    FROM dbo.asistencias a
    WHERE a.id_trabajador = @id_trabajador
      AND a.fecha BETWEEN @lunes AND @sabado
    ORDER BY a.fecha;

    -- Descansos de la semana
    SELECT fecha, motivo
    FROM dbo.dias_descanso
    WHERE id_trabajador = @id_trabajador
      AND fecha BETWEEN @lunes AND @sabado;
END;
GO

-- ============================================================
-- 13. DATOS INICIALES
-- ============================================================

-- Áreas de Talma
INSERT INTO dbo.areas (nombre) VALUES
    ('Rampa'),
    ('CCO'),
    ('Cabina'),
    ('Mantenimiento'),
    ('Administrativo'),
    ('Seguridad');
GO

-- Cargos
INSERT INTO dbo.cargos (nombre) VALUES
    ('Auxiliar'),
    ('Operador'),
    ('Cabinero'),
    ('Supervisor'),
    ('Técnico'),
    ('Coordinador');
GO

-- Plantillas de turno
INSERT INTO dbo.plantillas_turno
    (nombre, descripcion, horas_turno, dias_trabajo_ciclo, dias_descanso_ciclo, dias_semana_fijos)
VALUES
    ('8h Lun-Vie',  'Turno 8 horas, lunes a viernes',         8,  5, 2, 'L,M,X,J,V'),
    ('8h Lun-Sab',  'Turno 8 horas, lunes a sábado',          8,  6, 1, 'L,M,X,J,V,S'),
    ('12h 2x2',     'Turno 12 horas, 2 días trabajo 2 desc.', 12, 2, 2, NULL),
    ('12h 3x1',     'Turno 12 horas, 3 días trabajo 1 desc.', 12, 3, 1, NULL),
    ('8h 5x1x1',    'Turno 8 horas, 5 trabajo 1 desc 1 desc', 8,  5, 2, NULL);
GO

-- Admin por defecto (password: Admin123)
INSERT INTO dbo.administradores (username, password_hash, nombre, estado)
VALUES ('admin',
        '$2a$11$Z7vISnewEnbRosVOLKeBQeP.JlnRseZqwcSSLIK.WDakcaPnp1udO',
        'Administrador', 1);
GO

-- Trabajador de prueba
INSERT INTO dbo.trabajadores (dni, nombres, apellidos, correo, telefono, id_area, id_cargo, estado)
VALUES ('45678901', 'Carlos', 'Quispe Mamani', 'carlos@gmail.com', '987654321', 1, 1, 1);
GO

-- Horario del trabajador de prueba (8h Lun-Vie, desde hoy)
INSERT INTO dbo.horarios
    (id_trabajador, hora_entrada, hora_salida, tolerancia_minutos, dias_trabajo, fecha_inicio)
VALUES
    (1, '06:00:00', '15:00:00', 10, 'L,M,X,J,V', CAST(GETDATE() AS DATE));
GO

-- ============================================================
-- FIN DEL SCRIPT
-- ============================================================
PRINT 'Base de datos SistemaAsistencia v2.0 creada correctamente.';
GO