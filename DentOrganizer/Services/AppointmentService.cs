namespace DentOrganizer.Services;

public class AppointmentService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<Appointment>> GetDayAsync(DateOnly date)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Appointments.AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Session)
            .Where(a => a.Date == date)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }

    /// <summary>Number of active (not cancelled) appointments per day in [from, to].</summary>
    public async Task<Dictionary<DateOnly, int>> GetCountsAsync(DateOnly from, DateOnly to)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var dates = await db.Appointments.AsNoTracking()
            .Where(a => a.Date >= from && a.Date <= to && a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.Date)
            .ToListAsync();
        return dates.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<Appointment>> GetForPatientAsync(int patientId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Appointments.AsNoTracking()
            .Include(a => a.Session)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.Date).ThenByDescending(a => a.StartTime)
            .ToListAsync();
    }

    /// <summary>Active appointments on the same day whose time range overlaps the given one.</summary>
    public async Task<List<Appointment>> GetOverlapsAsync(DateOnly date, TimeOnly start, int durationMinutes, int excludeId)
    {
        var end = start.AddMinutes(durationMinutes);
        var sameDay = await GetDayAsync(date);
        return sameDay
            .Where(a => a.Id != excludeId
                        && a.Status is AppointmentStatus.Scheduled or AppointmentStatus.Completed
                        && a.StartTime < end && start < a.EndTime)
            .ToList();
    }

    public async Task SaveAsync(Appointment appointment)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        appointment.Patient = null;
        appointment.Session = null;
        if (appointment.Id == 0)
            db.Appointments.Add(appointment);
        else
            db.Appointments.Update(appointment);
        await db.SaveChangesAsync();
    }

    public async Task SetStatusAsync(int id, AppointmentStatus status)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Appointments.Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, status));
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var appt = await db.Appointments.FindAsync(id);
        if (appt is null) return;
        db.Appointments.Remove(appt);
        await db.SaveChangesAsync();
    }
}
