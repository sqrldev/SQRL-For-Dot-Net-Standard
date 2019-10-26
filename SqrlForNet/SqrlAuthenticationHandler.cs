using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SqrlForNet
{
    public class SqrlAuthenticationHandler : RemoteAuthenticationHandler<SqrlAuthenticationOptions>
    {

        internal SqrlCommandWorker CommandWorker;

        public SqrlAuthenticationHandler(
            IOptionsMonitor<SqrlAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) :
            base(options, logger, encoder, clock)
        {
            CommandWorker = new SqrlCommandWorker(Logger);
        }

        /// <summary>
        /// This is called when a Challenge is issued to this authentication schema
        /// </summary>
        /// <param name="properties">These are not used in this middleware</param>
        /// <returns>A completed task to indicate that the challenge was handled</returns>
        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Logger.LogInformation("Challenge started");
            Response.Redirect(Options.CallbackPath);
            return Task.CompletedTask;
        }

        /// <summary>
        /// This method allows the middleware to determine and report if the current request should be passed to HandleRemoteAuthenticateAsync 
        /// </summary>
        /// <returns>True the request is for the middleware. False the request is not for the middleware.</returns>
        public override Task<bool> ShouldHandleRequestAsync()
        {
            Logger.LogTrace("Started checking if request is handled");
            if (Options.EnableHelpers &&
                (
                    Options.HelpersPaths != null &&
                    Options.HelpersPaths.Any(x => Request.Path.StartsWithSegments(new PathString(x)))
                ))
            {
                Logger.LogInformation("Helpers are enabled");
                CommandWorker.Request = Request;
                CommandWorker.Response = Response;
                CommandWorker.Options = Options;
                CommandWorker.CacheHelperValues();
            }
            if (Request.Path.StartsWithSegments(new PathString(Options.CallbackPath)) ||
                (
                    Options.OtherAuthenticationPaths != null &&
                    Options.OtherAuthenticationPaths.Any(x => Request.Path.StartsWithSegments(x.Path))
                ))
            {
                Logger.LogInformation("Request will be handled my middleware");
                return Task.FromResult(true);
            }
            Logger.LogTrace("Request not handled");
            return base.ShouldHandleRequestAsync();
        }

        /// <summary>
        /// This method handles the routing of a request that the middleware is working with to the correct logic
        /// </summary>
        /// <returns>Handle which indicates that the request has been responded to or Success if the user has been logged in</returns>
        protected override Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {
            Logger.LogTrace("Started working on request");
            
            CommandWorker.Request = Request;
            CommandWorker.Response = Response;
            CommandWorker.Options = Options;//Options will probably never change but to make sure the static object is up to date lets set it

            if (Request.Query.ContainsKey("nut")) //Generally requests from a SQRL client
            {
                Logger.LogInformation("Processing nut request");
                CommandWorker.NutRequest();
            }
            else if (Request.Query.ContainsKey("check")) //Helper for initial page to check when a user has logged in
            {
                Logger.LogInformation("Processing check request");
                return CheckRequest();
            }
            else if (Request.Query.ContainsKey("cps"))
            {
                Logger.LogInformation("Processing cps request");
                return CheckCpsRequest();
            }
            else if (Request.Query.ContainsKey("diag") && Options.Diagnostics)
            {
                Logger.LogInformation("Processing diag request");
                return DiagnosticsPage();
            }
            else if (Request.Query.ContainsKey("helper"))
            {
                Logger.LogInformation("Processing helper request");
                CommandWorker.HelperJson();
            }
            else if (!Options.DisableDefaultLoginPage) //For everything else we should only return a login page
            {
                Logger.LogInformation("Processing default login page request");
                CommandWorker.QrCodePage();
            }
            
            Logger.LogTrace("Finished working on request");
            return Task.FromResult(HandleRequestResult.Handle());
        }

        private Task<HandleRequestResult> CheckRequest()
        {
            var result = CommandWorker.CheckPage();
            if (result != null)
            {
                Logger.LogTrace("User is authorized and can be logged in");
                var username = Options.GetUsernameInternal(result.Idk, Context);
                var claims = new[] {
                    new Claim(ClaimTypes.NameIdentifier, result.Idk),
                    new Claim(ClaimTypes.Name, username)
                };
                Logger.LogDebug("The userId is: {0}", result.Idk);
                Logger.LogDebug("The username is: {0}", username);

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return Task.FromResult(HandleRequestResult.Success(ticket));
            }
            return Task.FromResult(HandleRequestResult.Handle());
        }

        private Task<HandleRequestResult> CheckCpsRequest()
        {
            var result = Options.GetUserIdAndRemoveCpsSessionIdInternal(Request.Query["cps"], Context);
            if (!string.IsNullOrEmpty(result))
            {
                var username = Options.GetUsernameInternal(result, Context);

                var claims = new[] {
                    new Claim(ClaimTypes.NameIdentifier, result),
                    new Claim(ClaimTypes.Name, username)
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                
                return Task.FromResult(HandleRequestResult.Success(ticket));
            }
            //We are specifically not returning any body in the response here as they clearly don't have a valid purpose to be here
            return Task.FromResult(HandleRequestResult.Handle());
        }

        private Task<HandleRequestResult> DiagnosticsPage()
        {
            if (Request.Query["diag"] == "clear")
            {
                SqrlAuthenticationOptions.TransactionLog.Clear();
            }
            var responseMessage = new StringBuilder();
            responseMessage.AppendLine("<h1>Diagnostics</h1>");

            foreach (var log in SqrlAuthenticationOptions.TransactionLog)
            {
                responseMessage.AppendLine("<div>");
                responseMessage.AppendLine("<h2>" + log.RequestUrl + "</h2>");
                foreach (var body in log.Body)
                {
                    responseMessage.AppendLine("<p>" + body + "</p>");
                }
                responseMessage.AppendLine("<h2>Responded with</h2>");
                foreach (var body in log.ResponseBody)
                {
                    responseMessage.AppendLine("<p>" + body + "</p>");
                }
                responseMessage.AppendLine("</div>");
                responseMessage.AppendLine("<hr/>");
            }

            responseMessage.AppendLine("<a href=\"/login-sqrl?diag=clear\">Clear logs</a>");

            var responseMessageBytes = Encoding.ASCII.GetBytes(responseMessage.ToString());
            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "text/html";
            Response.ContentLength = responseMessageBytes.LongLength;
            Response.Body.WriteAsync(responseMessageBytes, 0, responseMessageBytes.Length);
            return Task.FromResult(HandleRequestResult.Handle());
        }
        
    }
}
