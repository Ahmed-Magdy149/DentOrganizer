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
            }

            return app;
        }
    }
}
