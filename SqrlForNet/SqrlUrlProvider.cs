using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace SqrlForNet
{
    public static class SqrlUrlProvider
    {

        public static string GetUrl(HttpRequest request)
        {
            return request.HttpContext.Items["CallbackUrl"].ToString();
        }

        public static string SqrlUrl(this HttpRequest request)
        {
            return GetUrl(request);
        }

        public static string SqrlUrl(this HttpResponse response)
        {
            return response.HttpContext.Request.SqrlUrl();
        }
        
        public static string GetQrData(HttpRequest request)
        {
            return request.HttpContext.Items["QrData"].ToString();
        }

        public static string SqrlQrData(this HttpRequest request)
        {
            return GetQrData(request);
        }

        public static string SqrlQrData(this HttpResponse response)
        {
            return response.HttpContext.Request.SqrlQrData();
        }

        public static string GetCheckMilliseconds(HttpRequest request)
        {
            return request.HttpContext.Items["CheckMilliSeconds"].ToString();
        }

        public static string SqrlCheckMilliseconds(this HttpRequest request)
        {
            return GetCheckMilliseconds(request);
        }

        public static string SqrlCheckMilliseconds(this HttpResponse response)
        {
            return response.HttpContext.Request.SqrlCheckMilliseconds();
        }

        public static string GetCheckUrl(HttpRequest request)
        {
            return request.HttpContext.Items["CheckUrl"].ToString();
        }

        public static string SqrlCheckUrl(this HttpRequest request)
        {
            return GetCheckUrl(request);
        }

        public static string SqrlCheckUrl(this HttpResponse response)
        {
            return response.HttpContext.Request.SqrlCheckUrl();
        }

        public static string GetUrl(HttpRequest request, string path)
        {
            var otherUrl = ((List<OtherUrlsData>)request.HttpContext.Items["OtherUrls"]).Single(x => x.Path == path);
            return otherUrl.Url;
        }

        public static string SqrlUrl(this HttpRequest request, string path)
        {
            return GetUrl(request, path);
        }

        public static string SqrlUrl(this HttpResponse response, string path)
        {
            return response.HttpContext.Request.SqrlUrl(path);
        }

        public static string GetQrData(HttpRequest request, string path)
        {
            var otherUrl = ((List<OtherUrlsData>)request.HttpContext.Items["OtherUrls"]).Single(x => x.Path == path);
            return otherUrl.QrCodeBase64;
        }

        public static string SqrlQrData(this HttpRequest request, string path)
        {
            return GetQrData(request, path);
        }

        public static string SqrlQrData(this HttpResponse response, string path)
        {
            return response.HttpContext.Request.SqrlQrData(path);
        }

        public static string GetCheckUrl(HttpRequest request, string path)
        {
            var otherUrl = ((List<OtherUrlsData>)request.HttpContext.Items["OtherUrls"]).Single(x => x.Path == path);
            return otherUrl.CheckUrl;
        }

        public static string SqrlCheckUrl(this HttpRequest request, string path)
        {
            return GetCheckUrl(request, path);
        }

        public static string SqrlCheckUrl(this HttpResponse response, string path)
        {
            return response.HttpContext.Request.SqrlCheckUrl(path);
        }
        
        public static string GetRedirectUrl(HttpRequest request)
        {
            return request.HttpContext.Items["RedirectUrl"].ToString();
        }

        public static string SqrlRedirectUrl(this HttpRequest request)
        {
            return GetRedirectUrl(request);
        }

        public static string SqrlRedirectUrl(this HttpResponse response)
        {
            return response.HttpContext.Request.SqrlRedirectUrl();
        }

        
    }
}
