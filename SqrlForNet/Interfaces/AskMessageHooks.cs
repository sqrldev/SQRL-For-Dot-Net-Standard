using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SqrlForNet.Interfaces
{
    public interface IAskMessageHooks
    {
        AskMessage GetAskQuestion(HttpRequest request, string nut);

        bool ProcessAskResponse(HttpRequest request, string nut, int button);
    }

    public interface IAskMessageHooksAsync
    {
        Task<AskMessage> GetAskQuestion(HttpRequest request, string nut);

        Task<bool> ProcessAskResponse(HttpRequest request, string nut, int button);
    }
}