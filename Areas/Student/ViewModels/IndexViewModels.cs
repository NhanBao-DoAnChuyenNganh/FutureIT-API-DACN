using DoAnCoSo_Web.Models;

namespace DoAnCoSo_Web.Areas.Student.ViewModels
{
    public class IndexViewModels
    {
        public TinTucTuyenDung TinMoi1 { get; set; }
        public TinTucTuyenDung TinMoi2 { get; set; }

        public List<KhoaHoc> list3KhoaHocPhoBien {  get; set; }

        public User GVTB1 { get; set; }
        public User GVTB2 { get; set; }
        public User GVTB3 { get; set; }
        public User GVTB4 { get; set; }
    }
}
