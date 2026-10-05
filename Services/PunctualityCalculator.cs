namespace SmartAttendanceApi.Services;

/// <summary>
/// Pure logic dan perhitungan matematis untuk Punctuality Rate (Ketepatan Waktu).
/// Tidak memiliki dependensi langsung ke database sehingga mudah diuji (unit testing).
/// </summary>
public static class PunctualityCalculator
{
    private static readonly Dictionary<string, DayOfWeek> DayOfWeekMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "senin",   DayOfWeek.Monday    },
            { "selasa",  DayOfWeek.Tuesday   },
            { "rabu",    DayOfWeek.Wednesday },
            { "kamis",   DayOfWeek.Thursday  },
            { "jumat",   DayOfWeek.Friday    },
            { "sabtu",   DayOfWeek.Saturday  },
            { "minggu",  DayOfWeek.Sunday    },
        };

    /// <summary>
    /// Parsing string hari kerja dari pengaturan sistem (misal: "senin,selasa,rabu,kamis,jumat").
    /// Jika kosong atau invalid, default ke Senin - Jumat.
    /// </summary>
    public static HashSet<DayOfWeek> ParseWorkDays(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new HashSet<DayOfWeek>
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday
            };
        }

        var result = new HashSet<DayOfWeek>();
        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (DayOfWeekMap.TryGetValue(part, out var dow))
                result.Add(dow);
        }

        return result.Count > 0 ? result : new HashSet<DayOfWeek>
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday
        };
    }

    /// <summary>
    /// Mendapatkan rentang tanggal periode (week: Senin-Minggu, month: 1 s.d. akhir bulan).
    /// </summary>
    public static (DateOnly From, DateOnly To) GetPeriodRange(string period, DateOnly refDate)
    {
        if (period.Equals("week", StringComparison.OrdinalIgnoreCase))
        {
            int diff = (int)refDate.DayOfWeek - (int)DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            var monday = refDate.AddDays(-diff);
            return (monday, monday.AddDays(6));
        }
        else // month
        {
            var first = new DateOnly(refDate.Year, refDate.Month, 1);
            var last = first.AddMonths(1).AddDays(-1);
            return (first, last);
        }
    }

    /// <summary>
    /// Mendapatkan rentang tanggal periode sebelumnya yang setara (7 hari sebelumnya untuk week, 1 bulan sebelumnya untuk month).
    /// </summary>
    public static (DateOnly From, DateOnly To) GetPreviousPeriodRange(string period, DateOnly refDate)
    {
        if (period.Equals("week", StringComparison.OrdinalIgnoreCase))
        {
            return GetPeriodRange(period, refDate.AddDays(-7));
        }
        else
        {
            return GetPeriodRange(period, refDate.AddMonths(-1));
        }
    }

    /// <summary>
    /// Menentukan apakah hari ini sudah dihitung sebagai hari yang sudah berlalu (elapsed).
    /// Sesuai aturan: termasuk hari ini HANYA jika sudah ada check-in ATAU jam masuk sudah lewat.
    /// </summary>
    public static bool IsTodayElapsed(bool hasCheckedInToday, TimeOnly nowTime, TimeOnly workStartTime)
    {
        return hasCheckedInToday || nowTime >= workStartTime;
    }

    /// <summary>
    /// Menghitung jumlah hari kerja yang sudah berlalu dalam rentang [from, to] sampai dengan hari ini.
    /// Tidak membagi dengan total hari kerja satu periode penuh bila periode belum selesai.
    /// </summary>
    public static int CountElapsedWorkDays(
        DateOnly from,
        DateOnly to,
        HashSet<DayOfWeek> workDays,
        DateOnly today,
        bool todayCountsAsElapsed)
    {
        // Tentukan batas akhir perhitungan
        DateOnly effectiveTo;
        if (to < today)
        {
            // Periode sudah lewat sepenuhnya di masa lalu
            effectiveTo = to;
        }
        else if (todayCountsAsElapsed)
        {
            // Periode sedang berjalan & hari ini sudah boleh dihitung
            effectiveTo = today < to ? today : to;
        }
        else
        {
            // Periode sedang berjalan tapi hari ini belum dihitung (sebelum jam masuk & belum absen)
            effectiveTo = today.AddDays(-1) < to ? today.AddDays(-1) : to;
        }

        if (effectiveTo < from) return 0;

        int count = 0;
        for (var d = from; d <= effectiveTo; d = d.AddDays(1))
        {
            if (workDays.Contains(d.DayOfWeek))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Rumus: % tepat waktu = (jumlah hari "hadir") ÷ (hari kerja efektif yang sudah berlalu) × 100
    /// Hari kerja efektif = elapsedWorkDays - excused (izin/sakit/cuti approved pada hari kerja).
    /// Jika penyebut <= 0, kembalikan 0.0 tanpa error division by zero.
    /// Dibulatkan ke 1 angka desimal.
    /// </summary>
    public static double ComputePunctualityPercent(int onTime, int elapsedWorkDays, int excused)
    {
        var denominator = elapsedWorkDays - excused;
        if (denominator <= 0) return 0.0;
        return Math.Round(onTime * 100.0 / denominator, 1);
    }

    /// <summary>
    /// Menentukan tren dibanding periode sebelumnya (up | down | same).
    /// Menggunakan epsilon 0.05 agar tidak flip-flop karena pembulatan desimal kecil.
    /// </summary>
    public static string ComputeTrend(double current, double previous)
    {
        const double epsilon = 0.05;
        if (current - previous > epsilon) return "up";
        if (previous - current > epsilon) return "down";
        return "same";
    }
}
