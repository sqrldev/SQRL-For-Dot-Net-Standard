using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

        /// <summary>
        /// This is the function that is called with the UserId so that the app can look up the user
        /// </summary>
        public Func<string, HttpContext, UserLookUpResult> UserExists;

        /// <summary>
        /// This is the function that is called when a users id for the website has be validated to have been changed
        /// This is a valid case for SQRL and helpful as it keeps the users security clean if they loose trust in there private key
        /// Param 1 - New user id
        /// Param 2 - New Suk
        /// Param 3 - New Vuk
        /// Param 4 - Old user id
        /// </summary>
        public Action<string, string, string, string, HttpContext> UpdateUserId;

        /// <summary>
        /// This is the function that is called when a user logs in for the first time and is allowed to be created
        /// Param 1 - UserId
        /// Param 2 - Suk
        /// Param 3 - Vuk
        /// Set to be Null to not allow creation of users
        /// </summary>
        public Action<string, string, string, HttpContext> CreateUser;

        public Func<string, HttpContext, string> GetUserVuk;

        public Func<string, HttpContext, string> GetUserSuk;

        public Action<string, HttpContext> UnlockUser;

        public Action<string, HttpContext> LockUser { get; set; }

        public Action<string,HttpContext> RemoveUser { get; set; }

        public Action<string, bool> RemoveNut;

        public Func<string, bool, NutInfo> GetNut;

        public Action<string, NutInfo, bool> StoreNut;

        public Func<string, bool> CheckNutAuthorized;

        public Func<string, string> GetNutIdk;

        /// <summary>
        /// Store the CPS session
        /// Param 1 - Session Id
        /// Param 2 - UserId
        /// </summary>
        public Action<string,string> StoreCpsSessionId;

        public Func<string, string> GetUserIdByCpsSessionId;

        public Action<string> RemoveCpsSessionId;

        /// <summary>
        /// Used to store the nuts
        /// </summary>
        private static readonly Dictionary<string, NutInfo> NutList = new Dictionary<string, NutInfo>();

        /// <summary>
        /// Used to store nuts that have been Authorized, tasty nuts
        /// </summary>
        private static readonly Dictionary<string, NutInfo> AuthorizedNutList = new Dictionary<string, NutInfo>();

        private NutInfo GetNutMethod(string nut, bool authorized)
        {
            if (authorized)
            {
                return AuthorizedNutList.ContainsKey(nut) ? AuthorizedNutList[nut] : null;
            }
            return NutList.ContainsKey(nut) ? NutList[nut] : null;
        }

        private void StoreNutMethod(string nut, NutInfo info, bool authorized)
        {
            if (authorized)
            {
                AuthorizedNutList.Add(nut, info);
            }
            else
            {
                NutList.Add(nut, info);
            }
        }

        private void RemoveNutMethod(string nut, bool authorized)
        {
            if (authorized)
            {
                AuthorizedNutList.Remove(nut);
            }
            else
            {
                NutList.Remove(nut);
            }
        }

        private bool CheckNutAuthorizedMethod(string nut)
        {
            return AuthorizedNutList.Any(x => x.Key == nut || x.Value.FirstNut == nut);
        }

        private string GetNutIdkMethod(string nut)
        {
            return AuthorizedNutList.Single(x => x.Key == nut || x.Value.FirstNut == nut).Value.Idk;
        }
        
        private static readonly Dictionary<string, string> CpsSessions = new Dictionary<string, string>();
        
        private void StoreCpsSessionIdMethod(string sessionId, string userId)
        {
            CpsSessions.Add(sessionId, userId);
        }

        private string GetUserIdByCpsSessionIdMethod(string sessionId)
        {
            return CpsSessions.ContainsKey(sessionId) ? CpsSessions[sessionId] : null;
        }
        
        private void RemoveCpsSessionIdMethod(string sessionId)
        {
            CpsSessions.Remove(sessionId);
        }

        public SqrlAuthenticationOptions()
        {

            CallbackPath = "/login-sqrl";
            NutExpiresInSeconds = 60;
            CheckMillieSeconds = 1000;
            NameForAnonymous = "SQRL anonymous user";

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

            if (!CallbackPath.HasValue || String.IsNullOrEmpty(CallbackPath))
            {
                throw new ArgumentException($"{nameof(CallbackPath)} this should have a value");
            }

            if (!CallbackPath.Value.StartsWith("/"))
            {
                throw new ArgumentException($"{nameof(CallbackPath)} must have a '/' at the start");
            }

            if (UserExists == null)
            {
                throw new ArgumentException($"{nameof(UserExists)} should be set so that you can validate users");
            }

            if (UpdateUserId == null)
            {
                throw new ArgumentException($"{nameof(UpdateUserId)} should be set so that you can update your user id for a SQRL user");
            }

            if (GetUserVuk == null)
            {
                throw new ArgumentException($"{nameof(GetUserVuk)} should be set");
            }

            if (UnlockUser == null)
            {
                throw new ArgumentException($"{nameof(UnlockUser)} should be set");
            }

            if (LockUser == null)
            {
                throw new ArgumentException($"{nameof(LockUser)} should be set");
            }

            if (GetUserVuk == null)
            {
                throw new ArgumentException($"{nameof(GetUserVuk)} should be set");
            }

            if (RemoveUser == null)
            {
                throw new ArgumentException($"{nameof(GetUserVuk)} should be set");
            }

        }
    }
}