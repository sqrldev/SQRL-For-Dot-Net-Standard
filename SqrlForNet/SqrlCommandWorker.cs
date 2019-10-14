using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Elskom.Generic.Libs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
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
        internal enum Tif
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
            Disable,
            Enable,
            Remove,
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
                case Command.Disable:
                    Disable();
                    break;
                case Command.Enable:
                    Enable();
                    break;
                case Command.Remove:
                    Remove();
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
            _logger.LogTrace("Started to validate NUT request");
            if (IsValidNutRequest())
            {
                NutValidationMessage(NutStatus());
            }
            else
            {
                BadCommand();
            }
            _logger.LogTrace("Removing NUT: {0}", Request.Query["nut"]);
            Options.RemoveNutInternal(Request.Query["nut"], false);
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
                    _logger.LogTrace("NUT request invalid empty client/server/ids");
                    _logger.LogDebug("client was empty: {0}", string.IsNullOrEmpty(clientInfo));
                    _logger.LogDebug("server was empty: {0}", string.IsNullOrEmpty(serverInfo));
                    _logger.LogDebug("ids was empty: {0}", string.IsNullOrEmpty(idsInfo));
                    return false;
                }

                var clientParams = GetClientParams();
                if (!ParseVersions(clientParams["ver"]).Any(clientVersion => SupportedVersions.Contains(clientVersion)))
                {
                    _logger.LogTrace("NUT request invalid ver is not a supported version");
                    return false;
                }

                _logger.LogTrace("NUT request valid message");
                return true;
            }
            _logger.LogTrace("NUT request invalid missing client/server/ids");
            _logger.LogDebug("client was found: {0}", Request.Form.ContainsKey("client"));
            _logger.LogDebug("server was found: {0}", Request.Form.ContainsKey("server"));
            _logger.LogDebug("ids was found: {0}", Request.Form.ContainsKey("ids"));
            return false;
        }

        private Dictionary<string, string> _clientParamsCache; //This is per-request as the SqrlCommandWorker is only initialized once per request

        private Dictionary<string, string> GetClientParams()
        {
            _logger.LogTrace("Getting cached client params");
            if (_clientParamsCache == null)
            {
                if (!Request.HasFormContentType)
                {
                    return new Dictionary<string, string>();
                }
                _logger.LogTrace("No cached client params updating cache");
                _clientParamsCache = Encoding.ASCII.GetString(Base64UrlTextEncoder.Decode(Request.Form["client"]))
                    .Replace("\r\n", "\n")
                    .Split('\n')
                    .Where(x => x.Contains("="))
                    .ToDictionary(x => x.Split('=')[0], x => x.Remove(0,x.Split('=')[0].Length + 1));
                _logger.LogTrace("Client params cache updated");
            }
            _logger.LogTrace("Returning client params");
            return _clientParamsCache;
        }

        private NutValidationResult NutStatus()
        {
            _logger.LogTrace("Getting status of NUT");
            var nut = Request.Query["nut"];
            var nutInfo = Options.GetNutInternal(nut, false);

            if (nutInfo == null)
            {
                _logger.LogTrace("No NUT info found for NUT: {0}", nut);
                return NutValidationResult.Expired;
            }

            if (nutInfo.CreatedDate.AddSeconds(Options.NutExpiresInSeconds) < DateTime.UtcNow)
            {
                _logger.LogTrace("NUT has expired as of {0}", nutInfo.CreatedDate.AddSeconds(Options.NutExpiresInSeconds));
                return NutValidationResult.Expired;
            }

            var clientParams = GetClientParams();
            if (clientParams.ContainsKey("opt") && !ParseOpts()[OptKey.noiptest])
            {
                _logger.LogTrace("Testing IP address");
                if (nutInfo.IpAddress != Request.HttpContext.Connection.RemoteIpAddress.ToString())
                {
                    _logger.LogTrace("IP address failed for {0} expected {1}", Request.HttpContext.Connection.RemoteIpAddress.ToString(), nutInfo.IpAddress);
                    return NutValidationResult.IpMismatch;
                }
            }

            _logger.LogTrace("NUT is valid");
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
                if (GetClientParams().ContainsKey("btn") && (Options.ProcessAskResponse != null || Options.ProcessAskResponseAsync != null))
                {
                    if (!int.TryParse(GetClientParams()["btn"], out var buttonValue))
                    {
                        BadCommand();
                    }
                    if (!Options.ProcessAskResponseInternal(Request, Request.Query["nut"], buttonValue))
                    {
                        SendResponse(Tif.IdMatch | Tif.IpMatch);
                        return;
                    }
                }
                CommandAction(GetCommand());
            }
            else
            {
                BadCommand();
            }
        }

        private bool ValidateSignature()
        {
            _logger.LogTrace("Validating signature of message");
            var message = Request.Form["client"] + Request.Form["server"];
            var ids = Base64UrlTextEncoder.Decode(Request.Form["ids"]);
            var idk = Base64UrlTextEncoder.Decode(GetClientParams()["idk"]);
            var verified = Ed25519.Verify(ids, Encoding.ASCII.GetBytes(message), idk);
            _logger.LogTrace("Message signature is: {0}", verified);
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

            var userExists = Options.UserExistsInternal(idk, Request.HttpContext);
            var prevUserExists = UserLookUpResult.Unknown;
            if (clientParams.ContainsKey("pidk"))
            {
                var pidk = clientParams["pidk"];
                prevUserExists = Options.UserExistsInternal(pidk, Request.HttpContext);
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
            var opts = ParseOpts();
            if (!opts[OptKey.cps])
            {
                var nutInfo = Options.GetNutInternal(nut, false);
                var authNutInfo = new NutInfo
                {
                    FirstNut = nutInfo.FirstNut,
                    CreatedDate = DateTime.UtcNow.AddSeconds(Options.NutExpiresInSeconds),
                    IpAddress = nutInfo.IpAddress,
                    Idk = nutInfo.Idk
                };
                Options.StoreNutInternal(nut, authNutInfo, true);
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
                Options.UpdateUserIdInternal(idk, clientParams["suk"], clientParams["vuk"], clientParams["pidk"], Request.HttpContext);
                SendResponse(Tif.IdMatch | Tif.IpMatch, !AuthorizeNut(Request.Query["nut"]));
            }
        }

        private void TryUnlockUser(Dictionary<string, string> clientParams)
        {
            if (!Request.Form.ContainsKey("urs"))
            {
                SendResponse(Tif.IpMatch | Tif.IdMatch | Tif.CommandFailed | Tif.ClientFailed);
                return;
            }

            var idk = clientParams["idk"];

            var message = Encoding.ASCII.GetBytes(Request.Form["client"] + Request.Form["server"]);
            var usersVuk = Base64UrlTextEncoder.Decode(Options.GetUserVukInternal(idk, Request.HttpContext));
            var urs = Base64UrlTextEncoder.Decode(Request.Form["urs"]);
            var valid = Ed25519.Verify(urs, message, usersVuk);

            if (!valid)
            {
                SendResponse(Tif.IdMatch | Tif.IpMatch | Tif.CommandFailed | Tif.ClientFailed);
                return;
            }

            Options.UnlockUserInternal(idk, Request.HttpContext);
            SendResponse(Tif.IdMatch | Tif.IpMatch, !AuthorizeNut(Request.Query["nut"]));
        }

        private void TryRemoveUser(Dictionary<string, string> clientParams)
        {
            if (!Request.Form.ContainsKey("urs"))
            {
                SendResponse(Tif.IpMatch | Tif.IdMatch | Tif.CommandFailed | Tif.ClientFailed);
                return;
            }

            var idk = clientParams["idk"];

            var message = Encoding.ASCII.GetBytes(Request.Form["client"] + Request.Form["server"]);
            var usersVuk = Base64UrlTextEncoder.Decode(Options.GetUserVukInternal(idk, Request.HttpContext));
            var urs = Base64UrlTextEncoder.Decode(Request.Form["urs"]);
            var valid = Ed25519.Verify(urs, message, usersVuk);

            if (!valid)
            {
                SendResponse(Tif.IpMatch | Tif.CommandFailed | Tif.ClientFailed);
                return;
            }

            Options.RemoveUserInternal(idk, Request.HttpContext);
            SendResponse(Tif.IdMatch | Tif.IpMatch, !AuthorizeNut(Request.Query["nut"]));
        }

        private void TryCreateNewUser(Dictionary<string, string> clientParams)
        {
            if (Options.CreateUser != null || Options.CreateUserAsync != null)
            {
                var idk = clientParams["idk"];
                Options.CreateUserInternal(idk, clientParams["suk"], clientParams["vuk"], Request.HttpContext);
                SendResponse(Tif.IdMatch | Tif.IpMatch, !AuthorizeNut(Request.Query["nut"]));
            }
            else
            {
                SendResponse(Tif.IpMatch | Tif.FunctionNotSupported);
            }
        }

        private void Disable()
        {
            var clientParams = GetClientParams();
            var userLookUp = LookUpUsers(clientParams);
            if (userLookUp.UserExists == UserLookUpResult.Exists)
            {
                var idk = clientParams["idk"];
                Options.LockUserInternal(idk, Request.HttpContext);
                SendResponse(Tif.IdMatch | Tif.IpMatch | Tif.SqrlDisabled);
            }
            else
            {
                Unsupported();
            }
        }

        private void Enable()
        {
            var clientParams = GetClientParams();
            var userLookUp = LookUpUsers(clientParams);
            if (userLookUp.UserExists == UserLookUpResult.Disabled)
            {
                TryUnlockUser(clientParams);
            }
            else
            {
                Unsupported();
            }
        }

        private void Remove()
        {
            var clientParams = GetClientParams();
            var userLookUp = LookUpUsers(clientParams);
            if (userLookUp.UserExists == UserLookUpResult.Exists || userLookUp.UserExists == UserLookUpResult.Disabled)
            {
                TryRemoveUser(clientParams);
            }
            else
            {
                Unsupported();
            }
        }

        private void Unsupported()
        {
            SendResponse(Tif.IpMatch | Tif.FunctionNotSupported);
        }

        private void BadCommand()
        {
            _logger.LogInformation("Sending bad command response");
            SendResponse(Tif.FunctionNotSupported | Tif.CommandFailed | Tif.IpMatch);
        }

        private void TimingError()
        {
            _logger.LogInformation("Sending Timing error response");
            SendResponse(Tif.TransientError | Tif.CommandFailed | Tif.IpMatch);
        }

        private void BadKeys()
        {
            _logger.LogInformation("Sending bad key response");
            SendResponse(Tif.CommandFailed | Tif.ClientFailed | Tif.BadId | Tif.IpMatch);
        }

        private void BadIp()
        {
            _logger.LogInformation("Sending bad IP address response");
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
            var diagUrl = $"{Request.Scheme}://{Request.Host}{Options.CallbackPath}?diag";
            var cancelUrl = Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes($"{Request.Scheme}://{Request.Host}{Options.CancelledPath}"));
            var responseMessageBytes = Encoding.ASCII.GetBytes(QrCodePageHtml(url, checkUrl, cancelUrl, diagUrl));
            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "text/html";
            Response.ContentLength = responseMessageBytes.LongLength;
            Response.Body.WriteAsync(responseMessageBytes, 0, responseMessageBytes.Length);
        }

        private string QrCodePageHtml(string url, string checkUrl, string cancelUrl, string diagUrl)
        {
            var responseMessage = new StringBuilder();
            responseMessage.AppendLine("<!DOCTYPE html>");
            responseMessage.AppendLine("<html lang=\"en-gb\">");
            responseMessage.AppendLine("<head>");
            responseMessage.AppendLine("<script>");
            responseMessage.AppendLine("function CpsProcess(e) {");
            responseMessage.AppendLine($@"var gifProbe = new Image();
                                            gifProbe.onload = function() {{
                                                document.location.href = ""http://localhost:25519/""+ btoa(e.getAttribute(""href""));
                                            }};
                                            gifProbe.onerror = function() {{
        	                                    setTimeout( function(){{ gifProbe.src = ""http://localhost:25519/"" + Date.now() + '.gif';	}}, 250 );
                                            }};
                                            gifProbe.onerror();");
            responseMessage.AppendLine("}");
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
            responseMessage.AppendLine($"<a href=\"{url}&can={cancelUrl}\" onclick=\"CpsProcess(this);\">Sign in with SQRL</a>");
            responseMessage.AppendLine($"<a href=\"{checkUrl}\">Check manually your login here</a>");
            responseMessage.AppendLine($"<noscript>You will have to use the check manually link as scripting is turned off</noscript>");
            if (Options.Diagnostics)
            {
                responseMessage.AppendLine($"<a href=\"{diagUrl}\">Diagnostics</a>");
            }
            responseMessage.AppendLine("</body>");
            responseMessage.AppendLine("</html>");
            return responseMessage.ToString();
        }

        public void HelperJson()
        {
            var nut = GenerateNut(Options.EncryptionKey);
            StoreNut(nut);
            var url = $"sqrl://{Request.Host}{Options.CallbackPath}?nut=" + nut;
            var checkUrl = $"{Request.Scheme}://{Request.Host}{Options.CallbackPath}?check=" + nut;
            var cancelUrl = Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes($"{Request.Scheme}://{Request.Host}{Options.CancelledPath}"));
            var responseMessage = new StringBuilder();
            responseMessage.Append("{");
            responseMessage.Append("\"url\":\"" + url + "\",");
            responseMessage.Append("\"checkUrl\":\"" + checkUrl + "\",");
            responseMessage.Append("\"cancelUrl\":\"" + cancelUrl + "\",");
            responseMessage.Append("\"qrCodeBase64\":\"" + GetBase64QrCode(url) + "\"");

            if (Options.OtherAuthenticationPaths != null && Options.OtherAuthenticationPaths.Any())
            {
                responseMessage.Append("\"OtherUrls\":[");

                foreach (var optionsOtherAuthenticationPath in Options.OtherAuthenticationPaths)
                {
                    var xParam = optionsOtherAuthenticationPath.AuthenticateSeparately ? "x=" + (optionsOtherAuthenticationPath.Path.Length) + "&" : string.Empty;
                    var otherUrl = $"sqrl://{Request.Host}{optionsOtherAuthenticationPath.Path}?{xParam}nut={nut}";
                    var otherCheckUrl = $"{Request.Scheme}://{Request.Host}{optionsOtherAuthenticationPath.Path}?check=" + nut;
                    var otherCancelUrl = Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes($"{Request.Scheme}://{Request.Host}{optionsOtherAuthenticationPath.Path}"));

                    responseMessage.Append("{");
                    responseMessage.Append("\"url\":\"" + otherUrl + "\",");
                    responseMessage.Append("\"checkUrl\":\"" + otherCheckUrl + "\",");
                    responseMessage.Append("\"cancelUrl\":\"" + otherCancelUrl + "\",");
                    responseMessage.Append("\"qrCodeBase64\":\"" + GetBase64QrCode(otherUrl) + "\"");
                    responseMessage.Append("}");
                }

                responseMessage.Append("]");
            }

            responseMessage.Append("}");
            var responseMessageBytes = Encoding.ASCII.GetBytes(responseMessage.ToString());
            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "application/json";
            Response.ContentLength = responseMessageBytes.LongLength;
            Response.Body.WriteAsync(responseMessageBytes, 0, responseMessageBytes.Length);
        }

        internal void CacheHelperValues()
        {
            _logger.LogTrace("Updating helpers cache");
            var nut = GenerateNut(Options.EncryptionKey);
            StoreNut(nut);
            var url = $"sqrl://{Request.Host}{Options.CallbackPath}?nut=" + nut;
            var qrCode = GetBase64QrCode(url);
            var checkUrl = $"{Request.Scheme}://{Request.Host}{Options.CallbackPath}?check=" + nut;
            
            _logger.LogTrace("Adding values to cache");
            _logger.LogDebug("The CallbackUrl is: {0}", url);
            _logger.LogDebug("The CheckMillieSeconds is: {0}", Options.CheckMillieSeconds);
            _logger.LogDebug("The CheckUrl is: {0}", checkUrl);
           

            Request.HttpContext.Items.Add("CallbackUrl", url);
            Request.HttpContext.Items.Add("QrData", qrCode);
            Request.HttpContext.Items.Add("CheckMillieSeconds", Options.CheckMillieSeconds);
            Request.HttpContext.Items.Add("CheckUrl", checkUrl);
            if (Options.OtherAuthenticationPaths != null && Options.OtherAuthenticationPaths.Any())
            {
                _logger.LogTrace("There are {0}", nameof(Options.OtherAuthenticationPaths));
                var otherUrls = new List<OtherUrlsData>();
                foreach (var optionsOtherAuthenticationPath in Options.OtherAuthenticationPaths)
                {
                    _logger.LogDebug("OtherAuthenticationPath: {0}", optionsOtherAuthenticationPath.Path);
                    _logger.LogDebug("OtherAuthenticationPath is authenticate separately: {0}", optionsOtherAuthenticationPath.AuthenticateSeparately);
                    
                    var xParam = optionsOtherAuthenticationPath.AuthenticateSeparately ? "x=" + (optionsOtherAuthenticationPath.Path.Length) + "&" : string.Empty;
                    var otherUrl = $"sqrl://{Request.Host}{optionsOtherAuthenticationPath.Path}?{xParam}nut={nut}";
                    var otherCheckUrl = $"{Request.Scheme}://{Request.Host}{optionsOtherAuthenticationPath.Path}?check=" + nut;

                    _logger.LogDebug("The CallbackUrl is: {0}", otherUrl);
                    _logger.LogDebug("The CheckUrl is: {0}", otherCheckUrl);

                    otherUrls.Add(new OtherUrlsData()
                    {
                        Path = optionsOtherAuthenticationPath.Path,
                        Url = otherUrl,
                        CheckUrl = otherCheckUrl,
                        QrCodeBase64 = GetBase64QrCode(otherUrl)
                    });
                }
                Request.HttpContext.Items.Add("OtherUrls", otherUrls);
            }
            _logger.LogTrace("Added values to cache");
        }

        private string GetBase64QrCode(string url)
        {
            return Convert.ToBase64String(GetBase64QrCodeData(url));
        }

        private byte[] GetBase64QrCodeData(string url)
        {
            var qrCode = QrCode.EncodeText(url, QrCode.Ecc.High);
            var img = qrCode.ToBitmap(3, 1);
            MemoryStream stream = new MemoryStream();
            img.Save(stream, ImageFormat.Bmp);
            return stream.ToArray();
        }
        
        public bool CheckPage()
        {
            var checkNut = Request.Query["check"];

            var isAuthorized = Options.CheckNutAuthorizedInternal(checkNut);
            if (!isAuthorized)
            {
                var responseMessage = new StringBuilder();
                responseMessage.Append("false");
                var responseMessageBytes = Encoding.ASCII.GetBytes(responseMessage.ToString());
                Response.StatusCode = StatusCodes.Status200OK;
                Response.ContentType = "application/json";
                Response.ContentLength = responseMessageBytes.LongLength;
                Response.Body.WriteAsync(responseMessageBytes, 0, responseMessageBytes.Length);
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
            responseMessageBuilder.AppendLine("qry=" + Request.Path + "?nut=" + nut);

            if (includeCpsUrl)
            {
                responseMessageBuilder.AppendLine("url=" + Request.Scheme + "://" + Request.Host + Request.Path + "?cps=" + GenerateCpsCode());
            }

            if ((tifValue.HasFlag(Tif.IdMatch) || tifValue.HasFlag(Tif.PreviousIdMatch) || tifValue.HasFlag(Tif.SqrlDisabled)))
            {
                var idk = GetClientParams()["idk"];
                var suk = Options.GetUserSukInternal(idk, Request.HttpContext);
                responseMessageBuilder.AppendLine("suk=" + Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes(suk)));
            }

            if (!(tifValue.HasFlag(Tif.TransientError) || tifValue.HasFlag(Tif.BadId) ||
                tifValue.HasFlag(Tif.ClientFailed) || tifValue.HasFlag(Tif.CommandFailed) ||
                tifValue.HasFlag(Tif.FunctionNotSupported)) && GetCommand() != Command.Query)
            {
                NoneQueryOptionHandling();
            }

            if (GetCommand() == Command.Query && tifValue.HasFlag(Tif.IdMatch) && (Options.GetAskQuestion != null || Options.GetAskQuestionAsync != null))
            {
                var message = Options.GetAskQuestionInternal(Request, nut);
                if (message != null)
                {
                    responseMessageBuilder.AppendLine($"ask={message.ToAskMessage()}");
                }
            }

            var responseMessageBytes = Encoding.ASCII.GetBytes(Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes(responseMessageBuilder.ToString())));
            Response.ContentType = "application/x-www-form-urlencoded";
            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentLength = responseMessageBytes.LongLength;
            Response.Body.WriteAsync(responseMessageBytes, 0, responseMessageBytes.Length);

            if (Options.Diagnostics)
            {
                SqrlAuthenticationOptions.LogTransaction(Request, responseMessageBuilder.ToString());
            }

        }

        private void StoreNut(string nut)
        {
            _logger.LogTrace("Storing NUT");

            _logger.LogDebug("The NUT been stored is: {0}", nut);
            Options.StoreNutInternal(nut, NewNutInfo(), false);

            _logger.LogTrace("NUT stored");
        }

        private NutInfo NewNutInfo()
        {
            NutInfo currentNut = null;
            if (Request.Query.ContainsKey("nut"))
            {
                currentNut = Options.GetNutInternal(Request.Query["nut"], false);
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

        private Dictionary<OptKey, bool> _optionsCache;

        public SqrlCommandWorker(ILogger logger)
        {
            _logger = logger;
        }

        private Dictionary<OptKey, bool> ParseOpts()
        {
            if (_optionsCache == null)
            {
                var options = GetClientParams()["opt"].Split('~');
                _optionsCache = Enum.GetNames(typeof(OptKey))
                    .ToDictionary(
                        supportedOption =>
                            (OptKey) Enum.Parse(typeof(OptKey), supportedOption),
                        supportedOption =>
                            options.Any(x => x.ToLower() == supportedOption));
            }
            return _optionsCache;
        }
        
        private string GenerateNut(byte[] key)
        {
            _logger.LogTrace("Generating a NUT");
            var now = DateTime.UtcNow;
            var counter = now.Day.ToString("00") + now.Month.ToString("00") + now.Year.ToString() + now.Ticks.ToString();//This is always be unique as day month and year will always go up and ticks will be unique on the day
            _logger.LogDebug("Counter is currently {0}", counter);
            var blowFish = new BlowFish(key);

            var cipherText = blowFish.EncryptCBC(counter.ToString());
            return Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes(cipherText));
        }

        public string GenerateCpsCode()
        {
            var code = Guid.NewGuid().ToString("N");
            Options.StoreCpsSessionIdInternal(code, GetClientParams()["idk"]);
            return code;
        }

        private void NoneQueryOptionHandling()
        {
            if (GetClientParams().ContainsKey("opt") && ParseOpts()[OptKey.sqrlonly] && (Options.SqrlOnlyReceived != null || Options.SqrlOnlyReceivedAsync != null))
            {
                Options.SqrlOnlyReceivedInternal(GetClientParams()["idk"], Request.HttpContext);
            }
            if (GetClientParams().ContainsKey("opt") && ParseOpts()[OptKey.hardlock] && (Options.HardlockReceived != null || Options.HardlockReceivedAsync != null))
            {
                Options.HardlockReceivedInternal(GetClientParams()["idk"], Request.HttpContext);
            }
        }

        private ILogger _logger;

    }
}
