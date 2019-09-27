using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SqrlForNet;

namespace InMemory
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.LoginPath = "/SignIn";
                    options.LogoutPath = "/SignOut";
                    options.ExpireTimeSpan = TimeSpan.FromHours(3);
                })
                .AddSqrl(options =>
                {
                    options.CheckMillieSeconds = 1000;
                    options.CreateUser = SqrlCreateUser;
                    options.GetUserVuk = GetUserVuk;
                    options.UserExists = UserExists;
                    options.UpdateUserId = UpdateUserId;
                    options.UnlockUser = UnlockUser;
                    options.LockUser = LockUser;
                    options.RemoveUser = RemoveUser;
                    options.GetUserSuk = GetUserSuk;
                    options.Events.OnTicketReceived += OnTicketReceived;
                });
            services.AddMvc();
        }

        private Task OnTicketReceived(TicketReceivedContext context)
        {

            return Task.CompletedTask;
        }

        public class SqrlUser
        {
            public string UserId { get; set; }

            public string Suk { get; set; }

            public string Vuk { get; set; }

            public bool Locked { get; set; }

        }

        private static List<SqrlUser> _sqrlUsers = new List<SqrlUser>();

        private void SqrlCreateUser(string userId, string suk, string vuk, HttpContext context)
        {
            _sqrlUsers.Add(new SqrlUser()
            {
                UserId = userId,
                Suk = suk,
                Vuk = vuk
            });
        }

        private void UnlockUser(string userId, HttpContext context)
        {
            _sqrlUsers.Single(x => x.UserId == userId).Locked = false;
        }

        private void LockUser(string userId, HttpContext context)
        {
            _sqrlUsers.Single(x => x.UserId == userId).Locked = true;
        }

        private void RemoveUser(string userId, HttpContext arg2)
        {
            _sqrlUsers.Remove(_sqrlUsers.Single(x => x.UserId == userId));
        }

        private void UpdateUserId(string newUserId, string newSuk, string newVuk, string userId, HttpContext context)
        {
            var user = _sqrlUsers.Single(x => x.UserId == userId);
            user.UserId = newUserId;
            user.Suk = newSuk;
            user.Vuk = newVuk;
        }

        private UserLookUpResult UserExists(string userId, HttpContext context)
        {
            var user = _sqrlUsers.SingleOrDefault(x => x.UserId == userId);
            return user == null ? UserLookUpResult.Unknown : user.Locked ? UserLookUpResult.Disabled : UserLookUpResult.Exists;
        }

        private string GetUserVuk(string userId, HttpContext context)
        {
            return _sqrlUsers.Single(x => x.UserId == userId).Vuk;
        }

        private string GetUserSuk(string userId, HttpContext context)
        {
            return _sqrlUsers.Single(x => x.UserId == userId).Suk;
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.UseMvc();
        }
    }
}
