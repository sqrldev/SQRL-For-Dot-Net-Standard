using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Net.Codecrete.QrCodeGenerator;

namespace SqrlForNet
{
    public class SqrlAuthenticationOptions : RemoteAuthenticationOptions
    {

        public byte[] EncryptionKey { get; set; }

        public int NutExpiresInSeconds { get; set; }

        public int CheckMilliSeconds { get; set; }

        public string NameForAnonymous { get; set; }

        public string CancelledPath { get; set; }

        public PathString RedirectPath { get; set; }
        
        public bool Diagnostics { get; set; }
        
        public bool DisableDefaultLoginPage { get; set; }
        
        public bool EnableHelpers { get; set; }

        public PathString[] HelpersPaths { get; set; }
        
        public int QrCodeBorderSize { get; set; }
        
        public int QrCodeScale { get; set; }
        
        public EccLevel QrCodeErrorCorrectionLevel { get; set; }

        internal QrCode.Ecc GetQrCodeErrorCorrectionLevel()
        {
            switch (QrCodeErrorCorrectionLevel)
            {
                case EccLevel.Low: return QrCode.Ecc.Low;
                case EccLevel.Medium: return QrCode.Ecc.Medium;
                case EccLevel.Quartile: return QrCode.Ecc.Quartile;
                case EccLevel.High: return QrCode.Ecc.High;
                default: throw new ArgumentOutOfRangeException();
            }
        }
        
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
                var task = GetUserSukAsync.Invoke(idk, context);
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

        public Func<string, HttpContext, NutInfo> GetAndRemoveNut;

        public Func<string, HttpContext, Task<NutInfo>> GetAndRemoveNutAsync;

        private KeyValuePair<string, NutInfo> _currentNutInfo;

        internal NutInfo GetAndRemoveNutInternal(string nut, HttpContext context)
        {
            if (_currentNutInfo.Key != nut)
            {
                if (GetAndRemoveNut != null)
                {
                    _currentNutInfo = new KeyValuePair<string, NutInfo>(nut, GetAndRemoveNut.Invoke(nut, context));
                }
                else if (GetAndRemoveNutAsync != null)
                {
                    var task = GetAndRemoveNutAsync.Invoke(nut, context);
                    task.Wait();
                    _currentNutInfo = new KeyValuePair<string, NutInfo>(nut, task.Result);
                }
                else
                {
                    _currentNutInfo = new KeyValuePair<string, NutInfo>(nut, GetAndRemoveNutMethod(nut, context));
                }
            }

            return _currentNutInfo.Value;
        }

        public Action<string, NutInfo, bool, HttpContext> StoreNut;

        public Func<string, NutInfo, bool, HttpContext, Task> StoreNutAsync;

        internal void StoreNutInternal(string nut, NutInfo info, bool authorized, HttpContext context)
        {
            if (StoreNut != null)
            {
                StoreNut.Invoke(nut, info, authorized, context);
            }
            else if (StoreNutAsync != null)
            {
                StoreNutAsync.Invoke(nut, info, authorized, context).Wait();
            }
            else
            {
                StoreNutMethod(nut, info, authorized, context);
            }
        }

        public Func<string, HttpContext, NutInfo> RemoveAuthorizedNut;

        public Func<string, HttpContext, Task<NutInfo>> RemoveAuthorizedNutAsync;

        internal NutInfo RemoveAuthorizedNutInternal(string nut, HttpContext context)
        {
            if (RemoveAuthorizedNut != null)
            {
                return RemoveAuthorizedNut.Invoke(nut, context);
            }
            else if (RemoveAuthorizedNutAsync != null)
            {
                var task = RemoveAuthorizedNutAsync.Invoke(nut, context);
                task.Wait();
                return task.Result;
            }
            else
            {
                return RemoveAuthorizedNutMethod(nut, context);
            }
        }

        public Action<string, string, HttpContext> StoreCpsSessionId;

        public Func<string,string, HttpContext, Task> StoreCpsSessionIdAsync;

        internal void StoreCpsSessionIdInternal(string code, string idk, HttpContext context)
        {
            if (StoreCpsSessionId != null)
            {
                StoreCpsSessionId.Invoke(code, idk, context);
            }
            else if (StoreCpsSessionIdAsync != null)
            {
                StoreCpsSessionIdAsync.Invoke(code, idk, context).Wait();
            }
            else
            {
                StoreCpsSessionIdMethod(code, idk, context);
            }
        }

        public Func<string, HttpContext, string> GetUserIdAndRemoveCpsSessionId;

        public Func<string, HttpContext, Task<string>> GetUserIdAndRemoveCpsSessionIdAsync;

        internal string GetUserIdAndRemoveCpsSessionIdInternal(string code, HttpContext context)
        {
            if (GetUserIdAndRemoveCpsSessionId != null)
            {
                return GetUserIdAndRemoveCpsSessionId.Invoke(code, context);
            }
            else if (GetUserIdAndRemoveCpsSessionIdAsync != null)
            {
                var task = GetUserIdAndRemoveCpsSessionIdAsync.Invoke(code, context);
                task.Wait();
                return task.Result;
            }
            else
            {
                return GetUserIdAndRemoveCpsSessionIdMethod(code, context);
            }
        }

        public Action<string, HttpContext> SqrlOnlyReceived;

        public Func<string, HttpContext, Task> SqrlOnlyReceivedAsync;

        internal void SqrlOnlyReceivedInternal(string idk, HttpContext context)
        {
            if (SqrlOnlyReceived != null)
            {
                SqrlOnlyReceived.Invoke(idk, context);
            }
            else
            {
                SqrlOnlyReceivedAsync.Invoke(idk, context).Wait();
            }
        }

        public Action<string, HttpContext> HardlockReceived;

        public Func<string, HttpContext, Task> HardlockReceivedAsync;

        internal void HardlockReceivedInternal(string idk, HttpContext context)
        {
            if (HardlockReceived != null)
            {
                HardlockReceived.Invoke(idk, context);
            }
            else
            {
                HardlockReceivedAsync.Invoke(idk, context).Wait();
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

        public Func<string, HttpContext, string> GetUsername;

        public Func<string, HttpContext, Task<string>> GetUsernameAsync;

        internal string GetUsernameInternal(string userId, HttpContext context)
        {
            if (GetUsername != null)
            {
                return GetUsername.Invoke(userId, context);
            }
            else if (GetUsernameAsync != null)
            {
                var task = GetUsernameAsync.Invoke(userId, context);
                task.Wait();
                return task.Result;
            }
            else
            {
                return NameForAnonymous;
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

        private NutInfo GetAndRemoveNutMethod(string nut, HttpContext httpContext)
        {
            ClearOldNuts();
            lock (NutList)
            {
                if (NutList.ContainsKey(nut))
                {
                    var info = NutList[nut];
                    NutList.Remove(nut);
                    return info;
                }
            }
            return null;
        }

        private void StoreNutMethod(string nut, NutInfo info, bool authorized, HttpContext arg4)
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

        private NutInfo RemoveAuthorizedNutMethod(string nut, HttpContext httpContext)
        {
            ClearOldNuts();
            lock (AuthorizedNutList)
            {
                var authorizedNut = AuthorizedNutList.SingleOrDefault(x => x.Key == nut || x.Value.FirstNut == nut);
                if (authorizedNut.Key == nut)
                {
                    AuthorizedNutList.Remove(authorizedNut.Key);
                    return authorizedNut.Value;
                }
                return authorizedNut.Value;
            }
        }

        private string GetNutIdkMethod(string nut, HttpContext httpContext)
        {
            ClearOldNuts();
            lock (AuthorizedNutList)
            {
                return AuthorizedNutList.Single(x => x.Key == nut || x.Value.FirstNut == nut).Value.Idk;
            }
        }
        
        private static readonly Dictionary<string, string> CpsSessions = new Dictionary<string, string>();
        
        private void StoreCpsSessionIdMethod(string sessionId, string userId, HttpContext context)
        {
            lock (CpsSessions)
            {
                CpsSessions.Add(sessionId, userId);
            }
        }

        private string GetUserIdAndRemoveCpsSessionIdMethod(string sessionId, HttpContext context)
        {
            lock (CpsSessions)
            {
                if (CpsSessions.ContainsKey(sessionId))
                {
                    return CpsSessions[sessionId];
                }
            }
            return null;
        }

        public SqrlAuthenticationOptions()
        {

            CallbackPath = "/login-sqrl";
            NutExpiresInSeconds = 60;
            CheckMilliSeconds = 1000;
            QrCodeBorderSize = 1;
            QrCodeScale = 3;
            QrCodeErrorCorrectionLevel = EccLevel.Low;
            NameForAnonymous = "SQRL anonymous user";
            Diagnostics = false;

            EncryptionKey = new byte[8];
            RandomNumberGenerator.Create().GetBytes(EncryptionKey);

            Events = new RemoteAuthenticationEvents();

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

            if (QrCodeBorderSize < 1)
            {
                throw new ArgumentException($"{nameof(QrCodeBorderSize)} must be 1 or higher.");
            }

            if (QrCodeScale < 1)
            {
                throw new ArgumentException($"{nameof(QrCodeScale)} must be 1 or higher.");
            }
            
            if (!CallbackPath.HasValue || string.IsNullOrEmpty(CallbackPath))
            {
                throw new ArgumentException($"The {nameof(CallbackPath)} should have a value");
            }

            if (OtherAuthenticationPaths != null)
            {
                var errorList = new List<string>();
                foreach (var otherAuthenticationPath in OtherAuthenticationPaths)
                {
                    if (!otherAuthenticationPath.Path.HasValue)
                    {
                        throw new ArgumentException($"One of the {nameof(OtherAuthenticationPath)} needs a path defining as it currently doesn't have one");
                    }
                    
                    if (OtherAuthenticationPaths.Count(y => y.Path == otherAuthenticationPath.Path) > 1)
                    {
                        errorList.Add($"{otherAuthenticationPath.Path} is entered more than once in {nameof(OtherAuthenticationPaths)}\r\n");
                    }
                    
                }

                if (errorList.Any())
                {
                    throw new ArgumentException(errorList.Aggregate((current, next) => current + next));
                }
            }

            if (EnableHelpers && HelpersPaths == null)
            {
                throw new ArgumentException($"{nameof(HelpersPaths)} must have at least one path when {nameof(EnableHelpers)} is true.");
            }
            
            if (HelpersPaths != null)
            {
                foreach (var helpersPath in HelpersPaths)
                {
                    if (HelpersPaths.Count(y => y == helpersPath.Value) > 1)
                    {
                        throw new ArgumentException($"{helpersPath.Value} is entered more than once in {nameof(HelpersPaths)}");
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

            if (GetUsername != null && GetUsernameAsync != null)
            {
                throw new ArgumentException($"{nameof(GetUsername)} and {nameof(GetUsernameAsync)} are both defined you should only define one of them.");
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