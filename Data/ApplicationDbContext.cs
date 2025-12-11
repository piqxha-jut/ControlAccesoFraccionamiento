using System;
using System.Collections.Generic;
using ControlAccesoFraccionamiento.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlAccesoFraccionamiento.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Incidente> Incidentes { get; set; }

    public virtual DbSet<Notificacione> Notificaciones { get; set; }

    public virtual DbSet<RegistrosAcceso> RegistrosAccesos { get; set; }

    public virtual DbSet<Residente> Residentes { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Vehiculo> Vehiculos { get; set; }

    public virtual DbSet<VehiculosListaNegra> VehiculosListaNegras { get; set; }

    public virtual DbSet<VehiculosPreaprobado> VehiculosPreaprobados { get; set; }

    public virtual DbSet<VehiculosResidente> VehiculosResidentes { get; set; }

    public virtual DbSet<VehiculosVisitante> VehiculosVisitantes { get; set; }

    public virtual DbSet<Visitante> Visitantes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=ControlAccesoFraccionamiento;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Incidente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Incident__3213E83FC456D429");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Descripcion)
                .IsUnicode(false)
                .HasColumnName("descripcion");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.RegistroAccesoId).HasColumnName("registro_acceso_id");
            entity.Property(e => e.ReportadoPor).HasColumnName("reportado_por");
            entity.Property(e => e.TipoIncidente)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("tipo_incidente");

            entity.HasOne(d => d.RegistroAcceso).WithMany(p => p.Incidentes)
                .HasForeignKey(d => d.RegistroAccesoId)
                .HasConstraintName("FK_Incidentes_RegistrosAcceso");

            entity.HasOne(d => d.ReportadoPorNavigation).WithMany(p => p.Incidentes)
                .HasForeignKey(d => d.ReportadoPor)
                .HasConstraintName("FK_Incidentes_Usuarios");
        });

        modelBuilder.Entity<Notificacione>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3213E83FC5DEACC5");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("enviado")
                .HasColumnName("estado");
            entity.Property(e => e.FechaEnvio)
                .HasColumnType("datetime")
                .HasColumnName("fecha_envio");
            entity.Property(e => e.FechaRespuesta)
                .HasColumnType("datetime")
                .HasColumnName("fecha_respuesta");
            entity.Property(e => e.RegistroAccesoId).HasColumnName("registro_acceso_id");
            entity.Property(e => e.ResidenteId).HasColumnName("residente_id");
            entity.Property(e => e.RespuestaResidente)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("respuesta_residente");

            entity.HasOne(d => d.RegistroAcceso).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.RegistroAccesoId)
                .HasConstraintName("FK_Notificaciones_RegistrosAcceso");

            entity.HasOne(d => d.Residente).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.ResidenteId)
                .HasConstraintName("FK_Notificaciones_Residentes");
        });

        modelBuilder.Entity<RegistrosAcceso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Registro__3213E83F79788EC1");

            entity.ToTable("Registros_Acceso");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EstadoAcceso)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("dentro")
                .HasColumnName("estado_acceso");
            entity.Property(e => e.EstadoAutorizacion)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pendiente")
                .HasColumnName("estado_autorizacion");
            entity.Property(e => e.FechaEntrada)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fecha_entrada");
            entity.Property(e => e.FechaSalida)
                .HasColumnType("datetime")
                .HasColumnName("fecha_salida");
            entity.Property(e => e.GuardiaId).HasColumnName("guardia_id");
            entity.Property(e => e.MotivoVisita)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("motivo_visita");
            entity.Property(e => e.Notas)
                .IsUnicode(false)
                .HasColumnName("notas");
            entity.Property(e => e.ResidenteDestinoId).HasColumnName("residente_destino_id");
            entity.Property(e => e.ResidenteId).HasColumnName("residente_id");
            entity.Property(e => e.TiempoEstancia)
                .HasComputedColumnSql("(case when [fecha_salida] IS NOT NULL then datediff(minute,[fecha_entrada],[fecha_salida])  end)", false)
                .HasColumnName("tiempo_estancia");
            entity.Property(e => e.TipoAcceso)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("visitante")
                .HasColumnName("tipo_acceso");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");
            entity.Property(e => e.VisitanteId).HasColumnName("visitante_id");

            entity.HasOne(d => d.Guardia).WithMany(p => p.RegistrosAccesos)
                .HasForeignKey(d => d.GuardiaId)
                .HasConstraintName("FK_RegistrosAcceso_Guardias");

            entity.HasOne(d => d.ResidenteDestino).WithMany(p => p.RegistrosAccesoResidenteDestinos)
                .HasForeignKey(d => d.ResidenteDestinoId)
                .HasConstraintName("FK_RegistrosAcceso_ResidenteDestino");

            entity.HasOne(d => d.Residente).WithMany(p => p.RegistrosAccesoResidentes)
                .HasForeignKey(d => d.ResidenteId)
                .HasConstraintName("FK_RegistrosAcceso_Residentes");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.RegistrosAccesos)
                .HasForeignKey(d => d.VehiculoId)
                .HasConstraintName("FK_RegistrosAcceso_Vehiculos");

            entity.HasOne(d => d.Visitante).WithMany(p => p.RegistrosAccesos)
                .HasForeignKey(d => d.VisitanteId)
                .HasConstraintName("FK_RegistrosAcceso_Visitantes");
        });

        modelBuilder.Entity<Residente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Resident__3213E83F2E85EB1B");

            entity.HasIndex(e => e.UsuarioId, "UQ__Resident__2ED7D2AE9A5DFB9A").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Direccion)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("direccion");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.Unidad)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("unidad");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithOne(p => p.Residente)
                .HasForeignKey<Residente>(d => d.UsuarioId)
                .HasConstraintName("FK_Residentes_Usuarios");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuarios__3213E83FA81B4345");

            entity.HasIndex(e => e.Email, "UQ__Usuarios__AB6E6164FF57556A").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Contrasena)
                .HasMaxLength(256)
                .IsUnicode(false)
                .HasColumnName("contrasena");
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("nombre");
            entity.Property(e => e.Rol)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("rol");
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("telefono");
            entity.Property(e => e.UltimoAcceso)
                .HasColumnType("datetime")
                .HasColumnName("ultimo_acceso");
        });

        modelBuilder.Entity<Vehiculo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Vehiculo__3213E83F6DDCAAE8");

            entity.HasIndex(e => e.Placa, "UQ__Vehiculo__0C057425C43866E1").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Color)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("color");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.Marca)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("marca");
            entity.Property(e => e.Modelo)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("modelo");
            entity.Property(e => e.Placa)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("placa");
            entity.Property(e => e.Tipo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("tipo");
        });

        modelBuilder.Entity<VehiculosListaNegra>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Vehiculo__3213E83FC07C991E");

            entity.ToTable("Vehiculos_ListaNegra");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AgregadoPor).HasColumnName("agregado_por");
            entity.Property(e => e.FechaAgregado)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fecha_agregado");
            entity.Property(e => e.Razon)
                .IsUnicode(false)
                .HasColumnName("razon");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");

            entity.HasOne(d => d.AgregadoPorNavigation).WithMany(p => p.VehiculosListaNegras)
                .HasForeignKey(d => d.AgregadoPor)
                .HasConstraintName("FK_ListaNegra_Usuarios");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.VehiculosListaNegras)
                .HasForeignKey(d => d.VehiculoId)
                .HasConstraintName("FK_ListaNegra_Vehiculos");
        });

        modelBuilder.Entity<VehiculosPreaprobado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Visitant__3213E83F0DA10BBF");

            entity.ToTable("Vehiculos_Preaprobados");

            entity.HasIndex(e => new { e.ResidenteId, e.VehiculoId }, "UQ_VehiculoPreaprobado").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.Notas)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("notas");
            entity.Property(e => e.ResidenteId).HasColumnName("residente_id");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");

            entity.HasOne(d => d.Residente).WithMany(p => p.VehiculosPreaprobados)
                .HasForeignKey(d => d.ResidenteId)
                .HasConstraintName("FK_Preaprobados_Residentes");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.VehiculosPreaprobados)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VehiculosPreaprobados_Vehiculos");
        });

        modelBuilder.Entity<VehiculosResidente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Vehiculo__3213E83F7E14DDC0");

            entity.ToTable("Vehiculos_Residentes");

            entity.HasIndex(e => new { e.ResidenteId, e.VehiculoId }, "UQ_VehiculoResidente").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Alias)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("alias");
            entity.Property(e => e.ResidenteId).HasColumnName("residente_id");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");

            entity.HasOne(d => d.Residente).WithMany(p => p.VehiculosResidentes)
                .HasForeignKey(d => d.ResidenteId)
                .HasConstraintName("FK_VehiculosResidentes_Residentes");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.VehiculosResidentes)
                .HasForeignKey(d => d.VehiculoId)
                .HasConstraintName("FK_VehiculosResidentes_Vehiculos");
        });

        modelBuilder.Entity<VehiculosVisitante>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Vehiculo__3213E83F43A907BB");

            entity.ToTable("Vehiculos_Visitantes");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VehiculoId).HasColumnName("vehiculo_id");
            entity.Property(e => e.VisitanteId).HasColumnName("visitante_id");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.VehiculosVisitantes)
                .HasForeignKey(d => d.VehiculoId)
                .HasConstraintName("FK_VehiculosVisitantes_Vehiculos");

            entity.HasOne(d => d.Visitante).WithMany(p => p.VehiculosVisitantes)
                .HasForeignKey(d => d.VisitanteId)
                .HasConstraintName("FK_VehiculosVisitantes_Visitantes");
        });

        modelBuilder.Entity<Visitante>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Visitant__3213E83FDBC36785");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("nombre");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
