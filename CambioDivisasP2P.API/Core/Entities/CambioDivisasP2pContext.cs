using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CambioDivisasP2P.API.Core.Entities;

public partial class CambioDivisasP2pContext : DbContext
{
    public CambioDivisasP2pContext()
    {
    }

    public CambioDivisasP2pContext(DbContextOptions<CambioDivisasP2pContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Ofertas> Ofertas { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=CambioDivisasP2P;Integrated Security=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            entity.Property(e => e.FechaTransaccion).HasColumnType("datetime");
            entity.Property(e => e.MontoOrigen).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TasaCambio).HasColumnType("decimal(18, 4)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
