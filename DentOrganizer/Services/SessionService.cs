namespace DentOrganizer.Services;

public class SessionService(IDbContextFactory<AppDbContext> dbFactory, AttachmentService attachments)
{
    private static IQueryable<Session> WithDetails(IQueryable<Session> q) => q
        .Include(s => s.Patient)
        .Include(s => s.Medications)
        .Include(s => s.Materials).ThenInclude(m => m.InventoryItem)
        .Include(s => s.Attachments)
        .AsSplitQuery();

    public async Task<Session?> GetAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await WithDetails(db.Sessions.AsNoTracking()).FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Session>> GetForPatientAsync(int patientId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await WithDetails(db.Sessions.AsNoTracking())
            .Where(s => s.PatientId == patientId)
            .OrderByDescending(s => s.Date).ThenByDescending(s => s.Id)
            .ToListAsync();
    }

    public async Task<List<Session>> GetForRangeAsync(DateOnly from, DateOnly to)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await WithDetails(db.Sessions.AsNoTracking())
            .Where(s => s.Date >= from && s.Date <= to)
            .OrderBy(s => s.Date).ThenBy(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Creates a session. If it belongs to an appointment, that appointment is marked Completed.</summary>
    public async Task<Session> CreateAsync(Session session)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        session.Patient = null;
        session.Appointment = null;
        db.Sessions.Add(session);
        if (session.AppointmentId is int apptId)
        {
            var appt = await db.Appointments.FindAsync(apptId);
            if (appt is not null) appt.Status = AppointmentStatus.Completed;
        }
        await db.SaveChangesAsync();
        return session;
    }

    /// <summary>Updates the main fields only (not medications/materials/attachments).</summary>
    public async Task UpdateAsync(Session session)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.Sessions.FindAsync(session.Id)
                       ?? throw new InvalidOperationException("Session not found.");
        existing.Date = session.Date;
        existing.Procedure = session.Procedure.Trim();
        existing.Teeth = session.Teeth;
        existing.Diagnosis = session.Diagnosis;
        existing.Notes = session.Notes;
        existing.Fee = session.Fee;
        existing.AmountPaid = session.AmountPaid;
        await db.SaveChangesAsync();
    }

    /// <summary>Deletes the session, returns its materials to stock and deletes its files.</summary>
    public async Task DeleteAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var session = await db.Sessions.Include(s => s.Materials).FirstOrDefaultAsync(s => s.Id == id);
        if (session is null) return;

        foreach (var m in session.Materials)
        {
            var item = await db.InventoryItems.FindAsync(m.InventoryItemId);
            if (item is not null) item.Quantity += m.Quantity;
        }

        if (session.AppointmentId is int apptId)
        {
            var appt = await db.Appointments.FindAsync(apptId);
            if (appt is not null && appt.Status == AppointmentStatus.Completed)
                appt.Status = AppointmentStatus.Scheduled;
        }

        db.Sessions.Remove(session);
        await db.SaveChangesAsync();
        attachments.DeleteSessionFolder(id);
    }

    // ---------- Medications ----------

    public async Task AddMedicationAsync(SessionMedication med)
    {
        med.Name = med.Name.Trim();
        await using var db = await dbFactory.CreateDbContextAsync();
        db.SessionMedications.Add(med);
        await db.SaveChangesAsync();
    }

    public async Task RemoveMedicationAsync(int medicationId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.SessionMedications.Where(m => m.Id == medicationId).ExecuteDeleteAsync();
    }

    // ---------- Tooth marks (dental chart) ----------

    public async Task<List<ToothMark>> GetToothMarksAsync(int sessionId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ToothMarks.AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.ToothNumber)
            .ToListAsync();
    }

    /// <summary>Replaces all tooth marks of a session (used when the doctor saves the dental chart).</summary>
    public async Task ReplaceToothMarksAsync(int sessionId, IEnumerable<ToothMark> marks)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.ToothMarks.Where(m => m.SessionId == sessionId).ToListAsync();
        db.ToothMarks.RemoveRange(existing);
        foreach (var m in marks)
        {
            db.ToothMarks.Add(new ToothMark
            {
                SessionId = sessionId,
                ToothNumber = m.ToothNumber,
                Procedure = m.Procedure,
                Part = m.Part,
                InventoryItemId = m.InventoryItemId,
            });
        }
        await db.SaveChangesAsync();
    }

    // ---------- Materials (stock) ----------

    /// <summary>Uses <paramref name="quantity"/> of a stock item in the session and deducts it from stock.</summary>
    public async Task AddMaterialAsync(int sessionId, int itemId, decimal quantity)
    {
        if (quantity <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var item = await db.InventoryItems.FindAsync(itemId)
                   ?? throw new InvalidOperationException("Item not found.");
        if (item.Quantity < quantity)
            throw new InvalidOperationException($"Not enough stock: only {item.Quantity:0.##} {item.Unit} of {item.Name} left.");

        item.Quantity -= quantity;
        db.SessionMaterials.Add(new SessionMaterial
        {
            SessionId = sessionId,
            InventoryItemId = itemId,
            Quantity = quantity,
            UnitCost = item.UnitCost,
        });
        await db.SaveChangesAsync();
    }

    /// <summary>Removes a material line from the session and returns the quantity to stock.</summary>
    public async Task RemoveMaterialAsync(int materialId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var line = await db.SessionMaterials.FindAsync(materialId);
        if (line is null) return;
        var item = await db.InventoryItems.FindAsync(line.InventoryItemId);
        if (item is not null) item.Quantity += line.Quantity;
        db.SessionMaterials.Remove(line);
        await db.SaveChangesAsync();
    }
}
