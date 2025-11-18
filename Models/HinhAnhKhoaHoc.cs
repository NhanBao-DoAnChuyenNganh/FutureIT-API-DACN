using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DoAnCoSo_Web.Models
{
    public class HinhAnhKhoaHoc
    {
        [Key]
        public int MaHinh { get; set; }
        [Required]
        public byte[]? NoiDungHinh { get; set; }
        public bool LaAnhDaiDien { get; set; } = false;
        public int KhoaHocMaKhoaHoc {  get; set; }
        [ForeignKey("KhoaHocMaKhoaHoc")]
        [ValidateNever]
        public KhoaHoc KhoaHoc { get; set; }
    }
}
