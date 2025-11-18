using DoAnCoSo_Web.Areas.Identity.Pages.Account;
using DoAnCoSo_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
namespace DoAnCoSo_Web.Areas.Admin.Pages.Users
{
    [Area("Admin")]

    [Authorize(Roles = "Admin")]
    public class AddUserModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AddUserModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _roleManager = roleManager;
        }

        [BindProperty]
        public RegisterModel.InputModel Input { get; set; }

        public async Task OnGetAsync()
        {
            Input = new RegisterModel.InputModel
            {
                RoleList = _roleManager.Roles.Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name
                })
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Input.RoleList = _roleManager.Roles.Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name
                });
                return Page();
            }

            var user = new User
            {
                UserName = Input.Email,
                Email = Input.Email,
                HoTen = Input.HoTen,
                SDT = Input.SDT,
                DiaChi = Input.DiaChi,
                NgayDK = DateTime.Now,
                IsApproved = true, 
                IsBanned = false,
                IsMainAdmin = false
                 
            };
            string defaultImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image/avatar.jpg");
            if (System.IO.File.Exists(defaultImagePath))
            {
                using (var image = await Image.LoadAsync<Rgba32>(defaultImagePath))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(50, 50),
                        Mode = ResizeMode.Crop
                    }));

                    using (var ms = new MemoryStream())
                    {
                        await image.SaveAsPngAsync(ms);
                        user.Avatar = ms.ToArray();
                    }
                }
            }
            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, Input.Role ?? "Student");
                TempData["SuccessMessage"] = "Tạo người dùng thành công!";
                return RedirectToAction("Index", "AdminHome", new { area = "Admin" });

            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            Input.RoleList = _roleManager.Roles.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Name
            });

            return Page();
        }

        private IUserEmailStore<User> GetEmailStore()
        {
            return (IUserEmailStore<User>)_userStore;
        }
    }
}
