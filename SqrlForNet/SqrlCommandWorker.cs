using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
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
            _logger.LogTrace("Start processing command {0}", command.ToString());
            _logger.LogInformation("{0} command sent by SQRL client", command.ToString());
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
                    _logger.LogError("Unsupported command ({0}) sent by SQRL client", command.ToString());
                    Unsupported();
                    break;
                default:
                    _logger.LogError("Unsupported command ({0}) sent by SQRL client", command.ToString());
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
            var nutInfo = Options.GetAndRemoveNutInternal(nut, Request.HttpContext);

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
                    _logger.LogInformation("Processing ASK response");
                    if (!int.TryParse(GetClientParams()["btn"], out var buttonValue))
                    {
                        _logger.LogError("BTN from SQRL client is not an integer");
                        BadCommand();
                    }
                    if (!Options.ProcessAskResponseInternal(Request, Request.Query["nut"], buttonValue))
                    {
                        _logger.LogWarning("Failed to process ASK response");
                        SendResponse(Tif.IdMatch | Tif.IpMatch);
                        return;
                    }
                    _logger.LogInformation("Processed ASK response");
                }
                CommandAction(GetCommand());
            }
            else
            {
                _logger.LogError("Failed to validate signature");
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
            _logger.LogTrace("Started to look up user");
            var idk = clientParams["idk"];

            var userExists = Options.UserExistsInternal(idk, Request.HttpContext);
            var prevUserExists = UserLookUpResult.Unknown;
            if (clientParams.ContainsKey("pidk"))
            {
                _logger.LogTrace("Using PIDK for user look up");
                var pidk = clientParams["pidk"];
                prevUserExists = Options.UserExistsInternal(pidk, Request.HttpContext);
            }
            _logger.LogDebug("User exists: {0}", userExists.ToString());
            _logger.LogDebug("Prev user exists: {0}", userExists.ToString());
            return new UserLookupResults()
            {
                UserExists = userExists,
                PrevUserExists = prevUserExists
            };
        }

        private void Query()
        {
            _logger.LogTrace("Started processing Query command");
            var clientParams = GetClientParams();
            var userLookUp = LookUpUsers(clientParams);
            QueryHandleUserExists(userLookUp.UserExists, userLookUp.PrevUserExists);
            _logger.LogTrace("Finished processing Query command");
        }

        private void QueryHandleUserExists(UserLookUpResult userExists, UserLookUpResult prevUserExists)
        {
            _logger.LogTrace("User exists {0} and Prev user exists {1}", userExists, prevUserExists);
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
            _logger.LogInformation("Responding to Query command with {0}", tifValue.ToString("F"));
            SendResponse(tifValue);
        }

        private void Ident()
        {
            _logger.LogTrace("Started processing Ident command");
            var clientParams = GetClientParams();
            var userLookUp = LookUpUsers(clientParams);

            if (userLookUp.UserExists == UserLookUpResult.Exists)
            {
                _logger.LogInformation("Responding to Ident command with valid user identified");
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
            _logger.LogTrace("Finished processing Ident command");
        }

        private bool AuthorizeNut(string nut)
        {
            var opts = ParseOpts();
            if (!opts[OptKey.cps])
            {
                var nutInfo = Options.GetAndRemoveNutInternal(nut, Request.HttpContext);
                var authNutInfo = new NutInfo
                {
                    FirstNut = nutInfo.FirstNut,
                    CreatedDate = DateTime.UtcNow.AddSeconds(Options.NutExpiresInSeconds),
                    IpAddress = nutInfo.IpAddress,
                    Idk = nutInfo.Idk
                };
                Options.StoreNutInternal(nut, authNutInfo, true, Request.HttpContext);
                return true;
            }
            return false;
        }

        private void UpdateOldUser(Dictionary<string, string> clientParams)
        {
            _logger.LogTrace("Starting to update user details");
            if (!clientParams.ContainsKey("suk") || !clientParams.ContainsKey("vuk"))
            {
                _logger.LogTrace("Missing SUK or VUK in request");
                _logger.LogDebug("Missing SUK: {0}", !clientParams.ContainsKey("suk"));
                _logger.LogDebug("Missing VUK: {0}", !clientParams.ContainsKey("vuk"));
                SendResponse(Tif.IpMatch | Tif.CommandFailed | Tif.ClientFailed);
            }
            else
            {
                var idk = clientParams["idk"];
                Options.UpdateUserIdInternal(idk, clientParams["suk"], clientParams["vuk"], clientParams["pidk"], Request.HttpContext);
                _logger.LogTrace("Updated user details and responding to client as valid identified user");
                SendResponse(Tif.IdMatch | Tif.IpMatch, !AuthorizeNut(Request.Query["nut"]));
            }
            _logger.LogTrace("Finished to update user details");
        }

        private void TryUnlockUser(Dictionary<string, string> clientParams)
        {
            _logger.LogTrace("Starting to unlock user");
            if (!Request.Form.ContainsKey("urs"))
            {
                _logger.LogError("Missing URS from unlock request");
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
                _logger.LogError("Invalid URS to unlock user.");
                _logger.LogDebug("Message to validate: {0}", Request.Form["client"] + Request.Form["server"]);
                _logger.LogDebug("VUK stored on server: {0}", Options.GetUserVukInternal(idk, Request.HttpContext));
                _logger.LogDebug("URS sent by client: {0}", Request.Form["urs"]);
                SendResponse(Tif.IdMatch | Tif.IpMatch | Tif.CommandFailed | Tif.ClientFailed);
                return;
            }

            Options.UnlockUserInternal(idk, Request.HttpContext);
            _logger.LogInformation("Unlocked user {0}", idk);
            _logger.LogTrace("Unlocked user and responding to client as valid identified user");
            SendResponse(Tif.IdMatch | Tif.IpMatch, !AuthorizeNut(Request.Query["nut"]));
        }

        private void TryRemoveUser(Dictionary<string, string> clientParams)
        {
            _logger.LogTrace("Starting to try and remove user");
            if (!Request.Form.ContainsKey("urs"))
            {
                _logger.LogError("Missing URS in request to remove user");
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
                _logger.LogError("Invalid URS to unlock user.");
                _logger.LogDebug("Message to validate: {0}", Request.Form["client"] + Request.Form["server"]);
                _logger.LogDebug("VUK stored on server: {0}", Options.GetUserVukInternal(idk, Request.HttpContext));
                _logger.LogDebug("URS sent by client: {0}", Request.Form["urs"]);
                SendResponse(Tif.IpMatch | Tif.CommandFailed | Tif.ClientFailed);
                return;
            }

            Options.RemoveUserInternal(idk, Request.HttpContext);
            _logger.LogInformation("Remove user {0}", idk);
            _logger.LogTrace("Remove user and responding to client as valid identified user");
            SendResponse(Tif.IdMatch | Tif.IpMatch, !AuthorizeNut(Request.Query["nut"]));
        }

        private void TryCreateNewUser(Dictionary<string, string> clientParams)
        {
            _logger.LogTrace("Starting to try and create a user");
            if (Options.CreateUser != null || Options.CreateUserAsync != null)
            {
                _logger.LogTrace("Creating users enabled on server");
                var idk = clientParams["idk"];
                Options.CreateUserInternal(idk, clientParams["suk"], clientParams["vuk"], Request.HttpContext);
                _logger.LogInformation("Created user {0}", idk);
                _logger.LogTrace("Created user and responding to client as valid identified user");
                SendResponse(Tif.IdMatch | Tif.IpMatch, !AuthorizeNut(Request.Query["nut"]));
            }
            else
            {
                _logger.LogTrace("Creating users is disabled by the server");
                SendResponse(Tif.IpMatch | Tif.FunctionNotSupported);
            }
        }

        private void Disable()
        {
            _logger.LogTrace("Started processing Disable command");
            var clientParams = GetClientParams();
            var userLookUp = LookUpUsers(clientParams);
            if (userLookUp.UserExists == UserLookUpResult.Exists)
            {
                var idk = clientParams["idk"];
                Options.LockUserInternal(idk, Request.HttpContext);
                _logger.LogTrace("Disabled the user {0} responding to client", idk);
                SendResponse(Tif.IdMatch | Tif.IpMatch | Tif.SqrlDisabled);
            }
            else
            {
                _logger.LogWarning("Attempted to disable a user that was {0}", userLookUp.UserExists.ToString());
                Unsupported();
            }
            _logger.LogTrace("Finished processing Disable command");
        }

        private void Enable()
        {
            _logger.LogTrace("Started processing Enable command");
            var clientParams = GetClientParams();
            var userLookUp = LookUpUsers(clientParams);
            if (userLookUp.UserExists == UserLookUpResult.Disabled)
            {
                TryUnlockUser(clientParams);
            }
            else
            {
                _logger.LogWarning("Attempted to enable a user that was {0}", userLookUp.UserExists.ToString());
                Unsupported();
            }
            _logger.LogTrace("Finished processing Enable command");
        }

        private void Remove()
        {
            _logger.LogTrace("Started processing Remove command");
            var clientParams = GetClientParams();
            var userLookUp = LookUpUsers(clientParams);
            if (userLookUp.UserExists == UserLookUpResult.Exists || userLookUp.UserExists == UserLookUpResult.Disabled)
            {
                TryRemoveUser(clientParams);
            }
            else
            {
                _logger.LogWarning("Attempted to remove a user that was {0}", userLookUp.UserExists.ToString());
                Unsupported();
            }
            _logger.LogTrace("Finished processing Remove command");
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
            responseMessage.AppendLine("<body onload=\"setInterval(function(){ CheckAuto(); }, " + Options.CheckMilliSeconds + ");\">");
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
            var redirectUrl = $"{Request.Scheme}://{Request.Host}{Options.RedirectPath}";
            var cancelUrl = Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes($"{Request.Scheme}://{Request.Host}{Options.CancelledPath}"));
            var responseMessage = new StringBuilder();
            responseMessage.Append("{");
            responseMessage.Append("\"url\":\"" + url + "\",");
            responseMessage.Append("\"checkUrl\":\"" + checkUrl + "\",");
            responseMessage.Append("\"cancelUrl\":\"" + cancelUrl + "\",");
            responseMessage.Append("\"qrCodeBase64\":\"" + GetBase64QrCode(url) + "\",");
            responseMessage.Append("\"redirectUrl\":\"" + redirectUrl + "\"");

            if (Options.OtherAuthenticationPaths != null && Options.OtherAuthenticationPaths.Any())
            {
                responseMessage.Append("\"OtherUrls\":[");

                foreach (var optionsOtherAuthenticationPath in Options.OtherAuthenticationPaths)
                {
                    var xParam = optionsOtherAuthenticationPath.AuthenticateSeparately ? "x=" + (optionsOtherAuthenticationPath.Path.Value.Length) + "&" : string.Empty;
                    var otherUrl = $"sqrl://{Request.Host}{optionsOtherAuthenticationPath.Path}?{xParam}nut={nut}";
                    var otherCheckUrl = $"{Request.Scheme}://{Request.Host}{optionsOtherAuthenticationPath.Path}?check=" + nut;
                    var otherRedirectUrl = $"{Request.Scheme}://{Request.Host}{optionsOtherAuthenticationPath.RedirectToPath}";
                    var otherCancelUrl = Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes($"{Request.Scheme}://{Request.Host}{optionsOtherAuthenticationPath.Path}"));

                    responseMessage.Append("{");
                    responseMessage.Append("\"url\":\"" + otherUrl + "\",");
                    responseMessage.Append("\"checkUrl\":\"" + otherCheckUrl + "\",");
                    responseMessage.Append("\"cancelUrl\":\"" + otherCancelUrl + "\",");
                    responseMessage.Append("\"qrCodeBase64\":\"" + GetBase64QrCode(otherUrl) + "\",");
                    responseMessage.Append("\"redirectUrl\":\"" + otherRedirectUrl + "\"");
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
            var redirectUrl = $"{Request.Scheme}://{Request.Host}{Options.RedirectPath}";
            
            _logger.LogTrace("Adding values to cache");
            _logger.LogDebug("The CallbackUrl is: {0}", url);
            _logger.LogDebug("The CheckMilliSeconds is: {0}", Options.CheckMilliSeconds);
            _logger.LogDebug("The CheckUrl is: {0}", checkUrl);
            _logger.LogDebug("The RedirectUrl is: {0}", redirectUrl);
           

            Request.HttpContext.Items.Add("CallbackUrl", url);
            Request.HttpContext.Items.Add("QrData", qrCode);
            Request.HttpContext.Items.Add("CheckMilliSeconds", Options.CheckMilliSeconds);
            Request.HttpContext.Items.Add("CheckUrl", checkUrl);
            Request.HttpContext.Items.Add("RedirectUrl", redirectUrl);
            if (Options.OtherAuthenticationPaths != null && Options.OtherAuthenticationPaths.Any())
            {
                _logger.LogTrace("There are {0}", nameof(Options.OtherAuthenticationPaths));
                var otherUrls = new List<OtherUrlsData>();
                foreach (var optionsOtherAuthenticationPath in Options.OtherAuthenticationPaths)
                {
                    _logger.LogDebug("OtherAuthenticationPath: {0}", optionsOtherAuthenticationPath.Path);
                    _logger.LogDebug("OtherAuthenticationPath is authenticate separately: {0}", optionsOtherAuthenticationPath.AuthenticateSeparately);
                    
                    var xParam = optionsOtherAuthenticationPath.AuthenticateSeparately ? "x=" + (optionsOtherAuthenticationPath.Path.Value.Length) + "&" : string.Empty;
                    var otherUrl = $"sqrl://{Request.Host}{optionsOtherAuthenticationPath.Path}?{xParam}nut={nut}";
                    var otherCheckUrl = $"{Request.Scheme}://{Request.Host}{optionsOtherAuthenticationPath.Path}?check=" + nut;
                    var otherRedirectUrl = $"{Request.Scheme}://{Request.Host}{optionsOtherAuthenticationPath.RedirectToPath}";

                    _logger.LogDebug("The CallbackUrl is: {0}", otherUrl);
                    _logger.LogDebug("The CheckUrl is: {0}", otherCheckUrl);
                    _logger.LogDebug("The RedirectUrl is: {0}", otherRedirectUrl);

                    otherUrls.Add(new OtherUrlsData()
                    {
                        Path = optionsOtherAuthenticationPath.Path,
                        RedirectUrl = otherRedirectUrl,
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
            var qrCode = QrCode.EncodeText(url, Options.GetQrCodeErrorCorrectionLevel());
            var img = qrCode.ToBitmap(Options.QrCodeScale, Options.QrCodeBorderSize);
            MemoryStream stream = new MemoryStream();
            img.Save(stream, ImageFormat.Bmp);
            return stream.ToArray();
        }
        
        public NutInfo CheckPage()
        {
            var checkNut = Request.Query["check"];

            var removedNutInfo = Options.RemoveAuthorizedNutInternal(checkNut, Request.HttpContext);
            if (removedNutInfo == null)
            {
                var responseMessage = new StringBuilder();
                responseMessage.Append("false");
                var responseMessageBytes = Encoding.ASCII.GetBytes(responseMessage.ToString());
                Response.StatusCode = StatusCodes.Status200OK;
                Response.ContentType = "application/json";
                Response.ContentLength = responseMessageBytes.LongLength;
                Response.Body.WriteAsync(responseMessageBytes, 0, responseMessageBytes.Length);
            }
            return removedNutInfo;
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
            Options.StoreNutInternal(nut, NewNutInfo(), false, Request.HttpContext);

            _logger.LogTrace("NUT stored");
        }

        private NutInfo NewNutInfo()
        {
            NutInfo currentNut = null;
            if (Request.Query.ContainsKey("nut"))
            {
                currentNut = Options.GetAndRemoveNutInternal(Request.Query["nut"], Request.HttpContext);
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
                var clientParams = GetClientParams();
                string[] options;
                if (clientParams.ContainsKey("opt"))
                {
                    options = clientParams["opt"].Split('~');
                }
                else
                {
                    options = new string[0];
                }

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
            return Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes(Guid.NewGuid().ToString("N")));
        }
        
        public string GenerateCpsCode()
        {
            var code = Guid.NewGuid().ToString("N");
            Options.StoreCpsSessionIdInternal(code, GetClientParams()["idk"], Request.HttpContext);
            return code;
        }

        private void NoneQueryOptionHandling()
        {
            if (ParseOpts()[OptKey.sqrlonly] && (Options.SqrlOnlyReceived != null || Options.SqrlOnlyReceivedAsync != null))
            {
                Options.SqrlOnlyReceivedInternal(GetClientParams()["idk"], Request.HttpContext);
            }
            if (ParseOpts()[OptKey.hardlock] && (Options.HardlockReceived != null || Options.HardlockReceivedAsync != null))
            {
                Options.HardlockReceivedInternal(GetClientParams()["idk"], Request.HttpContext);
            }
        }

        private ILogger _logger;

    }
}
