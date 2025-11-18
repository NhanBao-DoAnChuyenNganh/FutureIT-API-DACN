namespace DoAnCoSo_Web.Areas.Staff.ViewModels
{
    public class ToCaoDanhGiaViewModel
    {
        public int MaDanhGia { get; set; }
        public string NoiDungDanhGia { get; set; }
        public string LyDoToCao { get; set; }
        public string NguoiGuiDanhGia { get; set; }
        public string NguoiToCao { get; set; }
        public string UserDanhGiaId { get; set; } 
        public string UserToCaoId { get; set; }   
        public int ToCaoId { get; set; }
        public bool IsDanhGiaBanned { get; set; }
        public bool IsToCaoBanned { get; set; }
    }
}
