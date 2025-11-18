using DoAnCoSo_Web.Models;

namespace DoAnCoSo_Web.Areas.Student.ViewModels
{
    public class XemChiTietViewModels
    {
        public string? UserID { get; set; }
        public KhoaHoc KhoaHocCanXem { get; set; }
        public List<HinhAnhKhoaHoc> listHinh { get; set; } = new List<HinhAnhKhoaHoc>();
        public List<DanhGia> dsDanhGia { get; set; } = new List<DanhGia>();

        public int TongluotDanhGia { get; set; }
        public double SoSaoTrungBinh {  get; set; }

        public bool DaQuanTam {  get; set; }

        public bool DangChoXuLy {  get; set; }//check khóa học có đang được xử lý đăng ký
    }
}
