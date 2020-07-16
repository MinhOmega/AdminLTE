using LiteCommerce.BusiniessLayer;
using LiteCommerce.DomainModels;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace LiteCommerce.Admin.Controllers
{
    /// <summary>
    ///
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        /// <summary>
        /// Trang chủ mặc định
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            /*Employee data = AccountBLL.GetPass(User.Identity.Name);
            return View(data);*/
            WebUserData userData = User.GetUserData();
            Employee data = AccountBLL.GetPass(userData.UserID);
            return View(data);
        }

        /// <summary>
        /// Trang thay đổi mật khẩu
        /// </summary>
        /// <returns></returns>
        public ActionResult ChangePassword(string email, string currentpassword, string password, string confirmpassword)
        {
            if (Request.HttpMethod == "GET")
            {
                return View();
            }
            else
            {
                Employee employee = AccountBLL.GetPass(email);
                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmpassword))
                {
                    ModelState.AddModelError("errorNull", "Mật khẩu không được để trống");
                    return View();
                }
                //if (currentpassword.Equals(employee.Password))
                if (String.Equals(employee.Password, EncodeMD5.GetMD5(currentpassword)))
                {
                    if (password.Equals(confirmpassword))
                    {
                        bool result = AccountBLL.Update(EncodeMD5.GetMD5(password), email);
                        if (result)
                        {
                            return RedirectToAction("Index", "Dashboard");
                        }
                        else
                        {
                            return View();
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("errorCurrentpass", "Mật khẩu nhập lại không trùng khớp");
                        return View();
                    }
                }
                else
                {
                    ModelState.AddModelError("errorOlderpass", "Mật khẩu cũ không đúng");
                    return View();
                }
            }
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        /// <returns>~/Login -- chuyển về trang đăng nhập</returns>
        public ActionResult SignOut()
        {
            Session.Abandon();
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }

        /// <summary>
        /// Trang đăng nhập
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult Login(string email = "", string password = "")
        {
            if (Request.HttpMethod == "GET")
            {
                return View();
            }
            else
            {
                //TODO: Kiểm tra thông tin đăng nhập qua CSDL
                var userAccount = AccountBLL.Authorize(email, EncodeMD5.GetMD5(password), UserAccountTypes.Employee);
                if (userAccount != null)
                {
                    WebUserData cookiesData = new Admin.WebUserData()
                    {
                        UserID = userAccount.UserID,
                        FullName = userAccount.FullName,
                        GroupName = userAccount.GroupName,
                        LoginTime = DateTime.Now,
                        SessionID = Session.SessionID,
                        ClientIP = Request.UserHostAddress,
                        Photo = userAccount.Photo,
                    };
                    FormsAuthentication.SetAuthCookie(cookiesData.ToCookieString(), false);
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    ModelState.AddModelError("LoginError", "Đăng nhập thất bại");
                    ViewBag.Email = email;
                    return View();
                }
                /*bool result = AccountBLL.Login(email, password);
                if (result)
                {
                    FormsAuthentication.SetAuthCookie(email, false);
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    ModelState.AddModelError("LoginError", "Đăng nhập thất bại");
                    ViewBag.Email = email;
                    return View();
                }*/
            }
        }

        /// <summary>
        /// Hàm kiểm tra xem Email đã tồn tại hay chưa
        /// </summary>
        /// <param name="emailID">tên email</param>
        /// <returns>Nếu tồn tại trả về true và ngược lại</returns>
        [NonAction]
        public bool IsEmailExist(string emailID)
        {
            UserAccount account = UserAccountBLL.UserAccount_GetUser(emailID);
            if (account != null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Hàm gửi đường dẫn xác nhận việc đổi mật khẩu
        /// </summary>
        /// <param name="emailID">địa chỉ email</param>
        /// <param name="activationCode">mã reset password</param>
        /// <param name="emailFor">Định danh email là tạo mới hay thay đổi mk</param>
        [NonAction]
        public void SendVerificationLinkEmail(string emailID, string activationCode, string emailFor = "VerifyAccount")
        {
            var verifyUrl = "/Account/ResetPassword?Resetcode=" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

            var fromEmail = new MailAddress("Your_Email_Here", "Forgot Password");
            var toEmail = new MailAddress(emailID);
            var fromEmailPassword = "Your_Password_Here";
            // forward this link for more information https://www.google.com/settings/security/lesssecureapps

            string subject = "";
            string body = "";
            if (emailFor == "VerifyAccount")
            {
                subject = "Tai khoan cua ban da duoc tao!";

                body = "<br/><br/>We are excited to tell you that your Dotnet Awesome account is" +
                   " successfully created. Please click on the below link to verify your account" +
                   " <br/><br/><a href='" + link + "'>" + link + "</a> ";
            }
            else
            {
                subject = "Forgot your password || LiteCommerce";

                body = "<br/><br/>Click vào link bên dưới để xác nhận đổi mật khẩu" +
                   " <br/><br/><a href='" + link + "'>" + link + "</a> ";
            }

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
            };

            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
                smtp.Send(message);
        }

        /// <summary>
        /// Trang để điển email quên mật khẩu
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult ForgotPass()
        {
            return View();
        }

        /// <summary>
        /// Trang lấy lại mật khẩu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public ActionResult ForgotPass(UserAccount data)
        {
            if (string.IsNullOrEmpty(data.UserID))
            {
                ModelState.AddModelError("EmptyEmail", "Chưa nhập email để khôi phục mật khẩu");
            }
            if (IsEmailExist(data.UserID) == true)
            {
                ModelState.AddModelError("IsNotExistEmailInDb", "Tài khoản này không có trong hệ thống. Hãy kiểm tra xem " +
                    "bạn đã nhập đúng địa chỉ email khôi phục mật khẩu chưa?");
            }
            if (!ModelState.IsValid)
            {
                return View();
            }
            string message = "";

            UserAccount account = UserAccountBLL.UserAccount_GetUser(data.UserID);

            if (account.UserID != null)
            {
                string resetCode = Guid.NewGuid().ToString();
                SendVerificationLinkEmail(account.UserID, resetCode, "ResetPassword");
                account.ResetPasswordCode = resetCode;
                UserAccountBLL.UserAccount_AddResetCode(data.UserID, resetCode);
                message = "Đã gửi đường dẫn đến email của bạn. vui lòng check mail để lấy lại mật khẩu!";
            }
            else
            {
                message = "account not found";
            }

            ViewBag.Message = message;
            return View();
        }

        /// <summary>
        /// Đặt lại mật khẩu
        /// </summary>
        /// <param name="ResetCode"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult ResetPassword(string ResetCode)
        {
            UserAccount user = UserAccountBLL.UserAccount_GetUserByResetPasswordCode(ResetCode);

            if (user != null)
            {
                ResetPasswordModel model = new ResetPasswordModel();
                model.ResetCode = ResetCode;

                return View(model);
            }
            else
            {
                return RedirectToAction("ErrorRessetCode", "Error");
            }
        }

        /// <summary>
        /// Trang đặt lại mật khẩu
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            if (model.NewPassword.Length < 8)
            {
                ModelState.AddModelError("", "mật khẩu phái có ít nhất 8 ký tự!");
            }
            else
            {
                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError("", "mật khẩu nhập lại không đúng!");
                }
                else
                {
                    var message = "";
                    UserAccount user = UserAccountBLL.UserAccount_GetUserByResetPasswordCode(model.ResetCode);

                    if (user != null)
                    {
                        UserAccountBLL.UserAccount_ChangePassword(user.UserID, model.NewPassword);
                        UserAccountBLL.UserAccount_AddResetCode(user.UserID, "");
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        message = "fail";
                    }
                    ViewBag.message = message;
                }
            }

            return View(model);
        }

        /// <summary>
        /// Trang thay đổi thông tin cá nhân
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ChangeInfo(Employee data, HttpPostedFileBase fileImage = null)
        {
            if (data != null)
            {
                Employee getEmployee = HumanResourceBLL.GetEmployee(data.EmployeeID);
                data.PhotoPath = getEmployee.PhotoPath;
                data.Password = getEmployee.Password;
                data.Notes = getEmployee.Notes;

                if (fileImage != null)
                {
                    string get = DateTime.Now.ToString("ddMMyyyhhmmss");
                    string fileExtension = Path.GetExtension(fileImage.FileName);
                    string fileName = get + fileExtension;
                    string path = Path.Combine(Server.MapPath("~/Images"), fileName);
                    data.PhotoPath = fileName;
                    fileImage.SaveAs(path);
                }
                if (fileImage == null)
                {
                    data.PhotoPath = getEmployee.PhotoPath;
                }

                bool IsEmailExist = AccountBLL.IsEmailExist(getEmployee.Email, getEmployee.EmployeeID);
                if (IsEmailExist)
                {
                    ModelState.AddModelError("errorEmailDuplicate1", "Email đã tồn tại. Vui lòng nhập email khác!");
                    return RedirectToAction("Index");
                }
                else
                {
                    bool editUser = HumanResourceBLL.Employee_Update(data);
                }
            }
            return RedirectToAction("Index");
        }

    }
}