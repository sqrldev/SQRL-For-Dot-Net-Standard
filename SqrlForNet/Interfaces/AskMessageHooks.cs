using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SqrlForNet.Interfaces
{
    public interface AskMessageHooks
    {
        AskMessage GetAskQuestion(HttpRequest request, string nut);

        bool ProcessAskResponse(HttpRequest request, string nut, int button);
    }

    public interface AskMessageHooksAsync
    {
        Task<AskMessage> GetAskQuestion(HttpRequest request, string nut);

        Task<bool> ProcessAskResponse(HttpRequest request, string nut, int button);
    }
}