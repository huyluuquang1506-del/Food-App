using System.Linq;
using Web_Food_App.Models;

namespace Web_Food_App.Helpers
{
    public static class OrderService
    {
        // Hủy đơn: đổi trạng thái, trả lại tồn kho và lượt dùng voucher, tất cả trong 1 transaction.
        // (Script SQL chỉ có trigger TRỪ kho khi thêm chi tiết, nên việc cộng lại kho phải làm ở đây.)
        public static void Cancel(FoodAppContext db, Order order)
        {
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var details = db.OrderDetails.Where(d => d.OrderId == order.OrderId).ToList();
                    foreach (var d in details)
                    {
                        var food = db.Foods.Find(d.FoodId);
                        if (food != null) food.Stock += d.Quantity;
                    }

                    if (order.VoucherId.HasValue)
                    {
                        var voucher = db.Vouchers.Find(order.VoucherId.Value);
                        if (voucher != null) voucher.Quantity += 1;
                    }

                    order.Status = "Cancelled";
                    db.SaveChanges();
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }
    }
}
