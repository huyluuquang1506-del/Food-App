using System.Web.Mvc;
using System.Web.Routing;

namespace Web_Food_App.Filters
{
    // Bắt buộc đăng nhập (kiểm tra Session["UserId"], cùng cách làm với project sách)
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Session["UserId"] == null)
            {
                filterContext.Controller.TempData["Warning"] = "Vui lòng đăng nhập để tiếp tục.";
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    controller = "Account",
                    action = "Login",
                    returnUrl = filterContext.HttpContext.Request.RawUrl
                }));
            }
        }
    }

    // Chỉ cho tài khoản có Role = "Admin"
    public class AdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;
            if (session["UserId"] == null)
            {
                filterContext.Controller.TempData["Warning"] = "Vui lòng đăng nhập bằng tài khoản quản trị.";
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    controller = "Account",
                    action = "Login",
                    returnUrl = filterContext.HttpContext.Request.RawUrl
                }));
            }
            else if (!string.Equals(session["Role"] as string, "Admin"))
            {
                filterContext.Controller.TempData["Error"] = "Bạn không có quyền truy cập trang quản trị.";
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    controller = "Home",
                    action = "Index"
                }));
            }
        }
    }
}
