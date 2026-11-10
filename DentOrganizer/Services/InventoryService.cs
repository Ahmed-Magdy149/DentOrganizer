namespace DentOrganizer.Services;

public class InventoryService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<InventoryItem>> GetAllAsync(bool includeArchived = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.InventoryItems.AsNoTracking();
        if (!includeArchived) q = q.Where(i => !i.IsArchived);
        return await q.OrderBy(i => i.Category).ThenBy(i => i.Name).ToListAsync();
    }

    public async Task<List<InventoryItem>> GetLowStockAsync()
    {
        var all = await GetAllAsync();
        return all.Where(i => i.IsLowStock).OrderBy(i => i.Quantity).ToList();
    }

    public async Task SaveAsync(InventoryItem item)
    {
        item.Name = item.Name.Trim();
        await using var db = await dbFactory.CreateDbContextAsync();
        if (item.Id == 0)
            db.InventoryItems.Add(item);
        else
            db.InventoryItems.Update(item);
        await db.SaveChangesAsync();
    }

    /// <summary>Adds stock. If a new unit cost is given it replaces the current one.</summary>
    public async Task RestockAsync(int itemId, decimal quantity, decimal? newUnitCost)
    {
        if (quantity <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var item = await db.InventoryItems.FindAsync(itemId)
                   ?? throw new InvalidOperationException("Item not found.");
        item.Quantity += quantity;
        if (newUnitCost is decimal cost && cost >= 0) item.UnitCost = cost;
        await db.SaveChangesAsync();
    }

    /// <summary>Deletes the item, or archives it if past sessions used it. Returns true if deleted.</summary>
    public async Task<bool> DeleteOrArchiveAsync(int itemId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var item = await db.InventoryItems.FindAsync(itemId);
        if (item is null) return true;

        if (await db.SessionMaterials.AnyAsync(m => m.InventoryItemId == itemId))
        {
            item.IsArchived = true;
            await db.SaveChangesAsync();
            return false;
        }

        db.InventoryItems.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task UnarchiveAsync(int itemId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.InventoryItems.Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsArchived, false));
    }
}
