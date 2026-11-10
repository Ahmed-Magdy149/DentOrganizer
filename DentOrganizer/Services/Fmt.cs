using System.Globalization;

namespace DentOrganizer.Services;

/// <summary>Display formatting helpers and pick-lists.</summary>
public static class Fmt
{
    public const string Currency = "EGP";

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Money(decimal v) => v.ToString("#,0.##", Inv) + " " + Currency;
    public static string Qty(decimal v) => v.ToString("#,0.##", Inv);
    public static string Time(TimeOnly t) => t.ToString("hh:mm tt", Inv);
    public static string Date(DateOnly d) => d.ToString("ddd, d MMM yyyy", Inv);
    public static string ShortDate(DateOnly d) => d.ToString("d MMM yyyy", Inv);

    public static string FileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        _ => $"{bytes / 1024.0 / 1024.0:0.#} MB",
    };

    public static string StatusClass(AppointmentStatus s) => s switch
    {
        AppointmentStatus.Scheduled => "badge-scheduled",
        AppointmentStatus.Completed => "badge-completed",
        AppointmentStatus.Cancelled => "badge-cancelled",
        AppointmentStatus.NoShow => "badge-noshow",
        _ => "",
    };

    public static string StatusText(AppointmentStatus s) => s == AppointmentStatus.NoShow ? "No-show" : s.ToString();

    public static string KindText(AttachmentKind k) => k == AttachmentKind.XRay ? "X-ray" : k.ToString();

    public static readonly string[] CommonProcedures =
    [
        "Examination & consultation", "Scaling & polishing", "Composite filling", "Amalgam filling",
        "Root canal treatment", "Re-root canal treatment", "Pulpotomy", "Simple extraction",
        "Surgical extraction", "Wisdom tooth extraction", "Crown preparation", "Crown cementation",
        "Bridge", "Veneers", "Implant placement", "Implant abutment / crown", "Teeth whitening",
        "Removable denture", "Orthodontic adjustment", "Fluoride application", "Fissure sealant",
    ];

    public static readonly string[] CommonMedications =
    [
        "Amoxicillin 500mg", "Amoxicillin/Clavulanate 1g", "Clindamycin 300mg", "Metronidazole 500mg",
        "Azithromycin 500mg", "Ibuprofen 400mg", "Ibuprofen 600mg", "Paracetamol 500mg",
        "Diclofenac potassium 50mg", "Ketorolac 10mg", "Chlorhexidine mouthwash 0.12%",
        "Benzydamine mouthwash", "Dexamethasone 8mg",
    ];

    public static readonly string[] CommonUnits = ["pcs", "carpule", "box", "ml", "g", "syringe", "pack", "unit"];
}
