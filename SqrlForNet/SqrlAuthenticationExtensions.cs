using System;
using Microsoft.AspNetCore.Authentication;

namespace SqrlForNet
{
    public static class SqrlAuthenticationExtensions
    {
        public static AuthenticationBuilder AddSqrl(this AuthenticationBuilder builder, Action<SqrlAuthenticationOptions>? options = null)
        {
            return AddSqrl(builder, "SQRL", options);
        }

        public static AuthenticationBuilder AddSqrl(this AuthenticationBuilder builder, string authenticationScheme, Action<SqrlAuthenticationOptions>? options = null)
        {
            return AddSqrl(builder, authenticationScheme, "Secure Quick Reliable Login", options);
        }

        public static AuthenticationBuilder AddSqrl(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<SqrlAuthenticationOptions>? options = null)
        {
            return builder.AddScheme<SqrlAuthenticationOptions, SqrlAuthenticationHandler>(authenticationScheme, displayName, options);
        }
    }
}
