using Microsoft.EntityFrameworkCore;
using WebServer.Models.AIoTDB;
using WebServer.Services;

namespace Webserver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

           

            // �ϥ� Session
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // �ϥ� Cookie
            builder.Services
                .AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    //�s���Q���������
                    options.AccessDeniedPath = new PathString("/Account/Signin");
                    //�n�J��
                    options.LoginPath = new PathString("/Account/Signin");
                    //�n�X��
                    options.LogoutPath = new PathString("/Account/Signout");
                });

            //�]�w�s�u�r��
            builder.Services.AddDbContext<AIoTDBContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("AIoTDB"));
            });

            //�`�J�A��
            builder.Services.AddScoped<ValidatorService>();

            var app = builder.Build();
            // Configure the HTTP request pipeline.�����n��
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
