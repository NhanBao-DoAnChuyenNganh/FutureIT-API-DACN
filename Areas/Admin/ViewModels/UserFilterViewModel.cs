namespace DoAnCoSo_Web.Areas.Admin.ViewModels
{
    public class UserFilterViewModel
    {
        public string Role { get; set; }
        public string Status { get; set; }
        public List<UserWithRolesViewModel> Users { get; set; }
    }
}
