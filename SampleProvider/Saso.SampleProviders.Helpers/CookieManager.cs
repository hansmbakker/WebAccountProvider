using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.UserProfile;
using Windows.Web.Http;

namespace Saso.SampleProviders.Helpers
{
    public class CookieManager         
    {

        public void MergeCookie ( HttpCookie newCookie, ref HttpCookieCollection collection )
        {


        }

        public static HttpCookie GetAdIdCookie()
        {
            return GetAdIdCookie( GetDomain(Constants.ProviderId), Constants.DefaultCookiePath);
        }

        public static HttpCookie GetAdIdCookie(string domain, string path)
        {
            return MakeCookie(Constants.AdvertiserIdCookieKey, domain, path, AdvertisingManager.AdvertisingId);
        }

        public static string GetDomain(string providerId)
        {
            string[] prefixes = { "https://", "http://" };

            foreach (var prefix in prefixes)
            {
                if (providerId.StartsWith(prefix))
                {
                    string noprefix = providerId.Substring(prefix.Length);
                    if (noprefix.EndsWith("/"))
                        noprefix = noprefix.Substring(0, noprefix.Length - 1);

                    return noprefix;
                }
            }
            Debug.Assert(false, "this is not an error, but i don't expect it to not have prefix for this sample");
            return providerId;
        }
        public static HttpCookie MakeCookie(string cookieName, string domain, string path, string value)
        {

            HttpCookie cookie = new HttpCookie(cookieName, domain, path);
            cookie.Expires = DateTime.Now.AddMonths(3);
            cookie.Value = value;            
            //per other samples, this is a workaround to a bug.. HttpOnly must be false 
            cookie.HttpOnly = false;
            cookie.Secure = true;

            return cookie;
        }
    }
}
