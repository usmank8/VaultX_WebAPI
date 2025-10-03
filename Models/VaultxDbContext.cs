using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace VaultX_WebAPI.Models;

public partial class VaultxDbContext : DbContext
{
    public VaultxDbContext()
    {
    }

    public VaultxDbContext(DbContextOptions<VaultxDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Guest> Guests { get; set; }

    public virtual DbSet<Otp> Otps { get; set; }

    public virtual DbSet<Residence> Residences { get; set; }

    public virtual DbSet<Society> Societies { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_b9535a98350d5b26e7eb0c26af4");

            entity.ToTable("employees");

            entity.HasIndex(e => e.Userid, "REL_19fc098e857550a576b6b16112")
                .IsUnique()
                .HasFilter("([userid] IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.Department)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("department");
            entity.Property(e => e.InternalRole)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("internalRole");
            entity.Property(e => e.JoiningDate).HasColumnName("joiningDate");
            entity.Property(e => e.Shift)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("shift");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updatedAt");
            entity.Property(e => e.Userid)
                .HasMaxLength(255)
                .HasColumnName("userid");

            entity.HasOne(d => d.User).WithOne(p => p.Employee)
                .HasForeignKey<Employee>(d => d.Userid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_19fc098e857550a576b6b161125");
        });

        modelBuilder.Entity<Guest>(entity =>
        {
            entity.HasKey(e => e.GuestId).HasName("PK_a6145db6b105b373e1c1833a3ba");

            entity.ToTable("guests");

            entity.HasIndex(e => e.VehicleId, "REL_cac7e9be74c70ea79eae4ac5c3")
                .IsUnique()
                .HasFilter("([vehicleId] IS NOT NULL)");

            entity.Property(e => e.GuestId)
                .HasMaxLength(255)
                .HasColumnName("guestId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.Eta)
                .HasColumnType("datetime")
                .HasColumnName("eta");
            entity.Property(e => e.GuestName)
                .HasMaxLength(255)
                .HasColumnName("guestName");
            entity.Property(e => e.GuestPhoneNumber)
                .HasMaxLength(255)
                .HasColumnName("guestPhoneNumber");
            entity.Property(e => e.IsVerified).HasColumnName("isVerified");
            entity.Property(e => e.QrCode)
                .HasMaxLength(255)
                .HasDefaultValue("Processing")
                .HasColumnName("qrCode");
            entity.Property(e => e.ResidenceId).HasColumnName("residenceId");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updatedAt");
            entity.Property(e => e.VehicleId)
                .HasMaxLength(255)
                .HasColumnName("vehicleId");
            entity.Property(e => e.VisitCompleted).HasColumnName("visitCompleted");

            entity.HasOne(d => d.Residence).WithMany(p => p.Guests)
                .HasForeignKey(d => d.ResidenceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_426b35f2348dbfdb19a294d3d00");

            entity.HasOne(d => d.Vehicle).WithOne(p => p.Guest)
                .HasForeignKey<Guest>(d => d.VehicleId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_cac7e9be74c70ea79eae4ac5c38");
        });

        modelBuilder.Entity<Otp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_32556d9d7b22031d7d0e1fd6723");

            entity.ToTable("otp");

            entity.HasIndex(e => e.ExpiresAt, "IDX_844706d729b144ed62f482ac2b");

            entity.HasIndex(e => e.UserUserid, "REL_a4d3108840413c6e1ccce8ca43")
                .IsUnique()
                .HasFilter("([userUserid] IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(6)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.ExpiresAt).HasColumnName("expiresAt");
            entity.Property(e => e.IsUsed).HasColumnName("isUsed");
            entity.Property(e => e.UserUserid)
                .HasMaxLength(255)
                .HasColumnName("userUserid");

            entity.HasOne(d => d.UserUser).WithOne(p => p.Otp)
                .HasForeignKey<Otp>(d => d.UserUserid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_a4d3108840413c6e1ccce8ca436");
        });

        modelBuilder.Entity<Residence>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_505bad416f6552d9481a82385bb");

            entity.ToTable("residences");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.AddressLine1)
                .HasMaxLength(255)
                .HasColumnName("addressLine1");
            entity.Property(e => e.AddressLine2)
                .HasMaxLength(255)
                .HasColumnName("addressLine2");
            entity.Property(e => e.ApprovedAt).HasColumnName("approvedAt");
            entity.Property(e => e.ApprovedBy)
                .HasMaxLength(255)
                .HasColumnName("approvedBy");
            entity.Property(e => e.Block)
                .HasMaxLength(255)
                .HasColumnName("block");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.FlatNumber)
                .HasMaxLength(255)
                .HasColumnName("flatNumber");
            entity.Property(e => e.IsApprovedBySociety).HasColumnName("isApprovedBySociety");
            entity.Property(e => e.IsPrimary).HasColumnName("isPrimary");
            entity.Property(e => e.Residence1)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("flat")
                .HasColumnName("residence");
            entity.Property(e => e.ResidenceType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("owned")
                .HasColumnName("residenceType");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updatedAt");
            entity.Property(e => e.Userid)
                .HasMaxLength(255)
                .HasColumnName("userid");

            entity.HasOne(d => d.User).WithMany(p => p.Residences)
                .HasForeignKey(d => d.Userid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_d3685ad68ed3fa2fbb49d136990");
        });

        modelBuilder.Entity<Society>(entity =>
        {
            entity.HasKey(e => e.SocietyId).HasName("PK_54d022c07968203bbc2a1ccc9d8");

            entity.ToTable("societies");

            entity.HasIndex(e => e.UserId, "REL_3450f3eceecb321183417e8227")
                .IsUnique()
                .HasFilter("([user_id] IS NOT NULL)");

            entity.Property(e => e.SocietyId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("society_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("address");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("city");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.PostalCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("postalCode");
            entity.Property(e => e.State)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("state");
            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Society)
                .HasForeignKey<Society>(d => d.UserId)
                .HasConstraintName("FK_3450f3eceecb321183417e8227a");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Userid).HasName("PK_37b098e31baedfa2b76e7876998");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "UQ_97672ac88f789774dd47f7c8be3").IsUnique();

            entity.Property(e => e.Userid)
                .HasMaxLength(255)
                .HasColumnName("userid");
            entity.Property(e => e.Cnic)
                .HasMaxLength(15)
                .HasColumnName("cnic");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createdAt");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Firstname)
                .HasMaxLength(255)
                .HasColumnName("firstname");
            entity.Property(e => e.IsBlocked)
                .HasDefaultValue(false)
                .HasColumnName("isBlocked");
            entity.Property(e => e.IsEmailVerified).HasColumnName("isEmailVerified");
            entity.Property(e => e.IsVerified)
                .HasDefaultValue(false)
                .HasColumnName("isVerified");
            entity.Property(e => e.Lastname)
                .HasMaxLength(255)
                .HasColumnName("lastname");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasMaxLength(255)
                .HasDefaultValue("resident")
                .HasColumnName("role");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updatedAt");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("PK_cc2bbdf57cb1356341edef83d44");

            entity.ToTable("vehicles");

            entity.Property(e => e.VehicleId)
                .HasMaxLength(255)
                .HasColumnName("vehicleId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.IsGuest).HasColumnName("isGuest");
            entity.Property(e => e.Residentid).HasColumnName("residentid");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updatedAt");
            entity.Property(e => e.VehicleColor)
                .HasMaxLength(255)
                .HasColumnName("vehicleColor");
            entity.Property(e => e.VehicleLicensePlateNumber)
                .HasMaxLength(255)
                .HasColumnName("vehicleLicensePlateNumber");
            entity.Property(e => e.VehicleModel)
                .HasMaxLength(255)
                .HasColumnName("vehicleModel");
            entity.Property(e => e.VehicleName)
                .HasMaxLength(255)
                .HasColumnName("vehicleName");
            entity.Property(e => e.VehicleRfidtagId)
                .HasMaxLength(255)
                .HasColumnName("vehicleRFIDTagId");
            entity.Property(e => e.VehicleType)
                .HasMaxLength(255)
                .HasColumnName("vehicleType");

            entity.HasOne(d => d.Resident).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.Residentid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_768cdb766dfb621e783856f55ee");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
