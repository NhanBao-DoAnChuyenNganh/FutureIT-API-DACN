using DoAnCoSo_Web.Models;

namespace DoAnCoSo_Web.Areas.Student.ViewModels
{
    public class KhoaHocDaDangKyViewModels
    {
        public List<PhieuDangKyKhoaHoc> listPhieuDangKy { get; set; }
        public List<ChiTietHocTap> listDangHoc {  get; set; }

        public List<ChiTietHocTap> listDaHoc { get; set; }

        public List<PhieuDangKyKhoaHoc> listConNo { get; set; }
    }
}
