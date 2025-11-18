// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace DoAnCoSo_Web.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public IndexModel(
            UserManager<User> userManager,
            SignInManager< User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            public string HoTen { get; set; }
            [Phone]
            public string SDT { get; set; }
            public string? DiaChi { get; set; }
            public DateTime NgayDK { get; set; }
            [BindProperty]
            public IFormFile? Avatar { get; set; }
            public string? AvatarBase64 { get; set; } // Dùng để hiển thị ảnh cũ
        }

        private async Task LoadAsync(User user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                HoTen = user.HoTen,
                SDT = user.SDT,
                DiaChi = user.DiaChi,
                NgayDK = user.NgayDK,
                
                AvatarBase64 = user.Avatar != null ?
                   $"data:image/png;base64,{Convert.ToBase64String(user.Avatar)}" : null
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            user.HoTen = Input.HoTen;
            user.SDT = Input.SDT;
            user.DiaChi = Input.DiaChi;
            if (Input.Avatar != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    try
                    {
                        await Input.Avatar.CopyToAsync(memoryStream);
                        memoryStream.Position = 0; // Đảm bảo stream bắt đầu từ đầu

                        using (var image = await Image.LoadAsync(memoryStream))
                        {
                            image.Mutate(x => x.Resize(50, 50)); // Resize ảnh về 50x50

                            using (var outputStream = new MemoryStream())
                            {
                                await image.SaveAsPngAsync(outputStream);
                                user.Avatar = outputStream.ToArray();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Lỗi xử lý ảnh: " + ex.Message);
                    }
                }
            }
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Lỗi khi cập nhật thông tin.");
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Thông tin của bạn đã được cập nhật";
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                return RedirectToAction("Index", "AdminHome", new { area = "Admin" });
            }
            else if (roles.Contains("Staff"))
            {
                return RedirectToAction("Index", "StaffHome", new { area = "Staff" });
            }
            else if(roles.Contains("Teacher"))
            {
                return RedirectToAction("Index", "TeacherHome", new { area = "Teacher" });
            }
            return RedirectToAction("Index", "StudentHome", new { area = "Student" });
        }
        public async Task<IActionResult> OnPostCancelAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var appUser = user as User;
            if (appUser == null)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại.");
                return Page();
            }

            // Lấy danh sách vai trò của người dùng
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                return RedirectToAction("Index", "AdminHome", new { area = "Admin" });
            }
            else if (roles.Contains("Staff"))
            {
                return RedirectToAction("Index", "StaffHome", new { area = "Staff" });
            }
            else if (roles.Contains("Teacher"))
            {
                return RedirectToAction("Index", "TeacherHome", new { area = "Teacher" });
            }
            return RedirectToAction("Index", "StudentHome", new { area = "Student" });
        }
    }
}
