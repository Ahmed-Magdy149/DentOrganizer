using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentOrganizer.Data;

public enum Gender { Unspecified, Male, Female }

public enum AppointmentStatus { Scheduled, Completed, Cancelled, NoShow }

public enum AttachmentKind { XRay, Photo, Document, Other }

public enum ItemCategory { Anesthesia, Teeth, Veneers, Implants, Filling, Endodontic, Consumables, Other }

public class Patient
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = "";

    [MaxLength(30)]
    public string? Phone { get; set; }

    public Gender Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    /// <summary>Allergies, chronic diseases, current medication, etc.</summary>
    public string? MedicalHistory { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Appointment> Appointments { get; set; } = [];
    public List<Session> Sessions { get; set; } = [];

    [NotMapped]
    public int? Age
    {
        get
        {
            if (DateOfBirth is not { } dob) return null;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - dob.Year;
            if (dob > today.AddYears(-age)) age--;
            return age;
        }
    }
}

public class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public int DurationMinutes { get; set; } = 30;

    [MaxLength(200)]
    public string? Reason { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>The session that was recorded for this appointment (if any).</summary>
    public Session? Session { get; set; }

    [NotMapped]
    public TimeOnly EndTime => StartTime.AddMinutes(DurationMinutes);
}

/// <summary>One treatment session / visit: what was done, medications, materials, files and money.</summary>
public class Session
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>What was done in this session.</summary>
    [Required, MaxLength(300)]
    public string Procedure { get; set; } = "";

    /// <summary>Tooth numbers (FDI), e.g. "36, 37".</summary>
    [MaxLength(100)]
    public string? Teeth { get; set; }

    public string? Diagnosis { get; set; }

    public string? Notes { get; set; }

    /// <summary>What the patient is charged for this session.</summary>
    public decimal Fee { get; set; }

    /// <summary>What the patient actually paid.</summary>
    public decimal AmountPaid { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<SessionMedication> Medications { get; set; } = [];
    public List<SessionMaterial> Materials { get; set; } = [];
    public List<SessionAttachment> Attachments { get; set; } = [];

    [NotMapped] public decimal MaterialsCost => Materials.Sum(m => m.TotalCost);
    [NotMapped] public decimal Balance => Fee - AmountPaid;
    [NotMapped] public decimal NetProfit => Fee - MaterialsCost;
}

public class SessionMedication
{
    public int Id { get; set; }
    public int SessionId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    [MaxLength(100)] public string? Dosage { get; set; }
    [MaxLength(100)] public string? Frequency { get; set; }
    [MaxLength(100)] public string? Duration { get; set; }
    [MaxLength(250)] public string? Notes { get; set; }
}

/// <summary>A stock item consumed in a session. UnitCost is a snapshot of the cost at the time of use.</summary>
public class SessionMaterial
{
    public int Id { get; set; }
    public int SessionId { get; set; }

    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }

    [NotMapped] public decimal TotalCost => Quantity * UnitCost;
}

public class SessionAttachment
{
    public int Id { get; set; }
    public int SessionId { get; set; }

    public AttachmentKind Kind { get; set; }

    [MaxLength(260)] public string OriginalFileName { get; set; } = "";
    [MaxLength(100)] public string StoredFileName { get; set; } = "";
    [MaxLength(100)] public string? ThumbnailFileName { get; set; }
    [MaxLength(100)] public string? ContentType { get; set; }
    public long SizeBytes { get; set; }

    [MaxLength(250)] public string? Description { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.Now;

    [NotMapped] public bool IsImage => ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
}

public class InventoryItem
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    public ItemCategory Category { get; set; } = ItemCategory.Other;

    /// <summary>e.g. pcs, carpule, ml, g, box.</summary>
    [MaxLength(30)]
    public string Unit { get; set; } = "pcs";

    public decimal Quantity { get; set; }

    /// <summary>Cost of one unit (used to compute the materials cost of a session).</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Low-stock alert threshold.</summary>
    public decimal MinQuantity { get; set; }

    public string? Notes { get; set; }

    /// <summary>Archived items are hidden but kept because past sessions reference them.</summary>
    public bool IsArchived { get; set; }

    [NotMapped] public bool IsLowStock => !IsArchived && Quantity <= MinQuantity;
    [NotMapped] public decimal StockValue => Quantity * UnitCost;
}
