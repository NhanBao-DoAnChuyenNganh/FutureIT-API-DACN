using DoAnCoSo_Web.Areas.Student.MomoService;
using Microsoft.AspNetCore.Mvc;
using DoAnCoSo_Web.Models;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
using DoAnCoSo_Web.Data;
using System;
using DoAnCoSo_Web.Areas.Student.VnpayService;
using DoAnCoSo_Web.Models.Vnpay;
using Microsoft.AspNetCore.Authorization;
using DoAnCoSo_Web.Areas.Student.ViewModels;
using Microsoft.Extensions.Configuration.UserSecrets;
using DoAnCoSo_Web.Areas.Student.EmailService;
using Microsoft.EntityFrameworkCore;
namespace DoAnCoSo_Web.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize (Roles = "Student")]
    public class PaymentController : Controller
    {
        private IMomoService _momoService;
        private readonly IVnPayService _vnPayService;
        private readonly ApplicationDbContext _db;
        private readonly EmailServer _emailServer;

        public PaymentController(IMomoService momoService, ApplicationDbContext db, IVnPayService vnPayService, EmailServer emailServer)
        {
            _momoService = momoService;
            _db = db;
            _vnPayService = vnPayService;
            _emailServer = emailServer;
        }



        [HttpPost]
        public async Task<IActionResult> CreatePaymentMomo(OrderInfo model)
        {
            if (model.FullName.StartsWith("Hoàn tất học phí"))
            {
                //hoàn tất học phí = không kiểm tra lịch học
            }
            else
            {
                //check user Student đang có lịch bị trùng với KH định đăng ký hay ko
                var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                List<ChiTietHocTap> listLopDangTheoHoc = _db.ChiTietHocTap.Include(l => l.LopHoc).ThenInclude(p => p.KhoaHoc)
                                                        .Where(p => p.UserId == userID && p.LopHoc.NgayKetThuc.Date > DateTime.Now.Date).ToList();
                KhoaHoc KHDangDangKy = _db.KhoaHoc.FirstOrDefault(kh => kh.MaKhoaHoc == int.Parse(model.ExtraData));
                foreach (var item in listLopDangTheoHoc)
                {
                    //nếu định đăng ký 1KH có ngày học trùng với các khóa học theo học
                    //thì các khóa đang theo học phải gần kết thúc mới cho đăng ký
                    if (item.LopHoc.KhoaHoc.NgayHoc == KHDangDangKy.NgayHoc
                        && (item.LopHoc.NgayKetThuc.Date - DateTime.Now.Date) > TimeSpan.FromDays(5))
                    {
                        var message = "Bạn không thể đăng ký lớp học này vì đang trùng giờ học các lớp sau:<br/><br/>";
                        message += $"<span style='color:red;'>Lớp: {item.LopHoc.TenLopHoc} - <strong>{item.LopHoc.KhoaHoc.NgayHoc}</strong> - (Kết thúc vào: {item.LopHoc.NgayKetThuc:dd/MM/yyyy})</span><br/><br/>";
                        message += "Khi lớp học trên gần kết thúc thì hãy đăng ký thêm bạn nhé 😉";
                        TempData["RegisterFail"] = message;
                        return RedirectToAction("XemChiTiet", "StudentHome", new { id = KHDangDangKy.MaKhoaHoc });
                    }
                }

                List<PhieuDangKyKhoaHoc> listDangDangKy = _db.PhieuDangKyKhoaHoc.Where(p => p.UserId == userID && p.TrangThaiDangKy == "Chờ xử lý").Include(p => p.KhoaHoc).ToList();
                foreach (var item in listDangDangKy)
                {
                    if (item.KhoaHoc.NgayHoc == KHDangDangKy.NgayHoc)
                    {
                        var message = $"Bạn không thể đăng ký lớp học này vì phiếu đăng ký lớp <br/><br/><span style='color:red;'>{item.KhoaHoc.TenKhoaHoc} - {item.KhoaHoc.NgayHoc}</span> <br/><br/>vẫn đang được xử lý";
                        TempData["RegisterFail"] = message;
                        return RedirectToAction("XemChiTiet", "StudentHome", new { id = KHDangDangKy.MaKhoaHoc });
                    }
                }
            }

            var response = await _momoService.CreatePaymentMomo(model);
            return Redirect(response.PayUrl);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallBack()
        {
            var response = _momoService.PaymentExecuteAsync(HttpContext.Request.Query);
            var resquestQuery = HttpContext.Request.Query;
            //return Json(resquestQuery);
            
            if (resquestQuery["errorCode"] == "0")
                {
                
                if (resquestQuery["orderInfo"].ToString().StartsWith("Thanh toán"))
                {
                    string extraData = resquestQuery["extraData"];
                    int makh = int.Parse(extraData);
                    HoaDon hd = new HoaDon()
                    {
                        NgayLap = DateTime.Now,
                        UserUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                        TienDongLan1 = decimal.Parse(resquestQuery["Amount"]),
                        TienDongLan2 = null,
                        NgayDongLan2 = null
                    };
                    _db.HoaDon.Add(hd);
                    await _db.SaveChangesAsync();

                    if (resquestQuery["orderInfo"].ToString().EndsWith("50"))
                    {
                        PhieuDangKyKhoaHoc pdk = new PhieuDangKyKhoaHoc()
                        {
                            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                            MaHoaDon = hd.MaHoaDon,
                            TrangThaiDangKy = "Chờ xử lý",
                            NgayDangKy = DateTime.Now,
                            TrangThaiThanhToan = "Thanh toán 50%",
                            MaKhoaHoc = makh
                        };
                        _db.PhieuDangKyKhoaHoc.Add(pdk);
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        PhieuDangKyKhoaHoc pdk = new PhieuDangKyKhoaHoc()
                        {
                            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                            MaHoaDon = hd.MaHoaDon,
                            TrangThaiDangKy = "Chờ xử lý",
                            NgayDangKy = DateTime.Now,
                            TrangThaiThanhToan = "Hoàn tất học phí",
                            MaKhoaHoc = makh
                        };
                        _db.PhieuDangKyKhoaHoc.Add(pdk);
                        await _db.SaveChangesAsync();
                    }

                    //gửi email cho khách hàng
                    var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var user = _db.User.FirstOrDefault(p => p.Id == userID);
                    var khoaHoc = _db.KhoaHoc.FirstOrDefault(k => k.MaKhoaHoc == makh);
                    var ngayDangKy = DateTime.Now;
                    var emailSubject = "TRUNG TÂM TIN HỌC ITFUTURE - Xác nhận đăng ký khóa học";
                    var emailBody = $@"
                    <div style=' color:#000;'>
                    <strong>Chúc mừng!</strong> Bạn đã đăng ký khóa học thành công.
                    <h2 style='border-bottom: 1px solid #ccc; padding-bottom: 5px;'><b>Thông tin đăng ký khóa học</b></h2>              
                    <p><strong>Tên khóa học:</strong> {khoaHoc.TenKhoaHoc}</p>
                    <p><strong>Họ tên:</strong> {user.HoTen}</p>
                    <p><strong>Email:</strong> {user.Email}</p>
                    <p><strong>Số điện thoại:</strong> {user.SDT}</p>
                    <p><strong>Ngày đăng ký: </strong>{ngayDangKy.ToString("dd/MM/yyyy HH:mm")}</p>
                    <p>Chúng tôi sẽ xử lý yêu cầu của bạn trong thời gian sớm nhất.</p>               
                    <p>Trân trọng,</p>
                    <p>Trung Tâm Tin Học ITFuture</p>
                    </div>";

                    _emailServer.SendEmail(user.Email, emailSubject, emailBody);
                    //trở về trang xem chi tiết của khóa học
                    TempData["MomoSuccess"] = "Phiếu đăng ký của bạn đã được ghi nhận! <br/>Bạn sẽ được xếp lớp trong khoảng thời gian sắp tới";
                    return RedirectToAction("XemChiTiet", "StudentHome", new { id = makh });
                }
                else
                {
                    //xử lý hoàn tất học phí Momo
                    string orderInfo = resquestQuery["orderInfo"].ToString();
                    int lastSpaceIndex = orderInfo.LastIndexOf(' ');

                    int maHD = int.Parse(orderInfo.Substring(lastSpaceIndex + 1));
                    HoaDon hd = _db.HoaDon.FirstOrDefault(p => p.MaHoaDon == maHD);
                    hd.NgayDongLan2 = DateTime.Now;
                    hd.TienDongLan2 = decimal.Parse(resquestQuery["Amount"]);
                    _db.SaveChanges();
                    PhieuDangKyKhoaHoc pdk = _db.PhieuDangKyKhoaHoc.Include(p => p.KhoaHoc).FirstOrDefault(p => p.MaHoaDon == maHD);
                    pdk.TrangThaiThanhToan = "Hoàn tất học phí";
                    _db.SaveChanges();

                    //gửi email cho khách hàng
                    var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var user = _db.User.FirstOrDefault(p => p.Id == userID);
                    var khoaHoc = _db.KhoaHoc.FirstOrDefault(k => k.MaKhoaHoc == pdk.MaKhoaHoc);
                    var emailSubject = "TRUNG TÂM TIN HỌC ITFUTURE - Xác nhận hoàn tất học phí khóa học";
                    var emailBody = $@"
                    <div style=' color:#000;'>
                    <strong>Thông báo!</strong> Bạn vừa hoàn tất học phí của khóa học {khoaHoc.TenKhoaHoc}.
                    <h2 style='border-bottom: 1px solid #ccc; padding-bottom: 5px;'><b>Thông tin thanh toán</b></h2>
                    <p><strong>Họ tên:</strong> {user.HoTen}</p>
                    <p><strong>Email:</strong> {user.Email}</p>
                    <p><strong>Số điện thoại:</strong> {user.SDT}</p>
                    <p><strong>Tên khóa học:</strong> {khoaHoc.TenKhoaHoc},<br/>Tổng học phí: {khoaHoc.HocPhi}</p>
                    <p><strong>Ngày đóng lần đầu: </strong>{hd.NgayLap.ToString("dd/MM/yyyy HH:mm")}</p>
                    <p><strong>Tiền đóng lần đầu: </strong>{hd.TienDongLan1.ToString("N0")} VND</p>
                    <p><strong>Ngày hoàn tất: </strong>{hd.NgayDongLan2?.ToString("dd/MM/yyyy HH:mm")}</p>
                    <p><strong>Tiền đóng lần 2: </strong>{hd.TienDongLan2?.ToString("N0")} VND</p>              
                    <p>Trân trọng,</p>
                    <p>Trung Tâm Tin Học ITFuture</p>
                    </div>";
                    _emailServer.SendEmail(user.Email, emailSubject, emailBody);

                    TempData["Complete"] = $"Bạn đã hoàn tất thanh toán cho khóa học <br/> <span style='color: green;'> {pdk.KhoaHoc.TenKhoaHoc} </span>";
                    return RedirectToAction("KhoaHocDaDangKy", "StudentHome");
                }
            }
            else
            {
                TempData["MomoCancel"] = "Bạn đã hủy thanh toán khóa học!";
                return RedirectToAction("XemChiTiet", "StudentHome", new { id = int.Parse(resquestQuery["extraData"]) });
            }
        }
        [HttpPost]
        public IActionResult CreatePaymentUrlVnpay(PaymentInformationModel model, int MaKHDK)
        {
            if (model.OrderDescription.StartsWith("Hoàn tất học phí"))
            {
                //hoàn tất học phí = không kiểm tra lịch học
            }
            else
            {
                var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                List<ChiTietHocTap> listLopDangTheoHoc = _db.ChiTietHocTap.Include(l => l.LopHoc).ThenInclude(p => p.KhoaHoc)
                                                        .Where(p => p.UserId == userID && p.LopHoc.NgayKetThuc.Date > DateTime.Now.Date).ToList();
                KhoaHoc KHDangDangKy = _db.KhoaHoc.FirstOrDefault(kh => kh.MaKhoaHoc == MaKHDK);
                foreach (var item in listLopDangTheoHoc)
                {
                    //nếu định đăng ký 1KH có ngày học trùng với các khóa học theo học
                    //thì các khóa đang theo học phải gần kết thúc mới cho đăng ký
                    if (item.LopHoc.KhoaHoc.NgayHoc == KHDangDangKy.NgayHoc
                        && (item.LopHoc.NgayKetThuc.Date - DateTime.Now.Date) > TimeSpan.FromDays(5))
                    {
                        var message = "Bạn không thể đăng ký lớp học này vì đang trùng giờ học lớp các lớp sau:<br/><br/>";
                        message += $"<span style='color:red;'>Lớp: {item.LopHoc.TenLopHoc} - <strong>{item.LopHoc.KhoaHoc.NgayHoc}</strong> - (Kết thúc vào: {item.LopHoc.NgayKetThuc:dd/MM/yyyy})</span><br/><br/>";
                        message += "Khi lớp học trên gần kết thúc thì hãy đăng ký thêm bạn nhé 😉";
                        TempData["RegisterFail"] = message;
                        return RedirectToAction("XemChiTiet", "StudentHome", new { id = KHDangDangKy.MaKhoaHoc });
                    }
                }

                List<PhieuDangKyKhoaHoc> listDangDangKy = _db.PhieuDangKyKhoaHoc.Where(p => p.UserId == userID && p.TrangThaiDangKy == "Chờ xử lý").Include(p => p.KhoaHoc).ToList();
                foreach (var item in listDangDangKy)
                {
                    if (item.KhoaHoc.NgayHoc == KHDangDangKy.NgayHoc)
                    {
                        var message = $"Bạn không thể đăng ký lớp học này vì phiếu đăng ký lớp <br/><br/><span style='color:red;'>{item.KhoaHoc.TenKhoaHoc} - {item.KhoaHoc.NgayHoc}</span> <br/><br/>vẫn đang được xử lý";
                        TempData["RegisterFail"] = message;
                        return RedirectToAction("XemChiTiet", "StudentHome", new { id = KHDangDangKy.MaKhoaHoc });
                    }
                }
            }

            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);
            return Redirect(url);
        }



        public IActionResult PaymentCallBackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            int makh;
            if (response.VnPayResponseCode == "00")
            {
                if(response.OrderDescription.StartsWith("Hoan tat hoc phi"))
                {
                    string OrderDescription = response.OrderDescription;
                    int lastSpaceIndex = OrderDescription.LastIndexOf(' ');

                    int maHD = int.Parse(OrderDescription.Substring(lastSpaceIndex + 1));
                    HoaDon hd = _db.HoaDon.FirstOrDefault(p => p.MaHoaDon == maHD);
                    hd.NgayDongLan2 = DateTime.Now;
                    hd.TienDongLan2 = decimal.Parse(Request.Query["vnp_Amount"]) / 100;
                    _db.SaveChanges();

                    PhieuDangKyKhoaHoc pdk = _db.PhieuDangKyKhoaHoc.Include(p => p.KhoaHoc).FirstOrDefault(p => p.MaHoaDon == maHD);
                    pdk.TrangThaiThanhToan = "Hoàn tất học phí";
                    _db.SaveChanges();

                    //gửi email cho khách hàng
                    var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var user = _db.User.FirstOrDefault(p => p.Id == userID);
                    var khoaHoc = _db.KhoaHoc.FirstOrDefault(k => k.MaKhoaHoc == pdk.MaKhoaHoc);
                    var emailSubject = "TRUNG TÂM TIN HỌC ITFUTURE - Xác nhận hoàn tất học phí khóa học";
                    var emailBody = $@"
                    <div style=' color:#000;'>
                    <strong>Thông báo!</strong> Bạn vừa hoàn tất học phí của khóa học {khoaHoc.TenKhoaHoc}.
                    <h2 style='border-bottom: 1px solid #ccc; padding-bottom: 5px;'><b>Thông tin thanh toán</b></h2>
                    <p><strong>Họ tên:</strong> {user.HoTen}</p>
                    <p><strong>Email:</strong> {user.Email}</p>
                    <p><strong>Số điện thoại:</strong> {user.SDT}</p>
                    <p><strong>Tên khóa học:</strong> {khoaHoc.TenKhoaHoc},<br/>Tổng học phí: {khoaHoc.HocPhi}</p>
                    <p><strong>Ngày đóng lần đầu: </strong>{hd.NgayLap.ToString("dd/MM/yyyy HH:mm")}</p>
                    <p><strong>Tiền đóng lần đầu: </strong>{hd.TienDongLan1.ToString("N0")} VND</p>
                    <p><strong>Ngày hoàn tất: </strong>{hd.NgayDongLan2?.ToString("dd/MM/yyyy HH:mm")}</p>
                    <p><strong>Tiền đóng lần 2: </strong>{hd.TienDongLan2?.ToString("N0")} VND</p>              
                    <p>Trân trọng,</p>
                    <p>Trung Tâm Tin Học ITFuture</p>
                    </div>";
                    _emailServer.SendEmail(user.Email, emailSubject, emailBody);

                    TempData["Complete"] = $"Bạn đã hoàn tất thanh toán cho khóa học <br/> <span style='color: green;'> {pdk.KhoaHoc.TenKhoaHoc} </span>";
                    return RedirectToAction("KhoaHocDaDangKy", "StudentHome");
                }
                else
                {
                    HoaDon hd = new HoaDon()
                    {
                        NgayLap = DateTime.Now,
                        UserUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                        TienDongLan1 = decimal.Parse(Request.Query["vnp_Amount"]) / 100,
                        TienDongLan2 = null,
                        NgayDongLan2 = null
                    };
                    _db.HoaDon.Add(hd);
                    _db.SaveChanges();

                    if (response.OrderDescription.EndsWith("50"))//HoaDon đóng 50%
                    {
                        PhieuDangKyKhoaHoc pdk = new PhieuDangKyKhoaHoc()
                        {
                            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                            MaKhoaHoc = int.Parse(response.OrderDescription.Substring(0, response.OrderDescription.IndexOf(" "))),
                            MaHoaDon = hd.MaHoaDon,
                            TrangThaiDangKy = "Chờ xử lý",
                            NgayDangKy = DateTime.Now,
                            TrangThaiThanhToan = "Thanh toán 50%"
                        };
                        makh = pdk.MaKhoaHoc;
                        _db.PhieuDangKyKhoaHoc.Add(pdk);
                        _db.SaveChanges();
                    }
                    else
                    {
                        PhieuDangKyKhoaHoc pdk = new PhieuDangKyKhoaHoc()//HoaDon đóng đủ
                        {
                            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                            MaKhoaHoc = int.Parse(response.OrderDescription.Substring(0, response.OrderDescription.IndexOf(" "))),
                            MaHoaDon = hd.MaHoaDon,
                            TrangThaiDangKy = "Chờ xử lý",
                            NgayDangKy = DateTime.Now,
                            TrangThaiThanhToan = "Hoàn tất học phí"
                        };
                        makh = pdk.MaKhoaHoc;
                        _db.PhieuDangKyKhoaHoc.Add(pdk);
                        _db.SaveChanges();
                    }

                    //gửi email cho khách hàng
                    var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var user = _db.User.FirstOrDefault(p => p.Id == userID);
                    var khoaHoc = _db.KhoaHoc.FirstOrDefault(k => k.MaKhoaHoc == makh);
                    var ngayDangKy = DateTime.Now;
                    var emailSubject = "TRUNG TÂM TIN HỌC ITFUTURE - Xác nhận đăng ký khóa học";
                    var emailBody = $@"
                    <div style=' color:#000;'>
                    <strong>Chúc mừng!</strong> Bạn đã đăng ký khóa học thành công.
                    <h2 style='border-bottom: 1px solid #ccc; padding-bottom: 5px;'><b>Thông tin đăng ký khóa học</b></h2>              
                    <p><strong>Tên khoa học:</strong> {khoaHoc.TenKhoaHoc}</p>
                    <p><strong>Họ tên:</strong> {user.HoTen}</p>
                    <p><strong>Email:</strong> {user.Email}</p>
                    <p><strong>Số điện thoại:</strong> {user.SDT}</p>
                    <p><strong>Ngày đăng ký: </strong>{ngayDangKy.ToString("dd/MM/yyyy HH:mm")}</p>
                    <p>Chúng tôi sẽ xử lý yêu cầu của bạn trong thời gian sớm nhất.</p>               
                    <p>Trân trọng,</p>
                    <p>Trung Tâm Tin Học ITFuture</p>
                    </div>";

                    _emailServer.SendEmail(user.Email, emailSubject, emailBody);

                    TempData["VnpaySuccess"] = "Phiếu đăng ký của bạn đã được ghi nhận! <br/>Bạn sẽ được xếp lớp trong khoảng thời gian sắp tới";
                    return RedirectToAction("XemChiTiet", "StudentHome", new { id = makh });
                }

            }
            else
            {
                if (response.VnPayResponseCode == "24")
                {
                    TempData["VnpayCancel"] = "Giao dịch không thành công! <br/>Bạn đã hủy giao dịch";
                }
                else
                {
                    TempData["VnpayCancel"] = "Giao dịch không thành công! <br/>Lỗi: "+ response.VnPayResponseCode;
                }
                return RedirectToAction("XemChiTiet", "StudentHome", new { id = int.Parse(response.OrderDescription.Substring(0, response.OrderDescription.IndexOf(" "))) });
            }
           
        }

    }
}
