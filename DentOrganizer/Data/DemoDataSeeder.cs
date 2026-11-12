namespace DentOrganizer.Data;

/// <summary>Seeds realistic demo data the first time the app runs on an empty database.</summary>
public static class DemoDataSeeder
{
    public static void SeedIfEmpty(AppDbContext db)
    {
        if (db.Patients.Any()) return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var now = DateTime.Now;

        // ---------- Inventory ----------
        var anesthesia = new InventoryItem { Name = "ليدوكايين 2% (كاربocaine)", Category = ItemCategory.Anesthesia, Unit = "carpule", Quantity = 8, UnitCost = 45, MinQuantity = 5 };
        var articaine = new InventoryItem { Name = "آرتيكايين 4% (Septodont)", Category = ItemCategory.Anesthesia, Unit = "carpule", Quantity = 2, UnitCost = 55, MinQuantity = 4 };
        var gloves = new InventoryItem { Name = "قفازات لاتكس (كبير)", Category = ItemCategory.Consumables, Unit = "pair", Quantity = 380, UnitCost = 1.5m, MinQuantity = 100 };
        var masks = new InventoryItem { Name = "كمامات طبية", Category = ItemCategory.Consumables, Unit = "pc", Quantity = 200, UnitCost = 4, MinQuantity = 50 };
        var cotton = new InventoryItem { Name = "قطن طبي", Category = ItemCategory.Consumables, Unit = "roll", Quantity = 60, UnitCost = 5, MinQuantity = 20 };
        var needles = new InventoryItem { Name = "إبر تخدير 27G", Category = ItemCategory.Consumables, Unit = "pc", Quantity = 450, UnitCost = 6, MinQuantity = 150 };
        var etchant = new InventoryItem { Name = "إيتشانت 37% فوسفoric", Category = ItemCategory.Consumables, Unit = "bottle", Quantity = 5, UnitCost = 110, MinQuantity = 2 };
        var prophy = new InventoryItem { Name = "معجون تلميع (Prophy paste)", Category = ItemCategory.Consumables, Unit = "tube", Quantity = 7, UnitCost = 65, MinQuantity = 3 };
        var fluoride = new InventoryItem { Name = "فلورايد جل 1.23%", Category = ItemCategory.Consumables, Unit = "tube", Quantity = 8, UnitCost = 60, MinQuantity = 4 };
        var sutures = new InventoryItem { Name = "خيوط جراحية 4/0", Category = ItemCategory.Other, Unit = "pack", Quantity = 5, UnitCost = 40, MinQuantity = 2 };
        var compA2 = new InventoryItem { Name = "كومبوزيت A2 (ساعة واحدة)", Category = ItemCategory.Filling, Unit = "syringe", Quantity = 20, UnitCost = 95, MinQuantity = 5 };
        var amalgam = new InventoryItem { Name = "أمالجام فضي", Category = ItemCategory.Filling, Unit = "capsule", Quantity = 50, UnitCost = 18, MinQuantity = 20 };
        var bonding = new InventoryItem { Name = "بوندينج (Single Bond)", Category = ItemCategory.Filling, Unit = "bottle", Quantity = 5, UnitCost = 320, MinQuantity = 2 };
        var endoFiles = new InventoryItem { Name = "فيلات حشو عصب H", Category = ItemCategory.Endodontic, Unit = "pack", Quantity = 3, UnitCost = 180, MinQuantity = 3 };
        var gutta = new InventoryItem { Name = "جتة بيرشا", Category = ItemCategory.Endodontic, Unit = "pack", Quantity = 2, UnitCost = 220, MinQuantity = 2 };
        var tempCement = new InventoryItem { Name = "سمنت مؤقت (IRM)", Category = ItemCategory.Endodontic, Unit = "pack", Quantity = 4, UnitCost = 90, MinQuantity = 2 };
        var veneer = new InventoryItem { Name = "فينير بورسلين A1", Category = ItemCategory.Veneers, Unit = "pc", Quantity = 6, UnitCost = 850, MinQuantity = 3 };
        var implant = new InventoryItem { Name = "زرعة تيتانيوم Straumann", Category = ItemCategory.Implants, Unit = "pc", Quantity = 4, UnitCost = 1200, MinQuantity = 1 };
        var implantAbut = new InventoryItem { Name = "أباتمنت زراعة", Category = ItemCategory.Implants, Unit = "pc", Quantity = 3, UnitCost = 700, MinQuantity = 1 };

        db.InventoryItems.AddRange(anesthesia, articaine, gloves, masks, cotton, needles, etchant,
            prophy, fluoride, sutures, compA2, amalgam, bonding, endoFiles, gutta, tempCement,
            veneer, implant, implantAbut);
        db.SaveChanges();

        // ---------- Patients ----------
        Patient P(string name, string phone, Gender g, string dob, string? medical = null, string? notes = null) => new()
        {
            FullName = name, Phone = phone, Gender = g, DateOfBirth = DateOnly.Parse(dob),
            MedicalHistory = medical, Notes = notes, CreatedAt = now.AddDays(-60),
        };

        var patients = new[]
        {
            P("أحمد محمد علي", "01001234567", Gender.Male, "1985-03-14", "حساسية من البنسلين"),
            P("سارة محمود عبدالله", "01112223333", Gender.Female, "1992-07-22"),
            P("محمد حسن إبراهيم", "01223334445", Gender.Male, "1978-11-05", "ضغط دم مرتفع - يتناول أملور"),
            P("فاطمة الزهراء سيد", "01556667778", Gender.Female, "2001-01-30"),
            P("عمر خالد مصطفى", "01098765432", Gender.Male, "1995-09-18"),
            P("مريم عادل فتحي", "01155443322", Gender.Female, "1988-04-12", "حامل - الشهر الرابع"),
            P("كريم شريف النجار", "01201112233", Gender.Male, "2010-06-25"),
            P("نورهان سمير قنديل", "01554433221", Gender.Female, "1999-12-08"),
            P("ياسمين طارق حمدي", "01066554433", Gender.Female, "1993-02-17", "سكري - يتناول ميتفورمين"),
            P("خالد إبراهيم مراد", "01199887766", Gender.Male, "1970-08-30", "مريض قلب - يستخدم أسبرين"),
        };
        db.Patients.AddRange(patients);
        db.SaveChanges();

        // ---------- Appointments ----------
        Appointment A(Patient p, int dayOffset, string time, int duration, string reason, AppointmentStatus status = AppointmentStatus.Scheduled)
        {
            var parts = time.Split(':');
            return new Appointment
            {
                PatientId = p.Id, Date = today.AddDays(dayOffset),
                StartTime = new TimeOnly(int.Parse(parts[0]), int.Parse(parts[1])), DurationMinutes = duration,
                Reason = reason, Status = status, CreatedAt = now.AddDays(-30),
            };
        }

        var appts = new List<Appointment>
        {
            A(patients[0], -12, "10:00", 60, "علاج عصب 36"),
            A(patients[1], -10, "11:30", 45, "تنظيف وتلميع"),
            A(patients[2], -8, "09:00", 45, "خلع ضرس عقل 48"),
            A(patients[3], -6, "12:00", 90, "تركيب فينير 11"),
            A(patients[4], -5, "10:30", 60, "حشو 21, 22"),
            A(patients[5], -3, "13:00", 90, "علاج عصب 46"),
            A(patients[6], -2, "16:00", 45, "حشو لبني"),
            A(patients[7], -1, "11:00", 30, "كشف عام"),
            A(patients[8], 1, "10:00", 60, "متابعة حشو"),
            A(patients[9], 1, "12:30", 30, "كشف وتنظيف"),
            A(patients[0], 3, "09:30", 60, "تركيب تاج"),
            A(patients[2], 4, "11:00", 45, "متابعة خلع"),
            A(patients[4], 5, "14:00", 60, "حشو تجميلي"),
            A(patients[6], 6, "15:30", 45, "متابعة حشو لبني"),
            A(patients[3], 8, "10:00", 90, "تركيب فينير إضافي"),
        };
        // Past appointments with sessions are Completed; add a couple of NoShow / Cancelled ones.
        for (var i = 0; i <= 7; i++) appts[i].Status = AppointmentStatus.Completed;
        var noShow = A(patients[9], -4, "15:00", 30, "حشو بسيط");
        noShow.Status = AppointmentStatus.NoShow;
        var cancelled = A(patients[8], -3, "12:00", 45, "علاج عصب");
        cancelled.Status = AppointmentStatus.Cancelled;
        appts.Add(noShow);
        appts.Add(cancelled);
        db.Appointments.AddRange(appts);
        db.SaveChanges();

        // ---------- Sessions ----------
        Session S(Patient p, Appointment? appt, int dayOffset, string procedure, string? teeth,
            decimal fee, decimal paid, string? diagnosis = null, string? notes = null)
        {
            var s = new Session
            {
                PatientId = p.Id, AppointmentId = appt?.Id, Date = today.AddDays(dayOffset),
                Procedure = procedure, Teeth = teeth, Diagnosis = diagnosis, Notes = notes,
                Fee = fee, AmountPaid = paid, CreatedAt = now.AddDays(dayOffset),
            };
            db.Sessions.Add(s);
            return s;
        }

        var s1 = S(patients[0], appts[0], -12, "علاج عصب - تنظيف وتقويم القنوات", "36",
            1500, 1500, "التهاب لب مؤكد", "تم استكمال العلاج على جلستين.");
        var s2 = S(patients[1], appts[1], -10, "تنظيف وتلميع + تطبيق فلورايد", null,
            400, 400, "تراكم جير خفيف", null);
        var s3 = S(patients[2], appts[2], -8, "خلع ضرس عقل مطمور 48", "48",
            800, 500, "ضرس عقل مطمور أفقي", "المريض دفع عربون، المتبقي عند المتابعة.");
        var s4 = S(patients[3], appts[3], -6, "تركيب فينير بورسلين 11", "11",
            3500, 1000, "تغير لون وسوء اصطفاف", null);
        var s5 = S(patients[4], appts[4], -5, "حشو تجميلي ضوئي", "21, 22",
            600, 600, "تسوس سطحي", null);
        var s6 = S(patients[5], appts[5], -3, "علاج عصب 46 + تاج مؤقت", "46",
            2200, 2200, "التهاب لب حاد", null);
        var s7 = S(patients[6], appts[6], -2, "حشو لبني (كومبوزيت)", "55, 65",
            700, 700, "تسوس أسنان لبنية", null);
        var s8 = S(patients[7], appts[7], -1, "كشف عام وتنظيف", null,
            300, 100, "لثة سليمة", "المريض سدد جزئي فقط.");
        db.SaveChanges();

        // ---------- Session medications ----------
        void Meds(Session s, params (string name, string? dosage, string? freq, string? duration)[] meds)
        {
            foreach (var (name, dosage, freq, duration) in meds)
                db.SessionMedications.Add(new SessionMedication
                {
                    SessionId = s.Id, Name = name, Dosage = dosage, Frequency = freq, Duration = duration,
                });
        }

        Meds(s1, ("أموكسيسيلين 500mg", "1 كبسولة", "كل 8 ساعات", "5 أيام"),
                ("إيبوبروفين 400mg", "1 قرص", "عند اللزوم", "3 أيام"));
        Meds(s2, ("فلورايد جل منزلي", "طبقة رقيقة", "يومياً قبل النوم", "أسبوعان"));
        Meds(s3, ("كليندامايسين 300mg", "1 كبسولة", "كل 8 ساعات", "5 أيام"),
                ("باراسيتامول 500mg", "1 قرص", "كل 6 ساعات عند الألم", "3 أيام"));
        Meds(s5, ("أموكسيسيلين 500mg", "1 كبسولة", "كل 12 ساعة", "3 أيام"));
        Meds(s6, ("أموكسيسيلين 500mg", "1 كبسولة", "كل 8 ساعات", "7 أيام"),
                ("ناپروكسين 250mg", "1 قرص", "كل 12 ساعة", "5 أيام"));
        Meds(s8, ("غسول فموي كلورهكسيدين", "10 مل", "مرتين يومياً", "5 أيام"));

        // ---------- Session materials (quantities already reflect consumption in stock) ----------
        void Mats(Session s, params (InventoryItem item, decimal qty)[] mats)
        {
            foreach (var (item, qty) in mats)
                db.SessionMaterials.Add(new SessionMaterial
                {
                    SessionId = s.Id, InventoryItemId = item.Id, Quantity = qty, UnitCost = item.UnitCost,
                });
        }

        Mats(s1, (endoFiles, 1), (gutta, 1), (anesthesia, 2), (cotton, 4));
        Mats(s2, (prophy, 1), (fluoride, 1), (gloves, 2));
        Mats(s3, (articaine, 2), (needles, 2), (gloves, 2), (sutures, 1));
        Mats(s4, (veneer, 1), (bonding, 0.1m), (anesthesia, 1));
        Mats(s5, (compA2, 1), (etchant, 1), (bonding, 0.05m), (anesthesia, 1));
        Mats(s6, (endoFiles, 1), (gutta, 1), (tempCement, 1), (anesthesia, 2));
        Mats(s7, (compA2, 1), (amalgam, 2), (anesthesia, 1));
        Mats(s8, (prophy, 1), (fluoride, 1));

        // One demo attachment so the files UI is not empty: a small text report.
        var report = "Dental visit report\n==================\nPatient: أحمد محمد علي\nProcedure: Root canal treatment (tooth 36)\nDiagnoses: Pulpitis\nThis is a demo attachment generated automatically.";
        var folder = Path.Combine(Services.AttachmentService.RootFolder, s1.Id.ToString());
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, "visit-report.txt");
        File.WriteAllText(filePath, report);
        db.SessionAttachments.Add(new SessionAttachment
        {
            SessionId = s1.Id, Kind = AttachmentKind.Document,
            OriginalFileName = "visit-report.txt", StoredFileName = "visit-report.txt",
            ContentType = "text/plain", SizeBytes = new FileInfo(filePath).Length,
            Description = "تقرير الزيارة (تجريبي)", UploadedAt = s1.CreatedAt,
        });

        db.SaveChanges();
    }
}
