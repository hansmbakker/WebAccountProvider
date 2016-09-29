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
    public class Constants
    {
        // constants & keys 
        public const string StoredAccountIdKey = "accountId";
        public const string StoredProviderIdKey = "providerId";
        public const string AccountsFileName = "SasoProviderAccounts";
        public const string AdvertiserIdCookieKey = "adId";
        public const string DebugCookieKey = "LastCalled";

        public enum ErrorCodes
        {
            Unknown          = 1, 
            InvalidPassword  = 2, 
            AddAccountFailed = 3, 
            AccountNotFound =  4,
            AccountRequired = 5 , 
        }; 
         
        public const string ProviderId = "https://paxwaptest.azurewebsites.net";        
        public const string ProviderName = "Saso";
        public const string DefaultCookiePath = "/";
         

        //Errors and exceptions 
        public const string UnexpectedOperation = "Unexpected Operation: {0}";
        public const string NoToken = "Expected a tokenRequest, but did not get it";
        public const string UserNamePasswordMustMatch = "Username and password must match. You have {0} more tries";
        public const string AccountRequired = "Account id is required, and it was not provided";
        public const string InvalidPassword = "Invalid Password";
        public const string UnknownError = "Uknown Error";
        public const string AddAccountFailed = "Failed to add account";
        public const string UserNameAccountIdRequired = "Both username and accountid are required"; 

        public static string FormatError(string format, params object [] args  ) {
            return string.Format(format, args);  
        }


       

        
    }
     
}
