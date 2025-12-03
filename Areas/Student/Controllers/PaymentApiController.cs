using DoAnCoSo_Web.Areas.Student.MomoService;
using DoAnCoSo_Web.Areas.Student.EmailService;
using DoAnCoSo_Web.Data;
using DoAnCoSo_Web.Models;
using DoAnCoSo_Web.Models.AppSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DoAnCoSo_Web.Areas.Student.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Area("Student")]
    public class PaymentApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly AppSettings _appSettings;
        private readonly IMomoService _momoService;
        private readonly EmailServer _emailServer;
        private readonly IConfiguration _configuration;

        public PaymentApiController(
            ApplicationDbContext db,
            IOptions<AppSettings> options,
            IMomoService momoService,
            EmailServer emailServer,
            IConfiguration configuration)
        {
            _db = db;
            _appSettings = options.Value;
            _momoService = momoService;
            _emailServer = emailServer;
            _configuration = configuration;
        }
        
        // Tạo deep link redirect về Flutter app
        private string BuildFlutterDeepLink(bool success, string message, int? maKhoaHoc = null)
        {
            var scheme = _configuration["FlutterApp:DeepLinkScheme"] ?? "itfuture";
            var path = success 
                ? _configuration["FlutterApp:PaymentSuccessPath"] ?? "payment/success"
                : _configuration["FlutterApp:PaymentFailPath"] ?? "payment/fail";
            
            var encodedMessage = Uri.EscapeDataString(message);
            var deepLink = $"{scheme}://{path}?success={success}&message={encodedMessage}";
            
            if (maKhoaHoc.HasValue)
                deepLink += $"&maKhoaHoc={maKhoaHoc}";
                
            return deepLink;
        }
        
        // Trả về trang HTML để redirect về Flutter app
        private ContentResult BuildRedirectPage(bool success, string message, int? maKhoaHoc = null)
        {
            var deepLink = BuildFlutterDeepLink(success, message, maKhoaHoc);
            var statusText = success ? "Thanh toán thành công!" : "Thanh toán thất bại!";
            var statusColor = success ? "#28a745" : "#dc3545";
            var statusIcon = success ? "✓" : "✗";
            
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{statusText}</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }}
        .container {{
            text-align: center;
            background: white;
            padding: 40px;
            border-radius: 20px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            max-width: 400px;
            margin: 20px;
        }}
        .icon {{
            width: 80px;
            height: 80px;
            border-radius: 50%;
            background: {statusColor};
            color: white;
            font-size: 40px;
            display: flex;
            align-items: center;
            justify-content: center;
            margin: 0 auto 20px;
        }}
        h1 {{
            color: {statusColor};
            margin-bottom: 10px;
        }}
        p {{
            color: #666;
            margin-bottom: 30px;
        }}
        .btn {{
            display: inline-block;
            padding: 15px 30px;
            background: {statusColor};
            color: white;
            text-decoration: none;
            border-radius: 10px;
            font-weight: bold;
            transition: transform 0.2s;
        }}
        .btn:hover {{
            transform: scale(1.05);
        }}
        .loading {{
            color: #999;
            font-size: 14px;
            margin-top: 20px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>{statusIcon}</div>
        <h1>{statusText}</h1>
        <p>{message}</p>
        <a href='{deepLink}' class='btn'>Quay về ứng dụng</a>
        <p class='loading'>Đang chuyển hướng về ứng dụng...</p>
    </div>
    <script>
        // Tự động redirect về app sau 1 giây
        setTimeout(function() {{
            window.location.href = '{deepLink}';
        }}, 1000);
        
        // Fallback: thử mở deep link ngay
        window.location.href = '{deepLink}';
    </script>
</body>
</html>";
            
            return new ContentResult
            {
                Content = html,
                ContentType = "text/html",
                StatusCode = 200
            };
        }

        private string GetUserIdFromToken()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            var token = authHeader.Substring("Bearer ".Length);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Token dùng JwtRegisteredClaimNames.Sub nên tìm "sub"
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            return userId;
        }


        [HttpPost]

        public async Task<IActionResult> CreatePaymentMomo([FromBody] OrderInfo model)
        {

            var userID = GetUserIdFromToken();

            if (string.IsNullOrEmpty(userID))
                return Unauthorized(new { success = false, message = "Không xác thực được người dùng" });

            // Check trùng lịch học
            var listLopDangTheoHoc = await _db.ChiTietHocTap
                .Include(l => l.LopHoc).ThenInclude(p => p.KhoaHoc)
                .Where(p => p.UserId == userID && p.LopHoc.NgayKetThuc.Date > DateTime.Now.Date)
                .ToListAsync();

            var KHDangDangKy = await _db.KhoaHoc.FirstOrDefaultAsync(kh => kh.MaKhoaHoc == int.Parse(model.ExtraData));
            if (KHDangDangKy == null)
                return NotFound(new { success = false, message = "Không tìm thấy khóa học" });

            foreach (var item in listLopDangTheoHoc)
            {
                if (item.LopHoc.KhoaHoc.NgayHoc == KHDangDangKy.NgayHoc
                    && (item.LopHoc.NgayKetThuc.Date - DateTime.Now.Date) > TimeSpan.FromDays(5))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Bạn không thể đăng ký lớp học này vì đang trùng giờ học với lớp: {item.LopHoc.TenLopHoc} - {item.LopHoc.KhoaHoc.NgayHoc} (Kết thúc vào: {item.LopHoc.NgayKetThuc:dd/MM/yyyy})"
                    });
                }
            }

            var listDangDangKy = await _db.PhieuDangKyKhoaHoc
                .Where(p => p.UserId == userID && p.TrangThaiDangKy == "Chờ xử lý")
                .Include(p => p.KhoaHoc)
                .ToListAsync();

            foreach (var item in listDangDangKy)
            {
                if (item.KhoaHoc.NgayHoc == KHDangDangKy.NgayHoc)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Bạn không thể đăng ký lớp học này vì phiếu đăng ký lớp {item.KhoaHoc.TenKhoaHoc} - {item.KhoaHoc.NgayHoc} vẫn đang được xử lý"
                    });
                }
            }

            // Thêm userId vào extraData để callback có thể xác định user
            // Format: "maKhoaHoc|userId"
            model.ExtraData = $"{model.ExtraData}|{userID}";
            
            var response = await _momoService.CreatePaymentMomo(model);
            
            if (response.ErrorCode != 0)
            {
                return BadRequest(new 
                { 
                    success = false, 
                    message = response.LocalMessage ?? response.Message ?? "Lỗi tạo thanh toán Momo"
                });
            }

            return Ok(new 
            { 
                success = true, 
                payUrl = response.PayUrl,       // URL cho WebView
                deeplink = response.Deeplink,   // Deep link mở app Momo trực tiếp
                qrCodeUrl = response.QrCodeUrl  // QR code
            });
        }


        
        [HttpGet]
        public async Task<IActionResult> PaymentCallBackMomo()
        {
            var requestQuery = HttpContext.Request.Query;
            
            // Lấy orderId để kiểm tra trùng lặp
            var orderId = requestQuery["orderId"].ToString();
            
            // Lấy userId từ extraData (format: "maKhoaHoc|userId")
            var extraData = requestQuery["extraData"].ToString();
            string userID = null;
            int makh = 0;
            
            if (extraData.Contains("|"))
            {
                var parts = extraData.Split('|');
                makh = int.Parse(parts[0]);
                userID = parts[1];
            }
            else
            {
                makh = int.Parse(extraData);
            }

            if (requestQuery["errorCode"] == "0")
            {
                
                // Kiểm tra xem đã xử lý giao dịch này chưa (tránh duplicate từ ReturnUrl và NotifyUrl)
                var existingPdk = await _db.PhieuDangKyKhoaHoc
                    .FirstOrDefaultAsync(p => p.UserId == userID && p.MaKhoaHoc == makh 
                        && p.NgayDangKy.Date == DateTime.Now.Date);
                
                if (existingPdk != null)
                {
                    // Đã xử lý rồi, chỉ redirect về app
                    return BuildRedirectPage(true, "Đăng ký khóa học thành công!", makh);
                }
                
                
                var hd = new HoaDon()
                {
                    NgayLap = DateTime.Now,
                    UserUserId = userID,
                    TienDongLan1 = decimal.Parse(requestQuery["amount"]),
                    TienDongLan2 = null,
                    NgayDongLan2 = null
                };
                _db.HoaDon.Add(hd);
                await _db.SaveChangesAsync();

                var pdk = new PhieuDangKyKhoaHoc()
                {
                    UserId = userID,
                    MaHoaDon = hd.MaHoaDon,
                    TrangThaiDangKy = "Chờ xử lý",
                    NgayDangKy = DateTime.Now,
                    TrangThaiThanhToan = "Hoàn tất học phí",
                    MaKhoaHoc = makh
                };
                _db.PhieuDangKyKhoaHoc.Add(pdk);
                await _db.SaveChangesAsync();

                // Gửi email
                var user = await _db.User.FirstOrDefaultAsync(p => p.Id == userID);
                var khoaHoc = await _db.KhoaHoc.FirstOrDefaultAsync(k => k.MaKhoaHoc == makh);
                if (user != null && khoaHoc != null)
                {
                    var emailSubject = "TRUNG TÂM TIN HỌC ITFUTURE - Xác nhận đăng ký khóa học";
                    var emailBody = $@"
                    <div style='color:#000;'>
                    <strong>Chúc mừng!</strong> Bạn đã đăng ký khóa học thành công.
                    <h2 style='border-bottom: 1px solid #ccc; padding-bottom: 5px;'><b>Thông tin đăng ký khóa học</b></h2>              
                    <p><strong>Tên khóa học:</strong> {khoaHoc.TenKhoaHoc}</p>
                    <p><strong>Họ tên:</strong> {user.HoTen}</p>
                    <p><strong>Email:</strong> {user.Email}</p>
                    <p><strong>Số điện thoại:</strong> {user.SDT}</p>
                    <p><strong>Ngày đăng ký: </strong>{DateTime.Now:dd/MM/yyyy HH:mm}</p>
                    <p>Chúng tôi sẽ xử lý yêu cầu của bạn trong thời gian sớm nhất.</p>               
                    <p>Trân trọng,</p>
                    <p>Trung Tâm Tin Học ITFuture</p>
                    </div>";
                    _emailServer.SendEmail(user.Email, emailSubject, emailBody);
                }

                // Redirect về Flutter app
                return BuildRedirectPage(true, "Đăng ký khóa học thành công!", makh);
            }
            else
            {
                // Thanh toán thất bại hoặc hủy
                return BuildRedirectPage(false, "Bạn đã hủy thanh toán khóa học!", makh);
            }
        }
    }
}
