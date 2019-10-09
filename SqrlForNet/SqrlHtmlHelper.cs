using System;
using System.Linq.Expressions;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace SqrlForNet
{
    public static class SqrlHtmlHelper
    {
        
        public static HtmlString SqrlLink<TModel>(this IHtmlHelper<TModel> helper)
        {
            return SqrlLink(helper, "SQRL Login");
        }

        public static HtmlString SqrlLink<TModel>(this IHtmlHelper<TModel> helper, string text)
        {
            var linkTag = new TagBuilder("a");
            linkTag.MergeAttribute("href", SqrlAuthenticationOptions.CachedCallbackUrl);
            linkTag.InnerHtml.Append(text);

            var stringWriter = new System.IO.StringWriter();
            linkTag.WriteTo(stringWriter, HtmlEncoder.Default);
            return new HtmlString(stringWriter.ToString());
        }

    }
}
