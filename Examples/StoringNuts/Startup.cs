using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SqrlForNet;

namespace StoringNuts
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
                    options.UserExists = UserExists;
                    options.UpdateUserId = UpdateUserId;
                    options.RemoveUser = RemoveUser;
                    options.LockUser = LockUser;
                    options.UnlockUser = UnlockUser;
                    options.GetUserVuk = GetUserVuk;
                    options.GetUserSuk = GetUserSuk;
                    options.Events.OnTicketReceived += OnTicketReceived;
                    options.Diagnostics = true;
                    options.DisableDefaultLoginPage = true;

                    //These are used to manage nuts
                    options.StoreNut = StoreNut;
                    options.GetNut = GetNut;
                    options.RemoveNut = RemoveNut;
                    options.GetNutIdk = GetNutIdk;
                    options.CheckNutAuthorized = CheckNutAuthorized;

                });

            services.AddMvc();
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

        public class SqrlUser
        {
            public string UserId { get; set; }

            public string Suk { get; set; }

            public string Vuk { get; set; }

            public bool Locked { get; set; }

        }

        private static List<SqrlUser> _sqrlUsers = new List<SqrlUser>();

        private static List<string> _sqrlAdminUserIds = new List<string>();

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

        private Task OnTicketReceived(TicketReceivedContext context)
        {
            var userId = context.Principal.FindFirst(ClaimTypes.NameIdentifier).Value;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, _sqrlAdminUserIds.Contains(userId) ? "SqrlAdminRole" : "SqrlUserRole")
            };
            var appIdentity = new ClaimsIdentity(claims);

            context.Principal.AddIdentity(appIdentity);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Used to store the nuts
        /// </summary>
        private static readonly Dictionary<string, NutInfo> NutList = new Dictionary<string, NutInfo>();

        /// <summary>
        /// Used to store nuts that have been Authorized, tasty nuts
        /// </summary>
        private static readonly Dictionary<string, NutInfo> AuthorizedNutList = new Dictionary<string, NutInfo>();

        private NutInfo GetNut(string nut, bool authorized)
        {
            if (authorized)
            {
                return AuthorizedNutList.ContainsKey(nut) ? AuthorizedNutList[nut] : null;
            }
            return NutList.ContainsKey(nut) ? NutList[nut] : null;
        }

        private void StoreNut(string nut, NutInfo info, bool authorized)
        {
            if (authorized)
            {
                AuthorizedNutList.Add(nut, info);
            }
            else
            {
                NutList.Add(nut, info);
            }
        }

        private void RemoveNut(string nut, bool authorized)
        {
            if (authorized)
            {
                AuthorizedNutList.Remove(nut);
            }
            else
            {
                NutList.Remove(nut);
            }
        }

        private bool CheckNutAuthorized(string nut)
        {
            return AuthorizedNutList.Any(x => x.Key == nut || x.Value.FirstNut == nut);
        }

        private string GetNutIdk(string nut)
        {
            return AuthorizedNutList.Single(x => x.Key == nut || x.Value.FirstNut == nut).Value.Idk;
        }

    }
}
