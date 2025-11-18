using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo_Web.Models
{
    public class ChiTietHocTap
    {
        public string UserId {  get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        public int MaLopHoc {  get; set; }
        [ForeignKey("MaLopHoc")]
        public LopHoc LopHoc { get; set; }
        public string? NhanXetCuaGiaoVien {  get; set; }
        public string? TrangThai {  get; set; }
        public int? DiemTongKet {  get; set; }
    }
}
