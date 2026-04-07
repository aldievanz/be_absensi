using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using SmartAttendanceApi.Data;
using SmartAttendanceApi.DTOs;

namespace SmartAttendanceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FaceController : ControllerBase
{
    private readonly AppDbContext _context;

    // Threshold Euclidean Distance:
    // < 0.30 → sangat identik
    // < 0.38 → match (orang yang sama) ✅
    // > 0.38 → beda orang (strict)
    private const double MATCH_THRESHOLD = 0.38;

    // Cosine Similarity Threshold (0-1, semakin tinggi semakin mirip)
    // > 0.80 → sangat mirip
    // > 0.75 → match ✅
    // < 0.75 → beda orang
    private const double COSINE_THRESHOLD = 0.75;

    public FaceController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// POST api/face/register
    /// Mendaftarkan face embedding untuk user yang sedang login.
    /// 1 user hanya boleh punya 1 data wajah (bisa di-overwrite).
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterFace([FromBody] FaceRegisterRequest request)
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound(ApiResponse<string>.Fail("User tidak ditemukan"));

        // Validasi descriptor: face-api.js menghasilkan 128-dimensional float array
        if (request.Descriptor == null || request.Descriptor.Count != 128)
            return BadRequest(ApiResponse<string>.Fail(
                "Descriptor tidak valid. Harus berupa array 128 angka float."));

        // Simpan sebagai JSON string
        user.FaceEmbedding = JsonSerializer.Serialize(request.Descriptor);
        user.FaceRegisteredAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            message = "Wajah berhasil didaftarkan!",
            registeredAt = user.FaceRegisteredAt?.ToString("yyyy-MM-dd HH:mm:ss")
        }));
    }

    /// <summary>
    /// POST api/face/verify
    /// Memverifikasi apakah wajah yang dikirim cocok dengan wajah terdaftar milik user.
    /// Menggunakan Euclidean Distance antara 2 descriptor.
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyFace([FromBody] FaceVerifyRequest request)
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound(ApiResponse<string>.Fail("User tidak ditemukan"));

        if (string.IsNullOrEmpty(user.FaceEmbedding))
            return BadRequest(ApiResponse<string>.Fail(
                "Anda belum mendaftarkan wajah. Silakan registrasi wajah terlebih dahulu."));

        // Validasi descriptor input
        if (request.Descriptor == null || request.Descriptor.Count != 128)
            return BadRequest(ApiResponse<string>.Fail(
                "Descriptor tidak valid. Harus berupa array 128 angka float."));

        // Parse descriptor yang tersimpan di database
        var storedDescriptor = JsonSerializer.Deserialize<List<float>>(user.FaceEmbedding);
        if (storedDescriptor == null || storedDescriptor.Count != 128)
            return StatusCode(500, ApiResponse<string>.Fail(
                "Data wajah terdaftar rusak. Silakan registrasi ulang."));

        // Hitung Euclidean Distance + Cosine Similarity (dual verification)
        var distance = CalculateEuclideanDistance(request.Descriptor, storedDescriptor);
        var cosineSim = CalculateCosineSimilarity(request.Descriptor, storedDescriptor);

        // Harus lolos KEDUA pengecekan untuk dianggap match
        var isMatch = distance < MATCH_THRESHOLD && cosineSim > COSINE_THRESHOLD;

        var response = new FaceVerifyResponse
        {
            IsMatch = isMatch,
            Distance = Math.Round(distance, 4),
            Threshold = MATCH_THRESHOLD,
            Message = isMatch
                ? $"Wajah cocok! Verifikasi berhasil. (Jarak: {Math.Round(distance, 4)}, Kemiripan: {Math.Round(cosineSim * 100, 1)}%)"
                : $"Wajah tidak cocok. (Jarak: {Math.Round(distance, 4)}, Kemiripan: {Math.Round(cosineSim * 100, 1)}%)"
        };

        return Ok(ApiResponse<FaceVerifyResponse>.Ok(response, response.Message));
    }

    /// <summary>
    /// GET api/face/status
    /// Mengecek status registrasi wajah user yang sedang login.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetFaceStatus()
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound(ApiResponse<string>.Fail("User tidak ditemukan"));

        return Ok(ApiResponse<object>.Ok(new
        {
            hasRegistered = !string.IsNullOrEmpty(user.FaceEmbedding),
            registeredAt = user.FaceRegisteredAt?.ToString("yyyy-MM-dd HH:mm:ss")
        }));
    }

    /// <summary>
    /// DELETE api/face/reset
    /// Menghapus data wajah user (untuk registrasi ulang).
    /// </summary>
    [HttpDelete("reset")]
    public async Task<IActionResult> ResetFace()
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound(ApiResponse<string>.Fail("User tidak ditemukan"));

        user.FaceEmbedding = null;
        user.FaceRegisteredAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok("Data wajah berhasil dihapus. Silakan registrasi ulang."));
    }

    /// <summary>
    /// GET api/face/reset-all
    /// Menghitung ulang (hapus seluruh data wajah di DB). Sesuai permintaan.
    /// </summary>
    [HttpGet("reset-all")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetAllFaces()
    {
        var users = await _context.Users.ToListAsync();
        foreach (var user in users)
        {
            user.FaceEmbedding = null;
            user.FaceRegisteredAt = null;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok("SELURUH DATA WAJAH BERHASIL DIHAPUS DARI DATABASE. Silakan refresh frontend."));
    }

    /// <summary>
    /// Menghitung Euclidean Distance antara 2 face descriptor.
    /// Semakin kecil nilainya, semakin mirip wajahnya.
    /// </summary>
    private static double CalculateEuclideanDistance(List<float> a, List<float> b)
    {
        if (a.Count != b.Count)
            throw new ArgumentException("Panjang descriptor tidak sama");

        double sum = 0;
        for (int i = 0; i < a.Count; i++)
        {
            var diff = a[i] - b[i];
            sum += diff * diff;
        }
        return Math.Sqrt(sum);
    }

    /// <summary>
    /// Menghitung Cosine Similarity antara 2 face descriptor.
    /// Semakin mendekati 1.0, semakin mirip wajahnya.
    /// </summary>
    private static double CalculateCosineSimilarity(List<float> a, List<float> b)
    {
        if (a.Count != b.Count)
            throw new ArgumentException("Panjang descriptor tidak sama");

        double dotProduct = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Count; i++)
        {
            dotProduct += (double)a[i] * b[i];
            normA += (double)a[i] * a[i];
            normB += (double)b[i] * b[i];
        }

        var denominator = Math.Sqrt(normA) * Math.Sqrt(normB);
        if (denominator == 0) return 0;

        return dotProduct / denominator;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}
