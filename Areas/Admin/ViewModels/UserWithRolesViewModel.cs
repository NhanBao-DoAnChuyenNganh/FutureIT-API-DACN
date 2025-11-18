using DoAnCoSo_Web.Models;

namespace DoAnCoSo_Web.Areas.Admin.ViewModels
{
    public class UserWithRolesViewModel
    {
        public User User { get; set; }
        public IList<string> Roles { get; set; }
    }
}
