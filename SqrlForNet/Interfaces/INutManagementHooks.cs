using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SqrlForNet.Interfaces
{
    public interface INutManagementHooks
    {
        NutInfo GetAndRemoveNut(string nut, HttpContext context);

        void StoreNut(string nut, NutInfo info, bool authorized, HttpContext context);

        bool RemoveAuthorizedNut(string nut, HttpContext context);

        string GetNutIdk(string nut, HttpContext context);

    }

    public interface INutManagementHooksAsync
    {
        Task<NutInfo> GetAndRemoveNut(string nut, HttpContext context);

        Task StoreNut(string nut, NutInfo info, bool authorized, HttpContext context);

        Task<bool> RemoveAuthorizedNut(string nut, HttpContext context);

        Task<string> GetNutIdk(string nut, HttpContext context);

    }
}