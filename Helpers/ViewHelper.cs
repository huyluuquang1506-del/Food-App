using System.Collections.Generic;

namespace Web_Food_App.Helpers
{
    // Các hàm nhỏ dùng chung trong View
    public static class ViewHelper
    {
        // Tăng phiên bản này sau khi thay ảnh để trình duyệt/IIS không giữ ảnh cũ trong cache.
        private const string ImageVersion = "20261123-1";

        // ImageUrl trong DB dạng "/Content/img/com-tam.jpg" -> "~/Content/img/com-tam.jpg"
        public static string ImagePath(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return null;

            string path = imageUrl.Trim();
            if (path.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            path = "~/" + path.TrimStart('/');
            return path + (path.Contains("?") ? "&" : "?") + "v=" + ImageVersion;
        }

        public static string Money(decimal amount)
        {
            return string.Format("{0:N0} đ", amount);
        }

        public static string Time(System.TimeSpan t)
        {
            return t.ToString(@"hh\:mm");
        }

        public static string StatusText(string status)
        {
            switch (status)
            {
                case "Pending": return "Chờ xác nhận";
                case "Confirmed": return "Đã xác nhận";
                case "Delivering": return "Đang giao";
                case "Completed": return "Hoàn thành";
                case "Cancelled": return "Đã hủy";
                default: return status;
            }
        }

        public static string StatusBadge(string status)
        {
            switch (status)
            {
                case "Pending": return "badge-warning";
                case "Confirmed": return "badge-info";
                case "Delivering": return "badge-primary";
                case "Completed": return "badge-success";
                case "Cancelled": return "badge-secondary";
                default: return "badge-light";
            }
        }

        public static string PaymentText(string method)
        {
            switch (method)
            {
                case "COD": return "Tiền mặt khi nhận (COD)";
                case "Momo": return "Ví Momo";
                case "Banking": return "Chuyển khoản ngân hàng";
                default: return method;
            }
        }

        public static readonly string[] PaymentMethods = { "COD", "Momo", "Banking" };

        public static readonly string[] AllStatuses = { "Pending", "Confirmed", "Delivering", "Completed", "Cancelled" };

        // Các trạng thái được phép chuyển tiếp từ trạng thái hiện tại
        public static IEnumerable<string> NextStatuses(string current)
        {
            switch (current)
            {
                case "Pending": return new[] { "Confirmed", "Cancelled" };
                case "Confirmed": return new[] { "Delivering", "Cancelled" };
                case "Delivering": return new[] { "Completed", "Cancelled" };
                default: return new string[0];
            }
        }
    }
}
