using System.Data;
using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo_Web.Areas.Admin.ViewModels;
using DoAnCoSo_Web.Data;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DoAnCoSo_Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminHomeController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;
        public AdminHomeController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }
        // Hiển thị danh sách user chưa được duyệt
        public async Task<IActionResult> Index(string role, string status)
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<UserWithRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Lọc theo Role (nếu có)
                if (!string.IsNullOrEmpty(role) && !roles.Contains(role))
                    continue;

                // Lọc theo trạng thái (nếu có)
                if (status == "pending" && user.IsApproved)
                    continue;
                else if (status == "active" && (!user.IsApproved || user.IsBanned))
                    continue;
                else if (status == "banned" && !user.IsBanned)
                    continue;

                userList.Add(new UserWithRolesViewModel { User = user, Roles = roles });
            }

            var viewModel = new UserFilterViewModel
            {
                Role = role,
                Status = status,
                Users = userList
            };

            return View(viewModel);
        }

        // Duyệt user
        [HttpPost]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Index");
            }

            user.IsApproved = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Duyệt người dùng thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi duyệt người dùng.";
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> BanUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Index");
            }

            user.IsBanned = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Cấm người dùng thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cấm người dùng.";
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> UnBanUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Index");
            }

            user.IsBanned = false;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Gỡ cấm người dùng thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gỡ cấm người dùng.";
            }

            return RedirectToAction("Index");
        }
        // Từ chối user (xóa)
        [HttpPost]
        public async Task<IActionResult> RejectUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Index");
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Đã từ chối và xóa người dùng.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa người dùng.";
            }

            return RedirectToAction("Index");
        }
        private string TinhDoanhThuThang()
        {
            var result = _db.PhieuDangKyKhoaHoc.Include(p => p.HoaDon).Where(p => p.NgayDangKy.Month == DateTime.Now.Month).Sum(p => (p.HoaDon.TienDongLan1) + (p.HoaDon.TienDongLan2 ?? decimal.Zero));
            return result.ToString("N0");
        }
        private string TongTienChoThanhToan()
        {
            decimal TongTienConThieu = 0;
            List<PhieuDangKyKhoaHoc> listPhieuChuaDongDu = _db.PhieuDangKyKhoaHoc.Include(p => p.KhoaHoc).Include(p => p.HoaDon)
                                                            .Where(p => p.TrangThaiThanhToan == "Thanh toán 50%").ToList();
            foreach(var phieu in listPhieuChuaDongDu)
            {
                TongTienConThieu += phieu.KhoaHoc.HocPhi - phieu.HoaDon.TienDongLan1;
            }
            return TongTienConThieu.ToString("N0");
        }

        private int SoLuongKHBanThangHienTai()
        {
            var listKHBanRa = _db.PhieuDangKyKhoaHoc.Where(p => p.NgayDangKy.Month == DateTime.Now.Month).ToList();
            return listKHBanRa.Count;
        }

        public async Task<IActionResult> Index2(int? nam, int? thang)
        {
            int NamHienTai = DateTime.Now.Year;
            ViewBag.allYears = new[] { NamHienTai, NamHienTai-1, NamHienTai-2, NamHienTai-3, NamHienTai-4 };
            int NamDangChon = nam ?? DateTime.Now.Year;
            ViewBag.NamDangChon = NamDangChon;

            ViewBag.allMonths = new[] {1,2,3,4,5,6,7,8,9,10,11,12};
            int ThangDangChon = thang ?? DateTime.Now.Month;
            ViewBag.ThangDangChon = ThangDangChon;

            ViewBag.DoanhThuThang = TinhDoanhThuThang();
            ViewBag.TienConThieu = TongTienChoThanhToan();
            ViewBag.SoLuongKHBanThangHienTai = SoLuongKHBanThangHienTai();
            
            
            //phục vụ biểu đồ
            List<int> SLDKTheoThang = new List<int>();
            List<int> SLNoTheoThang = new List<int>();
            List<decimal> TongTienTheoThang = new List<decimal>();
            List<decimal> TongNoTheoThang = new List<decimal>();
            var Top5KhoaHoc = _db.PhieuDangKyKhoaHoc.Include(p => p.KhoaHoc)
                                .Where(p => p.NgayDangKy.Month == ThangDangChon && p.NgayDangKy.Year == NamDangChon)
                                .GroupBy(p => p.KhoaHoc.TenKhoaHoc)
                                .Select(g => new
                                {
                                    TenKhoaHoc = g.Key,
                                    SoLuongDangKy = g.Count()
                                }).OrderByDescending(g => g.SoLuongDangKy).Take(5).ToList();

            var listPhieu = await _db.PhieuDangKyKhoaHoc.Include(p => p.HoaDon).Where(p => p.NgayDangKy.Year == NamDangChon).ToListAsync();

            for (int i = 1; i <= 12; i++)
            {
                var PhieuTrongThang = listPhieu.Where(p => p.NgayDangKy.Month == i);
                var NoTrongThang = PhieuTrongThang.Where(p => p.TrangThaiThanhToan == "Thanh toán 50%");

                SLDKTheoThang.Add(PhieuTrongThang.Count());
                SLNoTheoThang.Add(NoTrongThang.Count());
                TongTienTheoThang.Add(PhieuTrongThang.Sum(p => (p.HoaDon.TienDongLan1) + (p.HoaDon.TienDongLan2 ?? decimal.Zero)));
                TongNoTheoThang.Add(NoTrongThang.Sum(p => p.HoaDon.TienDongLan1));
            }
            ViewBag.SLDKTheoThang = SLDKTheoThang;
            ViewBag.SLNoTheoThang = SLNoTheoThang;
            ViewBag.TongTienTheoThang = TongTienTheoThang;
            ViewBag.TongNoTheoThang = TongNoTheoThang;
            ViewBag.Top5KhoaHoc = Top5KhoaHoc;
            

            var listStudent = await _userManager.GetUsersInRoleAsync("Student");
            var listTeacher = await _userManager.GetUsersInRoleAsync("Teacher");
            var listStaff = await _userManager.GetUsersInRoleAsync("Staff");
            ViewBag.SLStudent = listStudent.Count;
            ViewBag.SLTeacher = listTeacher.Count;
            ViewBag.SLStaff = listStaff.Count;

            return View();
        }


        public async Task<IActionResult> DoanhThu(string month, string course)
        {
            var query = _db.HoaDon
                .Include(h => h.User)
                .Include(h => h.PhieuDangKyKhoaHoc).ThenInclude(p => p.User)
                .Include(h => h.PhieuDangKyKhoaHoc).ThenInclude(p => p.KhoaHoc)
                .AsQueryable();

            if (!string.IsNullOrEmpty(month) && int.TryParse(month, out int monthInt))
            {
                query = query.Where(h => h.NgayLap.Month == monthInt);
            }

            if (!string.IsNullOrEmpty(course))
            {
                query = query.Where(h => h.PhieuDangKyKhoaHoc.Any(p => p.KhoaHoc.TenKhoaHoc.Contains(course)));
            }

            var listDT = await query.ToListAsync();

            // Lấy danh sách TẤT CẢ khóa học để hiển thị dropdown
            var allCourses = await _db.KhoaHoc
                .Select(k => k.TenKhoaHoc)
                .Distinct()
                .ToListAsync();

            ViewData["SelectedMonth"] = month;
            ViewData["SelectedCourse"] = course;
            ViewData["AllCourses"] = allCourses;

            return View(listDT);
        }



        //Them-Xoa-Sua Staff
        public async Task<IActionResult> QuanLyStaff()
        {
            var listStaff = await _userManager.GetUsersInRoleAsync("Staff");
            var staff = listStaff.Select(u => new
            {
                Avatar = u.Avatar != null ? $"data:image/png;base64,{Convert.ToBase64String(u.Avatar)}" : "/image/avatar.png",
                u.Id,
                u.Email,
                u.HoTen,
                u.IsApproved,
                u.IsBanned,
                u.IsMainAdmin             
            }).ToList();
            return View(staff);
        }
        public async Task<IActionResult> DisplayStaff(string Id)
        {
            var user = await _db.Users.FindAsync(Id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
        public async Task<IActionResult> ChinhSua(String id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> ChinhSua(User userInput, IFormFile imageurl, string password)
        {
            // 1) Lấy lại user gốc từ DB
            var user = await _userManager.FindByIdAsync(userInput.Id.ToString());
            if (user == null) return NotFound();

            // 2) Cập nhật các trường cơ bản
            user.HoTen = userInput.HoTen;
            user.DiaChi = userInput.DiaChi;
            user.SDT = userInput.SDT;
            user.Email = userInput.Email;
            user.UserName = userInput.Email;

            // 3) Xử lý avatar (giống khi tạo)
            if (imageurl != null && imageurl.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await imageurl.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using var image = await SixLabors.ImageSharp.Image.LoadAsync(memoryStream);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(50, 50),
                    Mode = ResizeMode.Crop
                }));

                using var outputStream = new MemoryStream();
                await image.SaveAsPngAsync(outputStream);
                user.Avatar = outputStream.ToArray();
            }
            // nếu không upload mới, giữ nguyên avatar cũ

            // 4) Nếu admin nhập password mới (không bắt buộc), reset password
            if (!string.IsNullOrWhiteSpace(password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var pwResult = await _userManager.ResetPasswordAsync(user, token, password);
                if (!pwResult.Succeeded)
                {
                    TempData["Error"] = string.Join(" | ", pwResult.Errors.Select(e => e.Description));
                    return View(user);
                }
            }

            // 5) Lưu cập nhật lên DB
            var updateResult = await _userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault(); // Lấy vai trò đầu tiên (giả sử user chỉ có 1 vai trò)

                if (userRole == "Staff")
                {
                    TempData["Success"] = "Cập nhật thông tin nhân viên thành công!";
                    return RedirectToAction("DisplayStaff", "AdminHome", new { id = user.Id });
                }
                else if (userRole == "Student")
                {
                    TempData["Success"] = "Cập nhật thông tin học viên thành công!";
                    return RedirectToAction("DisplayStudent", "AdminHome", new { id = user.Id });
                }
                else if (userRole == "Teacher")
                {
                    TempData["Success"] = "Cập nhật thông tin giảng viên thành công!";
                    return RedirectToAction("DisplayTeacher", "AdminHome", new { id = user.Id });
                }              
            }

            // 6) Nếu có lỗi, hiển thị lại view
            TempData["Error"] = string.Join(" | ", updateResult.Errors.Select(e => e.Description));
            return View(user);
        }
        [HttpGet]
        public async Task<IActionResult> Xoa(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Xoá thất bại!";
            }
            else
            {
                TempData["Success"] = "Xoá thành công!";
            }
            return RedirectToAction("Index2");
        }
        //Them-Xoa-Sua Student 
        public async Task<IActionResult> QuanLyStudent()
        {
            var listStudent = await _userManager.GetUsersInRoleAsync("Student");
            var students = listStudent.Select(u => new
            {
                Avatar = u.Avatar != null ? $"data:image/png;base64,{Convert.ToBase64String(u.Avatar)}" : "/image/avatar.png",
                u.Id,
                u.Email,
                u.HoTen,
               u.IsApproved,
               u.IsBanned,
               u.IsMainAdmin
            }).ToList();
            return View(students);
        }
        public async Task<IActionResult> DisplayStudent(string Id)
        {
            var user = await _db.Users
                .Include(u => u.PhieuDangKyKhoaHoc)
                    .ThenInclude(p => p.KhoaHoc)
                .Include(u => u.ChiTietHocTap)
                    .ThenInclude(c => c.LopHoc)
                .FirstOrDefaultAsync(u => u.Id == Id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        //Them-Xoa-Sua Teacher
        public async Task<IActionResult> QuanLyTeacher()
        {
            var teacherIds = (await _userManager.GetUsersInRoleAsync("Teacher"))
                             .Select(t => t.Id)
                             .ToList();

            var teachers = await _db.Users
                .Include(u => u.ChuyenNganh)
                .Where(u => teacherIds.Contains(u.Id))
                .ToListAsync();

            var viewModel = teachers.Select(u => new
            {
                Avatar = u.Avatar != null ? $"data:image/png;base64,{Convert.ToBase64String(u.Avatar)}" : "/image/avatar.png",
                u.Id,
                u.Email,
                u.HoTen,
                u.IsApproved,
                u.IsBanned,
                u.IsMainAdmin,
                ChuyenNganh = u.ChuyenNganh?.TenChuyenNganh ?? "Chưa chọn"
            }).ToList();

            return View(viewModel);
        }


        public async Task<IActionResult> DisplayTeacher(string Id)
        {
            var user = await _db.Users
                      .Include(u => u.ChuyenNganh)
                      .FirstOrDefaultAsync(u => u.Id == Id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
        public async Task<IActionResult> EditTeacher(string id)
        {
            var user = await _db.Users.Include(u => u.ChuyenNganh).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound();

            ViewBag.ListChuyenNganh = new SelectList(_db.ChuyenNganh, "MaChuyenNganh", "TenChuyenNganh", user.ChuyenNganhMaChuyenNganh);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditTeacher(string id, int? ChuyenNganhMaChuyenNganh)
        {
            var user = await _db.Users.Include(u => u.ChuyenNganh).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound();

            user.ChuyenNganhMaChuyenNganh = ChuyenNganhMaChuyenNganh;
            _db.Update(user);
            await _db.SaveChangesAsync();

            return RedirectToAction("QuanLyTeacher");
        }

        //QuanLyNo
        public async Task<IActionResult> QuanLyNo(string han, string course)
        {
            var query = _db.PhieuDangKyKhoaHoc.Include(p => p.User).Include(p => p.HoaDon).Include(p => p.KhoaHoc)
                .Where(p => p.TrangThaiThanhToan == "Thanh toán 50%").AsQueryable();

            if (!string.IsNullOrEmpty(course))
            {
                query = query.Where(p => p.KhoaHoc.TenKhoaHoc.Contains(course));
            }

            if (!string.IsNullOrEmpty(han))
            {
                var now = DateTime.Now.Date;

                switch (han)
                {
                    case "Nợ trong vòng 30 ngày":
                        query = query.Where(p => p.HoaDon != null && EF.Functions.DateDiffDay(p.HoaDon.NgayLap, now) <= 30);
                        break;

                    case "Nợ từ 30 đến 60 ngày":
                        query = query.Where(p => p.HoaDon != null &&
                                EF.Functions.DateDiffDay(p.HoaDon.NgayLap, now) > 30 &&
                                EF.Functions.DateDiffDay(p.HoaDon.NgayLap, now) <= 60);
                        break;

                    case "Nợ quá hạn":
                        query = query.Where(p => p.HoaDon != null && EF.Functions.DateDiffDay(p.HoaDon.NgayLap, now) > 60);
                        break;
                }
            }

            var listNo = await query.ToListAsync();
            var allCourses = await _db.KhoaHoc.Select(k => k.TenKhoaHoc).Distinct().ToListAsync();

            ViewBag.TongTienConNo = TongTienChoThanhToan();
            ViewData["SelectedCourse"] = course;
            ViewData["SelectedHan"] = han;
            ViewData["AllCourses"] = allCourses;

            return View(listNo);
        }
    }
}
