using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DoAnCoSo_Web.Models
{
    public class HoaDon
    {
        [Key]
        public int MaHoaDon { get; set; }
        [Required]
        public DateTime NgayLap { get; set; }

        //Nếu đóng tiền mặt, chỗ nàu lưu mã Staff
        public string UserUserId {  get; set; }
        [ForeignKey("UserUserId")]
        [ValidateNever]
        public User User { get; set; }

        public decimal TienDongLan1 { get; set; }
        public decimal? TienDongLan2 { get; set; }
        public DateTime? NgayDongLan2 { get; set; }

        public ICollection<PhieuDangKyKhoaHoc> PhieuDangKyKhoaHoc { get; set; }

    }
}
