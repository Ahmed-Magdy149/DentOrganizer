using Microsoft.EntityFrameworkCore;

namespace DentOrganizer.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionMedication> SessionMedications => Set<SessionMedication>();
    public DbSet<SessionMaterial> SessionMaterials => Set<SessionMaterial>();
    public DbSet<SessionAttachment> SessionAttachments => Set<SessionAttachment>();
    public DbSet<ToothMark> ToothMarks => Set<ToothMark>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // SQLite has no decimal type; store money/quantities as REAL so they can be sorted and summed.
        builder.Properties<decimal>().HaveConversion<double>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Patient>().HasIndex(p => p.FullName);
        b.Entity<Patient>().HasIndex(p => p.Phone);

        b.Entity<Appointment>().HasIndex(a => a.Date);

        b.Entity<Session>().HasIndex(s => s.Date);
        b.Entity<Session>()
            .HasOne(s => s.Appointment)
            .WithOne(a => a.Session)
            .HasForeignKey<Session>(s => s.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<SessionMaterial>()
            .HasOne(m => m.InventoryItem)
            .WithMany()
            .HasForeignKey(m => m.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<ToothMark>().HasIndex(m => m.SessionId);
        b.Entity<ToothMark>()
            .HasOne(m => m.Session)
            .WithMany()
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<ToothMark>()
            .HasOne(m => m.Item)
            .WithMany()
            .HasForeignKey(m => m.InventoryItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
