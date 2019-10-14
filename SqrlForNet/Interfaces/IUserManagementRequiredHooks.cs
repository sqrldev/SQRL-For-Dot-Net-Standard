using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SqrlForNet.Interfaces
{
    public interface IUserManagementRequiredHooks
    {
        UserLookUpResult UserExists(string idk, HttpContext context);

        void UpdateUserId(string userId, string suk, string vuk, string oldUserId, HttpContext context);

        string GetUserVuk(string userId, HttpContext context);

        string GetUserSuk(string userId, HttpContext context);

        void UnlockUser(string userId, HttpContext context);

        void LockUser(string userId, HttpContext context);

        void RemoveUser(string idk, HttpContext context);
        
    }

    public interface IUserManagementRequiredHooksAsync
    {
        Task<UserLookUpResult> UserExists(string idk, HttpContext context);

        Task UpdateUserId(string userId, string suk, string vuk, string oldUserId, HttpContext context);

        Task<string> GetUserVuk(string userId, HttpContext context);

        Task GetUserSuk(string userId, HttpContext context);

        Task UnlockUser(string userId, HttpContext context);

        Task LockUser(string userId, HttpContext context);

        Task RemoveUser(string idk, HttpContext context);

    }

    public interface IUserManagementOptionalHooks
    {
        void CreateUser(string userId, string suk, string vuk, HttpContext context);

        void SqrlOnlyReceived(string userId);

        void HardlockReceived(string userId);
    }

    public interface IUserManagementOptionalHooksAsync
    {
        Task CreateUser(string userId, string suk, string vuk, HttpContext context);

        Task SqrlOnlyReceived(string userId);

        Task HardlockReceived(string userId);
    }

}