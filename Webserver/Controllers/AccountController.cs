﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebServer.Models.ViewModels;
using WebServer.Models.AIoTDB;

namespace WebServer.Controllers
{
    public class AccountController : Controller
    {
        private readonly AIoTDBContext _aiot;

        // 建構函式，用來初始化資料庫上下文
        public AccountController(AIoTDBContext aiot)
        {
            _aiot = aiot;
        }

        // 標記此方法為 HTTP GET 請求的處理方法
        [HttpGet]
        public async Task<IActionResult> Signin(string returnUrl)
        {
            // 使用 Task.Yield() 讓當前執行的任務暫時讓出控制權，允許其他任務執行
            await Task.Yield();

            // 創建一個 SigninViewModel 實例，並設置 ReturnUrl 屬性
            var model = new SigninViewModel
            {
                // 登入後要跳轉的頁面，從方法參數中獲取
                ReturnUrl = returnUrl,
            };

            // 返回登入視圖，並將模型傳遞給視圖
            return View(model);
        }

        // 標記此方法為 HTTP POST 請求的處理方法
        [HttpPost]
        // 防止 CSRF (Cross-Site Request Forgery) 跨站偽造請求的攻擊
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signin(SigninViewModel model)
        {
            try
            {
                // 檢查帳號密碼是否正確
                // 通常帳號會忽略大小寫
                if (string.IsNullOrEmpty(model.Account))
                {
                    throw new Exception("請輸入帳號");
                }
                if (string.IsNullOrEmpty(model.Password))
                {
                    throw new Exception("請輸入密碼");
                }

                // 允許使用 Account 或 Email 登入
                var account = model.Account.Trim().ToUpper();
                var query = from s in _aiot.User
                            where (s.AccountNormalize == account
                                 || s.EmailNormalize == account)
                                && s.PasswordHash == EncoderSHA512(model.Password)
                            select s;
                var user = query.FirstOrDefault();

                if (user == null)
                    throw new Exception("帳號或密碼錯誤");

                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
                    throw new Exception("帳號被鎖定");

                // 設定 Cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                    new Claim(ClaimTypes.Name, user.Account),
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(principal);

                // 沒有指定返回的頁面就導向 /Home/Index
                if (string.IsNullOrEmpty(model.ReturnUrl))
                    return RedirectToAction("Index", "Home");
                else
                    return Redirect(model.ReturnUrl);
            }
            catch (Exception e)
            {
                // 錯誤訊息
                ModelState.AddModelError(nameof(SigninViewModel.ErrorMessage), e.Message);
                return View(nameof(Signin), model);
            }
        }

        // 標記此方法為 HTTP GET 請求的處理方法
        [HttpGet]
        public async Task<IActionResult> Signup()
        {
            // 使用 Task.Yield() 讓當前執行的任務暫時讓出控制權，允許其他任務執行
            await Task.Yield();

            // 返回一個視圖，通常是顯示註冊表單的頁面
            return View();
        }

        // 標記此方法為 HTTP POST 請求的處理方法
        [HttpPost]
        // 防止 CSRF (Cross-Site Request Forgery) 跨站偽造請求的攻擊
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(SignupViewModel model)
        {
            try
            {
                // 驗證模型是否有效
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.Where(s => s.Errors.Any()).Select(s => s);
                    throw new Exception(errors.First().Errors.First().ErrorMessage);
                }
                // 設置新用戶屬性
                model.User.ID = Guid.NewGuid();
                model.User.Account = model.User.Account.Trim();
                model.User.AccountNormalize = model.User.Account.ToUpper();
                model.User.PasswordHash = EncoderSHA512(model.User.Password);
                model.User.Name = model.User.Name.Trim();
                model.User.Email = model.User.Email.Trim();
                model.User.EmailNormalize = model.User.Email.ToUpper();
                model.User.CreatedDT = DateTime.Now;

                // 將新用戶添加到資料庫
                await _aiot.User.AddAsync(model.User);
                await _aiot.SaveChangesAsync();
            }
            catch (Exception e)
            {
                // 錯誤訊息
                ModelState.AddModelError(nameof(SignupViewModel.ErrorMessage), e.Message);
                return View(model);
            }
            // 返回登入頁，並自動代入所註冊的帳號
            return View(nameof(Signin), new SigninViewModel
            {
                Account = model.User?.Account,
            });
        }

        /// <summary>
        /// 將輸入字符串進行 SHA-512 編碼
        /// </summary>
        /// <param name="input">要編碼的字符串</param>
        /// <returns>SHA-512 哈希值的十六進制表示</returns>
        private static string EncoderSHA512(string input)
        {
            // 檢查輸入是否為 null 或空字符串
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("Input cannot be null or empty.", nameof(input));
            }

            // 使用 SHA512 進行哈希計算
            using (SHA512 sha512 = SHA512.Create())
            {
                // 將輸入字符串轉換為字節數組
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                // 計算哈希值
                byte[] hashBytes = sha512.ComputeHash(inputBytes);

                // 將哈希值轉換為十六進制字符串
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2")); // 將每個字節轉換為兩位十六進制數
                }

                return sb.ToString(); // 返回十六進制表示的哈希值
            }
        }
    }
}