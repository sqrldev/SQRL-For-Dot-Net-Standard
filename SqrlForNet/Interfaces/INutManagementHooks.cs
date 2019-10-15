using System.Threading.Tasks;

namespace SqrlForNet.Interfaces
{
    public interface INutManagementHooks
    {
        NutInfo GetNut(string nut, bool authorized);

        void StoreNut(string nut, NutInfo info, bool authorized);

        void RemoveNut(string nut, bool authorized);

        bool CheckNutAuthorized(string nut);

        string GetNutIdk(string nut);

    }

    public interface INutManagementHooksAsync
    {
        Task<NutInfo> GetNut(string nut, bool authorized);

        Task StoreNut(string nut, NutInfo info, bool authorized);

        Task RemoveNut(string nut, bool authorized);

        Task CheckNutAuthorized(string nut);

        Task<string> GetNutIdk(string nut);

    }
}