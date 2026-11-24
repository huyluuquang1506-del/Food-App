using System.Linq;
using System.Web.Mvc;
using Web_Food_App.Models;

namespace Web_Food_App.Controllers
{
    // Controller gốc: giữ DbContext dùng chung và giải phóng khi xong request
    public abstract class BaseController : Controller
    {
        protected FoodAppContext db = new FoodAppContext();

        protected int? CurrentUserId
        {
            get { return Session["UserId"] as int?; }
        }

        // Chỉ cho phép chuyển hướng về địa chỉ nội bộ của website
        protected ActionResult RedirectBack(string returnUrl, string fallbackAction = "Index", string fallbackController = "Home")
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(fallbackAction, fallbackController);
        }

        // Nếu tài khoản đang đăng nhập bị quản trị viên khóa thì đăng xuất ngay ở request kế tiếp
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (filterContext.IsChildAction) return;

            int? uid = CurrentUserId;
            if (uid.HasValue && !db.Users.Any(u => u.UserId == uid.Value && u.IsActive))
            {
                Session.Clear();
                TempData["Error"] = "Tài khoản của bạn đã bị khóa hoặc không còn tồn tại.";
                filterContext.Result = RedirectToAction("Login", "Account");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
