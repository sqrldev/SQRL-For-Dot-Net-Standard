using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace SqrlForNet
{
    public class SqrlAuthenticationOptions : RemoteAuthenticationOptions
    {

        public byte[] EncryptionKey { get; set; }

        public int NutExpiresInSeconds { get; set; }

        public int CheckMillieSeconds { get; set; }

        public string NameForAnonymous { get; set; }

        public string CancelledPath { get; set; }
        
        public bool Diagnostics { get; set; }
        
        public bool DisableDefaultLoginPage { get; set; }
        
        public bool EnableHelpers { get; set; }

        public string[] HelpersPaths { get; set; }

        public OtherAuthenticationPath[] OtherAuthenticationPaths { get; set; }
        
        public Func<string, HttpContext, UserLookUpResult> UserExists;

        public Func<string, HttpContext, Task<UserLookUpResult>> UserExistsAsync;

        internal UserLookUpResult UserExistsInternal(string idk, HttpContext context)
        {
            if (UserExists != null)
            {
                return UserExists.Invoke(idk, context);
            }
            else
            {
                var task = UserExistsAsync.Invoke(idk, context);
                task.Wait();
                return task.Result;
            }
        }

        public Action<string, string, string, string, HttpContext> UpdateUserId;
        
        public Func<string, string, string, string, HttpContext, Task> UpdateUserIdAsync;

        internal void UpdateUserIdInternal(string idk, string suk, string vuk, string pidk, HttpContext context)
        {
            if (UpdateUserId != null)
            {
                UpdateUserId.Invoke(idk, suk, vuk, pidk, context);
            }
            else
            {
                var task = UpdateUserIdAsync.Invoke(idk, suk, vuk, pidk, context);
                task.Wait();
            }
        }

        public Action<string, string, string, HttpContext> CreateUser;

        public Func<string, string, string, HttpContext, Task> CreateUserAsync;

        internal void CreateUserInternal(string idk, string suk, string vuk, HttpContext context)
        {
            if (CreateUser != null)
            {
                CreateUser.Invoke(idk, suk, vuk, context);
            }
            else
            {
                var task = CreateUserAsync.Invoke(idk, suk, vuk, context);
                task.Wait();
            }
        }

        public Func<string, HttpContext, string> GetUserVuk;

        public Func<string, HttpContext, Task<string>> GetUserVukAsync;

        internal string GetUserVukInternal(string idk, HttpContext context)
        {
            if (GetUserVuk != null)
            {
                return GetUserVuk.Invoke(idk, context);
            }
            else
            {
                var task = GetUserVukAsync.Invoke(idk, context);
                task.Wait();
                return task.Result;
            }
        }

        public Func<string, HttpContext, string> GetUserSuk;

        public Func<string, HttpContext, Task<string>> GetUserSukAsync;

        internal string GetUserSukInternal(string idk, HttpContext context)
        {
            if (GetUserSuk != null)
            {
                return GetUserSuk.Invoke(idk, context);
            }
            else
            {
                var task = GetUserVukAsync.Invoke(idk, context);
                task.Wait();
                return task.Result;
            }
        }

        public Action<string, HttpContext> UnlockUser;
        
        public Func<string, HttpContext, Task> UnlockUserAsync;

        internal void UnlockUserInternal(string idk, HttpContext context)
        {
            if (UnlockUser != null)
            {
                UnlockUser.Invoke(idk, context);
            }
            else
            {
                UnlockUserAsync.Invoke(idk, context).Wait();
            }
        }

        public Action<string, HttpContext> LockUser { get; set; }

        public Func<string, HttpContext, Task> LockUserAsync { get; set; }

        internal void LockUserInternal(string idk, HttpContext context)
        {
            if (LockUser != null)
            {
                LockUser.Invoke(idk, context);
            }
            else
            {
                LockUserAsync.Invoke(idk, context).Wait();
            }
        }

        public Action<string, HttpContext> RemoveUser { get; set; }

        public Func<string, HttpContext, Task> RemoveUserAsync { get; set; }

        internal void RemoveUserInternal(string idk, HttpContext context)
        {
            if (RemoveUser != null)
            {
                RemoveUser.Invoke(idk, context);
            }
            else
            {
                RemoveUserAsync.Invoke(idk, context).Wait();
            }
        }

        public Action<string, bool> RemoveNut;

        public Func<string, bool, Task> RemoveNutAsync;

        internal void RemoveNutInternal(string nut, bool authorized)
        {
            if (RemoveNut != null)
            {
                RemoveNut.Invoke(nut, authorized);
            }
            else
            {
                RemoveNutAsync.Invoke(nut, authorized).Wait();
            }
        }

        public Func<string, bool, NutInfo> GetNut;

        public Func<string, bool, Task<NutInfo>> GetNutAsync;

        internal NutInfo GetNutInternal(string nut, bool authorized)
        {
            if (GetNut != null)
            {
                return GetNut.Invoke(nut, authorized);
            }
            else
            {
                var task = GetNutAsync.Invoke(nut, authorized);
                task.Wait();
                return task.Result;
            }
        }

        public Action<string, NutInfo, bool> StoreNut;

        public Func<string, NutInfo, bool, Task> StoreNutAsync;

        internal void StoreNutInternal(string nut, NutInfo info, bool authorized)
        {
            if (StoreNut != null)
            {
                StoreNut.Invoke(nut, info, authorized);
            }
            else
            {
                StoreNutAsync.Invoke(nut, info, authorized).Wait();
            }
        }

        public Func<string, bool> CheckNutAuthorized;

        public Func<string, Task<bool>> CheckNutAuthorizedAsync;

        internal bool CheckNutAuthorizedInternal(string nut)
        {
            if (CheckNutAuthorized != null)
            {
                return CheckNutAuthorized.Invoke(nut);
            }
            else
            {
                var task = CheckNutAuthorizedAsync.Invoke(nut);
                task.Wait();
                return task.Result;
            }
        }

        public Func<string, string> GetNutIdk;

        public Func<string, Task<string>> GetNutIdkAsync;

        internal string GetNutIdkInternal(string nut)
        {
            if (GetNutIdk != null)
            {
                return GetNutIdk.Invoke(nut);
            }
            else
            {
                var task = GetNutIdkAsync.Invoke(nut);
                task.Wait();
                return task.Result;
            }
        }

        public Action<string,string> StoreCpsSessionId;

        public Func<string,string, Task> StoreCpsSessionIdAsync;

        internal void StoreCpsSessionIdInternal(string code, string idk)
        {
            if (StoreCpsSessionId != null)
            {
                StoreCpsSessionId.Invoke(code, idk);
            }
            else
            {
                StoreCpsSessionIdAsync.Invoke(code, idk).Wait();
            }
        }

        public Func<string, string> GetUserIdByCpsSessionId;

        public Func<string, Task<string>> GetUserIdByCpsSessionIdAsync;

        internal string GetUserIdByCpsSessionIdInternal(string code)
        {
            if (GetUserIdByCpsSessionId != null)
            {
                return GetUserIdByCpsSessionId.Invoke(code);
            }
            else
            {
                var task = GetUserIdByCpsSessionIdAsync.Invoke(code);
                task.Wait();
                return task.Result;
            }
        }

        public Action<string> RemoveCpsSessionId;

        public Func<string, Task> RemoveCpsSessionIdAsync;

        internal void RemoveCpsSessionIdInternal(string code)
        {
            if (RemoveCpsSessionId != null)
            {
                RemoveCpsSessionId.Invoke(code);
            }
            else
            {
                RemoveCpsSessionIdAsync.Invoke(code).Wait();
            }
        }

        public Action<string> SqrlOnlyReceived;

        public Func<string, Task> SqrlOnlyReceivedAsync;

        internal void SqrlOnlyReceivedInternal(string idk)
        {
            if (SqrlOnlyReceived != null)
            {
                SqrlOnlyReceived.Invoke(idk);
            }
            else
            {
                SqrlOnlyReceivedAsync.Invoke(idk).Wait();
            }
        }

        public Action<string> HardlockReceived;

        public Func<string, Task> HardlockReceivedAsync;

        internal void HardlockReceivedInternal(string idk)
        {
            if (HardlockReceived != null)
            {
                HardlockReceived.Invoke(idk);
            }
            else
            {
                HardlockReceivedAsync.Invoke(idk).Wait();
            }
        }

        public Func<HttpRequest, string, AskMessage> GetAskQuestion;

        public Func<HttpRequest, string, Task<AskMessage>> GetAskQuestionAsync;

        internal AskMessage GetAskQuestionInternal(HttpRequest request, string nut)
        {
            if (GetAskQuestion != null)
            {
                return GetAskQuestion.Invoke(request, nut);
            }
            else
            {
                var task = GetAskQuestionAsync.Invoke(request, nut);
                task.Wait();
                return task.Result;
            }
        }

        public Func<HttpRequest, string, int, bool> ProcessAskResponse;

        public Func<HttpRequest, string, int, Task<bool>> ProcessAskResponseAsync;

        internal bool ProcessAskResponseInternal(HttpRequest request, string nut, int button)
        {
            if (ProcessAskResponse != null)
            {
                return ProcessAskResponse.Invoke(request, nut, button);
            }
            else
            {
                var task = ProcessAskResponseAsync.Invoke(request, nut, button);
                task.Wait();
                return task.Result;
            }
        }

        private static readonly Dictionary<string, NutInfo> NutList = new Dictionary<string, NutInfo>();

        private static readonly Dictionary<string, NutInfo> AuthorizedNutList = new Dictionary<string, NutInfo>();

        private void ClearOldNuts()
        {
            lock (NutList)
            {
                var oldNuts = NutList.Where(x => x.Value.CreatedDate.AddSeconds(NutExpiresInSeconds * 2) < DateTime.UtcNow).ToArray();//NutExpiresInSeconds*2 to allow the clients to work out a nut expired
                foreach (var oldNut in oldNuts)
                {
                    NutList.Remove(oldNut.Key);
                }

            }

            lock (AuthorizedNutList)
            {
                var oldAuthNuts = AuthorizedNutList.Where(x => x.Value.CreatedDate.AddSeconds(NutExpiresInSeconds * 2) < DateTime.UtcNow).ToArray();//NutExpiresInSeconds*2 to allow the clients to work out a nut expired
                foreach (var oldAuthNut in oldAuthNuts)
                {
                    AuthorizedNutList.Remove(oldAuthNut.Key);
                }
            }
        }

        private NutInfo GetNutMethod(string nut, bool authorized)
        {
            ClearOldNuts();
            if (authorized)
            {
                lock (AuthorizedNutList)
                {
                    return AuthorizedNutList.ContainsKey(nut) ? AuthorizedNutList[nut] : null;
                }
            }

            lock (NutList)
            {
                return NutList.ContainsKey(nut) ? NutList[nut] : null;
            }
        }

        private void StoreNutMethod(string nut, NutInfo info, bool authorized)
        {
            ClearOldNuts();
            if (authorized)
            {
                lock (AuthorizedNutList)
                {
                    AuthorizedNutList.Add(nut, info);
                }
            }
            else
            {
                lock (NutList)
                {
                    NutList.Add(nut, info);
                }
            }
        }

        private void RemoveNutMethod(string nut, bool authorized)
        {
            ClearOldNuts();
            if (authorized)
            {
                lock (AuthorizedNutList)
                {
                    AuthorizedNutList.Remove(nut);
                }
            }
            else
            {
                lock (NutList)
                {
                    NutList.Remove(nut);
                }
            }
        }

        private bool CheckNutAuthorizedMethod(string nut)
        {
            ClearOldNuts();
            lock (AuthorizedNutList)
            {
                return AuthorizedNutList.Any(x => x.Key == nut || x.Value.FirstNut == nut);
            }
        }

        private string GetNutIdkMethod(string nut)
        {
            ClearOldNuts();
            lock (AuthorizedNutList)
            {
                return AuthorizedNutList.Single(x => x.Key == nut || x.Value.FirstNut == nut).Value.Idk;
            }
        }
        
        private static readonly Dictionary<string, string> CpsSessions = new Dictionary<string, string>();
        
        private void StoreCpsSessionIdMethod(string sessionId, string userId)
        {
            lock (CpsSessions)
            {
                CpsSessions.Add(sessionId, userId);
            }
        }

        private string GetUserIdByCpsSessionIdMethod(string sessionId)
        {
            lock (CpsSessions)
            {
                return CpsSessions.ContainsKey(sessionId) ? CpsSessions[sessionId] : null;
            }
        }
        
        private void RemoveCpsSessionIdMethod(string sessionId)
        {
            lock (CpsSessions)
            {
                CpsSessions.Remove(sessionId);
            }
        }

        public SqrlAuthenticationOptions()
        {

            CallbackPath = "/login-sqrl";
            NutExpiresInSeconds = 60;
            CheckMillieSeconds = 1000;
            NameForAnonymous = "SQRL anonymous user";
            Diagnostics = false;

            EncryptionKey = new byte[56];
            RandomNumberGenerator.Create().GetBytes(EncryptionKey);

            Events = new RemoteAuthenticationEvents();

            RemoveNut = RemoveNutMethod;
            GetNut = GetNutMethod;
            StoreNut = StoreNutMethod;
            CheckNutAuthorized = CheckNutAuthorizedMethod;
            GetNutIdk = GetNutIdkMethod;

            StoreCpsSessionId = StoreCpsSessionIdMethod;
            GetUserIdByCpsSessionId = GetUserIdByCpsSessionIdMethod;
            RemoveCpsSessionId = RemoveCpsSessionIdMethod;

        }

        public override void Validate()
        {
            if (EncryptionKey == null || EncryptionKey.Length < 1)
            {
                throw new ArgumentException($"{nameof(EncryptionKey)} must be set with some bytes or don't override it and secure random bytes are generated.");
            }

            if (NutExpiresInSeconds < 1)
            {
                throw new ArgumentException($"{nameof(NutExpiresInSeconds)} must be grater than 0 so that a SQRL client can have a chance to communicate, we suggest a value of 60");
            }

            if (!CallbackPath.HasValue || string.IsNullOrEmpty(CallbackPath))
            {
                throw new ArgumentException($"{nameof(CallbackPath)} this should have a value");
            }

            if (!CallbackPath.Value.StartsWith("/"))
            {
                throw new ArgumentException($"{nameof(CallbackPath)} must have a '/' at the start");
            }

            if (OtherAuthenticationPaths != null)
            {
                foreach (var otherAuthenticationPath in OtherAuthenticationPaths)
                {
                    if (OtherAuthenticationPaths.Count(y => y == otherAuthenticationPath) > 1)
                    {
                        throw new ArgumentException($"{nameof(OtherAuthenticationPaths)} is entered more than once");
                    }

                    if (!otherAuthenticationPath.Path.StartsWith("/"))
                    {
                        throw new ArgumentException($"{otherAuthenticationPath} in {nameof(OtherAuthenticationPaths)} must have a '/' at the start");
                    }
                }
            }

            if (HelpersPaths != null)
            {
                foreach (var helpersPath in HelpersPaths)
                {
                    if (HelpersPaths.Count(y => y == helpersPath) > 1)
                    {
                        throw new ArgumentException($"{nameof(HelpersPaths)} is entered more than once");
                    }

                    if (!helpersPath.StartsWith("/"))
                    {
                        throw new ArgumentException($"{helpersPath} in {nameof(HelpersPaths)} must have a '/' at the start");
                    }
                }
            }

            if (UserExists == null && UserExistsAsync == null)
            {
                throw new ArgumentException($"{nameof(UserExists)} should be set so that you can validate users");
            }

            if (UserExists != null && UserExistsAsync != null)
            {
                throw new ArgumentException($"{nameof(UserExists)} and {nameof(UserExistsAsync)} are both defined you should only define one of them.");
            }

            if (UpdateUserId == null && UpdateUserIdAsync == null)
            {
                throw new ArgumentException($"{nameof(UpdateUserId)} should be set so that you can update your user id for a SQRL user");
            }

            if (UpdateUserId != null && UpdateUserIdAsync != null)
            {
                throw new ArgumentException($"{nameof(UpdateUserId)} and {nameof(UpdateUserIdAsync)} are both defined you should only define one of them.");
            }

            if (CreateUser != null && CreateUserAsync != null)
            {
                throw new ArgumentException($"{nameof(CreateUser)} and {nameof(CreateUserAsync)} are both defined you should only define one of them.");
            }

            if (GetUserVuk == null && GetUserVukAsync == null)
            {
                throw new ArgumentException($"{nameof(GetUserVuk)} should be set");
            }

            if (GetUserVuk != null && GetUserVukAsync != null)
            {
                throw new ArgumentException($"{nameof(GetUserVuk)} and {nameof(GetUserVukAsync)} are both defined you should only define one of them.");
            }

            if (UnlockUser == null && UnlockUserAsync == null)
            {
                throw new ArgumentException($"{nameof(UnlockUser)} should be set");
            }
            
            if (UnlockUser != null && UnlockUserAsync != null)
            {
                throw new ArgumentException($"{nameof(UnlockUser)} and {nameof(UnlockUserAsync)} are both defined you should only define one of them.");
            }

            if (LockUser == null && LockUserAsync == null)
            {
                throw new ArgumentException($"{nameof(LockUser)} should be set");
            }

            if (LockUser != null && LockUserAsync != null)
            {
                throw new ArgumentException($"{nameof(LockUser)} and {nameof(LockUserAsync)} are both defined you should only define one of them.");
            }
            
            if (GetUserSuk == null && GetUserSukAsync == null)
            {
                throw new ArgumentException($"{nameof(GetUserSuk)} should be set");
            }

            if (GetUserSuk != null && GetUserSukAsync != null)
            {
                throw new ArgumentException($"{nameof(GetUserSuk)} and {nameof(GetUserSukAsync)} are both defined you should only define one of them.");
            }

            if (RemoveUser == null && RemoveUserAsync == null)
            {
                throw new ArgumentException($"{nameof(RemoveUser)} should be set");
            }

            if (RemoveUser != null && RemoveUserAsync != null)
            {
                throw new ArgumentException($"{nameof(RemoveUser)} and {nameof(RemoveUserAsync)} are both defined you should only define one of them.");
            }

            if (RemoveNut != null && RemoveNutAsync != null)
            {
                throw new ArgumentException($"{nameof(RemoveNut)} and {nameof(RemoveNutAsync)} are both defined you should only define one of them.");
            }

            if (GetNut != null && GetNutAsync != null)
            {
                throw new ArgumentException($"{nameof(GetNut)} and {nameof(GetNutAsync)} are both defined you should only define one of them.");
            }

            if (StoreNut != null && StoreNutAsync != null)
            {
                throw new ArgumentException($"{nameof(StoreNut)} and {nameof(StoreNutAsync)} are both defined you should only define one of them.");
            }

            if (CheckNutAuthorized != null && CheckNutAuthorizedAsync != null)
            {
                throw new ArgumentException($"{nameof(CheckNutAuthorized)} and {nameof(CheckNutAuthorized)} are both defined you should only define one of them.");
            }

            if (GetNutIdk != null && GetNutIdkAsync != null)
            {
                throw new ArgumentException($"{nameof(GetNutIdk)} and {nameof(GetNutIdkAsync)} are both defined you should only define one of them.");
            }

            if (StoreCpsSessionId != null && StoreCpsSessionIdAsync != null)
            {
                throw new ArgumentException($"{nameof(StoreCpsSessionId)} and {nameof(StoreCpsSessionIdAsync)} are both defined you should only define one of them.");
            }

            if (GetUserIdByCpsSessionId != null && GetUserIdByCpsSessionIdAsync != null)
            {
                throw new ArgumentException($"{nameof(GetUserIdByCpsSessionId)} and {nameof(GetUserIdByCpsSessionIdAsync)} are both defined you should only define one of them.");
            }

            if (RemoveCpsSessionId != null && RemoveCpsSessionIdAsync != null)
            {
                throw new ArgumentException($"{nameof(RemoveCpsSessionId)} and {nameof(RemoveCpsSessionIdAsync)} are both defined you should only define one of them.");
            }

            if (SqrlOnlyReceived != null && SqrlOnlyReceivedAsync != null)
            {
                throw new ArgumentException($"{nameof(SqrlOnlyReceived)} and {nameof(SqrlOnlyReceivedAsync)} are both defined you should only define one of them.");
            }

            if (HardlockReceived != null && HardlockReceivedAsync != null)
            {
                throw new ArgumentException($"{nameof(HardlockReceived)} and {nameof(HardlockReceivedAsync)} are both defined you should only define one of them.");
            }

            if (GetAskQuestion != null && GetAskQuestionAsync != null)
            {
                throw new ArgumentException($"{nameof(GetAskQuestion)} and {nameof(GetAskQuestionAsync)} are both defined you should only define one of them.");
            }

            if (ProcessAskResponse != null && ProcessAskResponseAsync != null)
            {
                throw new ArgumentException($"{nameof(ProcessAskResponse)} and {nameof(ProcessAskResponseAsync)} are both defined you should only define one of them.");
            }
            
        }

        protected internal static readonly List<DiagnosticsInfo> TransactionLog = new List<DiagnosticsInfo>();

        protected internal class DiagnosticsInfo
        {

            public string RequestUrl;
            public List<string> Body;

            public List<string> ResponseBody;

        }
        
        public static void LogTransaction(HttpRequest request, string response)
        {
            var info = new DiagnosticsInfo()
            {
                RequestUrl = "[" + request.Method + "]" + request.Host + request.Path + request.QueryString,
                Body = new List<string>(),
                ResponseBody = new List<string>()
            };
            if (request.HasFormContentType)
            {
                foreach (var form in request.Form)
                {
                    if (form.Key == "client" || form.Key == "server")
                    {
                        var clientParams = Encoding.ASCII.GetString(Base64UrlTextEncoder.Decode(form.Value))
                            .Replace("\r\n", "\n")
                            .Split('\n')
                            .Where(x => x.Contains("="))
                            .ToDictionary(x => x.Split('=')[0], x => x.Remove(0,x.Split('=')[0].Length + 1));
                        
                        foreach (var param in clientParams)
                        {
                            info.Body.Add(form.Key + "." + param.Key + ": " + param.Value);
                        }
                        continue;
                    }
                    info.Body.Add(form.Key + ": " + form.Value);
                }
            }
            
            var responseValues = response
                .Replace("\r\n", "\n")
                .Split('\n')
                .Where(x => x.Contains("="))
                .ToDictionary(x => x.Split('=')[0], x => x.Remove(0,x.Split('=')[0].Length + 1));

            foreach (var responseValue in responseValues)
            {
                if (responseValue.Key == "tif")
                {
                    var intValue = int.Parse(responseValue.Value, System.Globalization.NumberStyles.HexNumber);
                    var translate = Enum.TryParse<SqrlCommandWorker.Tif>(intValue.ToString(), out var tifText);
                    if (translate)
                    {
                        info.ResponseBody.Add(responseValue.Key + ": " + responseValue.Value + " ( " + tifText.ToString("F") + " ) ");
                    }
                    else
                    {
                        info.ResponseBody.Add(responseValue.Key + ": " + responseValue.Value + " ( !!!UNKNOWN!!! ) ");
                    }
                }
                else
                {
                    info.ResponseBody.Add(responseValue.Key + ": " + responseValue.Value);
                }
            }
            
            TransactionLog.Add(info);
        }

    }
}