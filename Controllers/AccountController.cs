using System;
using System.Linq;
using System.Web.Mvc;
using Web_Food_App.Filters;
using Web_Food_App.Helpers;
using Web_Food_App.Models;

namespace Web_Food_App.Controllers
{
    public class AccountController : BaseController
    {
        // GET: Account/Register
        [HttpGet]
        public ActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string username = model.Username.Trim();
            string email = model.Email.Trim();

            if (db.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError("Username", "Tên tài khoản đã tồn tại. Vui lòng chọn tên khác.");
            }
            if (db.Users.Any(u => u.Email == email))
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký.");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new AppUser
            {
                Username = username,
                PasswordHash = SecurityHelper.HashPassword(model.Password),
                FullName = model.FullName.Trim(),
                Email = email,
                Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
                Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim(),
                Role = "Customer",
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            db.Users.Add(user);
            db.SaveChanges();

            TempData["Success"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tên tài khoản và mật khẩu để đăng nhập.";
                return View();
            }

            string name = username.Trim();
            string hash = SecurityHelper.HashPassword(password);
            var user = db.Users.FirstOrDefault(u => u.Username == name && u.PasswordHash == hash);

            if (user == null)
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                return View();
            }
            if (!user.IsActive)
            {
                ViewBag.Error = "Tài khoản này đã bị khóa. Vui lòng liên hệ quản trị viên.";
                return View();
            }

            Session["UserId"] = user.UserId;
            Session["FullName"] = user.FullName;
            Session["Role"] = user.Role;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return user.Role == "Admin" ? RedirectToAction("Dashboard", "Admin") : RedirectToAction("Index", "Home");
        }

        // ---------- Tài khoản của tôi ----------

        [HttpGet]
        [RequireLogin]
        public ActionResult Profile()
        {
            var user = db.Users.Find(CurrentUserId.Value);
            if (user == null) return RedirectToAction("Logout");

            return View(new ProfileViewModel
            {
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address
            });
        }

        [HttpPost]
        [RequireLogin]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(ProfileViewModel model)
        {
            var user = db.Users.Find(CurrentUserId.Value);
            if (user == null) return RedirectToAction("Logout");

            model.Username = user.Username; // tên tài khoản không cho đổi
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = model.Email.Trim();
            if (db.Users.Any(u => u.Email == email && u.UserId != user.UserId))
            {
                ModelState.AddModelError("Email", "Email này đã được dùng bởi tài khoản khác.");
                return View(model);
            }

            user.FullName = model.FullName.Trim();
            user.Email = email;
            user.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
            user.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            db.SaveChanges();

            Session["FullName"] = user.FullName;
            TempData["Success"] = "Đã cập nhật thông tin tài khoản.";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        [RequireLogin]
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [RequireLogin]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = db.Users.Find(CurrentUserId.Value);
            if (user == null) return RedirectToAction("Logout");

            string currentHash = SecurityHelper.HashPassword(model.CurrentPassword);
            if (!string.Equals(user.PasswordHash, currentHash, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            user.PasswordHash = SecurityHelper.HashPassword(model.NewPassword);
            db.SaveChanges();

            TempData["Success"] = "Đã đổi mật khẩu thành công.";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }
    }
}
