using System;
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
            CommandWorker = new SqrlCommandWorker();
        }

        /// <summary>
        /// This is called when a Challenge is issued to this authentication schema
        /// </summary>
        /// <param name="properties">These are not used in this middleware</param>
        /// <returns>A completed task to indicate that the challenge was handled</returns>
        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Redirect(Options.CallbackPath);
            return Task.CompletedTask;
        }

        /// <summary>
        /// This method allows the middleware to determine and report if the current request should be passed to HandleRemoteAuthenticateAsync 
        /// </summary>
        /// <returns>True the request is for the middleware. False the request is not for the middleware.</returns>
        public override Task<bool> ShouldHandleRequestAsync()
        {
            if (Request.Path.StartsWithSegments(new PathString(Options.CallbackPath)))
            {
                return Task.FromResult(true);
            }
            return base.ShouldHandleRequestAsync();
        }

        /// <summary>
        /// This method handles the routing of a request that the middleware is working with to the correct logic
        /// </summary>
        /// <returns>Handle which indicates that the request has been responded to or Success if the user has been logged in</returns>
        protected override Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {
            CommandWorker.Request = Request;
            CommandWorker.Response = Response;
            CommandWorker.Options = Options;//Options will probably never change but to make sure the static object is up to date lets set it

            if (Request.Query.ContainsKey("nut")) //Generally requests from a SQRL client
            {
                CommandWorker.NutRequest();
            }
            else if (Request.Query.ContainsKey("check")) //Helper for initial page to check when a user has logged in
            {
                return CheckRequest();
            }
            else if (Request.Query.ContainsKey("cps"))
            {
                return CheckCpsRequest();
            }
            else if (Request.Query.ContainsKey("diag") && Options.Diagnostics)
            {
                return DiagnosticsPage();
            }
            else if (Request.Query.ContainsKey("Helper"))
            {
                CommandWorker.HelperHtml();
            }
            else if (!Options.DisableDefaultLoginPage) //For everything else we should only return a login page
            {
                CommandWorker.QrCodePage();
            }
            
            return Task.FromResult(HandleRequestResult.Handle());
        }

        private Task<HandleRequestResult> CheckRequest()
        {
            var result = CommandWorker.CheckPage();
            if (result)
            {
                var claims = new[] {
                    new Claim(ClaimTypes.NameIdentifier, Options.GetNutIdk.Invoke(Request.Query["check"])),
                    new Claim(ClaimTypes.Name, Options.NameForAnonymous)
                };

                Options.RemoveNut.Invoke(Request.Query["check"], true);

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                Options.Events.TicketReceived(new TicketReceivedContext(Context, Scheme, Options, ticket));

                return Task.FromResult(HandleRequestResult.Success(ticket));
            }
            return Task.FromResult(HandleRequestResult.Handle());
        }

        private Task<HandleRequestResult> CheckCpsRequest()
        {
            var result = Options.GetUserIdByCpsSessionId.Invoke(Request.Query["cps"]);
            if (!string.IsNullOrEmpty(result))
            {
                var claims = new[] {
                    new Claim(ClaimTypes.NameIdentifier, result),
                    new Claim(ClaimTypes.Name, Options.NameForAnonymous)
                };

                Options.RemoveCpsSessionId.Invoke(Request.Query["cps"]);

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                Options.Events.TicketReceived(new TicketReceivedContext(Context, Scheme, Options, ticket));

                return Task.FromResult(HandleRequestResult.Success(ticket));
            }
            //We are specifically not returning any body in the response here as they clearly don't have a valid purpose to be here
            return Task.FromResult(HandleRequestResult.Handle());
        }

        private Task<HandleRequestResult> DiagnosticsPage()
        {
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
            
            var responseMessageBytes = Encoding.ASCII.GetBytes(responseMessage.ToString());
            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "text/html";
            Response.ContentLength = responseMessageBytes.LongLength;
            Response.Body.Write(responseMessageBytes, 0, responseMessageBytes.Length);
            return Task.FromResult(HandleRequestResult.Handle());
        }
        
    }
}
