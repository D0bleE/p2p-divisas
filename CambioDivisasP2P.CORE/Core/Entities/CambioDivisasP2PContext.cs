using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CambioDivisasP2P.CORE.Core.Entities;

public partial class CambioDivisasP2PContext : DbContext
{
    public CambioDivisasP2PContext()
    {
    }

    public CambioDivisasP2PContext(DbContextOptions<CambioDivisasP2PContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Billeteras> Billeteras { get; set; }

    public virtual DbSet<Calificaciones> Calificaciones { get; set; }

    public virtual DbSet<CuentasBancarias> CuentasBancarias { get; set; }

    public virtual DbSet<Disputas> Disputas { get; set; }

    public virtual DbSet<Monedas> Monedas { get; set; }

    public virtual DbSet<MovimientosFondos> MovimientosFondos { get; set; }

    public virtual DbSet<Ofertas> Ofertas { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<Usuarios> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=CambioDivisasP2P;Integrated Security=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Billeteras>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Billeter__3214EC075CC933DC");

            entity.HasIndex(e => new { e.UsuarioId, e.MonedaId }, "UQ_Usuario_Moneda").IsUnique();

            entity.Property(e => e.SaldoBloqueado).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SaldoDisponible).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Moneda).WithMany(p => p.Billeteras)
                .HasForeignKey(d => d.MonedaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Billetera__Moned__671F4F74");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Billeteras)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Billetera__Usuar__662B2B3B");
        });

        modelBuilder.Entity<Calificaciones>(entity =>
        {
            // 1. Llave primaria limpia
            entity.HasKey(e => e.Id).HasName("PK__Califica__3214EC07FD593ED7");

            // 2. Mapeo explícito de columnas
            entity.Property(e => e.OfertaId).HasColumnName("OfertaId");
            entity.Property(e => e.UsuarioEvaluadorId).HasColumnName("UsuarioEvaluadorId");
            entity.Property(e => e.UsuarioEvaluadoId).HasColumnName("UsuarioEvaluadoId");
            entity.Property(e => e.Puntuacion).HasColumnName("Puntuacion");

            entity.Property(e => e.Comentario)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            // 3. Relación limpia con la tabla Ofertas
            entity.HasOne(d => d.Oferta)
                .WithMany()
                .HasForeignKey(d => d.OfertaId)
                .HasConstraintName("FK_Calificaciones_Ofertas")
                .OnDelete(DeleteBehavior.Cascade); // Si se borra la oferta, se borra su calificación

            // 4. Relación con el Usuario Evaluado
            entity.HasOne(d => d.UsuarioEvaluado)
                .WithMany(p => p.CalificacionesUsuarioEvaluado)
                .HasForeignKey(d => d.UsuarioEvaluadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Calificac__Usuar__12FDD1B2");

            // 5. Relación con el Usuario Evaluador
            entity.HasOne(d => d.UsuarioEvaluador)
                .WithMany(p => p.CalificacionesUsuarioEvaluador)
                .HasForeignKey(d => d.UsuarioEvaluadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Calificac__Usuar__1209AD79");
        });

        modelBuilder.Entity<CuentasBancarias>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CuentasB__3214EC073BD2D111");

            entity.Property(e => e.Banco)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NumeroCCI)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NumeroCCI");
            entity.Property(e => e.NumeroCuenta)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TitularNombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Moneda).WithMany(p => p.CuentasBancarias)
                .HasForeignKey(d => d.MonedaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CuentasBa__Moned__6AEFE058");

            entity.HasOne(d => d.Usuario).WithMany(p => p.CuentasBancarias)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CuentasBa__Usuar__69FBBC1F");
        });

        modelBuilder.Entity<Disputas>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Disputas__3214EC078A019DC0");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ABIERTA");
            entity.Property(e => e.FechaApertura)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaResolucion).HasColumnType("datetime");
            entity.Property(e => e.Motivo)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Resolucion).IsUnicode(false);

            entity.HasKey(e => e.Id);
            entity.Property(e => e.OfertaId).HasColumnName("OfertaId");

            entity.HasOne(d => d.Oferta)
                .WithMany()
                .HasForeignKey(d => d.OfertaId)
                .HasConstraintName("FK_Disputas_Ofertas");

            entity.HasOne(d => d.UsuarioDemandante).WithMany(p => p.Disputas)
                .HasForeignKey(d => d.UsuarioDemandanteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Disputas__Usuari__19AACF41");
        });

        modelBuilder.Entity<Monedas>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Monedas__3214EC07F2F3EC9B");

            entity.HasIndex(e => e.CodigoIso, "UQ__Monedas__F2D69746B201C8B5").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CodigoIso)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("CodigoISO");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RutaBandera)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Simbolo)
                .HasMaxLength(5)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MovimientosFondos>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Movimien__3214EC07A97B91C9");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDIENTE");
            entity.Property(e => e.FechaProcesado).HasColumnType("datetime");
            entity.Property(e => e.FechaSolicitud)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RutaVoucher)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TipoMovimiento)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Moneda).WithMany(p => p.MovimientosFondos)
                .HasForeignKey(d => d.MonedaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Movimient__Moned__73852659");

            entity.HasOne(d => d.Usuario).WithMany(p => p.MovimientosFondos)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Movimient__Usuar__72910220");
        });

        modelBuilder.Entity<Ofertas>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ofertas__3214EC07A6CB6F44");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVA");
            entity.Property(e => e.FechaPublicacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MontoOrigen).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TasaCambio).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.MonedaDestino).WithMany(p => p.OfertasMonedaDestino)
                .HasForeignKey(d => d.MonedaDestinoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ofertas__MonedaD__7D0E9093");

            entity.HasOne(d => d.MonedaOrigen).WithMany(p => p.OfertasMonedaOrigen)
                .HasForeignKey(d => d.MonedaOrigenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ofertas__MonedaO__7C1A6C5A");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Ofertas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ofertas__Usuario__7B264821");
            entity.Property(e => e.UsuarioCompradorId).HasColumnName("UsuarioCompradorId");
            entity.Property(e => e.FechaTransaccion).HasColumnType("datetime").HasColumnName("FechaTransaccion");
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC0751F4D2EC");

            entity.HasIndex(e => e.Nombre, "UQ__Roles__75E3EFCFA4E5CB22").IsUnique();

            entity.Property(e => e.Descripcion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        
        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuarios__3214EC0755E1D146");

            entity.HasIndex(e => e.Email, "UQ__Usuarios__A9D105349AF96746").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NombreCompleto)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Rol)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("USU");

            entity.HasOne(d => d.RolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuarios__RolId__3D5E1FD2");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
