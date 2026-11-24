using System.Data.Entity;

namespace Web_Food_App.Models
{
    // Tương đương DBWebBanSachEntities của project sách, nhưng dùng Code First trỏ vào CSDL có sẵn
    public class FoodAppContext : DbContext
    {
        public FoodAppContext() : base("name=FoodAppContext")
        {
            // Không để EF tự tạo/sửa CSDL: CSDL do script FoodApp.sql tạo
            Database.SetInitializer<FoodAppContext>(null);
        }

        public virtual DbSet<AppUser> Users { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Restaurant> Restaurants { get; set; }
        public virtual DbSet<Food> Foods { get; set; }
        public virtual DbSet<Voucher> Vouchers { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderDetail> OrderDetails { get; set; }
        public virtual DbSet<Review> Reviews { get; set; }
        public virtual DbSet<Favorite> Favorites { get; set; }
        public virtual DbSet<PriceHistory> PriceHistories { get; set; }
        public virtual DbSet<FoodDetail> FoodDetails { get; set; }
    }
}
