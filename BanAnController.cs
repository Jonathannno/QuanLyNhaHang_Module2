// Họ và tên: Trần Tuấn Sơn
// Mã sinh viên: 23203100043
// Nội dung thực hiện: Quản lý bàn ăn + Tìm kiếm + Lọc + Sắp xếp + Phân trang
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHang.Models;

namespace QuanLyNhaHang.Controllers
{
    public class BanAnController : Controller
    {
        private readonly ApplicationDbContext _db;
        public BanAnController(ApplicationDbContext db) => _db = db;

        // Danh sách bàn: công khai cho cả khách hàng và admin xem/tra cứu
        // Quy trình: Truy vấn -> Tìm kiếm -> Lọc -> Sắp xếp -> Phân trang -> Hiển thị
        [HttpGet]
        public IActionResult Index(string? tuKhoa, int? maLoaiBan, int? soCho, TrangThaiBan? trangThai, string? sapXep, int trang = 1)
        {
            IQueryable<BanAn> query = _db.BanAns.Include(b => b.LoaiBan);

            // ----- Tìm kiếm (LINQ) theo Tên bàn hoặc Vị trí -----
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                query = query.Where(b => b.TenBan.Contains(tuKhoa) || b.ViTri.Contains(tuKhoa));
            }

            // ----- Lọc: có thể kết hợp nhiều điều kiện -----
            if (maLoaiBan.HasValue)
                query = query.Where(b => b.MaLoaiBan == maLoaiBan.Value);

            if (soCho.HasValue)
                query = query.Where(b => b.SoChoNgoi >= soCho.Value);

            if (trangThai.HasValue)
                query = query.Where(b => b.TrangThai == trangThai.Value);

            // ----- Sắp xếp -----
            query = sapXep switch
            {
                "ten_za" => query.OrderByDescending(b => b.TenBan).ThenBy(b => b.MaBan),
                "choNgoi_tang" => query.OrderBy(b => b.SoChoNgoi).ThenBy(b => b.MaBan),
                "choNgoi_giam" => query.OrderByDescending(b => b.SoChoNgoi).ThenBy(b => b.MaBan),
                _ => query.OrderBy(b => b.TenBan).ThenBy(b => b.MaBan) // mặc định A -> Z
            };

            var vm = new BanAnListVM
            {
                TuKhoa = tuKhoa,
                MaLoaiBanLoc = maLoaiBan,
                SoChoNgoiLoc = soCho,
                TrangThaiLoc = trangThai,
                SapXep = sapXep,
                DanhSachLoaiBan = _db.LoaiBans.OrderBy(l => l.TenLoaiBan).ToList()
            };

            vm.TongSoBan = query.Count();

            if (trang < 1) trang = 1;
            if (vm.TongSoTrang == 0)
                trang = 1;
            else if (trang > vm.TongSoTrang)
                trang = vm.TongSoTrang;
            vm.TrangHienTai = trang;

            // ----- Phân trang bằng Skip()/Take() trên truy vấn EF Core -----
            vm.DanhSachBan = query
                .Skip((trang - 1) * vm.SoDongMoiTrang)
                .Take(vm.SoDongMoiTrang)
                .ToList();

            return View(vm);
        }

        public IActionResult Details(int id, string? tuKhoa, int? maLoaiBan, int? soCho,
            TrangThaiBan? trangThai, string? sapXep, int trang = 1)
        {
            var ban = _db.BanAns.Include(b => b.LoaiBan).FirstOrDefault(b => b.MaBan == id);
            if (ban == null) return NotFound();

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaLoaiBan = maLoaiBan;
            ViewBag.SoCho = soCho;
            ViewBag.TrangThai = trangThai;
            ViewBag.SapXep = sapXep;
            ViewBag.Trang = trang;

            return View(ban);
        }

        // ---------------- CRUD (Admin) ----------------
        [RequireAdmin, HttpGet]
        public IActionResult Create()
        {
            ViewBag.DanhSachLoaiBan = _db.LoaiBans
                .Where(l => l.TrangThai)
                .OrderBy(l => l.TenLoaiBan)
                .ToList();
            return View();
        }

        [RequireAdmin, HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BanAn model)
        {
            model.TenBan = model.TenBan?.Trim() ?? string.Empty;
            ModelState.Remove(nameof(model.TenBan));
            TryValidateModel(model);

            if (_db.BanAns.Any(b => b.TenBan == model.TenBan))
                ModelState.AddModelError(nameof(model.TenBan), "Tên bàn đã tồn tại.");

            if (!_db.LoaiBans.Any(l => l.MaLoaiBan == model.MaLoaiBan && l.TrangThai))
                ModelState.AddModelError(nameof(model.MaLoaiBan), "Loại bàn không tồn tại hoặc đã ngừng sử dụng.");

            if (!ModelState.IsValid)
            {
                ViewBag.DanhSachLoaiBan = _db.LoaiBans
                    .Where(l => l.TrangThai)
                    .OrderBy(l => l.TenLoaiBan)
                    .ToList();
                return View(model);
            }

            _db.BanAns.Add(model);
            _db.SaveChanges();
            TempData["ThongBao"] = "Thêm bàn ăn thành công.";
            return RedirectToAction(nameof(Index));
        }

        [RequireAdmin, HttpGet]
        public IActionResult Edit(int id)
        {
            var ban = _db.BanAns.Find(id);
            if (ban == null) return NotFound();
            ViewBag.DanhSachLoaiBan = _db.LoaiBans
                .Where(l => l.TrangThai || l.MaLoaiBan == ban.MaLoaiBan)
                .OrderBy(l => l.TenLoaiBan)
                .ToList();
            return View(ban);
        }

        [RequireAdmin, HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, BanAn model)
        {
            if (id != model.MaBan) return NotFound();

            var banHienTai = _db.BanAns.AsNoTracking().FirstOrDefault(b => b.MaBan == id);
            if (banHienTai == null) return NotFound();

            model.TenBan = model.TenBan?.Trim() ?? string.Empty;
            ModelState.Remove(nameof(model.TenBan));
            TryValidateModel(model);

            if (_db.BanAns.Any(b => b.TenBan == model.TenBan && b.MaBan != id))
                ModelState.AddModelError(nameof(model.TenBan), "Tên bàn đã tồn tại.");

            if (!_db.LoaiBans.Any(l => l.MaLoaiBan == model.MaLoaiBan && (l.TrangThai || l.MaLoaiBan == banHienTai.MaLoaiBan)))
                ModelState.AddModelError(nameof(model.MaLoaiBan), "Loại bàn không tồn tại hoặc đã ngừng sử dụng.");

            if (!ModelState.IsValid)
            {
                ViewBag.DanhSachLoaiBan = _db.LoaiBans
                    .Where(l => l.TrangThai || l.MaLoaiBan == model.MaLoaiBan)
                    .OrderBy(l => l.TenLoaiBan)
                    .ToList();
                return View(model);
            }

            _db.BanAns.Update(model);
            _db.SaveChanges();
            TempData["ThongBao"] = "Cập nhật bàn ăn thành công.";
            return RedirectToAction(nameof(Index));
        }

        [RequireAdmin, HttpGet]
        public IActionResult Delete(int id)
        {
            var ban = _db.BanAns.Include(b => b.LoaiBan).FirstOrDefault(b => b.MaBan == id);
            if (ban == null) return NotFound();
            ViewBag.ConLienKet = _db.PhieuDatBans.Any(p => p.MaBan == id);
            return View(ban);
        }

        [RequireAdmin, HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var ban = _db.BanAns.Find(id);
            if (ban == null) return NotFound();

            if (_db.PhieuDatBans.Any(p => p.MaBan == id))
            {
                TempData["Loi"] = "Không thể xóa: bàn đã có phiếu đặt liên quan.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _db.BanAns.Remove(ban);
            _db.SaveChanges();
            TempData["ThongBao"] = "Xóa bàn ăn thành công.";
            return RedirectToAction(nameof(Index));
        }

        [RequireAdmin, HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CapNhatTrangThai(int id, TrangThaiBan trangThai)
        {
            var ban = _db.BanAns.Find(id);
            if (ban == null) return NotFound();

            if (!ModelState.IsValid || !Enum.IsDefined(typeof(TrangThaiBan), trangThai))
            {
                TempData["Loi"] = "Trạng thái bàn không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ban.TrangThai = trangThai;
            _db.SaveChanges();
            TempData["ThongBao"] = "Cập nhật trạng thái bàn thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
