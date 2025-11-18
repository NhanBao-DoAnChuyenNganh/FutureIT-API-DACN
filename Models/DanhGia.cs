using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo_Web.Models
{
    public class DanhGia
    {
        [Key]
        public int MaDanhGia { get; set; }

        [Required]
        public int SoSaoDanhGia { get; set; }
        [Required]
        public string NoiDungDanhGia { get; set; }
        [Required]
        public DateTime NgayDanhGia { get; set; } = DateTime.Now;
        public int KhoaHocMaKhoaHoc { get; set; }
        [ForeignKey("KhoaHocMaKhoaHoc")]
        [ValidateNever]
        public KhoaHoc KhoaHoc { get; set; }

        public string UserUserId {  get; set; }
        [ForeignKey("UserUserId")]
        [ValidateNever]
        public User User { get; set; }
        public ICollection<ToCaoDanhGia> ToCaoDanhGia { get; set; }


    }
}
