// Họ và tên: Trần Tuấn Sơn
// Mã sinh viên: 23203100043
// Nội dung thực hiện: Entity Bàn ăn
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhaHang.Models
{
    public enum TrangThaiBan
    {
        [Display(Name = "Sẵn sàng")]
        SanSang = 1,
        [Display(Name = "Đang sử dụng")]
        DangSuDung = 2,
        [Display(Name = "Tạm ngừng phục vụ")]
        TamNgung = 3
    }

    public class BanAn
    {
        [Key]
        public int MaBan { get; set; }

        [Required(ErrorMessage = "Tên bàn không được để trống")]
        [StringLength(50)]
        [Display(Name = "Tên bàn")]
        public string TenBan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại bàn")]
        [Display(Name = "Loại bàn")]
        public int MaLoaiBan { get; set; }

        [ForeignKey(nameof(MaLoaiBan))]
        public LoaiBan? LoaiBan { get; set; }

        [Required(ErrorMessage = "Số chỗ ngồi không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "Số chỗ ngồi phải lớn hơn 0")]
        [Display(Name = "Số chỗ ngồi")]
        public int SoChoNgoi { get; set; }

        [Required(ErrorMessage = "Vị trí không được để trống")]
        [StringLength(100)]
        [Display(Name = "Vị trí")]
        public string ViTri { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        [DataType(DataType.MultilineText)]
        public string? MoTa { get; set; }

        [Display(Name = "Trạng thái")]
        public TrangThaiBan TrangThai { get; set; } = TrangThaiBan.SanSang;

        public ICollection<PhieuDatBan> DanhSachPhieu { get; set; } = new List<PhieuDatBan>();
    }
}
