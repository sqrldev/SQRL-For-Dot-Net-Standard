using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqrlForNet;
using WithDatabase.Database;

namespace WithDatabase
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
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
                    options.CheckMilliSeconds = 10000;
                    options.CreateUserAsync = new SqrlManager().CreateUser;
                    options.UserExistsAsync = new SqrlManager().UserExists;
                    options.UpdateUserIdAsync = new SqrlManager().UpdateUserId;
                    options.UnlockUserAsync = new SqrlManager().UnlockUser;
                    options.LockUserAsync = new SqrlManager().LockUser;
                    options.RemoveUserAsync = new SqrlManager().RemoveUser;
                    options.GetUserVukAsync = new SqrlManager().GetUserVuk;
                    options.GetUserSukAsync = new SqrlManager().GetUserSuk;
                    options.GetUsernameAsync = new SqrlManager().GetUsername;
                    options.GetAndRemoveNutAsync = new SqrlManager().GetAndRemoveNut;
                    options.StoreNutAsync = new SqrlManager().StoreNut;
                    options.RemoveAuthorizedNutAsync = new SqrlManager().RemoveAuthorizedNut;
                    options.Events.OnTicketReceived += OnTicketReceived;
                });
            var connectionString = Configuration.GetConnectionString("DatabaseContext");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Please enter a valid connection string to create the database.");
            }
            services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(connectionString));
            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
            });
        }

        private async Task OnTicketReceived(TicketReceivedContext context)
        {
            var userId = context.Principal.FindFirst(ClaimTypes.NameIdentifier).Value;
            var database = context.HttpContext.RequestServices.GetRequiredService<DatabaseContext>();
            var user = await database.User.SingleAsync(x => x.SqrlUser.UserId == userId);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, user.Role)
            };
            var appIdentity = new ClaimsIdentity(claims);
            context.Principal.AddIdentity(appIdentity);
            context.Properties.IsPersistent = true;
            context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddHours(3);
            context.Properties.IssuedUtc = DateTimeOffset.UtcNow;
            context.Properties.AllowRefresh = true;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
