using System.Threading.Tasks;

namespace SqrlForNet.Interfaces
{
    public interface ICpsSessionManagementHooks
    {
        void StoreCpsSessionId(string code, string userId);

        string GetUserIdByCpsSessionId(string code);

        void RemoveCpsSessionId(string code);

    }

    public interface ICpsSessionManagementHooksAsync
    {
        Task StoreCpsSessionId(string code, string userId);

        Task<string> GetUserIdByCpsSessionId(string code);

        Task RemoveCpsSessionId(string code);

    }
}