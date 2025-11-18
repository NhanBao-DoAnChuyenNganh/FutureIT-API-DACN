using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo_Web.Models
{
    public class PhieuDangKyKhoaHoc
    {
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        public int MaKhoaHoc {  get; set; }
        [ForeignKey("MaKhoaHoc")]
        public KhoaHoc KhoaHoc { get; set; }

        public int MaHoaDon { get; set; }
        [ForeignKey("MaHoaDon")]
        public HoaDon HoaDon { get; set; }
        public string TrangThaiDangKy {  get; set; }
        public DateTime NgayDangKy { get; set; }
        public string TrangThaiThanhToan {  get; set; }
    }
}
