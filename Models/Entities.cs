using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Các lớp thực thể ánh xạ 1-1 với bảng trong script FoodApp.sql.
// (Dùng Code First "trỏ vào CSDL có sẵn" thay cho file .edmx, vì .edmx phải tạo bằng trình thiết kế của Visual Studio.)
namespace Web_Food_App.Models
{
    [Table("Users")]
    public class AppUser
    {
        [Key]
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<Favorite> Favorites { get; set; }
    }

    [Table("Categories")]
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }

        public virtual ICollection<Food> Foods { get; set; }
    }

    [Table("Restaurants")]
    public class Restaurant
    {
        [Key]
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<Food> Foods { get; set; }
    }

    [Table("Foods")]
    public class Food
    {
        [Key]
        public int FoodId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên món.")]
        [StringLength(150, ErrorMessage = "Tên món tối đa 150 ký tự.")]
        public string FoodName { get; set; }

        public int CategoryId { get; set; }
        public int RestaurantId { get; set; }

        [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Giá phải từ 0 trở lên.")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho phải từ 0 trở lên.")]
        public int Stock { get; set; }

        public string Description { get; set; }

        [StringLength(255, ErrorMessage = "Đường dẫn ảnh tối đa 255 ký tự.")]
        public string ImageUrl { get; set; }

        public bool IsActive { get; set; }

        public virtual Category Category { get; set; }
        public virtual Restaurant Restaurant { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<Favorite> Favorites { get; set; }
    }

    [Table("Vouchers")]
    public class Voucher : IValidatableObject
    {
        [Key]
        public int VoucherId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã voucher.")]
        [StringLength(30, ErrorMessage = "Mã voucher tối đa 30 ký tự.")]
        [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Mã chỉ gồm chữ không dấu, số, gạch ngang và gạch dưới.")]
        public string Code { get; set; }

        [Range(1, 100, ErrorMessage = "Phần trăm giảm từ 1 đến 100.")]
        public int DiscountPercent { get; set; }

        [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Mức giảm tối đa phải từ 0 trở lên.")]
        public decimal MaxDiscount { get; set; }

        [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Giá trị đơn tối thiểu phải từ 0 trở lên.")]
        public decimal MinOrderAmount { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượt dùng phải từ 0 trở lên.")]
        public int Quantity { get; set; }

        public bool IsActive { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.Year < 2000)
            {
                yield return new ValidationResult("Vui lòng chọn ngày bắt đầu.", new[] { "StartDate" });
            }
            if (EndDate.Year < 2000)
            {
                yield return new ValidationResult("Vui lòng chọn ngày kết thúc.", new[] { "EndDate" });
            }
            else if (EndDate.Date < StartDate.Date)
            {
                yield return new ValidationResult("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.", new[] { "EndDate" });
            }
        }
    }

    [Table("Orders")]
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public int? VoucherId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }

        // Cột tính toán (TotalAmount + ShippingFee - DiscountAmount) do SQL Server tự tính
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal FinalAmount { get; set; }

        public virtual AppUser User { get; set; }
        public virtual Voucher Voucher { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }

    [Table("OrderDetails")]
    public class OrderDetail
    {
        [Key]
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public int FoodId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Note { get; set; }

        [NotMapped]
        public decimal LineTotal { get { return Quantity * UnitPrice; } }

        public virtual Order Order { get; set; }
        public virtual Food Food { get; set; }
    }

    [Table("Reviews")]
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }
        public int FoodId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Food Food { get; set; }
        public virtual AppUser User { get; set; }
    }

    // Khóa chính kép (UserId, FoodId)
    [Table("Favorites")]
    public class Favorite
    {
        [Key, Column(Order = 0)]
        public int UserId { get; set; }

        [Key, Column(Order = 1)]
        public int FoodId { get; set; }

        public DateTime AddedAt { get; set; }

        public virtual AppUser User { get; set; }
        public virtual Food Food { get; set; }
    }

    [Table("PriceHistory")]
    public class PriceHistory
    {
        [Key]
        public int HistoryId { get; set; }
        public int FoodId { get; set; }
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public DateTime ChangedAt { get; set; }

        public virtual Food Food { get; set; }
    }

    // Ánh xạ view vw_FoodDetails (chỉ đọc): món + nhóm + quán + điểm đánh giá trung bình
    [Table("vw_FoodDetails")]
    public class FoodDetail
    {
        [Key]
        public int FoodId { get; set; }
        public string FoodName { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public double? AvgRating { get; set; }
        public int ReviewCount { get; set; }
    }
}
