namespace DentOrganizer.Services;

public record PatientRow(Patient Patient, int SessionsCount, DateOnly? LastVisit, DateOnly? NextAppointment, decimal Balance);

public class PatientService(IDbContextFactory<AppDbContext> dbFactory, AttachmentService attachments)
{
    /// <summary>Quick search by name or phone (used by pickers).</summary>
    public async Task<List<Patient>> SearchAsync(string? term, int take = 20)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.Patients.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim();
            q = q.Where(p => EF.Functions.Like(p.FullName, $"%{term}%") || (p.Phone != null && p.Phone.Contains(term)));
        }
        return await q.OrderBy(p => p.FullName).Take(take).ToListAsync();
    }

    /// <summary>Patients list with summary info (last visit, next appointment, balance).</summary>
    public async Task<List<PatientRow>> GetListAsync(string? term)
    {
        var patients = await SearchAsync(term, take: 500);
        var ids = patients.Select(p => p.Id).ToList();

        await using var db = await dbFactory.CreateDbContextAsync();
        var sessions = await db.Sessions.AsNoTracking()
            .Where(s => ids.Contains(s.PatientId))
            .Select(s => new { s.PatientId, s.Date, s.Fee, s.AmountPaid })
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var upcoming = await db.Appointments.AsNoTracking()
            .Where(a => ids.Contains(a.PatientId) && a.Status == AppointmentStatus.Scheduled && a.Date >= today)
            .Select(a => new { a.PatientId, a.Date })
            .ToListAsync();

        var sessionsByPatient = sessions.ToLookup(s => s.PatientId);
        var upcomingByPatient = upcoming.ToLookup(a => a.PatientId);

        return patients.Select(p =>
        {
            var ps = sessionsByPatient[p.Id].ToList();
            return new PatientRow(
                p,
                ps.Count,
                ps.Count > 0 ? ps.Max(s => s.Date) : null,
                upcomingByPatient[p.Id].Select(a => (DateOnly?)a.Date).Min(),
                ps.Sum(s => s.Fee - s.AmountPaid));
        }).ToList();
    }

    public async Task<Patient?> GetAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Patient> SaveAsync(Patient patient)
    {
        patient.FullName = patient.FullName.Trim();
        await using var db = await dbFactory.CreateDbContextAsync();
        if (patient.Id == 0)
            db.Patients.Add(patient);
        else
            db.Patients.Update(patient);
        await db.SaveChangesAsync();
        return patient;
    }

    /// <summary>Deletes the patient with all appointments, sessions and files. Stock is NOT restored.</summary>
    public async Task DeleteAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var sessionIds = await db.Sessions.Where(s => s.PatientId == id).Select(s => s.Id).ToListAsync();
        var patient = await db.Patients.FindAsync(id);
        if (patient is null) return;
        db.Patients.Remove(patient);
        await db.SaveChangesAsync();
        foreach (var sid in sessionIds)
            attachments.DeleteSessionFolder(sid);
    }
}
