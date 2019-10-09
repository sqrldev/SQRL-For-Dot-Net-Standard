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

        public static string GetCheckMillieSeconds(HttpRequest request)
        {
            return request.HttpContext.Items["CheckMillieSeconds"].ToString();
        }

        public static string SqrlCheckMillieSeconds(this HttpRequest request)
        {
            return GetCheckMillieSeconds(request);
        }

        public static string SqrlCheckMillieSeconds(this HttpResponse response)
        {
            return response.HttpContext.Request.SqrlCheckMillieSeconds();
        }

        public static string GetCheckUrl(HttpRequest request)
        {
            return request.HttpContext.Items["CheckMillieSeconds"].ToString();
        }

        public static string SqrlCheckUrl(this HttpRequest request)
        {
            return GetCheckUrl(request);
        }

        public static string SqrlCheckUrl(this HttpResponse response)
        {
            return response.HttpContext.Request.SqrlCheckUrl();
        }

    }
}
