using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Web_Food_App.Models
{
    // ---------- Trang chủ ----------
    public class HomeIndexViewModel
    {
        public List<FoodDetail> Foods { get; set; }
        public List<RecommendedFood> Recommended { get; set; }
        public int? CategoryId { get; set; }
        public int? RestaurantId { get; set; }
        public string CategoryTitle { get; set; }
        public string Keyword { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
    }

    public class HomePageViewModel
    {
        public List<RestaurantHomeCard> Restaurants { get; set; }
        public List<Category> Categories { get; set; }

        public HomePageViewModel()
        {
            Restaurants = new List<RestaurantHomeCard>();
            Categories = new List<Category>();
        }
    }

    public class RestaurantHomeCard
    {
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public string Address { get; set; }
        public string ImageUrl { get; set; }
        public string CategoryName { get; set; }
        public int FoodCount { get; set; }
        public double? AvgRating { get; set; }
        public int ReviewCount { get; set; }
    }

    // Kết quả của stored procedure sp_RecommendFoods (tên thuộc tính khớp tên cột trả về)
    public class RecommendedFood
    {
        public int FoodId { get; set; }
        public string FoodName { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; }
        public string RestaurantName { get; set; }
        public double AvgRating { get; set; }
        public int ReviewCount { get; set; }
    }

    // ---------- Chi tiết món ----------
    public class FoodDetailsViewModel
    {
        public Food Food { get; set; }
        public List<Review> Reviews { get; set; }
        public Review MyReview { get; set; }
        public double AvgRating { get; set; }
        public bool IsFavorite { get; set; }
        public List<FoodDetail> Related { get; set; }
    }

    // ---------- Giỏ hàng / đặt hàng ----------
    public class CartLineViewModel
    {
        public Food Food { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get { return Food.Price * Quantity; } }
        public bool OverStock { get { return Quantity > Food.Stock; } }
    }

    public class CartViewModel
    {
        public List<CartLineViewModel> Lines { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal EstimatedTotal { get { return Subtotal + ShippingFee; } }
    }

    public class CheckoutViewModel
    {
        public CartViewModel Cart { get; set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; }
        public string VoucherCode { get; set; }
        public List<Voucher> AvailableVouchers { get; set; }
    }

    // ---------- Tài khoản ----------
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên tài khoản.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên tài khoản từ 3 đến 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Tên tài khoản chỉ gồm chữ không dấu, số, dấu chấm và gạch dưới.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
        public string Email { get; set; }

        [RegularExpression(@"^[0-9+]{9,15}$", ErrorMessage = "Số điện thoại gồm 9–15 chữ số.")]
        public string Phone { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }

    public class ProfileViewModel
    {
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
        public string Email { get; set; }

        [RegularExpression(@"^[0-9+]{9,15}$", ErrorMessage = "Số điện thoại gồm 9–15 chữ số.")]
        public string Phone { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
        public string Address { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [DataType(DataType.Password)]
        public string ConfirmNewPassword { get; set; }
    }

    // ---------- Quản trị ----------
    public class MonthlyRevenue
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopFood
    {
        public string FoodName { get; set; }
        public int Quantity { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<MonthlyRevenue> Monthly { get; set; }
        public List<TopFood> TopFoods { get; set; }
        public List<PriceHistory> PriceChanges { get; set; }
    }
}
