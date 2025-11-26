using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using DoAnCoSo_Web.Areas.Staff.ViewModels;
using DoAnCoSo_Web.Data;
using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo_Web.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = "Staff,Admin")]
    public class PhieuDangKyKhoaHocController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _db;

        public PhieuDangKyKhoaHocController(UserManager<User> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;

            _db = db;
        }
        public IActionResult Index()
        {

            var tatCaPhieu = _db.PhieuDangKyKhoaHoc
                                .Include(p => p.KhoaHoc)
                                .ToList();

            var danhSachKhoaHocChuaXepLop = tatCaPhieu
                                  .GroupBy(p => p.MaKhoaHoc)
                                  .Select(g => new
                                  {
                                      MaKhoaHoc = g.Key,
                                      TenKhoaHoc = g.First().KhoaHoc.TenKhoaHoc,
                                      NgayHoc = g.First().KhoaHoc.NgayHoc,
                                      SoLuongPhieuDangKy = g.Count(p => p.TrangThaiDangKy == "Chờ xử lý")
                                  })
                                  .Where(x => x.SoLuongPhieuDangKy > 0) // Chỉ hiển thị khóa học có phiếu chờ xử lý
                                  .ToList();

            var danhSachKhoaHocDaXepLop = tatCaPhieu
                                  .GroupBy(p => p.MaKhoaHoc)
                                  .Select(g => new
                                  {
                                      MaKhoaHoc = g.Key,
                                      TenKhoaHoc = g.First().KhoaHoc.TenKhoaHoc,
                                      NgayHoc = g.First().KhoaHoc.NgayHoc,
                                      SoLuongPhieuDangKy = g.Count(p => p.TrangThaiDangKy == "Đã xếp lớp")
                                  })
                                  .Where(x => x.SoLuongPhieuDangKy > 0) // Chỉ hiển thị khóa học có phiếu đã xếp lớp
                                  .ToList();

            // Tạo view model
            var model = new DanhSachKhoaHocViewModel
            {
                DanhSachChuaXepLop = danhSachKhoaHocChuaXepLop.Select(kh => new KhoaHocWithPhieuDangKyViewModel
                {
                    MaKhoaHoc = kh.MaKhoaHoc,
                    TenKhoaHoc = kh.TenKhoaHoc,
                    NgayHoc = kh.NgayHoc,
                    SoLuongPhieuDangKy = kh.SoLuongPhieuDangKy
                }).ToList(),

                DanhSachDaXepLop = danhSachKhoaHocDaXepLop.Select(kh => new KhoaHocWithPhieuDangKyViewModel
                {
                    MaKhoaHoc = kh.MaKhoaHoc,
                    TenKhoaHoc = kh.TenKhoaHoc,
                    NgayHoc = kh.NgayHoc,
                    SoLuongPhieuDangKy = kh.SoLuongPhieuDangKy
                }).ToList()
            };

            return View(model);
        }

        public IActionResult DanhSachPhieuDangKyChoXuLy(int maKhoaHoc)
        {
            var khoaHoc = _db.KhoaHoc.FirstOrDefault(k => k.MaKhoaHoc == maKhoaHoc);
            if (khoaHoc == null)
            {
                return NotFound();
            }

            var danhSachPhieu = _db.PhieuDangKyKhoaHoc
                                    .Where(p => p.MaKhoaHoc == maKhoaHoc && p.TrangThaiDangKy == "Chờ xử lý")
                                    .Include(p => p.User)
                                    .ToList();

            ViewBag.TenKhoaHoc = khoaHoc.TenKhoaHoc;
            return View(danhSachPhieu);
        }
        public IActionResult DanhSachPhieuDangKyDaXepLop(int maKhoaHoc)
        {
            var khoaHoc = _db.KhoaHoc.FirstOrDefault(k => k.MaKhoaHoc == maKhoaHoc);
            if (khoaHoc == null)
            {
                return NotFound();
            }

            var danhSachPhieu = _db.PhieuDangKyKhoaHoc
                                    .Where(p => p.MaKhoaHoc == maKhoaHoc && p.TrangThaiDangKy == "Đã xếp lớp")
                                    .Include(p => p.User)
                                    .ToList();

            ViewBag.TenKhoaHoc = khoaHoc.TenKhoaHoc;
            return View(danhSachPhieu);
        }
        [HttpGet]
        public IActionResult XepLop(int maKhoaHoc)
        {
            // Lấy thông tin khóa học
            var khoaHoc = _db.KhoaHoc.FirstOrDefault(k => k.MaKhoaHoc == maKhoaHoc);
            if (khoaHoc == null)
            {
                return NotFound();
            }


            var danhSachLop = _db.LopHoc
                                 .Include(l => l.ChiTietHocTap)
                                 .Include(l => l.PhongHoc)
                                 .Where(l => l.KhoaHocMaKhoaHoc == maKhoaHoc && l.ChiTietHocTap.Count < l.PhongHoc.SucChua)
                                 .ToList();

            // Truyền dữ liệu ra View
            ViewBag.KhoaHoc = khoaHoc;

            return View(danhSachLop);
        }

        [HttpPost]
        public IActionResult XepLop(int maKhoaHoc, int? maLopHoc)
        {
            var khoaHoc = _db.KhoaHoc.FirstOrDefault(k => k.MaKhoaHoc == maKhoaHoc);
            if (khoaHoc == null)
            {
                return NotFound("Không tìm thấy khóa học.");
            }

            // Lấy danh sách phiếu đăng ký cần xếp lớp
            var phieuDangKys = _db.PhieuDangKyKhoaHoc
                .Where(p => p.MaKhoaHoc == maKhoaHoc && p.TrangThaiDangKy == "Chờ xử lý")
                .ToList();

            if (!phieuDangKys.Any())
            {
                TempData["Message"] = "Không có học viên cần xếp lớp.";
                return RedirectToAction("Index");
            }

            // Kiểm tra lớp học đã có
            LopHoc lopHocDuocChon = null;

            if (maLopHoc.HasValue)
            {
                lopHocDuocChon = _db.LopHoc
                    .Include(l => l.ChiTietHocTap)
                    .Include(l => l.PhongHoc)
                    .FirstOrDefault(l => l.MaLopHoc == maLopHoc);

                var soLuongHienTai = lopHocDuocChon?.ChiTietHocTap?.Count ?? 0;

                var sucChuaPhong = lopHocDuocChon?.PhongHoc?.SucChua ?? 0;
                if (lopHocDuocChon != null && soLuongHienTai + phieuDangKys.Count <= sucChuaPhong)
                {
                    // Đủ chỗ
                }
                else
                {
                    lopHocDuocChon = null;
                }
            }

            // Nếu không có lớp đủ chỗ, gọi action tạo lớp mới
            if (lopHocDuocChon == null)
            {
                // Gọi action tạo lớp mới (hoặc service tạo lớp mới)
                return RedirectToAction("TaoLop", new { maKhoaHoc = maKhoaHoc });
            }
            var userIdsDaXep = _db.ChiTietHocTap
    .Where(c => c.MaLopHoc == lopHocDuocChon.MaLopHoc)
    .Select(c => c.UserId)
    .ToHashSet();

            // Thêm chi tiết học tập cho các sinh viên trong phiếu đăng ký
            var chiTietHocTapList = phieuDangKys.Select(p => new ChiTietHocTap
            {
                UserId = p.UserId,
                MaLopHoc = lopHocDuocChon.MaLopHoc,
                TrangThai = "Đã xếp lớp",
                DiemTongKet = 0
            }).ToList();

            _db.ChiTietHocTap.AddRange(chiTietHocTapList);

            // Cập nhật trạng thái của phiếu đăng ký
            foreach (var p in phieuDangKys)
            {
                p.TrangThaiDangKy = "Đã xếp lớp";
            }

            _db.SaveChanges();

            TempData["Message"] = "Xếp lớp thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> TaoLop(int maKhoaHoc)
        {
            var khoaHoc = await _db.KhoaHoc.FirstOrDefaultAsync(k => k.MaKhoaHoc == maKhoaHoc);
            if (khoaHoc == null) return NotFound();

            var phongHocs = await _db.PhongHoc.Select(p => new SelectListItem
            {
                Value = p.MaPhongHoc.ToString(),
                Text = p.TenPhongHoc
            }).ToListAsync();
            DateTime ngayKhaiGiang = DateTime.Now;
            DateTime ngayKetThuc = TinhNgayKetThuc(khoaHoc.TenKhoaHoc, ngayKhaiGiang);

            var danhSachGiaoVien = await GetDanhSachGiaoVienPhuHopAsync(khoaHoc, ngayKhaiGiang, ngayKetThuc);

            var model = new TaoLopViewModel
            {
                MaKhoaHoc = maKhoaHoc,
                TenKhoaHoc = khoaHoc.TenKhoaHoc,
                NgayKhaiGiang = ngayKhaiGiang,
                NgayKetThuc = ngayKetThuc,
                DanhSachPhongHoc = phongHocs,
                DanhSachGiaoVien = danhSachGiaoVien
            };

            return View(model);
        }
        private async Task<List<SelectListItem>> GetDanhSachGiaoVienPhuHopAsync(
     KhoaHoc khoaHoc,
     DateTime ngayKhaiGiang,
     DateTime ngayKetThuc)
        {
            var giaoViensTrongRoleIds = (await _userManager.GetUsersInRoleAsync("Teacher"))
                .Select(u => u.Id)
                .ToList();

            var giaoViensCoChuyenNganh = await _db.Users
                .Include(u => u.ChuyenNganh)
                .Where(u => giaoViensTrongRoleIds.Contains(u.Id))
                .ToListAsync();

            var tenKhoaHoc = khoaHoc.TenKhoaHoc?.ToLower().Trim();

            var giaoViensPhuHop = giaoViensCoChuyenNganh
                .Where(gv => gv.ChuyenNganh != null &&
                             !string.IsNullOrWhiteSpace(gv.ChuyenNganh.TenChuyenNganh) &&
                             tenKhoaHoc.Contains(gv.ChuyenNganh.TenChuyenNganh.ToLower().Trim()))
                .ToList();

            var danhSachGiaoVien = new List<SelectListItem>();
            foreach (var gv in giaoViensPhuHop)
            {
                var lopHocGiaoVien = await _db.LopHoc
                    .Include(lh => lh.KhoaHoc)
                    .Where(lh => lh.ChiTietGiangDay.Any(c => c.UserId == gv.Id))
                    .ToListAsync();

                bool isConflict = false;
                foreach (var lopHoc in lopHocGiaoVien)
                {
                    var khoaHocGiaoVien = lopHoc.KhoaHoc;

                    isConflict = await CheckTeacherScheduleConflict(
     khoaHoc.NgayHoc,
     ngayKhaiGiang,
     ngayKetThuc,
     khoaHocGiaoVien.GioBatDau,
     khoaHocGiaoVien.GioKetThuc,
     gv.Id
 );

                    if (isConflict) break;
                }

                if (!isConflict)
                {
                    danhSachGiaoVien.Add(new SelectListItem
                    {
                        Value = gv.Id,
                        Text = gv.HoTen
                    });
                }
            }

            return danhSachGiaoVien;
        }
            private async Task<bool> CheckTeacherScheduleConflict(string ngayHocMoi, DateTime ngayKhaiGiangMoi, DateTime ngayKetThucMoi, string gioBatDauMoi, string gioKetThucMoi, string teacherId, int? lopHocId = null)
            {
                var ngayHocMoiParts = ngayHocMoi.Split(' ');
                var daysOfWeekMoi = ngayHocMoiParts[1].Split(',').Select(d => d.Trim().Replace("T", "")).ToList();
            var gioBatDauMoiTime = DateTime.ParseExact(gioBatDauMoi, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;
            var gioKetThucMoiTime = DateTime.ParseExact(gioKetThucMoi, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;


            // Lấy tất cả lớp học của giáo viên, trừ lớp hiện tại (nếu sửa)
            var lopHocList = await _db.LopHoc
                    .Include(lh => lh.KhoaHoc)
                    .Where(lh => lh.ChiTietGiangDay.Any(c => c.UserId == teacherId) && (lopHocId == null || lh.MaLopHoc != lopHocId))
                    .ToListAsync();

                foreach (var lopHoc in lopHocList)
                {
                    var khoaHoc = lopHoc.KhoaHoc;

                    var ngayHocLopHocParts = khoaHoc.NgayHoc.Split(' ');
                    var daysOfWeekLopHoc = ngayHocLopHocParts[1].Split(',').Select(d => d.Trim().Replace("T", "")).ToList();

                    bool trungNgayHoc = daysOfWeekLopHoc.Any(d => daysOfWeekMoi.Contains(d));
                    bool giaoThoiGianHoc = lopHoc.NgayKhaiGiang <= ngayKetThucMoi && lopHoc.NgayKetThuc >= ngayKhaiGiangMoi;

                    if (trungNgayHoc && giaoThoiGianHoc)
                    {
                        var gioBatDauLop = DateTime.ParseExact(khoaHoc.GioBatDau, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;
                        var gioKetThucLop = DateTime.ParseExact(khoaHoc.GioKetThuc, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;

                    bool giaoGioHoc = gioKetThucMoiTime > gioBatDauLop && gioBatDauMoiTime < gioKetThucLop;


                    if (giaoGioHoc)
                        {
                            return true; // Xung đột lịch học
                        }
                    }
                }

                return false; // Không xung đột
            }
        private DateTime TinhNgayKetThuc(string tenKhoaHoc, DateTime ngayBatDau)
        {
            var regex = new Regex(@"(\d+)\s*(tháng|tuần)", RegexOptions.IgnoreCase);
            var match = regex.Match(tenKhoaHoc);

            if (match.Success && int.TryParse(match.Groups[1].Value, out int soLuong))
            {
                var donVi = match.Groups[2].Value.ToLower();
                if (donVi == "tháng")
                    return ngayBatDau.AddMonths(soLuong);
                else if (donVi == "tuần")
                    return ngayBatDau.AddDays(soLuong * 7);
            }

            // Mặc định nếu không tìm thấy thông tin thì cho 3 tháng
            return ngayBatDau.AddMonths(3);
        }

        [HttpGet]
        public IActionResult TinhNgayKetThuc(int maKhoaHoc, DateTime ngayKhaiGiang)
        {
            var khoaHoc = _db.KhoaHoc.FirstOrDefault(k => k.MaKhoaHoc == maKhoaHoc);
            if (khoaHoc == null)
                return NotFound();

            DateTime ngayKetThuc = TinhNgayKetThuc(khoaHoc.TenKhoaHoc, ngayKhaiGiang);
            return Json(new { ngayKetThuc = ngayKetThuc.ToString("yyyy-MM-dd") });
        }

        [HttpGet]
        public async Task<IActionResult> GiaoVienPartial(int maKhoaHoc, DateTime ngayKhaiGiang, DateTime ngayKetThuc)
        {
            var khoaHoc = await _db.KhoaHoc.FindAsync(maKhoaHoc);
            if (khoaHoc == null) return NotFound();

            var danhSachGiaoVien = await GetDanhSachGiaoVienPhuHopAsync(khoaHoc, ngayKhaiGiang, ngayKetThuc);

            var vm = new TaoLopViewModel();
            vm.DanhSachGiaoVien = danhSachGiaoVien;

            return PartialView("_GiaoVienDropdown", vm);
        }


        [HttpPost]
        public async Task<IActionResult> TaoLop(TaoLopViewModel model)
        {
            var khoaHoc = await _db.KhoaHoc.FirstOrDefaultAsync(k => k.MaKhoaHoc == model.MaKhoaHoc);
            if (khoaHoc == null) return NotFound();
            model.DanhSachPhongHoc = await _db.PhongHoc.Select(p => new SelectListItem
            {
                Value = p.MaPhongHoc.ToString(),
                Text = p.TenPhongHoc
            }).ToListAsync();
            // Kiểm tra xung đột phòng học
            var coXungDot = await CheckRoomConflict(
                model.SelectedPhongHocId,
                khoaHoc.NgayHoc,
                model.NgayKhaiGiang,
                model.NgayKetThuc,
                khoaHoc.GioBatDau,
                khoaHoc.GioKetThuc
            );

            if (coXungDot)
            {
                model.DanhSachPhongHoc = await _db.PhongHoc.Select(p => new SelectListItem
                {
                    Value = p.MaPhongHoc.ToString(),
                    Text = p.TenPhongHoc
                }).ToListAsync();
                model.DanhSachGiaoVien = await GetDanhSachGiaoVienPhuHopAsync(khoaHoc, model.NgayKhaiGiang, model.NgayKetThuc);
                ModelState.AddModelError(nameof(model.SelectedPhongHocId), "Phòng học đã có lớp trùng lịch học.");
                model.TenKhoaHoc = khoaHoc.TenKhoaHoc;
                return View(model);
            }

            if (model.NgayKetThuc <= model.NgayKhaiGiang)
            {
                model.DanhSachPhongHoc = await _db.PhongHoc.Select(p => new SelectListItem
                {
                    Value = p.MaPhongHoc.ToString(),
                    Text = p.TenPhongHoc
                }).ToListAsync();
                model.DanhSachGiaoVien = await GetDanhSachGiaoVienPhuHopAsync(khoaHoc, model.NgayKhaiGiang, model.NgayKetThuc);
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải sau ngày khai giảng.");
                model.TenKhoaHoc = khoaHoc.TenKhoaHoc;
                return View(model);
            }

            // Lấy danh sách phiếu đăng ký
            var phieuDangKys = await _db.PhieuDangKyKhoaHoc
                .Where(p => p.MaKhoaHoc == model.MaKhoaHoc && p.TrangThaiDangKy == "Chờ xử lý")
                .ToListAsync();

            if (!phieuDangKys.Any())
            {
                TempData["Message"] = "Không có học viên cần xếp lớp.";
                return RedirectToAction("Index");
            }

            // Tạo lớp mới
            var lopHoc = new LopHoc
            {
                TenLopHoc = model.TenLopHoc,
                NgayKhaiGiang = model.NgayKhaiGiang,
                NgayKetThuc = model.NgayKetThuc,
                KhoaHocMaKhoaHoc = model.MaKhoaHoc,
                PhongHocMaPhongHoc = model.SelectedPhongHocId
            };
            _db.LopHoc.Add(lopHoc);
            await _db.SaveChangesAsync();

            // Thêm chi tiết học tập cho học viên
            var chiTietHocTapList = phieuDangKys.Select(p => new ChiTietHocTap
            {
                UserId = p.UserId,
                MaLopHoc = lopHoc.MaLopHoc,
                TrangThai = "Đã xếp lớp",
                DiemTongKet = 0
            }).ToList();

            _db.ChiTietHocTap.AddRange(chiTietHocTapList);
            if (!string.IsNullOrEmpty(model.SelectedGiaoVienId))
            {
                var chiTietGiangDay = new ChiTietGiangDay
                {
                    UserId = model.SelectedGiaoVienId,
                    MaLopHoc = lopHoc.MaLopHoc
                };
                _db.ChiTietGiangDay.Add(chiTietGiangDay);
            }

            // Cập nhật trạng thái phiếu đăng ký
            foreach (var p in phieuDangKys)
            {
                p.TrangThaiDangKy = "Đã xếp lớp";
            }

            await _db.SaveChangesAsync();

            TempData["Message"] = "Tạo lớp và xếp lớp thành công!";
            return RedirectToAction("Index");
        }

        private async Task<bool> CheckRoomConflict(int phongHocId, string ngayHocMoi, DateTime ngayKhaiGiangMoi, DateTime ngayKetThucMoi, string gioBatDauMoi, string gioKetThucMoi, int? lopHocId = null)
        {
            // Tách ngày học mới từ chuỗi dạng "Sáng T2,4,6" thành các ngày "T2", "T4", "T6"
            var ngayHocMoiParts = ngayHocMoi.Split(' '); // Tách thành "Sáng"/"Chiều" và "T2,4,6"
            var daysOfWeekMoi = ngayHocMoiParts[1].Split(',').Select(d => d.Trim().Replace("T", "")).ToList(); // Loại bỏ "T" và lấy danh sách ngày học

            // Parse giờ học mới
            var gioBatDauMoiTime = DateTime.ParseExact(gioBatDauMoi, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;
            var gioKetThucMoiTime = DateTime.ParseExact(gioKetThucMoi, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;


            // Nếu có lopHocId, ta kiểm tra xem lớp học hiện tại có thay đổi gì không
            if (lopHocId != null)
            {
                var lopHoc = await _db.LopHoc.Include(lh => lh.KhoaHoc).FirstOrDefaultAsync(lh => lh.MaLopHoc == lopHocId);
                if (lopHoc != null)
                {
                    // Tách ngày học của lớp hiện tại để so sánh
                    var khoaHoc = lopHoc.KhoaHoc;
                    var ngayHocLopHocParts = khoaHoc.NgayHoc.Split(' '); // Tách "Sáng" hoặc "Chiều" và các ngày học
                    var daysOfWeekLopHoc = ngayHocLopHocParts[1].Split(',').Select(d => d.Trim().Replace("T", "")).ToList(); // Loại bỏ "T" và lấy danh sách ngày học

                    // Kiểm tra nếu phòng học, ngày học, giờ học của lớp không thay đổi thì bỏ qua kiểm tra
                    bool isPhongHocChanged = lopHoc.PhongHocMaPhongHoc != phongHocId;
                    bool isGioHocChanged = khoaHoc.GioBatDau != gioBatDauMoi || khoaHoc.GioKetThuc != gioKetThucMoi;
                    bool isNgayHocChanged = !daysOfWeekLopHoc.SequenceEqual(daysOfWeekMoi); // Kiểm tra sự thay đổi của ngày học

                    if (!isPhongHocChanged && !isGioHocChanged && !isNgayHocChanged && lopHoc.NgayKhaiGiang == ngayKhaiGiangMoi && lopHoc.NgayKetThuc == ngayKetThucMoi)
                    {
                        return false; // Không thay đổi gì, bỏ qua kiểm tra xung đột
                    }
                }
            }

            // Lấy danh sách lớp học đã có trong phòng học
            var lopHocList = await _db.LopHoc
    .Include(lh => lh.KhoaHoc)
    .Where(lh => lh.PhongHocMaPhongHoc == phongHocId
              && (lopHocId == null || lh.MaLopHoc != lopHocId)
              && lh.NgayKetThuc >= DateTime.Now) // bỏ qua lớp đã kết thúc
    .ToListAsync();


            foreach (var lopHoc in lopHocList)
            {
                var khoaHoc = lopHoc.KhoaHoc;

                // Lấy ngày học của lớp hiện tại
                var ngayHocLopHocParts = khoaHoc.NgayHoc.Split(' '); // Tách "Sáng" hoặc "Chiều" và các ngày học
                var daysOfWeekLopHoc = ngayHocLopHocParts[1].Split(',').Select(d => d.Trim().Replace("T", "")).ToList(); // Loại bỏ "T" và lấy danh sách ngày học

                // Kiểm tra xem có trùng ngày học không
                bool trungNgayHoc = daysOfWeekLopHoc.Any(d => daysOfWeekMoi.Contains(d));

                // Kiểm tra ngày khai giảng và ngày kết thúc có trùng không
                bool giaoThoiGianHoc = lopHoc.NgayKhaiGiang <= ngayKetThucMoi && lopHoc.NgayKetThuc >= ngayKhaiGiangMoi;

                if (trungNgayHoc && giaoThoiGianHoc)
                {
                    // Parse giờ học của lớp đã có
                    var gioBatDauLop = DateTime.ParseExact(khoaHoc.GioBatDau, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;
                    var gioKetThucLop = DateTime.ParseExact(khoaHoc.GioKetThuc, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;

                    // Kiểm tra giao giờ học
                    bool giaoGioHoc = gioBatDauMoiTime < gioKetThucLop && gioKetThucMoiTime > gioBatDauLop;

                    if (giaoGioHoc)
                    {
                        return true; // Trùng phòng học
                    }
                }
            }

            return false; // Không trùng
        }
        

        [HttpGet]
        public IActionResult ThemPhieuDangKyTheoGmail()
        {
            ViewBag.DanhSachKhoaHoc = _db.KhoaHoc.ToList();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ThemPhieuDangKyTheoGmail(string gmail, int khoaHocId, string TrangThaiThanhToan)
        {
            if (string.IsNullOrEmpty(gmail) || string.IsNullOrEmpty(TrangThaiThanhToan))
            {
                ModelState.AddModelError("", "Vui lòng nhập đủ thông tin.");
                ViewBag.DanhSachKhoaHoc = _db.KhoaHoc.ToList();
                return View();
            }

            var user = await _userManager.FindByEmailAsync(gmail);
            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy người dùng với Gmail này.");
                ViewBag.DanhSachKhoaHoc = _db.KhoaHoc.ToList();
                return View();
            }

            var khoaHoc = await _db.KhoaHoc.FindAsync(khoaHocId);
            if (khoaHoc == null)
            {
                ModelState.AddModelError("", "Khóa học không tồn tại.");
                ViewBag.DanhSachKhoaHoc = _db.KhoaHoc.ToList();
                return View();
            }
            // Kiểm tra xem người dùng đã đăng ký khóa học này chưa
            bool daDangKy = await _db.PhieuDangKyKhoaHoc
                .AnyAsync(p => p.UserId == user.Id && p.MaKhoaHoc == khoaHocId);

            if (daDangKy)
            {
                ModelState.AddModelError("", "Người dùng này đã đăng ký khóa học này rồi.");
                ViewBag.DanhSachKhoaHoc = _db.KhoaHoc.ToList();
                return View();
            }

            var lopHoc = await _db.LopHoc.FirstOrDefaultAsync(l => l.KhoaHocMaKhoaHoc == khoaHocId);
            var conflicts = await GetLichHocBiTrung(
                user.Id,
                khoaHoc.NgayHoc,
                lopHoc?.NgayKhaiGiang ?? DateTime.MinValue,
                lopHoc?.NgayKetThuc ?? DateTime.MaxValue,
                khoaHoc.GioBatDau,
                khoaHoc.GioKetThuc
            );

            if (conflicts.Any())
            {
                foreach (var conflict in conflicts)
                {
                    ModelState.AddModelError("", conflict);
                }
                ViewBag.DanhSachKhoaHoc = _db.KhoaHoc.ToList();
                return View();
            }


            var currentUserId = _userManager.GetUserId(User);

            // Bước 1: Tạo hóa đơn
            var hoaDon = new HoaDon
            {
                NgayLap = DateTime.Now,
                UserUserId = currentUserId, // người học phải đóng tiền
                TienDongLan1 = TrangThaiThanhToan == "Hoàn tất học phí" ? khoaHoc.HocPhi : khoaHoc.HocPhi * 0.5m,
                TienDongLan2 = TrangThaiThanhToan == "Hoàn tất học phí" ? 0 : null,
                NgayDongLan2 = null
            };
            _db.HoaDon.Add(hoaDon);
            await _db.SaveChangesAsync(); // Lưu để có MaHoaDon

            // Bước 2: Tạo phiếu đăng ký
            var phieu = new PhieuDangKyKhoaHoc
            {
                UserId = user.Id,
                MaKhoaHoc = khoaHocId,
                NgayDangKy = DateTime.Now,
                TrangThaiDangKy = "Chờ xử lý",
                TrangThaiThanhToan = TrangThaiThanhToan,
                MaHoaDon = hoaDon.MaHoaDon,
            };
            _db.PhieuDangKyKhoaHoc.Add(phieu);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã thêm phiếu đăng ký và hóa đơn thành công!";
            return RedirectToAction("Index");
        }
        private async Task<List<string>> GetLichHocBiTrung(
    string userId,
    string ngayHocMoi,
    DateTime ngayBatDauMoi,
    DateTime ngayKetThucMoi,
    string gioBatDauMoi,
    string gioKetThucMoi)
        {
            var conflicts = new List<string>();

            // Helper kiểm tra giờ học có giao nhau
            bool GiaoThoiGian(TimeSpan start1, TimeSpan end1, TimeSpan start2, TimeSpan end2)
            {
                return start1 < end2 && start2 < end1;
            }

            // Tách ca học và ngày học mới
            var partsMoi = ngayHocMoi.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partsMoi.Length < 2) return conflicts;

            var caHocMoi = partsMoi[0].Trim();
            var daysOfWeekMoi = partsMoi[1].Split(',')
                .Select(d => d.Trim().Replace("T", ""))
                .Where(d => !string.IsNullOrEmpty(d))
                .ToList();

            var gioBatDauMoiTime = DateTime.ParseExact(gioBatDauMoi, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;
            var gioKetThucMoiTime = DateTime.ParseExact(gioKetThucMoi, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;

            // Lấy tất cả lớp hiện tại của user
            var lopHocList = await _db.ChiTietHocTap
                .AsNoTracking()
                .Include(ct => ct.LopHoc)
                    .ThenInclude(lh => lh.KhoaHoc)
                .Where(ct => ct.UserId == userId)
                .Select(ct => ct.LopHoc)
                .ToListAsync();

            // Kiểm tra trùng với lớp đang học
            foreach (var lop in lopHocList)
            {
                var khoaHoc = lop.KhoaHoc;

                // Kiểm tra khoảng thời gian lớp mới có giao với lớp hiện tại không
                if (ngayKetThucMoi < lop.NgayKhaiGiang || ngayBatDauMoi > lop.NgayKetThuc)
                    continue;

                var partsLop = khoaHoc.NgayHoc.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (partsLop.Length < 2) continue;

                var caHocLop = partsLop[0].Trim();
                var daysOfWeekLop = partsLop[1].Split(',')
                    .Select(d => d.Trim().Replace("T", ""))
                    .Where(d => !string.IsNullOrEmpty(d))
                    .ToList();

                // Ca học khác → bỏ qua
                if (caHocMoi != caHocLop) continue;

                // Ngày học giao nhau → xét tiếp
                if (!daysOfWeekMoi.Any(d => daysOfWeekLop.Contains(d)))
                    continue;

                var gioBatDauLop = DateTime.ParseExact(khoaHoc.GioBatDau, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;
                var gioKetThucLop = DateTime.ParseExact(khoaHoc.GioKetThuc, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;

                if (GiaoThoiGian(gioBatDauMoiTime, gioKetThucMoiTime, gioBatDauLop, gioKetThucLop))
                {
                    conflicts.Add($"Trùng với lớp đang học: '{khoaHoc.TenKhoaHoc}' học {khoaHoc.NgayHoc} từ {khoaHoc.GioBatDau} đến {khoaHoc.GioKetThuc}");
                    break;
                }
            }

            // Kiểm tra phiếu đăng ký chưa xếp lớp
            var phieuDangKyChuaXepLop = await _db.PhieuDangKyKhoaHoc
                .AsNoTracking()
                .Include(p => p.KhoaHoc)
                .Where(p => p.UserId == userId && p.TrangThaiDangKy != "Đã xếp lớp")
                .Select(p => p.KhoaHoc)
                .ToListAsync();

            foreach (var khoaHoc in phieuDangKyChuaXepLop)
            {
                var partsLop = khoaHoc.NgayHoc.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (partsLop.Length < 2) continue;

                var caHocLop = partsLop[0].Trim();
                var daysOfWeekLop = partsLop[1].Split(',')
                    .Select(d => d.Trim().Replace("T", ""))
                    .Where(d => !string.IsNullOrEmpty(d))
                    .ToList();

                if (caHocMoi != caHocLop) continue;
                if (!daysOfWeekMoi.Any(d => daysOfWeekLop.Contains(d))) continue;

                var gioBatDauLop = DateTime.ParseExact(khoaHoc.GioBatDau, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;
                var gioKetThucLop = DateTime.ParseExact(khoaHoc.GioKetThuc, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;

                if (GiaoThoiGian(gioBatDauMoiTime, gioKetThucMoiTime, gioBatDauLop, gioKetThucLop))
                {
                    conflicts.Add($"Đã đăng ký khóa '{khoaHoc.TenKhoaHoc}' học {khoaHoc.NgayHoc} từ {khoaHoc.GioBatDau} đến {khoaHoc.GioKetThuc} (chưa xếp lớp)");
                    break;
                }
            }

            return conflicts;
        }














    }
}
