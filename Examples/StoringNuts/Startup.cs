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
                    options.CheckMilliSeconds = 1000;
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
                    options.GetAndRemoveNut = GetAndRemoveNut;
                    options.RemoveAuthorizedNut = RemoveAuthorizedNut;

                    options.StoreCpsSessionId = StoreCpsSessionId;
                    options.GetUserIdAndRemoveCpsSessionId = GetUserIdAndRemoveCpsSessionId;
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

        private NutInfo GetAndRemoveNut(string nut, HttpContext httpContext)
        {
            if (NutList.ContainsKey(nut))
            {
                var info = NutList[nut];
                NutList.Remove(nut);
                return info;
            }
            return null;
        }

        private void StoreNut(string nut, NutInfo info, bool authorized, HttpContext arg4)
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

        private NutInfo RemoveAuthorizedNut(string nut, HttpContext httpContext)
        {
            var authorizedNut = AuthorizedNutList.SingleOrDefault(x => x.Key == nut || x.Value.FirstNut == nut);
            if (authorizedNut.Key == nut)
            {
                AuthorizedNutList.Remove(nut);
                return authorizedNut.Value;
            }
            return authorizedNut.Value;
        }

        private string GetNutIdk(string nut, HttpContext httpContext)
        {
            return AuthorizedNutList.Single(x => x.Key == nut || x.Value.FirstNut == nut).Value.Idk;
        }


        private static readonly Dictionary<string, string> CpsSessions = new Dictionary<string, string>();

        private void StoreCpsSessionId(string sessionId, string userId, HttpContext arg3)
        {
            CpsSessions.Add(sessionId, userId);
        }

        private string GetUserIdAndRemoveCpsSessionId(string sessionId, HttpContext httpContext)
        {
            if (CpsSessions.ContainsKey(sessionId))
            {
                var userId = CpsSessions[sessionId];
                CpsSessions.Remove(userId);
                return userId;
            }

            return null;
        }
    }
}
