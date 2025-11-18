using DoAnCoSo_Web.Models;

namespace DoAnCoSo_Web.Areas.Student.ViewModels
{
    public class sub_KhoaHocViewModels
    {
        public KhoaHoc KhoaHoc { get; set; }
        public int TongLuotBinhLuan {  get; set; }
        public int TongLuotQuanTam { get; set; }
        public double SoSaoTrungBinh {  get; set; }
        public bool DaYeuThich {  get; set; }
    }
}
