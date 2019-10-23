using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SqrlForNet
{
    public static class SqrlHtmlHelper
    {
        
        public static HtmlString SqrlLink<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request)
        {
            return SqrlLink(helper, request, "SQRL Login");
        }

        public static HtmlString SqrlLink<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string text)
        {
            return SqrlLink(helper, request, text, true);
        }

        public static HtmlString SqrlLink<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string text, bool poll)
        {
            ValidateRequestData(request);
            return SqrlLink(helper, request, text, poll, int.Parse(request.HttpContext.Items["CheckMilliSeconds"].ToString()));
        }

        public static HtmlString SqrlLink<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string text, bool poll, int pollTime)
        {
            ValidateRequestData(request);
            var linkTag = new TagBuilder("a");
            linkTag.MergeAttribute("href", request.HttpContext.Items["CallbackUrl"].ToString());
            linkTag.MergeAttribute("onclick", "CpsProcess(this);");
            linkTag.InnerHtml.Append(text);

            var script = new TagBuilder("script");
            script.InnerHtml.AppendHtml("function CpsProcess(e)");
            script.InnerHtml.AppendHtml("{");
            script.InnerHtml.AppendHtml("var gifProbe = new Image();");
            script.InnerHtml.AppendHtml("gifProbe.onload = function() {");
            script.InnerHtml.AppendHtml("document.location.href = \"http://localhost:25519/\"+ btoa(e.getAttribute(\"href\"));");
            script.InnerHtml.AppendHtml("};");
            script.InnerHtml.AppendHtml("gifProbe.onerror = function() {");
            script.InnerHtml.AppendHtml("setTimeout( function(){ gifProbe.src = \"http://localhost:25519/\" + Date.now() + '.gif';}, 250 );");
            script.InnerHtml.AppendHtml("};");
            script.InnerHtml.AppendHtml("gifProbe.onerror();");
            script.InnerHtml.AppendHtml("};");
            if (poll) { 
                script.InnerHtml.AppendHtml("function CheckAuto()");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("var xhttp = new XMLHttpRequest();");
                script.InnerHtml.AppendHtml("xhttp.onreadystatechange = function()");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("if (this.readyState == 4 && this.status == 200)");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("if(this.responseText !== \"false\")");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("window.location = \"" + request.HttpContext.Items["RedirectUrl"] + "\";");
                script.InnerHtml.AppendHtml("}");
                script.InnerHtml.AppendHtml("}");
                script.InnerHtml.AppendHtml("};");
                script.InnerHtml.AppendHtml("xhttp.open(\"GET\", \""+ request.HttpContext.Items["CheckUrl"] + "\", true);");
                script.InnerHtml.AppendHtml("xhttp.send();");
                script.InnerHtml.AppendHtml("};");
                script.InnerHtml.AppendHtml("document.onload = setInterval(function(){ CheckAuto(); }, " + pollTime + ");");
            }

            var stringWriter = new System.IO.StringWriter();
            linkTag.WriteTo(stringWriter, HtmlEncoder.Default);
            script.WriteTo(stringWriter, HtmlEncoder.Default);
            return new HtmlString(stringWriter.ToString());
        }

        public static HtmlString SqrlLinkForPath<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string path)
        {
            return SqrlLinkForPath(helper, request, "SQRL Login", path);
        }

        public static HtmlString SqrlLinkForPath<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string text, string path)
        {
            return SqrlLinkForPath(helper, request, text, true, path);
        }

        public static HtmlString SqrlLinkForPath<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string text, bool poll, string path)
        {
            ValidateRequestData(request, path);
            return SqrlLinkForPath(helper, request, text, poll, int.Parse(request.HttpContext.Items["CheckMilliSeconds"].ToString()), path);
        }

        public static HtmlString SqrlLinkForPath<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string text, bool poll, int pollTime, string path)
        {
            ValidateRequestData(request, path);
            var otherUrl = ((List<OtherUrlsData>)request.HttpContext.Items["OtherUrls"]).Single(x => x.Path == path);
            var linkTag = new TagBuilder("a");
            linkTag.MergeAttribute("href", otherUrl.Url);
            linkTag.MergeAttribute("onclick", "CpsProcess(this);");
            linkTag.InnerHtml.Append(text);

            var script = new TagBuilder("script");
            script.InnerHtml.AppendHtml("function CpsProcess(e)");
            script.InnerHtml.AppendHtml("{");
            script.InnerHtml.AppendHtml("var gifProbe = new Image();");
            script.InnerHtml.AppendHtml("gifProbe.onload = function() {");
            script.InnerHtml.AppendHtml("document.location.href = \"http://localhost:25519/\"+ btoa(e.getAttribute(\"href\"));");
            script.InnerHtml.AppendHtml("};");
            script.InnerHtml.AppendHtml("gifProbe.onerror = function() {");
            script.InnerHtml.AppendHtml("setTimeout( function(){ gifProbe.src = \"http://localhost:25519/\" + Date.now() + '.gif';}, 250 );");
            script.InnerHtml.AppendHtml("};");
            script.InnerHtml.AppendHtml("gifProbe.onerror();");
            script.InnerHtml.AppendHtml("};");
            if (poll)
            {
                script.InnerHtml.AppendHtml("function CheckAuto()");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("var xhttp = new XMLHttpRequest();");
                script.InnerHtml.AppendHtml("xhttp.onreadystatechange = function()");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("if (this.readyState == 4 && this.status == 200)");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("if(this.responseText !== \"false\")");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("window.location = \"" + otherUrl.RedirectUrl + "\";");
                script.InnerHtml.AppendHtml("}");
                script.InnerHtml.AppendHtml("}");
                script.InnerHtml.AppendHtml("};");
                script.InnerHtml.AppendHtml("xhttp.open(\"GET\", \"" + otherUrl.CheckUrl + "\", true);");
                script.InnerHtml.AppendHtml("xhttp.send();");
                script.InnerHtml.AppendHtml("};");
                script.InnerHtml.AppendHtml("document.onload = setInterval(function(){ CheckAuto(); }, " + pollTime + ");");
            }

            var stringWriter = new System.IO.StringWriter();
            linkTag.WriteTo(stringWriter, HtmlEncoder.Default);
            script.WriteTo(stringWriter, HtmlEncoder.Default);
            return new HtmlString(stringWriter.ToString());
        }

        public static HtmlString SqrlQrImage<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request)
        {
            return SqrlQrImage(helper, request, true);
        }

        public static HtmlString SqrlQrImage<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, bool poll)
        {
            ValidateRequestData(request);
            return SqrlQrImage(helper, request, poll, int.Parse(request.HttpContext.Items["CheckMilliSeconds"].ToString()));
        }

        public static HtmlString SqrlQrImage<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, bool poll, int pollTime)
        {
            ValidateRequestData(request);
            var stringWriter = new System.IO.StringWriter();
            var imgTag = new TagBuilder("img");
            imgTag.MergeAttribute("src", "data:image/bmp;base64," + request.HttpContext.Items["QrData"].ToString());
            imgTag.WriteTo(stringWriter, HtmlEncoder.Default);
            if (poll)
            {
                var script = new TagBuilder("script");
                script.InnerHtml.AppendHtml("function CheckAuto()");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("var xhttp = new XMLHttpRequest();");
                script.InnerHtml.AppendHtml("xhttp.onreadystatechange = function()");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("if (this.readyState == 4 && this.status == 200)");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("if(this.responseText !== \"false\")");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("window.location = \"" + request.HttpContext.Items["RedirectUrl"] + "\";");
                script.InnerHtml.AppendHtml("}");
                script.InnerHtml.AppendHtml("}");
                script.InnerHtml.AppendHtml("};");
                script.InnerHtml.AppendHtml("xhttp.open(\"GET\", \"" + request.HttpContext.Items["CheckUrl"] + "\", true);");
                script.InnerHtml.AppendHtml("xhttp.send();");
                script.InnerHtml.AppendHtml("};");
                script.InnerHtml.AppendHtml("document.onload = setInterval(function(){ CheckAuto(); }, " + pollTime + ");");
                script.WriteTo(stringWriter, HtmlEncoder.Default);
            }
            return new HtmlString(stringWriter.ToString());
        }

        public static HtmlString SqrlQrImageForPath<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string path)
        {
            return SqrlQrImageForPath(helper, request, true, path);
        }

        public static HtmlString SqrlQrImageForPath<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, bool poll, string path)
        {
            ValidateRequestData(request, path);
            return SqrlQrImageForPath(helper, request, poll, int.Parse(request.HttpContext.Items["CheckMilliSeconds"].ToString()), path);
        }

        public static HtmlString SqrlQrImageForPath<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, bool poll, int pollTime, string path)
        {
            ValidateRequestData(request, path);
            var otherUrl = ((List<OtherUrlsData>)request.HttpContext.Items["OtherUrls"]).Single(x => x.Path == path);
            var stringWriter = new System.IO.StringWriter();
            var imgTag = new TagBuilder("img");
            imgTag.MergeAttribute("src", "data:image/bmp;base64," + otherUrl.QrCodeBase64.ToString());
            imgTag.WriteTo(stringWriter, HtmlEncoder.Default);
            if (poll)
            {
                var script = new TagBuilder("script");
                script.InnerHtml.AppendHtml("function CheckAuto()");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("var xhttp = new XMLHttpRequest();");
                script.InnerHtml.AppendHtml("xhttp.onreadystatechange = function()");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("if (this.readyState == 4 && this.status == 200)");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("if(this.responseText !== \"false\")");
                script.InnerHtml.AppendHtml("{");
                script.InnerHtml.AppendHtml("window.location = \"" + otherUrl.RedirectUrl + "\";");
                script.InnerHtml.AppendHtml("}");
                script.InnerHtml.AppendHtml("}");
                script.InnerHtml.AppendHtml("};");
                script.InnerHtml.AppendHtml("xhttp.open(\"GET\", \"" + otherUrl.CheckUrl + "\", true);");
                script.InnerHtml.AppendHtml("xhttp.send();");
                script.InnerHtml.AppendHtml("};");
                script.InnerHtml.AppendHtml("document.onload = setInterval(function(){ CheckAuto(); }, " + pollTime + ");");
                script.WriteTo(stringWriter, HtmlEncoder.Default);
            }
            return new HtmlString(stringWriter.ToString());
        }

        public static HtmlString SqrlLinkAndImage<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request)
        {
            return SqrlLinkAndImage(helper, request, "SQRL Login");
        }

        public static HtmlString SqrlLinkAndImage<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string text)
        {
            return SqrlLinkAndImage(helper, request, text, true);
        }

        public static HtmlString SqrlLinkAndImage<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string text, bool poll)
        {
            ValidateRequestData(request);
            return SqrlLinkAndImage(helper, request, text, poll, int.Parse(request.HttpContext.Items["CheckMilliSeconds"].ToString()));
        }

        public static HtmlString SqrlLinkAndImage<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string text, bool poll, int pollTime)
        {
            ValidateRequestData(request);
            return new HtmlString(SqrlLink(helper, request, text, poll, pollTime).ToString() + SqrlQrImage(helper, request, false).ToString());
        }

        public static HtmlString SqrlLinkAndImageForPath<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string path)
        {
            return SqrlLinkAndImageForPath(helper, request, "SQRL Login", path);
        }

        public static HtmlString SqrlLinkAndImageForPath<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string text, string path)
        {
            return SqrlLinkAndImageForPath(helper, request, text, true, path);
        }

        public static HtmlString SqrlLinkAndImageForPath<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string text, bool poll, string path)
        {
            ValidateRequestData(request, path);
            return SqrlLinkAndImageForPath(helper, request, text, poll, int.Parse(request.HttpContext.Items["CheckMilliSeconds"].ToString()), path);
        }

        public static HtmlString SqrlLinkAndImageForPath<TModel>(this IHtmlHelper<TModel> helper, HttpRequest request, string text, bool poll, int pollTime, string path)
        {
            ValidateRequestData(request, path);
            return new HtmlString(SqrlLinkForPath(helper, request, text, poll, pollTime, path).ToString() + SqrlQrImageForPath(helper, request, false, path).ToString());
        }

        private static void ValidateRequestData(HttpRequest request, string path = null)
        {
            if (path != null)
            {
                if (!request.HttpContext.Items.ContainsKey("OtherUrls"))
                {
                    throw new InvalidOperationException("EnableHelpers is disabled for this URL");
                }
                if (!(request.HttpContext.Items["OtherUrls"] is List<OtherUrlsData> otherUrls) || otherUrls.All(x => x.Path != path))
                {
                    throw new InvalidOperationException("This path is not set in the OtherAuthenticationPaths options");
                }
            }
            if (!request.HttpContext.Items.ContainsKey("CallbackUrl") ||
                !request.HttpContext.Items.ContainsKey("QrData") ||
                !request.HttpContext.Items.ContainsKey("CheckMilliSeconds") ||
                !request.HttpContext.Items.ContainsKey("CheckUrl")
            )
            {
                throw new InvalidOperationException("EnableHelpers is disabled for this URL");
            }
        }

    }
}
