
using System.Security.Claims;

using DoAnCoSo_Web.Areas.Student.ViewModels;
using DoAnCoSo_Web.Data;
using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo_Web.Areas.Student.EmailService;
using static System.Net.Mime.MediaTypeNames;

namespace DoAnCoSo_Web.Areas.Student.Controllers
{
    [Area("Student")]
    public class StudentHomeController : Controller
    {

        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly EmailServer _emailService;
        public StudentHomeController(ApplicationDbContext db, UserManager<User> userManager, EmailServer emailService)
        {
            _db = db;
            _userManager = userManager;
            _emailService = emailService;
        }

        
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated) // Kiểm tra user có đăng nhập không
            {
                var userRoles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();

                if (userRoles.Contains("Teacher")) //teacher = redirect sang trang teacher
                {
                    return RedirectToAction("Index", "TeacherHome", new { area = "Teacher" });
                }
                if (userRoles.Contains("Staff")) //staff = redirect sang trang Staff
                {
                    return RedirectToAction("Index", "StaffHome", new { area = "Staff" });
                }
                if (userRoles.Contains("Admin")) //admin = redirect sang trang admin
                {
                    return RedirectToAction("Index2", "AdminHome", new { area = "Admin" });
                }
            }
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //check user Student có đang nợ học phí ở lớp học nào ko

            var listConNoHP = _db.PhieuDangKyKhoaHoc.Where(p => p.TrangThaiThanhToan == "Thanh toán 50%" && p.UserId == userid && p.TrangThaiDangKy == "Đã xếp lớp" && p.NgayDangKy.Date.AddDays(60) < DateTime.Now.Date).ToList();
            if(listConNoHP.Count > 0)
            {
                TempData["ThongBaoNo"] = "Bạn vui lòng thanh toán các khóa học còn nợ trước khi đăng ký khóa học mới";
                return RedirectToAction("KhoaHocDaDangKy", "StudentHome");
            }

            var list2TinMoiNhat = _db.TinTucTuyenDung.OrderByDescending(t => t.NgayDang).Take(2).ToList();
            var listPhoBien = _db.KhoaHoc.Where(p => p.MaKhoaHoc == 3 || p.MaKhoaHoc == 5 || p.MaKhoaHoc == 7).Include(h => h.HinhAnhKhoaHoc).ToList();


            IndexViewModels viewmodel = new IndexViewModels()
            {
                TinMoi1 = list2TinMoiNhat[0],
                TinMoi2 = list2TinMoiNhat[1],
                list3KhoaHocPhoBien = listPhoBien,
                GVTB1 = _db.User.Include(p => p.ChuyenNganh).FirstOrDefault(p => p.ChuyenNganhMaChuyenNganh == 2),
                GVTB2 = _db.User.Include(p => p.ChuyenNganh).FirstOrDefault(p => p.ChuyenNganhMaChuyenNganh == 3),
                GVTB3 = _db.User.Include(p => p.ChuyenNganh).FirstOrDefault(p => p.ChuyenNganhMaChuyenNganh == 4),
                GVTB4 = _db.User.Include(p => p.ChuyenNganh).FirstOrDefault(p => p.ChuyenNganhMaChuyenNganh == 5),
            };

            return View(viewmodel);
          
        }
        double TinhTrungBinhSao(int makh)
        {
            var listSoSao = _db.DanhGia.Where(p => p.KhoaHocMaKhoaHoc == makh).Select(p => p.SoSaoDanhGia).ToList();
            var tongLuot = listSoSao.Count();
            var TongSoSao = listSoSao.Sum();
            var TrungBinhSao = tongLuot > 0 ? (double)TongSoSao / tongLuot : 0;
            return LamTron(TrungBinhSao);
        }

        [AllowAnonymous]
        public IActionResult KhoaHoc()
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //check user Student có đang nợ học phí ở lớp học nào ko
            var listConNoHP = _db.PhieuDangKyKhoaHoc.Where(p => p.TrangThaiThanhToan == "Thanh toán 50%" && p.UserId == userid && p.TrangThaiDangKy == "Đã xếp lớp" && p.NgayDangKy.Date.AddDays(60) < DateTime.Now.Date).ToList();
            if (listConNoHP.Count > 0)
            {
                TempData["ThongBaoNo"] = "Bạn vui lòng thanh toán các khóa học còn nợ trước khi đăng ký khóa học mới";
                return RedirectToAction("KhoaHocDaDangKy", "StudentHome");
            }


            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var listKhoaHoc = _db.KhoaHoc.Include(p => p.HinhAnhKhoaHoc).OrderBy(p => p.TenKhoaHoc).ToList();
            var listDaQuanTam = _db.DanhSachQuanTam.Where(ds => ds.UserId == userId).Select(ds => ds.MaKhoaHoc).ToList();
            KhoaHocViewModels model = new KhoaHocViewModels
            {
                listKhoaHoc = listKhoaHoc.Select(kh => new sub_KhoaHocViewModels
                {
                    KhoaHoc = kh,
                    TongLuotQuanTam = _db.DanhSachQuanTam.Count(q => q.MaKhoaHoc == kh.MaKhoaHoc),
                    TongLuotBinhLuan = _db.DanhGia.Count(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc),
                    DaYeuThich = listDaQuanTam.Contains(kh.MaKhoaHoc),
                    SoSaoTrungBinh = TinhTrungBinhSao(kh.MaKhoaHoc)
                }).ToList()
            };
            return View(model);
        }
        
        [AllowAnonymous]
        public IActionResult LocTheoLoai(string TenLoai)
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var listKhoaHocTheoLoai = _db.KhoaHoc.Where(p => p.TenKhoaHoc.ToLower().Contains(TenLoai)).Include(p => p.HinhAnhKhoaHoc).ToList();
            var listDaQuanTam = _db.DanhSachQuanTam.Where(ds => ds.UserId == userId).Select(ds => ds.MaKhoaHoc).ToList();
            if (string.IsNullOrEmpty(TenLoai))
            {
                return RedirectToAction("KhoaHoc");
            }

            KhoaHocViewModels model = new KhoaHocViewModels
            {
                listKhoaHoc = listKhoaHocTheoLoai.Select(kh => new sub_KhoaHocViewModels
                {
                    KhoaHoc = kh,
                    TongLuotQuanTam = _db.DanhSachQuanTam.Count(q => q.MaKhoaHoc == kh.MaKhoaHoc),
                    TongLuotBinhLuan = _db.DanhGia.Count(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc),
                    DaYeuThich = listDaQuanTam.Contains(kh.MaKhoaHoc),
                    SoSaoTrungBinh = TinhTrungBinhSao(kh.MaKhoaHoc)
                }).ToList()
            };

            return View("KhoaHoc", model);
            
        }

        [AllowAnonymous]        
        public IActionResult LocTheoGia(int LuaChon = 0)
        {
            switch (LuaChon)
            {
                case 1:
                    //Giá cao -> thấp
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var listKhoaHoc = _db.KhoaHoc.Include(p => p.HinhAnhKhoaHoc).OrderByDescending(p => p.HocPhi).ToList();
                    var listDaQuanTam = _db.DanhSachQuanTam.Where(ds => ds.UserId == userId).Select(ds => ds.MaKhoaHoc).ToList();
                    KhoaHocViewModels model = new KhoaHocViewModels
                    {
                        listKhoaHoc = listKhoaHoc.Select(kh => new sub_KhoaHocViewModels
                        {
                            KhoaHoc = kh,
                            TongLuotQuanTam = _db.DanhSachQuanTam.Count(q => q.MaKhoaHoc == kh.MaKhoaHoc),
                            TongLuotBinhLuan = _db.DanhGia.Count(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc),
                            DaYeuThich = listDaQuanTam.Contains(kh.MaKhoaHoc),
                            SoSaoTrungBinh = TinhTrungBinhSao(kh.MaKhoaHoc)
                        }).ToList()
                    };
                    ViewBag.LuaChon = 1;
                    return View("KhoaHoc", model);

                case 2:
                    //Giá thấp -> cao
                    var userId2 = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var listKhoaHoc2 = _db.KhoaHoc.Include(p => p.HinhAnhKhoaHoc).OrderBy(p => p.HocPhi).ToList();
                    var listDaQuanTam2 = _db.DanhSachQuanTam.Where(ds => ds.UserId == userId2).Select(ds => ds.MaKhoaHoc).ToList();
                    KhoaHocViewModels model2 = new KhoaHocViewModels
                    {
                        listKhoaHoc = listKhoaHoc2.Select(kh => new sub_KhoaHocViewModels
                        {
                            KhoaHoc = kh,
                            TongLuotQuanTam = _db.DanhSachQuanTam.Count(q => q.MaKhoaHoc == kh.MaKhoaHoc),
                            TongLuotBinhLuan = _db.DanhGia.Count(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc),
                            DaYeuThich = listDaQuanTam2.Contains(kh.MaKhoaHoc),
                            SoSaoTrungBinh = TinhTrungBinhSao(kh.MaKhoaHoc)
                        }).ToList()
                    };
                    ViewBag.LuaChon = 2;
                    return View("KhoaHoc", model2);
                case 3:
                    //Dưới 5.000.000 VND
                    var userId3= User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var listKhoaHoc3 = _db.KhoaHoc.Include(p => p.HinhAnhKhoaHoc).Where(p => p.HocPhi < 5000000).OrderBy(p => p.HocPhi).ToList();
                    var listDaQuanTam3 = _db.DanhSachQuanTam.Where(ds => ds.UserId == userId3).Select(ds => ds.MaKhoaHoc).ToList();
                    KhoaHocViewModels model3 = new KhoaHocViewModels
                    {
                        listKhoaHoc = listKhoaHoc3.Select(kh => new sub_KhoaHocViewModels
                        {
                            KhoaHoc = kh,
                            TongLuotQuanTam = _db.DanhSachQuanTam.Count(q => q.MaKhoaHoc == kh.MaKhoaHoc),
                            TongLuotBinhLuan = _db.DanhGia.Count(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc),
                            DaYeuThich = listDaQuanTam3.Contains(kh.MaKhoaHoc),
                            SoSaoTrungBinh = TinhTrungBinhSao(kh.MaKhoaHoc)
                        }).ToList()
                    };
                    ViewBag.LuaChon = 3;
                    return View("KhoaHoc", model3);
                case 4:
                    //Từ 5.000.000 -> 7.000.000 VND
                    var userId4 = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var listKhoaHoc4 = _db.KhoaHoc.Include(p => p.HinhAnhKhoaHoc).Where(p => p.HocPhi >= 5000000 && p.HocPhi <7000000).OrderBy(p => p.HocPhi).ToList();
                    var listDaQuanTam4 = _db.DanhSachQuanTam.Where(ds => ds.UserId == userId4).Select(ds => ds.MaKhoaHoc).ToList();
                    KhoaHocViewModels model4 = new KhoaHocViewModels
                    {
                        listKhoaHoc = listKhoaHoc4.Select(kh => new sub_KhoaHocViewModels
                        {
                            KhoaHoc = kh,
                            TongLuotQuanTam = _db.DanhSachQuanTam.Count(q => q.MaKhoaHoc == kh.MaKhoaHoc),
                            TongLuotBinhLuan = _db.DanhGia.Count(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc),
                            DaYeuThich = listDaQuanTam4.Contains(kh.MaKhoaHoc),
                            SoSaoTrungBinh = TinhTrungBinhSao(kh.MaKhoaHoc)
                        }).ToList()
                    };
                    ViewBag.LuaChon = 4;
                    return View("KhoaHoc", model4);
                case 5:
                    //Từ 10.000.000 VND trở lên
                    var userId5 = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var listKhoaHoc5 = _db.KhoaHoc.Include(p => p.HinhAnhKhoaHoc).Where(p => p.HocPhi >= 7000000).OrderBy(p => p.HocPhi).ToList();
                    var listDaQuanTam5 = _db.DanhSachQuanTam.Where(ds => ds.UserId == userId5).Select(ds => ds.MaKhoaHoc).ToList();
                    KhoaHocViewModels model5 = new KhoaHocViewModels
                    {
                        listKhoaHoc = listKhoaHoc5.Select(kh => new sub_KhoaHocViewModels
                        {
                            KhoaHoc = kh,
                            TongLuotQuanTam = _db.DanhSachQuanTam.Count(q => q.MaKhoaHoc == kh.MaKhoaHoc),
                            TongLuotBinhLuan = _db.DanhGia.Count(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc),
                            DaYeuThich = listDaQuanTam5.Contains(kh.MaKhoaHoc),
                            SoSaoTrungBinh = TinhTrungBinhSao(kh.MaKhoaHoc)
                        }).ToList()
                    };
                    ViewBag.LuaChon = 5;
                    return View("KhoaHoc", model5);
                case 0:
                    return RedirectToAction("KhoaHoc");
            }
            return View();
        }

        [AllowAnonymous]
        public IActionResult TimKiemKhoaHoc(string Search)
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //check user Student có đang nợ học phí ở lớp học nào ko
            var listConNoHP = _db.PhieuDangKyKhoaHoc.Where(p => p.TrangThaiThanhToan == "Thanh toán 50%" && p.UserId == userid && p.TrangThaiDangKy == "Đã xếp lớp" && p.NgayDangKy.Date.AddDays(60) < DateTime.Now.Date).ToList();
            if (listConNoHP.Count > 0)
            {
                TempData["ThongBaoNo"] = "Bạn vui lòng thanh toán các khóa học còn nợ trước khi đăng ký khóa học mới";
                return RedirectToAction("KhoaHocDaDangKy", "StudentHome");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var listKhoaHocDaTim = _db.KhoaHoc.Where(p => p.TenKhoaHoc.ToLower().Contains(Search.ToLower())).Include(p => p.HinhAnhKhoaHoc).ToList();
            var listDaQuanTam = _db.DanhSachQuanTam.Where(ds => ds.UserId == userId).Select(ds => ds.MaKhoaHoc).ToList();
            KhoaHocViewModels model = new KhoaHocViewModels
            {
                listKhoaHoc = listKhoaHocDaTim.Select(kh => new sub_KhoaHocViewModels
                {
                    KhoaHoc = kh,
                    TongLuotQuanTam = _db.DanhSachQuanTam.Count(q => q.MaKhoaHoc == kh.MaKhoaHoc),
                    TongLuotBinhLuan = _db.DanhGia.Count(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc),
                    DaYeuThich = listDaQuanTam.Contains(kh.MaKhoaHoc),
                    SoSaoTrungBinh = TinhTrungBinhSao(kh.MaKhoaHoc)
                }).ToList()
            };
            return View("KhoaHoc", model);
        }
        
        double LamTron(double x)
        {
            var nguyen = Math.Floor(x);
            var le = x - nguyen;
            if (le < 0.25)
            {
                return nguyen;
            }
            if (le < 0.75)
            {
                return nguyen + 0.5;
            }
            return nguyen + 1.0;
        }

        
        [AllowAnonymous]
        public async Task<IActionResult> XemChiTiet (int id)
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                User u = _db.User.FirstOrDefault(p => p.Id == userId);
                ViewBag.User = u;
            }
            bool checkDaQuanTam = _db.DanhSachQuanTam.Any(q => q.MaKhoaHoc == id && q.UserId == userId);

            var kh = await _db.KhoaHoc.FirstOrDefaultAsync(p => p.MaKhoaHoc == id);
            if(kh == null)
            {
                return NotFound("Khong tim thay khoa hoc");
            }
            var listHinhAnh = await _db.HinhAnhKhoaHoc.Where(h => h.KhoaHocMaKhoaHoc == id).ToListAsync();
            var listDanhGia = await _db.DanhGia.Where(d => d.KhoaHocMaKhoaHoc == id).Include(d => d.User).ToListAsync();
            var model = new XemChiTietViewModels
            {
                KhoaHocCanXem = kh,
                listHinh = listHinhAnh,
                dsDanhGia = listDanhGia,
                TongluotDanhGia = listDanhGia.Count,
                SoSaoTrungBinh = TinhTrungBinhSao(kh.MaKhoaHoc),
                DaQuanTam = checkDaQuanTam,
                DangChoXuLy = _db.PhieuDangKyKhoaHoc.Any(p => p.MaKhoaHoc == kh.MaKhoaHoc && p.UserId == userId && p.TrangThaiDangKy == "Chờ xử lý")
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public IActionResult BaoCaoDanhGia(int MaDanhGia, string LyDo, int MaKhoaHoc)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ToCaoDanhGia tc = new ToCaoDanhGia()
            {
                MaDanhGia = MaDanhGia,
                UserId = userId,
                LyDo = LyDo
            };
            if (_db.ToCaoDanhGia.Any(p => p.MaDanhGia == MaDanhGia && p.UserId == userId))
            {
                TempData["Title"] = "Bạn đã báo cáo bình luận này trước đó!";
                TempData["Message"] = "Vui lòng chờ hệ thống xử lý báo cáo";
                TempData["Type"] = "warning";
            }
            else
            {
                _db.ToCaoDanhGia.Add(tc);
                _db.SaveChanges();
                TempData["Title"] = "Báo cáo bình luận thành công!";
                TempData["Message"] = "Vui lòng chờ hệ thống xử lý báo cáo";
                TempData["Type"] = "success";
            }
            return RedirectToAction("XemChiTiet", new {id = MaKhoaHoc });
        }

        [Authorize(Roles = "Student")]
        public IActionResult KhoaHocDaDangKy(DateTime? startDate)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var listDaDangKy = _db.PhieuDangKyKhoaHoc.Where(p => p.UserId == userId)
                                .Include(p => p.KhoaHoc).ThenInclude(p => p.HinhAnhKhoaHoc).ToList();

            var listLopDangHoc = _db.ChiTietHocTap.Where(m => m.UserId == userId && m.LopHoc.NgayKetThuc > DateTime.Now)
                                .Include(m => m.LopHoc).ThenInclude(m => m.PhongHoc)
                                .Include(m => m.LopHoc).ThenInclude(m => m.KhoaHoc).ThenInclude(m => m.HinhAnhKhoaHoc).ToList();

            var listLopDaHoc = _db.ChiTietHocTap.Where(m => m.UserId == userId && m.LopHoc.NgayKetThuc < DateTime.Now)
                                .Include(p => p.LopHoc).ThenInclude(p => p.KhoaHoc).ThenInclude(p => p.HinhAnhKhoaHoc).ToList();

            var listConNoHP = _db.PhieuDangKyKhoaHoc.Where(p => p.TrangThaiThanhToan == "Thanh toán 50%" && p.UserId == userId)
                                .Include(p => p.HoaDon).Include(p => p.KhoaHoc).ThenInclude(p => p.HinhAnhKhoaHoc).ToList();

            // Nếu không có startDate -> lấy thứ 2 tuần hiện tại
            DateTime today = startDate ?? DateTime.Today;
            // nếu là Chủ nhật -> lùi đúng 6 ngày để về t2. Các ngày khác -> lùi về t2
            int x;
            if(today.DayOfWeek == DayOfWeek.Sunday)
            {
                x = -6;
            }
            else
            {
                x = -(int)today.DayOfWeek + 1;
            }
            DateTime weekStart = today.AddDays(x);

            ViewBag.CurrentWeekStart = weekStart;

            KhoaHocDaDangKyViewModels model = new KhoaHocDaDangKyViewModels()
            {
                listPhieuDangKy = listDaDangKy,
                listDangHoc = listLopDangHoc,
                listDaHoc = listLopDaHoc,
                listConNo = listConNoHP,
            };
            return View(model);
        }

        [Authorize(Roles = "Student")]
        public IActionResult ThemVaoDSQuanTam(int MaKhoaHoc)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            DanhSachQuanTam dsqt = new DanhSachQuanTam()
            {
                MaKhoaHoc = MaKhoaHoc,
                UserId = userId
            };
            if (_db.DanhSachQuanTam.Any(q => q.MaKhoaHoc == MaKhoaHoc && q.UserId == userId))
            {
                _db.DanhSachQuanTam.Remove(dsqt);
                _db.SaveChanges();
            }
            else
            {
                _db.DanhSachQuanTam.Add(dsqt);
                _db.SaveChanges();
            }
            return Json(new { success = true });
        }

        [Authorize(Roles = "Student")]
        public IActionResult DanhSachQuanTam()
        {


            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var danhSach = _db.DanhSachQuanTam.Include(d => d.KhoaHoc).ThenInclude(d => d.HinhAnhKhoaHoc).Where(d => d.UserId == userId).ToList();
            KhoaHocViewModels model = new KhoaHocViewModels
            {
                listKhoaHoc = danhSach.Select(kh => new sub_KhoaHocViewModels
                {
                    KhoaHoc = kh.KhoaHoc,
                    TongLuotQuanTam = _db.DanhSachQuanTam.Count(q => q.MaKhoaHoc == kh.MaKhoaHoc),
                    TongLuotBinhLuan = _db.DanhGia.Count(d => d.KhoaHocMaKhoaHoc == kh.MaKhoaHoc),
                    SoSaoTrungBinh = TinhTrungBinhSao(kh.MaKhoaHoc)
                }).ToList()
            };
            return View(model);
        }
        [Authorize(Roles = "Student")]
        [HttpPost]
        public IActionResult GuiDanhGia(int KhoaHocDanhGia, string UserDanhGia, int SoSaoDanhGia, string NoiDung)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            DanhGia dg = new DanhGia()
            {
                SoSaoDanhGia = SoSaoDanhGia,
                NoiDungDanhGia = NoiDung,
                NgayDanhGia = DateTime.Now,
                KhoaHocMaKhoaHoc = KhoaHocDanhGia,
                UserUserId = userId
            };
            _db.DanhGia.Add(dg);
            _db.SaveChanges();
            return RedirectToAction("XemChiTiet", new {id = KhoaHocDanhGia });
        }

        

        [AllowAnonymous]
        public IActionResult TinTucTuyenDung()
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //check user Student có đang nợ học phí ở lớp học nào ko
            var listConNoHP = _db.PhieuDangKyKhoaHoc.Where(p => p.TrangThaiThanhToan == "Thanh toán 50%" && p.UserId == userid && p.TrangThaiDangKy == "Đã xếp lớp" && p.NgayDangKy.Date.AddDays(60) < DateTime.Now.Date).ToList();
            if (listConNoHP.Count > 0)
            {
                TempData["ThongBaoNo"] = "Bạn vui lòng thanh toán các khóa học còn nợ trước khi đăng ký khóa học mới";
                return RedirectToAction("KhoaHocDaDangKy", "StudentHome");
            }
            var danhSachTin = _db.TinTucTuyenDung.OrderByDescending(t => t.NgayDang).ToList();
            return View(danhSachTin);
        }

        [AllowAnonymous]
        public IActionResult ChiTietTinTucTuyenDung(int id)
        {

            var tinTuyenDung = _db.TinTucTuyenDung.FirstOrDefault(t => t.MaTinTuc == id);
            if (tinTuyenDung == null)
            {
                return NotFound();
            }

            return View(tinTuyenDung);
        }


        [AllowAnonymous]
        public async Task<IActionResult> About()
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //check user Student có đang nợ học phí ở lớp học nào ko
            var listConNoHP = _db.PhieuDangKyKhoaHoc.Where(p => p.TrangThaiThanhToan == "Thanh toán 50%" && p.UserId == userid && p.TrangThaiDangKy == "Đã xếp lớp" && p.NgayDangKy.Date.AddDays(60) < DateTime.Now.Date).ToList();
            if (listConNoHP.Count > 0)
            {
                TempData["ThongBaoNo"] = "Bạn vui lòng thanh toán các khóa học còn nợ trước khi đăng ký khóa học mới";
                return RedirectToAction("KhoaHocDaDangKy", "StudentHome");
            }
            var ListStudent = await _userManager.GetUsersInRoleAsync("Student");
            var ListTeacher = await _userManager.GetUsersInRoleAsync("Teacher");
            var ListStaff = await _userManager.GetUsersInRoleAsync("Staff");
            var ListAdmin = await _userManager.GetUsersInRoleAsync("Admin");


            int countStudent = ListStudent.Count();
            int countTeacher = ListTeacher.Count();
            int countStaff = ListStaff.Count();
            int countAdmin = ListAdmin.Count();


            ViewBag.countStudent = countStudent;
            ViewBag.countTeacher = countTeacher;
            ViewBag.countStaff = countStaff;
            ViewBag.countAdmin = countAdmin;
            return View();
        }
        [AllowAnonymous]
        public async Task<IActionResult> Teacher()
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //check user Student có đang nợ học phí ở lớp học nào ko
            var listConNoHP = _db.PhieuDangKyKhoaHoc.Where(p => p.TrangThaiThanhToan == "Thanh toán 50%" && p.UserId == userid && p.TrangThaiDangKy == "Đã xếp lớp" && p.NgayDangKy.Date.AddDays(60) < DateTime.Now.Date).ToList();
            if (listConNoHP.Count > 0)
            {
                TempData["ThongBaoNo"] = "Bạn vui lòng thanh toán các khóa học còn nợ trước khi đăng ký khóa học mới";
                return RedirectToAction("KhoaHocDaDangKy", "StudentHome");
            }

            var listTeacher = await _userManager.GetUsersInRoleAsync("Teacher");
            var teacher = listTeacher.Select(u => new
            {
                Avatar = u.Avatar != null ? $"data:image/png;base64,{Convert.ToBase64String(u.Avatar)}": "/image/avatar.png",
                u.HoTen,
                u.DiaChi,
                ChuyenNganh = u.ChuyenNganhMaChuyenNganh != null ? _db.ChuyenNganh.FirstOrDefault(p => p.MaChuyenNganh == u.ChuyenNganhMaChuyenNganh).TenChuyenNganh : "Chưa cập nhật"
            }).ToList();
            return View(teacher);
        }
    }
}
