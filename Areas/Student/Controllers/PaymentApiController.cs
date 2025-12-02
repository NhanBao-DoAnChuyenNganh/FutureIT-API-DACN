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

        public PaymentApiController(
            ApplicationDbContext db,
            IOptions<AppSettings> options,
            IMomoService momoService,
            EmailServer emailServer)
        {
            _db = db;
            _appSettings = options.Value;
            _momoService = momoService;
            _emailServer = emailServer;
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

            if (!model.FullName.StartsWith("Hoàn tất học phí"))
            {
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
            }

            var response = await _momoService.CreatePaymentMomo(model);
            return Ok(new { success = true, payUrl = response.PayUrl });
        }


        [HttpGet]
        public async Task<IActionResult> PaymentCallBackMomo()
        {
            var userID = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var requestQuery = HttpContext.Request.Query;

            if (requestQuery["errorCode"] == "0")
            {
                if (requestQuery["orderInfo"].ToString().StartsWith("Thanh toán"))
                {
                    string extraData = requestQuery["extraData"];
                    int makh = int.Parse(extraData);

                    var hd = new HoaDon()
                    {
                        NgayLap = DateTime.Now,
                        UserUserId = userID,
                        TienDongLan1 = decimal.Parse(requestQuery["Amount"]),
                        TienDongLan2 = null,
                        NgayDongLan2 = null
                    };
                    _db.HoaDon.Add(hd);
                    await _db.SaveChangesAsync();

                    var trangThaiThanhToan = requestQuery["orderInfo"].ToString().EndsWith("50")
                        ? "Thanh toán 50%"
                        : "Hoàn tất học phí";

                    var pdk = new PhieuDangKyKhoaHoc()
                    {
                        UserId = userID,
                        MaHoaDon = hd.MaHoaDon,
                        TrangThaiDangKy = "Chờ xử lý",
                        NgayDangKy = DateTime.Now,
                        TrangThaiThanhToan = trangThaiThanhToan,
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

                    return Ok(new
                    {
                        success = true,
                        message = "Phiếu đăng ký của bạn đã được ghi nhận! Bạn sẽ được xếp lớp trong khoảng thời gian sắp tới",
                        maKhoaHoc = makh
                    });
                }
                else
                {
                    // Hoàn tất học phí Momo
                    string orderInfo = requestQuery["orderInfo"].ToString();
                    int lastSpaceIndex = orderInfo.LastIndexOf(' ');
                    int maHD = int.Parse(orderInfo.Substring(lastSpaceIndex + 1));

                    var hd = await _db.HoaDon.FirstOrDefaultAsync(p => p.MaHoaDon == maHD);
                    if (hd != null)
                    {
                        hd.NgayDongLan2 = DateTime.Now;
                        hd.TienDongLan2 = decimal.Parse(requestQuery["Amount"]);
                        await _db.SaveChangesAsync();
                    }

                    var pdk = await _db.PhieuDangKyKhoaHoc
                        .Include(p => p.KhoaHoc)
                        .FirstOrDefaultAsync(p => p.MaHoaDon == maHD);
                    if (pdk != null)
                    {
                        pdk.TrangThaiThanhToan = "Hoàn tất học phí";
                        await _db.SaveChangesAsync();

                        // Gửi email
                        var user = await _db.User.FirstOrDefaultAsync(p => p.Id == userID);
                        var khoaHoc = await _db.KhoaHoc.FirstOrDefaultAsync(k => k.MaKhoaHoc == pdk.MaKhoaHoc);
                        if (user != null && khoaHoc != null && hd != null)
                        {
                            var emailSubject = "TRUNG TÂM TIN HỌC ITFUTURE - Xác nhận hoàn tất học phí khóa học";
                            var emailBody = $@"
                            <div style='color:#000;'>
                            <strong>Thông báo!</strong> Bạn vừa hoàn tất học phí của khóa học {khoaHoc.TenKhoaHoc}.
                            <h2 style='border-bottom: 1px solid #ccc; padding-bottom: 5px;'><b>Thông tin thanh toán</b></h2>
                            <p><strong>Họ tên:</strong> {user.HoTen}</p>
                            <p><strong>Email:</strong> {user.Email}</p>
                            <p><strong>Số điện thoại:</strong> {user.SDT}</p>
                            <p><strong>Tên khóa học:</strong> {khoaHoc.TenKhoaHoc},<br/>Tổng học phí: {khoaHoc.HocPhi}</p>
                            <p><strong>Ngày đóng lần đầu: </strong>{hd.NgayLap:dd/MM/yyyy HH:mm}</p>
                            <p><strong>Tiền đóng lần đầu: </strong>{hd.TienDongLan1:N0} VND</p>
                            <p><strong>Ngày hoàn tất: </strong>{hd.NgayDongLan2:dd/MM/yyyy HH:mm}</p>
                            <p><strong>Tiền đóng lần 2: </strong>{hd.TienDongLan2:N0} VND</p>              
                            <p>Trân trọng,</p>
                            <p>Trung Tâm Tin Học ITFuture</p>
                            </div>";
                            _emailServer.SendEmail(user.Email, emailSubject, emailBody);
                        }

                        return Ok(new
                        {
                            success = true,
                            message = $"Bạn đã hoàn tất thanh toán cho khóa học {pdk.KhoaHoc?.TenKhoaHoc}"
                        });
                    }

                    return BadRequest(new { success = false, message = "Không tìm thấy phiếu đăng ký" });
                }
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Bạn đã hủy thanh toán khóa học!",
                    maKhoaHoc = int.TryParse(requestQuery["extraData"], out int id) ? id : 0
                });
            }
        }
    }
}
