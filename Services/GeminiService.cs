using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using DoAnCoSo_Web.Data;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo_Web.Services
{
    public class GeminiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ApplicationDbContext _context;

        public GeminiService(IConfiguration config, ApplicationDbContext context)
        {
            _http = new HttpClient();
            _apiKey = config["Gemini:ApiKey"]!;
            _model = config["Gemini:Model"] ?? "models/gemini-2.5-flash";
            _context = context;
        }

        public async Task<string> AskAsync(string prompt)
        {
            // Lấy dữ liệu từ database
            var databaseInfo = await GetDatabaseInfo();
            
            // Tạo prompt để AI có thể quyết định trả lời như thế nào
            var enrichedPrompt = $@"{databaseInfo}

---

Câu hỏi của người dùng: {prompt}

Hướng dẫn trả lời:
- Bạn là một trợ lý AI thân thiện, vui vẻ và hữu ích
- Nếu câu hỏi liên quan đến thông tin hệ thống (khóa học, giáo viên, học sinh, lớp học, etc.), hãy dùng thông tin từ hệ thống ở trên để trả lời một cách chi tiết và hữu ích
- Nếu câu hỏi không liên quan đến hệ thống, hãy trả lời bình thường dựa trên kiến thức của bạn
- Luôn tao thái độ lịch sự, vui vẻ, không quá cộc cằn
- Sử dụng ngôn ngữ tự nhiên, dễ hiểu và thân thiện
- Nếu có thể, hãy cung cấp thêm lời khuyên hữu ích hoặc câu hỏi tiếp theo";
            
            // Endpoint v1beta với models khả dụng
            var url = $"https://generativelanguage.googleapis.com/v1beta/{_model}:generateContent?key={_apiKey}";

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = enrichedPrompt }
                        }
                    }
                }
            };

            var response = await _http.PostAsJsonAsync(url, body);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return $"Xin lỗi, tôi gặp lỗi khi xử lý yêu cầu của bạn. Vui lòng thử lại sau!";
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            var text =
                json.GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

            return text ?? "Xin lỗi, tôi không thể trả lời câu hỏi này. Bạn có thể thử lại không?";
        }

        private async Task<string> GetDatabaseInfo()
        {
            var info = "=== THÔNG TIN HỆ THỐNG ===\n";

            // Lấy danh sách khóa học với đánh giá và phiếu đăng ký
            var khoaHocs = await _context.KhoaHoc
                .Include(k => k.PhieuDangKyKhoaHoc)
                .Include(k => k.DanhSachQuanTam)
                .ToListAsync();
            
            if (khoaHocs.Count > 0)
            {
                info += "\n**Danh sách Khóa Học:**\n";
                foreach (var kh in khoaHocs)
                {
                    var registrationCount = kh.PhieuDangKyKhoaHoc?.Count ?? 0;
                    var favoriteCount = kh.DanhSachQuanTam?.Count ?? 0;
                    
                    // Lấy đánh giá cho khóa học này
                    var ratings = await _context.DanhGia
                        .Where(dg => dg.KhoaHocMaKhoaHoc == kh.MaKhoaHoc)
                        .ToListAsync();
                    
                    var avgRating = ratings.Count > 0 ? Math.Round(ratings.Average(r => r.SoSaoDanhGia), 1) : 0;
                    var ratingCount = ratings.Count;
                    
                    info += $"- {kh.TenKhoaHoc} -{kh.NgayHoc:dd/MM/yyyy}\n";
                    info += $"  Học phí: {kh.HocPhi} VND\n";
                    info += $"  Mô tả: {kh.MoTa}\n";
                    info += $"  Số phiếu đăng ký: {registrationCount}\n";
                    info += $"  Số người quan tâm: {favoriteCount}\n";
                    info += $"  Đánh giá: {avgRating}/5 sao ({ratingCount} đánh giá)\n";
                }
            }

            // Lấy danh sách lớp học
            var lopHocs = await _context.LopHoc.Include(l => l.KhoaHoc).ToListAsync();
            if (lopHocs.Count > 0)
            {
                info += "\n**Danh sách Lớp Học:**\n";
                foreach (var lh in lopHocs)
                {
                    info += $"- {lh.TenLopHoc}: {lh.KhoaHoc?.TenKhoaHoc} - {lh.NgayKhaiGiang:dd/MM/yyyy} đến {lh.NgayKetThuc:dd/MM/yyyy}\n";
                }
            }

            // Lấy thống kê người dùng
            var totalUsers = await _context.User.CountAsync();
            info += $"\n**Thống kê Người Dùng:**\n- Tổng số người dùng: {totalUsers}\n";
            //Lấy thông tin vai trò
            var roles = await _context.Roles.ToListAsync();
            //Lấy số lượng người dùng theo vai trò student
            var studentRole = roles.FirstOrDefault(r => r.Name == "Student");
            if (studentRole != null)
            {
                var studentCount = await _context.UserRoles
                    .Where(ur => ur.RoleId == studentRole.Id)
                    .CountAsync();
                info += $"- Tổng số học sinh : {studentCount}\n";
            }
            // Lấy thông tin giáo viên
            var teachers = await _context.ChiTietGiangDay
                .Include(c => c.User)
                .Include(c => c.LopHoc)
                .GroupBy(c => c.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();
            
            var totalTeachers = teachers.Count;
            info += $"- Tổng số giáo viên: {totalTeachers}\n";

            if (totalTeachers > 0)
            {
                info += "\n**Danh sách Giáo Viên:**\n";
                var teacherDetails = await _context.ChiTietGiangDay
                    .Include(c => c.User)
                    .ThenInclude(u => u.ChuyenNganh)
                    .Include(c => c.LopHoc)
                    .GroupBy(c => c.User)
                    .Select(g => new { User = g.Key, LopCount = g.Count() })
                    .ToListAsync();

                foreach (var teacher in teacherDetails)
                {
                    var chuyenNganhName = teacher.User.ChuyenNganh?.TenChuyenNganh ?? "Chưa xác định";
                    info += $"- {teacher.User.HoTen} (Email: {teacher.User.Email}): Chuyên ngành: {chuyenNganhName} - Dạy {teacher.LopCount} lớp\n";
                }
            }

            // Lấy thông tin học sinh
            var students = await _context.ChiTietHocTap
                .GroupBy(c => c.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();
            
            var totalStudents = students.Count;
            info += $"- Tổng số học sinh đang học 1 khóa học: {totalStudents}\n";

            return info;
        }
    }
}
