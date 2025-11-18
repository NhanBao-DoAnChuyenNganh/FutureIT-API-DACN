using DoAnCoSo_Web.Data;
using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo_Web.Areas.Staff.ViewModels;
using System.Text.RegularExpressions;
using System.Globalization;
namespace DoAnCoSo_Web.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = "Staff,Admin")]
    public class LopHocController : Controller
    {
        private readonly ApplicationDbContext _db;

        public LopHocController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            var danhSachLopHoc = _db.LopHoc
                .Include(lh => lh.KhoaHoc)
                .Include(lh => lh.PhongHoc)
                .Include(lh => lh.ChiTietHocTap)
                .Select(lh => new LopHocViewModel
                {
                    LopHoc = lh,
                    SoLuongHocVien = lh.ChiTietHocTap.Count()
                })
                .ToList();

            return View(danhSachLopHoc);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
            ViewBag.PhongHocList = _db.PhongHoc.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(LopHoc lopHoc)
        {
            // Kiểm tra các dữ liệu đầu vào
            if (string.IsNullOrEmpty(lopHoc.TenLopHoc))
            {
                ModelState.AddModelError("TenLopHoc", "Tên lớp học không được để trống.");
                ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                ViewBag.PhongHocList = _db.PhongHoc.ToList();
                return View(lopHoc);
            }

            if (lopHoc.NgayKhaiGiang == default(DateTime))
            {
                ModelState.AddModelError("NgayKhaiGiang", "Ngày khai giảng không hợp lệ.");
                ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                ViewBag.PhongHocList = _db.PhongHoc.ToList();
                return View(lopHoc);
            }

            if (lopHoc.NgayKetThuc == default(DateTime))
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc không hợp lệ.");
                ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                ViewBag.PhongHocList = _db.PhongHoc.ToList();
                return View(lopHoc);
            }

            if (lopHoc.NgayKhaiGiang >= lopHoc.NgayKetThuc)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải sau ngày khai giảng.");
                ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                ViewBag.PhongHocList = _db.PhongHoc.ToList();
                return View(lopHoc);
            }

            // Kiểm tra xem phòng học và khóa học có tồn tại không
            if (!_db.KhoaHoc.Any(k => k.MaKhoaHoc == lopHoc.KhoaHocMaKhoaHoc))
            {
                ModelState.AddModelError("KhoaHocMaKhoaHoc", "Khóa học không tồn tại.");
                ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                ViewBag.PhongHocList = _db.PhongHoc.ToList();
                return View(lopHoc);
            }

            if (!_db.PhongHoc.Any(p => p.MaPhongHoc == lopHoc.PhongHocMaPhongHoc))
            {
                ModelState.AddModelError("PhongHocMaPhongHoc", "Phòng học không tồn tại.");
                ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                ViewBag.PhongHocList = _db.PhongHoc.ToList();
                return View(lopHoc);
            }

            var khoaHoc = await _db.KhoaHoc.FindAsync(lopHoc.KhoaHocMaKhoaHoc);
            var phongHoc = await _db.PhongHoc.FindAsync(lopHoc.PhongHocMaPhongHoc);

            if (khoaHoc != null && phongHoc != null)
            {
                // Gọi hàm kiểm tra xung đột phòng học
                bool phongBiTrung = await CheckRoomConflict(
                    lopHoc.PhongHocMaPhongHoc, // Mã phòng học
                    khoaHoc.NgayHoc, // Ngày học khóa học
                    lopHoc.NgayKhaiGiang, // Ngày khai giảng của lớp mới
                    lopHoc.NgayKetThuc, // Ngày kết thúc của lớp mới
                    khoaHoc.GioBatDau, // Giờ bắt đầu khóa học
                    khoaHoc.GioKetThuc // Giờ kết thúc khóa học
                );

                if (phongBiTrung)
                {
                    ModelState.AddModelError("PhongHocMaPhongHoc", "Phòng học đã có lớp khác trùng thời gian.");
                    ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                    ViewBag.PhongHocList = _db.PhongHoc.ToList();
                    return View(lopHoc);
                }
            }

            // Nếu không có lỗi, thêm lớp học vào cơ sở dữ liệu
            try
            {
                _db.LopHoc.Add(lopHoc);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu vào cơ sở dữ liệu: " + ex.Message);
                ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                ViewBag.PhongHocList = _db.PhongHoc.ToList();
                return View(lopHoc); // Quay lại trang Create nếu có lỗi
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var lopHoc = await _db.LopHoc.Include(p => p.KhoaHoc).Include(p => p.PhongHoc)
                                          .FirstOrDefaultAsync(l => l.MaLopHoc == id);

            if (lopHoc == null)
            {

                return NotFound();
            }

            if (lopHoc.NgayKhaiGiang == default)
            {
                lopHoc.NgayKhaiGiang = DateTime.Now;
            }

            // Nếu có khóa học, tự tính ngày kết thúc
            if (lopHoc.KhoaHoc != null)
            {
                lopHoc.NgayKetThuc = TinhNgayKetThuc(lopHoc.KhoaHoc.TenKhoaHoc, lopHoc.NgayKhaiGiang);
            }
            ViewBag.KhoaHocList = await _db.KhoaHoc.ToListAsync();
            ViewBag.PhongHocList = await _db.PhongHoc.ToListAsync();

            return View(lopHoc);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, LopHoc lopHoc)
        {
            // Kiểm tra xem lớp học có tồn tại trong cơ sở dữ liệu không
            if (id != lopHoc.MaLopHoc)
            {
                return NotFound();
            }

            // Kiểm tra các dữ liệu đầu vào
            if (string.IsNullOrEmpty(lopHoc.TenLopHoc))
            {
                ModelState.AddModelError("TenLopHoc", "Tên lớp học không được để trống.");
                ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                ViewBag.PhongHocList = _db.PhongHoc.ToList();
                return View(lopHoc);
            }

            if (lopHoc.NgayKhaiGiang == default(DateTime))
            {
                ModelState.AddModelError("NgayKhaiGiang", "Ngày khai giảng không hợp lệ.");
                ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                ViewBag.PhongHocList = _db.PhongHoc.ToList();
                return View(lopHoc);
            }

            if (lopHoc.NgayKetThuc == default(DateTime))
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc không hợp lệ.");
                ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                ViewBag.PhongHocList = _db.PhongHoc.ToList();
                return View(lopHoc);
            }

            if (lopHoc.NgayKhaiGiang >= lopHoc.NgayKetThuc)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải sau ngày khai giảng.");
                ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                ViewBag.PhongHocList = _db.PhongHoc.ToList();
                return View(lopHoc);
            }

            // Kiểm tra xem phòng học và khóa học có tồn tại không
            if (!_db.KhoaHoc.Any(k => k.MaKhoaHoc == lopHoc.KhoaHocMaKhoaHoc))
            {
                ModelState.AddModelError("KhoaHocMaKhoaHoc", "Khóa học không tồn tại.");
                ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                ViewBag.PhongHocList = _db.PhongHoc.ToList();
                return View(lopHoc);
            }

            if (!_db.PhongHoc.Any(p => p.MaPhongHoc == lopHoc.PhongHocMaPhongHoc))
            {
                ModelState.AddModelError("PhongHocMaPhongHoc", "Phòng học không tồn tại.");
                ViewBag.KhoaHocList = _db.KhoaHoc.ToList();
                ViewBag.PhongHocList = _db.PhongHoc.ToList();
                return View(lopHoc);
            }
            // Kiểm tra thông tin khóa học và phòng học từ cơ sở dữ liệu
            var khoaHoc = await _db.KhoaHoc.FindAsync(lopHoc.KhoaHocMaKhoaHoc);
            if (khoaHoc == null)
            {
                ModelState.AddModelError("KhoaHocMaKhoaHoc", "Khóa học không tồn tại.");
            }

            var phongHoc = await _db.PhongHoc.FindAsync(lopHoc.PhongHocMaPhongHoc);
            if (phongHoc == null)
            {
                ModelState.AddModelError("PhongHocMaPhongHoc", "Phòng học không tồn tại.");
            }

            // Kiểm tra xung đột phòng học nếu cần
            if (khoaHoc != null && phongHoc != null)
            {
                bool phongBiTrung = await CheckRoomConflictForEdit(
                    lopHoc.PhongHocMaPhongHoc,
                    khoaHoc.NgayHoc,
                    lopHoc.NgayKhaiGiang,
                    lopHoc.NgayKetThuc,
                    khoaHoc.GioBatDau,
                    khoaHoc.GioKetThuc,
                    lopHoc.MaLopHoc
                );

                if (phongBiTrung)
                {
                    ModelState.AddModelError("PhongHocMaPhongHoc", "Phòng học đã có lớp khác trùng thời gian.");
                    ViewBag.KhoaHocList = await _db.KhoaHoc.ToListAsync();
                    ViewBag.PhongHocList = await _db.PhongHoc.ToListAsync();
                    return View(lopHoc);
                }
            }
            // Cập nhật lớp học nếu không có lỗi
            try
            {
                _db.Update(lopHoc);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi cập nhật lớp học: " + ex.Message);
                ViewBag.KhoaHocList = await _db.KhoaHoc.ToListAsync();
                ViewBag.PhongHocList = await _db.PhongHoc.ToListAsync();
                return View(lopHoc);
            }
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
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var lopHoc = await _db.LopHoc.FindAsync(id);
            if (lopHoc == null)
            {
                return Json(new { success = false, message = "Lớp học không tồn tại." });
            }

            try
            {
                _db.LopHoc.Remove(lopHoc);
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa lớp học: " + ex.Message });
            }
        }
        private async Task<bool> CheckRoomConflictForEdit(
            int phongHocId,
            string ngayHocMoi,
            DateTime ngayKhaiGiangMoi,
            DateTime ngayKetThucMoi,
            string gioBatDauMoi,
            string gioKetThucMoi,
            int? lopHocId = null)
        {
            // Tách ngày học mới: "Sáng T2,4,6" → "Sáng", ["2", "4", "6"]
            var ngayHocMoiParts = ngayHocMoi.Split(' ');
            if (ngayHocMoiParts.Length != 2)
                return false; // định dạng không hợp lệ

            string sessionTypeMoi = ngayHocMoiParts[0].Trim(); // "Sáng" hoặc "Chiều"
            var daysOfWeekMoi = ngayHocMoiParts[1]
                .Split(',')
                .Select(d => d.Trim().Replace("T", ""))
                .ToList(); // ["2", "4", "6"]

            var gioBatDauMoiTime = DateTime.ParseExact(gioBatDauMoi, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;
            var gioKetThucMoiTime = DateTime.ParseExact(gioKetThucMoi, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;

            var conflicts = await _db.LopHoc
                .Include(lh => lh.KhoaHoc)
                .Where(lh => lh.PhongHocMaPhongHoc == phongHocId && (lopHocId == null || lh.MaLopHoc != lopHocId))
                .ToListAsync();

            foreach (var conflict in conflicts)
            {
                var conflictParts = conflict.KhoaHoc.NgayHoc.Split(' ');
                if (conflictParts.Length != 2)
                    continue; // bỏ qua nếu format sai

                string conflictSessionType = conflictParts[0].Trim();
                var conflictDays = conflictParts[1]
                    .Split(',')
                    .Select(d => d.Trim().Replace("T", ""))
                    .ToList();

                // ✅ Chỉ kiểm tra nếu cùng buổi và có ít nhất 1 ngày trùng
                bool cungBuoi = conflictSessionType == sessionTypeMoi;
                bool trungNgayHoc = conflictDays.Intersect(daysOfWeekMoi).Any();

                if (cungBuoi && trungNgayHoc)
                {
                    bool giaoThoiGianNgay = conflict.NgayKhaiGiang <= ngayKetThucMoi && conflict.NgayKetThuc >= ngayKhaiGiangMoi;

                    if (giaoThoiGianNgay)
                    {
                        var conflictStartTime = DateTime.ParseExact(conflict.KhoaHoc.GioBatDau, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;
                        var conflictEndTime = DateTime.ParseExact(conflict.KhoaHoc.GioKetThuc, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;

                        // ✅ Kiểm tra giờ học trùng
                        bool giaoGioHoc = gioBatDauMoiTime < conflictEndTime && gioKetThucMoiTime > conflictStartTime;

                        if (giaoGioHoc)
                        {
                            return true; // Có xung đột thực sự
                        }
                    }
                }
            }

            return false; // Không có xung đột
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
                .Where(lh => lh.PhongHocMaPhongHoc == phongHocId && (lopHocId == null || lh.MaLopHoc != lopHocId)) // Tránh lớp hiện tại
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
                    var gioBatDauLop =  DateTime.ParseExact(khoaHoc.GioBatDau, "h:mm tt", CultureInfo.InvariantCulture).TimeOfDay;
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

        public async Task<IActionResult> DanhSachHocVien(int id)
        {
            var chiTietHocTaps = await _db.ChiTietHocTap
                .Include(ct => ct.User)
                .Where(ct => ct.MaLopHoc == id)
                .ToListAsync();

            var lop = await _db.LopHoc
                .Include(l => l.ChiTietGiangDay)
                    .ThenInclude(ctgd => ctgd.User)
                .FirstOrDefaultAsync(l => l.MaLopHoc == id);

            if (lop == null)
            {
                return NotFound();
            }

            ViewBag.TenLop = lop.TenLopHoc;

            var giangVien = lop.ChiTietGiangDay.FirstOrDefault()?.User;
        
            ViewBag.TenGiangVien = giangVien != null ? giangVien.HoTen : "Chưa phân công";

            return View(chiTietHocTaps);
        }






    }
}
