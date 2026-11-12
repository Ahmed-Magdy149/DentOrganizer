using System.Globalization;
using Microsoft.Extensions.Logging;

namespace DentOrganizer
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // Consistent number/date formats regardless of the Windows region settings.
            var culture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "dentorganizer.db");
            builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

            builder.Services.AddSingleton<AttachmentService>();
            builder.Services.AddSingleton<PatientService>();
            builder.Services.AddSingleton<AppointmentService>();
            builder.Services.AddSingleton<SessionService>();
            builder.Services.AddSingleton<InventoryService>();
            builder.Services.AddSingleton<DialogService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            using (var db = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext())
            {
                db.Database.EnsureCreated();
                // EnsureCreated is a no-op on databases created before ToothMarks existed.
                db.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS "ToothMarks" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_ToothMarks" PRIMARY KEY AUTOINCREMENT,
                        "SessionId" INTEGER NOT NULL,
                        "ToothNumber" INTEGER NOT NULL,
                        "InventoryItemId" INTEGER NULL,
                        CONSTRAINT "FK_ToothMarks_Sessions_SessionId" FOREIGN KEY ("SessionId")
                            REFERENCES "Sessions" ("Id") ON DELETE CASCADE,
                        CONSTRAINT "FK_ToothMarks_InventoryItems_InventoryItemId" FOREIGN KEY ("InventoryItemId")
                            REFERENCES "InventoryItems" ("Id") ON DELETE SET NULL
                    );
                    CREATE INDEX IF NOT EXISTS "IX_ToothMarks_SessionId" ON "ToothMarks" ("SessionId");
                    """);
                // Databases created before the Procedure column existed need it added manually.
                var toothMarkColumns = db.Database
                    .SqlQuery<string>($"SELECT name AS Value FROM pragma_table_info('{nameof(AppDbContext.ToothMarks)}')")
                    .ToList();
                var toothMarkTable = nameof(AppDbContext.ToothMarks);
                foreach (var col in new[] { nameof(ToothMark.Procedure), nameof(ToothMark.Part) })
                {
                    if (!toothMarkColumns.Contains(col))
                    {
                        // Identifiers can't be parameterized; col comes from nameof() constants only.
#pragma warning disable EF1003
                        db.Database.ExecuteSqlRaw(
                            "ALTER TABLE \"" + toothMarkTable + "\" ADD COLUMN \"" + col + "\" TEXT NULL");
#pragma warning restore EF1003
                    }
                }
                DemoDataSeeder.SeedIfEmpty(db);
            }

            return app;
        }
    }
}
