using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SqrlForNet;
using SqrlForNet.Interfaces;
using WithDatabase.Database;

namespace WithDatabase
{
    public class SqrlManager : IUserManagementRequiredHooksAsync, IUserManagementOptionalHooksAsync, INutManagementHooksAsync
    {

        public async Task<UserLookUpResult> UserExists(string userId, HttpContext context)
        {
            var _database = context.RequestServices.GetRequiredService<DatabaseContext>();
            var user = await _database.User.Include(x => x.SqrlUser).SingleOrDefaultAsync(x => x.SqrlUser.UserId == userId);
            if (user == null)
            {
                return UserLookUpResult.Unknown;
            }
            return user.SqrlUser.Locked ? UserLookUpResult.Disabled : UserLookUpResult.Exists;
        }

        public async Task UpdateUserId(string userId, string suk, string vuk, string oldUserId, HttpContext context)
        {
            var _database = context.RequestServices.GetRequiredService<DatabaseContext>();
            var user = await _database.User.Include(x => x.SqrlUser).SingleAsync(x => x.SqrlUser.UserId == oldUserId);
            user.SqrlUser.UserId = userId;
            user.SqrlUser.Suk = suk;
            user.SqrlUser.Vuk = vuk;
            await _database.SaveChangesAsync();
        }

        public async Task<string> GetUserVuk(string userId, HttpContext context)
        {
            var _database = context.RequestServices.GetRequiredService<DatabaseContext>();
            var user = await _database.User.Include(x => x.SqrlUser).SingleAsync(x => x.SqrlUser.UserId == userId);
            return user.SqrlUser.Vuk;
        }

        public async Task<string> GetUserSuk(string userId, HttpContext context)
        {
            var _database = context.RequestServices.GetRequiredService<DatabaseContext>();
            var user = await _database.User.Include(x => x.SqrlUser).SingleAsync(x => x.SqrlUser.UserId == userId);
            return user.SqrlUser.Suk;
        }

        public async Task UnlockUser(string userId, HttpContext context)
        {
            var _database = context.RequestServices.GetRequiredService<DatabaseContext>();
            var user = await _database.User.Include(x => x.SqrlUser).SingleAsync(x => x.SqrlUser.UserId == userId);
            user.SqrlUser.Locked = false;
            await _database.SaveChangesAsync();
        }

        public async Task LockUser(string userId, HttpContext context)
        {
            var _database = context.RequestServices.GetRequiredService<DatabaseContext>();
            var user = await _database.User.Include(x => x.SqrlUser).SingleAsync(x => x.SqrlUser.UserId == userId);
            user.SqrlUser.Locked = true;
            await _database.SaveChangesAsync();
        }

        public async Task RemoveUser(string userId, HttpContext context)
        {
            var _database = context.RequestServices.GetRequiredService<DatabaseContext>();
            var user = await _database.User.Include(x => x.SqrlUser).SingleAsync(x => x.SqrlUser.UserId == userId);
            _database.SqrlUser.Remove(user.SqrlUser);
            await _database.SaveChangesAsync();
        }

        public async Task CreateUser(string userId, string suk, string vuk, HttpContext context)
        {
            var _database = context.RequestServices.GetRequiredService<DatabaseContext>();
            _database.User.Add(new User()
            {
                Username = "New user",
                Role = "User",
                SqrlUser = new SqrlUser()
                {
                    UserId = userId,
                    Suk = suk,
                    Vuk = vuk,
                    Locked = false
                }
            });
            await _database.SaveChangesAsync();
        }

        public Task SqrlOnlyReceived(string userId, HttpContext context)
        {
            throw new System.NotImplementedException();
        }

        public Task HardlockReceived(string userId, HttpContext context)
        {
            throw new System.NotImplementedException();
        }

        public async Task<string> GetUsername(string userId, HttpContext context)
        {
            var _database = context.RequestServices.GetRequiredService<DatabaseContext>();
            var user = await _database.User.Include(x => x.SqrlUser).SingleAsync(x => x.SqrlUser.UserId == userId);
            return user.Username;
        }

        public async Task<NutInfo> GetAndRemoveNut(string nut, HttpContext context)
        {
            var _database = context.RequestServices.GetRequiredService<DatabaseContext>();
            var nutData = await _database.Nuts.FindAsync(nut);
            _database.Nuts.Remove(nutData);
            await _database.SaveChangesAsync();
            return nutData;
        }

        public async Task StoreNut(string nut, NutInfo info, bool authorized, HttpContext context)
        {
            var _database = context.RequestServices.GetRequiredService<DatabaseContext>();
            await _database.Nuts.AddAsync(new NutInfoData()
            {
                Nut = nut,
                Authorized = authorized,
                CreatedDate = info.CreatedDate,
                FirstNut = info.FirstNut,
                Idk = info.Idk,
                IpAddress = info.IpAddress
            });
            await _database.SaveChangesAsync();
        }

        public async Task<NutInfo> RemoveAuthorizedNut(string nut, HttpContext context)
        {
            var _database = context.RequestServices.GetRequiredService<DatabaseContext>();
            var isAuthorized = await _database.Nuts.Where(x => x.Authorized).AnyAsync(x => x.Nut == nut || x.FirstNut == nut);
            if (isAuthorized)
            {
                var authorizedNut = await _database.Nuts.Where(x => x.Authorized).SingleAsync(x => x.Nut == nut || x.FirstNut == nut);
                _database.Nuts.Remove(authorizedNut);
                await _database.SaveChangesAsync();
                return authorizedNut;
            }
            return null;
        }

        public async Task<string> GetNutIdk(string nut, HttpContext context)
        {
            var _database = context.RequestServices.GetRequiredService<DatabaseContext>();
            var nutInfo = await _database.Nuts.FindAsync(nut);
            return nutInfo.Idk;
        }
    }
}
