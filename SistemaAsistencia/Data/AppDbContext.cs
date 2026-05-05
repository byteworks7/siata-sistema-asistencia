using Microsoft.EntityFrameworkCore;
using SistemaAsistencia.Models;

namespace SistemaAsistencia.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Administrador> Administradores { get; set; }
        public DbSet<Area> Areas { get; set; }
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<PlantillaTurno> PlantillasTurno { get; set; }
        public DbSet<Trabajador> Trabajadores { get; set; }
        public DbSet<Horario> Horarios { get; set; }
        public DbSet<Asistencia> Asistencias { get; set; }
        public DbSet<DiaDescanso> DiasDescanso { get; set; }
        public DbSet<Amonestacion> Amonestaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder m)
        {
            // ── ADMINISTRADORES ──────────────────────────────────────
            m.Entity<Administrador>(e => {
                e.ToTable("administradores");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Username).HasColumnName("username").HasMaxLength(50);
                e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(200);
                e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100);
                e.Property(x => x.Estado).HasColumnName("estado").HasDefaultValue(true);
                e.HasIndex(x => x.Username).IsUnique();
            });

            // ── AREAS ────────────────────────────────────────────────
            m.Entity<Area>(e => {
                e.ToTable("areas");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(80);
                e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(200);
                e.Property(x => x.Activo).HasColumnName("activo").HasDefaultValue(true);
                e.HasIndex(x => x.Nombre).IsUnique();
            });

            // ── CARGOS ───────────────────────────────────────────────
            m.Entity<Cargo>(e => {
                e.ToTable("cargos");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(80);
                e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(200);
                e.Property(x => x.Activo).HasColumnName("activo").HasDefaultValue(true);
                e.HasIndex(x => x.Nombre).IsUnique();
            });

            // ── PLANTILLAS DE TURNO ──────────────────────────────────
            m.Entity<PlantillaTurno>(e => {
                e.ToTable("plantillas_turno");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(80);
                e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(200);
                e.Property(x => x.HorasTurno).HasColumnName("horas_turno");
                e.Property(x => x.DiasTrabajoCiclo).HasColumnName("dias_trabajo_ciclo");
                e.Property(x => x.DiasDescansoCiclo).HasColumnName("dias_descanso_ciclo");
                e.Property(x => x.DiasSemanaFijos).HasColumnName("dias_semana_fijos").HasMaxLength(20);
                e.Property(x => x.Activo).HasColumnName("activo").HasDefaultValue(true);
                e.HasIndex(x => x.Nombre).IsUnique();
            });

            // ── TRABAJADORES ─────────────────────────────────────────
            m.Entity<Trabajador>(e => {
                e.ToTable("trabajadores");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Dni).HasColumnName("dni").HasMaxLength(8);
                e.Property(x => x.Nombres).HasColumnName("nombres").HasMaxLength(60);
                e.Property(x => x.Apellidos).HasColumnName("apellidos").HasMaxLength(60);
                e.Property(x => x.Correo).HasColumnName("correo").HasMaxLength(100);
                e.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(12);
                e.Property(x => x.FotoUrl).HasColumnName("foto_url").HasMaxLength(300);
                e.Property(x => x.IdArea).HasColumnName("id_area");
                e.Property(x => x.IdCargo).HasColumnName("id_cargo");
                e.Property(x => x.Estado).HasColumnName("estado").HasDefaultValue(true);
                e.Property(x => x.FechaRegistro).HasColumnName("fecha_registro").HasDefaultValueSql("GETDATE()");
                e.HasIndex(x => x.Dni).IsUnique();
                e.HasOne(x => x.Area).WithMany(a => a.Trabajadores).HasForeignKey(x => x.IdArea);
                e.HasOne(x => x.Cargo).WithMany(c => c.Trabajadores).HasForeignKey(x => x.IdCargo);
            });

            // ── HORARIOS ─────────────────────────────────────────────
            m.Entity<Horario>(e => {
                e.ToTable("horarios");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.IdTrabajador).HasColumnName("id_trabajador");
                e.Property(x => x.IdPlantilla).HasColumnName("id_plantilla");
                e.Property(x => x.HoraEntrada).HasColumnName("hora_entrada");
                e.Property(x => x.HoraSalida).HasColumnName("hora_salida");
                e.Property(x => x.ToleranciaMinutos).HasColumnName("tolerancia_minutos").HasDefaultValue((byte)5);
                e.Property(x => x.DiasTrabajo).HasColumnName("dias_trabajo").HasMaxLength(20);
                e.Property(x => x.DiasTrabajoCiclo).HasColumnName("dias_trabajo_ciclo");
                e.Property(x => x.DiasDescansoCiclo).HasColumnName("dias_descanso_ciclo");
                e.Property(x => x.FechaInicio).HasColumnName("fecha_inicio");
                e.Property(x => x.FechaFin).HasColumnName("fecha_fin");
                e.Property(x => x.CreadoPor).HasColumnName("creado_por");
                e.Property(x => x.CreadoEn).HasColumnName("creado_en").HasDefaultValueSql("GETDATE()");
                e.HasOne(x => x.Trabajador).WithMany(t => t.Horarios).HasForeignKey(x => x.IdTrabajador);
                e.HasOne(x => x.Plantilla).WithMany(p => p.Horarios).HasForeignKey(x => x.IdPlantilla);
            });

            // ── ASISTENCIAS ──────────────────────────────────────────
            m.Entity<Asistencia>(e => {
                e.ToTable("asistencias");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.IdTrabajador).HasColumnName("id_trabajador");
                e.Property(x => x.Fecha).HasColumnName("fecha");
                e.Property(x => x.HoraEntrada).HasColumnName("hora_entrada");
                e.Property(x => x.HoraSalida).HasColumnName("hora_salida");
                e.Property(x => x.EstadoEntrada).HasColumnName("estado_entrada").HasMaxLength(25).HasDefaultValue("SIN_REGISTRO");
                e.Property(x => x.EstadoSalida).HasColumnName("estado_salida").HasMaxLength(25).HasDefaultValue("PENDIENTE");
                e.Property(x => x.MinutosTardanza).HasColumnName("minutos_tardanza").HasDefaultValue((short)0);
                e.Property(x => x.MinutosSalidaAnticipada).HasColumnName("minutos_salida_anticipada").HasDefaultValue((short)0);
                e.Property(x => x.HoraEntradaProgramada).HasColumnName("hora_entrada_programada");
                e.Property(x => x.HoraSalidaProgramada).HasColumnName("hora_salida_programada");
                e.Property(x => x.CorregidoPorAdmin).HasColumnName("corregido_por_admin").HasDefaultValue(false);
                e.Property(x => x.IdAdminCorrector).HasColumnName("id_admin_corrector");
                e.Property(x => x.FechaCorreccion).HasColumnName("fecha_correccion");
                e.Property(x => x.Observacion).HasColumnName("observacion").HasMaxLength(300);
                e.HasIndex(x => new { x.IdTrabajador, x.Fecha }).IsUnique();
                e.HasOne(x => x.Trabajador).WithMany(t => t.Asistencias).HasForeignKey(x => x.IdTrabajador);
            });

            // ── DÍAS DE DESCANSO ─────────────────────────────────────
            m.Entity<DiaDescanso>(e => {
                e.ToTable("dias_descanso");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.IdTrabajador).HasColumnName("id_trabajador");
                e.Property(x => x.Fecha).HasColumnName("fecha");
                e.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(20).HasDefaultValue("PROGRAMADO");
                e.Property(x => x.Observacion).HasColumnName("observacion").HasMaxLength(200);
                e.Property(x => x.CreadoPor).HasColumnName("creado_por");
                e.Property(x => x.CreadoEn).HasColumnName("creado_en").HasDefaultValueSql("GETDATE()");
                e.HasIndex(x => new { x.IdTrabajador, x.Fecha }).IsUnique();
                e.HasOne(x => x.Trabajador).WithMany(t => t.DiasDescanso).HasForeignKey(x => x.IdTrabajador);
            });

            // ── AMONESTACIONES ───────────────────────────────────────
            m.Entity<Amonestacion>(e => {
                e.ToTable("amonestaciones");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.IdTrabajador).HasColumnName("id_trabajador");
                e.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20);
                e.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(300);
                e.Property(x => x.FechaEmision).HasColumnName("fecha_emision");
                e.Property(x => x.DiasSuspension).HasColumnName("dias_suspension").HasDefaultValue((byte)0);
                e.Property(x => x.CorreoEnviado).HasColumnName("correo_enviado").HasDefaultValue(false);
                e.Property(x => x.FechaCorreo).HasColumnName("fecha_correo");
                e.Property(x => x.CreadoPor).HasColumnName("creado_por");
                e.Property(x => x.CreadoEn).HasColumnName("creado_en").HasDefaultValueSql("GETDATE()");
                e.HasOne(x => x.Trabajador).WithMany(t => t.Amonestaciones).HasForeignKey(x => x.IdTrabajador);
            });
        }
    }
}