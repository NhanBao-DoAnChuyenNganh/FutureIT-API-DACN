using DoAnCoSo_Web.Data;
using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo_Web.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = "Staff,Admin")]
    public class StaffHomeController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _db;
        public StaffHomeController(UserManager<User> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;

            _db = db;
        }
        public IActionResult Index()
        {
            return View();

        }
        public IActionResult TinTucTuyenDung()
        {
            var danhSachTin = _db.TinTucTuyenDung
                                    .OrderByDescending(t => t.NgayDang)
                                    .ToList();
            return View(danhSachTin);
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(TinTucTuyenDung tinTuc, IFormFile? imageFile)
        {
            // Gán ngày đăng là ngày hiện tại
            tinTuc.NgayDang = DateTime.Now;

            // Kiểm tra dữ liệu hợp lệ
            if (string.IsNullOrEmpty(tinTuc.TieuDeTinTuc))
            {
                ModelState.AddModelError("TieuDeTinTuc", "Tiêu đề không được để trống.");
            }
            if (string.IsNullOrEmpty(tinTuc.NoiDungTinTuc))
            {
                ModelState.AddModelError("NoiDungTinTuc", "Nội dung không được để trống.");
            }
            if (tinTuc.NgayKetThuc < tinTuc.NgayDang)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải sau ngày đăng.");
            }

            if (!ModelState.IsValid)
            {
                return View(tinTuc);
            }

            // Xử lý file hình ảnh nếu có
            if (imageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("", "Chỉ chấp nhận hình ảnh .jpg, .jpeg, .png, .gif.");
                    return View(tinTuc);
                }

                using (var memoryStream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(memoryStream);
                    tinTuc.HinhTinTuc = memoryStream.ToArray();
                }
            }

            try
            {
                _db.TinTucTuyenDung.Add(tinTuc);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(TinTucTuyenDung));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu vào cơ sở dữ liệu: " + ex.Message);
                return View(tinTuc);
            }
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var tinTuc = _db.TinTucTuyenDung.Find(id);
            if (tinTuc == null)
            {
                return NotFound();
            }

            return View(tinTuc);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(int id, TinTucTuyenDung tinTuc, IFormFile? imageFile)
        {
            if (id != tinTuc.MaTinTuc)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(tinTuc);
            }

            var existingTinTuc = await _db.TinTucTuyenDung.FindAsync(id);
            if (existingTinTuc == null)
            {
                return NotFound();
            }

            // Gán lại các thuộc tính ngoại trừ ảnh
            existingTinTuc.TieuDeTinTuc = tinTuc.TieuDeTinTuc;
            existingTinTuc.NoiDungTinTuc = tinTuc.NoiDungTinTuc;
            existingTinTuc.NgayDang = tinTuc.NgayDang;
            existingTinTuc.NgayKetThuc = tinTuc.NgayKetThuc;

            // Kiểm tra logic
            if (existingTinTuc.NgayKetThuc < existingTinTuc.NgayDang)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải sau ngày đăng.");
                return View(tinTuc);
            }

            // Xử lý hình nếu có hình mới
            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("HinhTinTuc", "Chỉ chấp nhận ảnh có định dạng .jpg, .jpeg, .png, .gif.");
                    return View(tinTuc);
                }

                using (var memoryStream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(memoryStream);
                    existingTinTuc.HinhTinTuc = memoryStream.ToArray();
                }
            }

            try
            {
                _db.Update(existingTinTuc);
                await _db.SaveChangesAsync();
                TempData["MessageTinTuc"] = "Cập nhật tin tức thành công!";
                return RedirectToAction(nameof(TinTucTuyenDung));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi cập nhật dữ liệu: " + ex.Message);
                return View(tinTuc);
            }
        }
        [HttpGet]
        public IActionResult DeleteConfirm(int id)
        {
            var tinTuc = _db.TinTucTuyenDung.Find(id);
            if (tinTuc == null)
            {
                return NotFound();
            }
            return View(tinTuc); // Trả về view xác nhận xóa
        }
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var tinTuc = _db.TinTucTuyenDung.Find(id);
            if (tinTuc == null)
            {
                return Json(new { success = false });
            }

            _db.TinTucTuyenDung.Remove(tinTuc);
            _db.SaveChanges();
            return Json(new { success = true });
        }
        [HttpGet]
        public IActionResult Detail(int id)
        {
            var tinTuc = _db.TinTucTuyenDung.FirstOrDefault(t => t.MaTinTuc == id);
            if (tinTuc == null)
            {
                return NotFound();
            }

            return View(tinTuc);
        }
    }
}

