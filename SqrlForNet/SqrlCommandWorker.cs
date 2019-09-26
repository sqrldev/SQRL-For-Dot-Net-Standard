using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Elskom.Generic.Libs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Net.Codecrete.QrCodeGenerator;
using SqrlForNet.Chaos.NaCl;

namespace SqrlForNet
{
    internal class SqrlCommandWorker
    {
        public HttpRequest Request { private get; set; }
        public HttpResponse Response { private get; set; }
        public SqrlAuthenticationOptions Options { private get; set; }

        private enum OptKey
        {
            noiptest,
            sqrlonly,//Event driven in options
            hardlock,//Event driven in options
            cps,
            suk
        }

        [Flags]
        private enum Tif
        {
            IdMatch = 0x1,
            PreviousIdMatch = 0x2,
            IpMatch = 0x4,
            SqrlDisabled = 0x8,
            FunctionNotSupported = 0x10,
            TransientError = 0x20,
            CommandFailed = 0x40,
            ClientFailed = 0x80,
            BadId = 0x100,
            IdentitySuspended = 0x200
        }

        private enum Command
        {
            Query,
            Ident,
            Unsupported
        }

        private enum NutValidationResult
        {
            Valid,
            Expired,
            KeyMismatch,
            IpMismatch
        }

        /// <summary>
        /// This is just a list of version that are supported
        /// </summary>
        private static readonly string[] SupportedVersions = { "1" };

        private void CommandAction(Command command)
        {
            switch (command)
            {
                case Command.Query:
                    Query();
                    break;
                case Command.Ident:
                    Ident();
                    break;
                case Command.Unsupported:
                    Unsupported();
                    break;
                default:
                    Unsupported();
                    break;
            }
        }

        /// <summary>
        /// This method is used to work on nut requests
        /// </summary>
        public void NutRequest()
        {
            if (IsValidNutRequest())
            {
                NutValidationMessage(NutStatus());
            }
            else
            {
                BadCommand();
            }
            Options.RemoveNut.Invoke(Request.Query["nut"], false);
        }

        private bool IsValidNutRequest()
        {
            if (Request.Form.ContainsKey("client") &&
                Request.Form.ContainsKey("server") &&
                Request.Form.ContainsKey("ids"))
            {
                var clientInfo = Request.Form["client"];
                var serverInfo = Request.Form["server"];
                var idsInfo = Request.Form["ids"];
                if (string.IsNullOrEmpty(clientInfo) ||
                    string.IsNullOrEmpty(serverInfo) ||
                    string.IsNullOrEmpty(idsInfo))
                {
                    return false;
                }

                var clientParams = GetClientParams();
                if (!ParseVersions(clientParams["ver"]).Any(clientVersion => SupportedVersions.Contains(clientVersion)))
                {
                    return false;
                }

                return true;
            }
            return false;
        }

        private Dictionary<string, string> GetClientParams()
        {
            if (!Request.HasFormContentType)
            {
                return new Dictionary<string, string>();
            }
            return Encoding.ASCII.GetString(Base64UrlTextEncoder.Decode(Request.Form["client"]))
                .Replace("\r\n", "\n")
                .Split('\n')
                .Where(x => x.Contains("="))
                .ToDictionary(x => x.Split('=')[0], x => x.Split('=')[1]);
        }

        private NutValidationResult NutStatus()
        {
            var nut = Request.Query["nut"];
            var nutInfo = Options.GetNut.Invoke(nut, false);

            if (nutInfo == null)
            {
                return NutValidationResult.Expired;
            }

            if (nutInfo.CreatedDate.AddSeconds(Options.NutExpiresInSeconds) < DateTime.UtcNow)
            {
                return NutValidationResult.Expired;
            }

            var clientParams = GetClientParams();
            if (clientParams.ContainsKey("opt") && !ParseOpts(clientParams["opt"])[OptKey.noiptest])
            {
                if (nutInfo.IpAddress != Request.HttpContext.Connection.RemoteIpAddress.ToString())
                {
                    return NutValidationResult.IpMismatch;
                }
            }

            return NutValidationResult.Valid;
        }

        private void NutValidationMessage(NutValidationResult result)
        {
            switch (result)
            {
                case NutValidationResult.Valid:
                    ValidNutRequest();
                    break;
                case NutValidationResult.Expired:
                    TimingError();
                    break;
                case NutValidationResult.KeyMismatch:
                    BadKeys();
                    break;
                case NutValidationResult.IpMismatch:
                    BadIp();
                    break;
                default:
                    TimingError();
                    break;
            }
        }

        private void ValidNutRequest()
        {
            if (ValidateSignature())
            {
                CommandAction(GetCommand());
            }
            else
            {
                BadCommand();
            }
        }

        private bool ValidateSignature()
        {
            var message = Request.Form["client"] + Request.Form["server"];
            var ids = Base64UrlTextEncoder.Decode(Request.Form["ids"]);
            var idk = Base64UrlTextEncoder.Decode(GetClientParams()["idk"]);
            var verified = Ed25519.Verify(ids, Encoding.ASCII.GetBytes(message), idk);
            return verified;
        }

        private Command GetCommand()
        {
            return Enum.TryParse(GetClientParams()["cmd"], true, out Command command) ? command : Command.Unsupported;
        }

        private class UserLookupResults
        {
            public UserLookUpResult UserExists;
            public UserLookUpResult PrevUserExists;
        }

        private UserLookupResults LookUpUsers(Dictionary<string, string> clientParams)
        {
            var idk = clientParams["idk"];

            var userExists = Options.UserExists.Invoke(idk, Request.HttpContext);
            var prevUserExists = UserLookUpResult.Unknown;
            if (clientParams.ContainsKey("pidk"))
            {
                var pidk = clientParams["pidk"];
                prevUserExists = Options.UserExists.Invoke(pidk, Request.HttpContext);
            }
            return new UserLookupResults()
            {
                UserExists = userExists,
                PrevUserExists = prevUserExists
            };
        }

        private void Query()
        {
            var clientParams = GetClientParams();
            var userLookUp = LookUpUsers(clientParams);
            QueryHandleUserExists(userLookUp.UserExists, userLookUp.PrevUserExists);
        }

        private void QueryHandleUserExists(UserLookUpResult userExists, UserLookUpResult prevUserExists)
        {
            var tifValue = (Tif.IpMatch);
            if (userExists == UserLookUpResult.Exists)
            {
                tifValue |= Tif.IdMatch;
            }
            else if (prevUserExists == UserLookUpResult.Exists)
            {
                tifValue |= Tif.PreviousIdMatch;
            }
            else if (userExists == UserLookUpResult.Disabled)
            {
                tifValue |= Tif.IdMatch | Tif.SqrlDisabled;
            }
            else if (prevUserExists == UserLookUpResult.Disabled)
            {
                tifValue |= Tif.PreviousIdMatch | Tif.SqrlDisabled;
            }
            SendResponse(tifValue);
        }

        private void Ident()
        {
            var clientParams = GetClientParams();
            var userLookUp = LookUpUsers(clientParams);

            if (userLookUp.UserExists == UserLookUpResult.Exists)
            {
                SendResponse(Tif.IdMatch | Tif.IpMatch, !AuthorizeNut(Request.Query["nut"]));
            }
            else if (userLookUp.PrevUserExists == UserLookUpResult.Exists)
            {
                UpdateOldUser(clientParams);
            }
            else if (userLookUp.UserExists == UserLookUpResult.Disabled)
            {
                TryUnlockUser(clientParams);
            }
            else
            {
                TryCreateNewUser(clientParams);
            }
        }

        private bool AuthorizeNut(string nut)
        {
            var clientParams = GetClientParams();
            var opts = ParseOpts(clientParams["opt"]);
            if (!opts[OptKey.cps])
            {
                var nutInfo = Options.GetNut.Invoke(nut, false);
                var authNutInfo = new NutInfo
                {
                    FirstNut = nutInfo.FirstNut,
                    CreatedDate = DateTime.UtcNow.AddSeconds(Options.NutExpiresInSeconds),
                    IpAddress = nutInfo.IpAddress,
                    Idk = nutInfo.Idk
                };
                Options.StoreNut.Invoke(nut, authNutInfo, true);
                return true;
            }
            return false;
        }

        private void UpdateOldUser(Dictionary<string, string> clientParams)
        {
            if (!clientParams.ContainsKey("suk") || !clientParams.ContainsKey("vuk"))
            {
                SendResponse(Tif.IpMatch | Tif.CommandFailed | Tif.ClientFailed);
            }
            else
            {
                var idk = clientParams["idk"];
                Options.UpdateUserId.Invoke(idk, clientParams["suk"], clientParams["vuk"], clientParams["pidk"], Request.HttpContext);
                SendResponse(Tif.IdMatch | Tif.IpMatch, !AuthorizeNut(Request.Query["nut"]));
            }
        }

        private void TryUnlockUser(Dictionary<string, string> clientParams)
        {
            if (!clientParams.ContainsKey("suk") || !clientParams.ContainsKey("vuk"))
            {
                SendResponse(Tif.IpMatch | Tif.CommandFailed | Tif.ClientFailed);
            }
            var idk = clientParams["idk"];
            var usersVuk = Options.GetUserVuk.Invoke(idk, Request.HttpContext);
            if (usersVuk != clientParams["vuk"])
            {
                SendResponse(Tif.IpMatch | Tif.CommandFailed | Tif.ClientFailed);
            }
            Options.UnlockUser.Invoke(idk, Request.HttpContext);
            SendResponse(Tif.IdMatch | Tif.IpMatch, !AuthorizeNut(Request.Query["nut"]));
        }

        private void TryCreateNewUser(Dictionary<string, string> clientParams)
        {
            if (Options.CreateUser != null)
            {
                var idk = clientParams["idk"];
                Options.CreateUser.Invoke(idk, clientParams["suk"], clientParams["vuk"], Request.HttpContext);
                SendResponse(Tif.IdMatch | Tif.IpMatch, !AuthorizeNut(Request.Query["nut"]));
            }
            else
            {
                SendResponse(Tif.IpMatch | Tif.FunctionNotSupported);
            }
        }

        private void Disable()
        {

        }

        private void Enable()
        {

        }

        private void Remove()
        {

        }

        private void Unsupported()
        {
            SendResponse(Tif.IpMatch | Tif.FunctionNotSupported);
        }

        private void BadCommand()
        {
            SendResponse(Tif.FunctionNotSupported | Tif.CommandFailed | Tif.IpMatch);
        }

        private void TimingError()
        {
            SendResponse(Tif.TransientError | Tif.CommandFailed | Tif.IpMatch);
        }

        private void BadKeys()
        {
            SendResponse(Tif.CommandFailed | Tif.ClientFailed | Tif.BadId | Tif.IpMatch);
        }

        private void BadIp()
        {
            SendResponse(Tif.CommandFailed | Tif.ClientFailed | Tif.TransientError);
        }

        /// <summary>
        /// This is not a command from a SQRL client but helpful for bate of SQRL
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <param name="response">The HTTP response to write to</param>
        /// <param name="options">The options from the middleware</param>
        public void QrCodePage()
        {
            var nut = GenerateNut(Options.EncryptionKey);
            StoreNut(nut);
            var url = $"sqrl://{Request.Host}{Options.CallbackPath}?nut=" + nut;
            var checkUrl = $"{Request.Scheme}://{Request.Host}{Options.CallbackPath}?check=" + nut;
            var cancelUrl = Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes($"{Request.Scheme}://{Request.Host}{Options.CancelledPath}"));
            var responseMessageBytes = Encoding.ASCII.GetBytes(QrCodePageHtml(url, checkUrl, cancelUrl));
            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "text/html";
            Response.ContentLength = responseMessageBytes.LongLength;
            Response.Body.Write(responseMessageBytes, 0, responseMessageBytes.Length);
        }

        private string QrCodePageHtml(string url, string checkUrl, string cancelUrl)
        {
            var responseMessage = new StringBuilder();
            responseMessage.AppendLine("<!DOCTYPE html>");
            responseMessage.AppendLine("<html lang=\"en-gb\">");
            responseMessage.AppendLine("<head>");
            responseMessage.AppendLine("<script>");
            responseMessage.AppendLine("function CheckAuto() {");
            responseMessage.AppendLine($@"var xhttp = new XMLHttpRequest();
                                          xhttp.onreadystatechange = function() {{
                                            if (this.readyState == 4 && this.status == 200) {{
                                                if(this.responseText !== ""false""){{
                                                    window.location = ""{checkUrl}"";
                                                }}
                                            }}
                                          }};
                                          xhttp.open(""GET"", ""{checkUrl}"", true);
                                          xhttp.send();");
            responseMessage.AppendLine("}");
            responseMessage.AppendLine("</script>");
            responseMessage.AppendLine("</head>");
            responseMessage.AppendLine("<body onload=\"setInterval(function(){ CheckAuto(); }, " + Options.CheckMillieSeconds + ");\">");
            responseMessage.AppendLine("<h1>SQRL login page</h1>");
            responseMessage.AppendLine("<img src=\"data:image/bmp;base64," + GetBase64QrCode(url) + "\">");
            responseMessage.AppendLine($"<a href=\"{url}&can={cancelUrl}\">Sign in with SQRL</a>");
            responseMessage.AppendLine($"<a href=\"{checkUrl}\">Check manually your login here</a>");
            responseMessage.AppendLine("</body>");
            responseMessage.AppendLine("</html>");
            return responseMessage.ToString();
        }

        private string GetBase64QrCode(string url)
        {
            var qrCode = QrCode.EncodeText(url, QrCode.Ecc.High);
            var img = qrCode.ToBitmap(3, 1);
            MemoryStream stream = new MemoryStream();
            img.Save(stream, ImageFormat.Bmp);
            byte[] imageBytes = stream.ToArray();
            return Convert.ToBase64String(imageBytes);
        }

        public bool CheckPage()
        {
            var checkNut = Request.Query["check"];

            var isAuthorized = Options.CheckNutAuthorized.Invoke(checkNut);
            if (!isAuthorized)
            {
                var responseMessage = new StringBuilder();
                responseMessage.Append("false");
                var responseMessageBytes = Encoding.ASCII.GetBytes(responseMessage.ToString());
                Response.StatusCode = StatusCodes.Status200OK;
                Response.ContentType = "application/json";
                Response.ContentLength = responseMessageBytes.LongLength;
                Response.Body.Write(responseMessageBytes, 0, responseMessageBytes.Length);
            }
            return isAuthorized;
        }

        private void SendResponse(Tif tifValue, bool includeCpsUrl = false)
        {
            var responseMessageBuilder = new StringBuilder();
            var nut = GenerateNut(Options.EncryptionKey);
            StoreNut(nut);
            responseMessageBuilder.AppendLine("ver=1");
            responseMessageBuilder.AppendLine("nut=" + nut);
            responseMessageBuilder.AppendLine("tif=" + tifValue.ToString("X"));
            responseMessageBuilder.AppendLine("qry=/login-sqrl?nut=" + nut);

            if (includeCpsUrl)
            {
                responseMessageBuilder.AppendLine("url=/login-sqrl?cps=" + GenerateCpsCode());
            }

            var responseMessageBytes = Encoding.ASCII.GetBytes(Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes(responseMessageBuilder.ToString())));
            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentLength = responseMessageBytes.LongLength;
            Response.Body.Write(responseMessageBytes, 0, responseMessageBytes.Length);
        }

        private void StoreNut(string nut)
        {
            Options.StoreNut.Invoke(nut, NewNutInfo(), false);
        }

        private NutInfo NewNutInfo()
        {
            NutInfo currentNut = null;
            if (Request.Query.ContainsKey("nut"))
            {
                currentNut = Options.GetNut.Invoke(Request.Query["nut"], false);
            }
            return new NutInfo
            {
                CreatedDate = DateTime.UtcNow,
                IpAddress = currentNut != null ? currentNut?.IpAddress : Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                Idk = currentNut != null && currentNut.Idk != null ? currentNut?.Idk : Request.Query.ContainsKey("nut") ? GetClientParams()["idk"] : null,
                FirstNut = string.IsNullOrEmpty(currentNut?.FirstNut) ? Request.Query["nut"].ToString() : currentNut.FirstNut
            };
        }

        private string[] ParseVersions(string versionString)
        {
            var versionRanges = versionString.Split(',');
            var versions = new List<string>();
            foreach (var versionRange in versionRanges)
            {
                if (versionRange.Contains("-"))
                {
                    var range = versionRange.Split('-');
                    if (int.TryParse(range[0], out var firstValue) && int.TryParse(range[1], out int secondValue))
                    {
                        if (firstValue < secondValue)
                        {
                            var temp = secondValue;
                            secondValue = firstValue;
                            firstValue = temp;
                        }
                        for (var v = firstValue; v <= secondValue; v++)
                        {
                            versions.Add(v.ToString());
                        }
                    }
                }
                else
                {
                    if (int.TryParse(versionRange, out _))
                    {
                        versions.Add(versionRange);
                    }
                }
            }

            if (!versions.Any())
            {
                throw new InvalidOperationException("No valid version number was provided.");
            }

            return versions.ToArray();
        }

        private Dictionary<OptKey, bool> ParseOpts(string opts)
        {
            var options = opts.Split('~');
            return Enum.GetNames(typeof(OptKey))
                .ToDictionary(
                    supportedOption =>
                        (OptKey)Enum.Parse(typeof(OptKey), supportedOption),
                    supportedOption =>
                        options.Any(x => x.ToLower() == supportedOption));
        }
        
        /// <summary>
        /// This is used to get a good source of entropy and it the 64-bit counter
        /// </summary>
        /// <param name="counter"></param>
        /// <returns></returns>
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long counter);

        public static string GenerateNut(byte[] key)
        {
            QueryPerformanceCounter(out var counter);
            var blowFish = new BlowFish(key);

            var cipherText = blowFish.EncryptCBC(counter.ToString());
            return Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes(cipherText));
        }

        public string GenerateCpsCode()
        {
            var code = Guid.NewGuid().ToString("N");
            Options.StoreCpsSessionId.Invoke(code, GetClientParams()["idk"]);
            return code;
        }

    }
}
