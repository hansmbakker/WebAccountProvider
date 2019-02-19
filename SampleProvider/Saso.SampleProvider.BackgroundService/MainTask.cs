using Saso.SampleProviders.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Authentication.Web.Provider;
using Windows.Security.Credentials;
using Windows.Web.Http;
using Trace = Saso.SampleProviders.Helpers.Trace;

namespace Saso.SampleProvider.BackgroundService
{
    public sealed class MainTask : IBackgroundTask
    {
        BackgroundTaskDeferral serviceDeferral = null;
        IWebAccountProviderOperation operation = null;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            try
            {                 
                serviceDeferral = taskInstance.GetDeferral();

                WebAccountProviderTriggerDetails triggerDetails =
                    taskInstance.TriggerDetails as WebAccountProviderTriggerDetails;
                operation = triggerDetails.Operation;

                switch (operation.Kind)
                {
                    case WebAccountProviderOperationKind.SignOutAccount:

                        WebAccountProviderSignOutAccountOperation signoutOperation = operation as WebAccountProviderSignOutAccountOperation;

                        //
                        // Sign-out operation tells IDP that the calling applicaiton wants
                        // to stop working with the particular account. 
                        //
                        // Only sign out the account for that application! 
                        // Other applications should continue to work. 
                        //
                        // Note that by the time of this activation application's response 
                        // cache has been cleared
                        //
                        // Properties of the sign-out operation:
                        //   WebAccount - account from which to sign out the application
                        //   ApplicationCallbackUri - Identifier of the calling application that
                        //     requested a sign-out (see comments in responsepage.xaml.cs)
                        //   ClientId - optional client ID for desktop applications
                        //   

                        
                        Uri appCallbackUri = signoutOperation.ApplicationCallbackUri;
                        string clientId = signoutOperation.ClientId;

                        WebAccount accountToSignOut = signoutOperation.WebAccount;
                        accountToSignOut.SignOutAsync().AsTask().Wait();                          

                        signoutOperation.ReportCompleted();
                        break;

                    case WebAccountProviderOperationKind.GetTokenSilently:

                        WebAccountProviderGetTokenSilentOperation getTokenOperation = operation as WebAccountProviderGetTokenSilentOperation;
                        WebProviderTokenRequest providerRequest = getTokenOperation.ProviderRequest;

                        
                        string scope = (String.IsNullOrEmpty(providerRequest.ClientRequest.Scope) ? "empty scope" : providerRequest.ClientRequest.Scope);
                        WebAccount account = (providerRequest.WebAccounts.Count > 0 ? providerRequest.WebAccounts[0] : null);
                        if (account == null)
                        {
                            //TODO: this sample requires an account.  Not all samples have to do that, we did it for illustrative purposes 
                            getTokenOperation.ReportUserInteractionRequired(); 
                            // getTokenOperation.ReportError(new WebProviderError((uint)Constants.ErrorCodes.AccountRequired, Constants.AccountRequired));
                            break;
                        }
                        else
                        {                       
                            string accessToken = "Access token for: " + scope;
                            Token token = AccountManager.Current.GetToken(account); 
                            WebTokenResponse response; 

                            if ( token != null )
                              response = new WebTokenResponse( token.Value , account);
                            else
                              response = new WebTokenResponse("", account);
                            WebProviderTokenResponse providerResponse = new WebProviderTokenResponse(response);

                            // Cache is used to record the responses for a given request so that they can
                            // be efficiently replayed back to the calling app. Setting it to "now" will 
                            // tell Web Account Manager not to cache. 
                            //
                            // We recommend that IDP always cache responses with the time that is close to 
                            // access token lifetime. However, it this a transactional request (e.g. payment)
                            // the cache should not be used. 
                            //
                            getTokenOperation.CacheExpirationTime = DateTimeOffset.Now;
                            getTokenOperation.ProviderResponses.Add(providerResponse);                             
                            getTokenOperation.ReportCompleted();
                        }
                        break;

                    case WebAccountProviderOperationKind.DeleteAccount:

                        //
                        // the delete account tells IDP that some account has ALREADY been 
                        // deleted by the system. 
                        //
                        // This activation is meant for plug-in to clean-up it's data related to 
                        // that account (e.g. refresh tokens). 
                        //
                        // To trigger this, delete account from settings. Note that there is a
                        // small delay before account is actually deleted. This is done to allow
                        // system components to react to account deletion
                        //
                        CommonBaseOperation delete = new CommonBaseOperation(operation);
                        delete.Delete();
                        delete.ReportCompleted();   
                        break;
                    case WebAccountProviderOperationKind.RetrieveCookies:

                        WebAccountProviderRetrieveCookiesOperation cookiesOperation = operation as WebAccountProviderRetrieveCookiesOperation;

                        // the following are the properties of the operation

                        //
                        // cookiesOperation.ApplicationCallbackUri tells IDP where the web page is being rendered
                        // this is useful to deny request from apps that are trying to login user in-proc and should
                        // not be given the cookies
                        //

                        //
                        // cookiesOperation.Context the URI of the "windows.tbauth://<...> of the page that riggered 
                        // the operation. 
                        //
                        // "windows.tbauth" protocol is supported on all Windows editions after November
                        // update. For the first release of the Windows 10 desktop SKUs, the "tbauth"
                        // must be used. However, the "tbauth" will not work on other editions such as
                        // Mobile or HoloLens. If provider has to support all Windows 10 editions and 
                        // SKUs, it should implement a fallback logic from "windows.tbauth" to "tbauth"
                        //

                        //
                        // cookiesOperation.Cookies use this collection to provide the cookies that will
                        // be set in the browser
                        //

                        //
                        // cookiesOperation.Uri - set the URI for which the cookies should be set
                        //

                        //
                        // Note that WebAccountManager.PushCookiesAsync() could be used at any time
                        // to set the cookies (e.g. during user logon or request processing)
                        //
                     
                         

                          //Append two cookies for testing purposes.. 
                         cookiesOperation.Cookies.Add(CookieManager.GetAdIdCookie( )) ; 
                         cookiesOperation.Cookies.Add(CookieManager.MakeCookie ( Constants.DebugCookieKey, CookieManager.GetDomain ( Constants.ProviderId), Constants.DefaultCookiePath,
                                  Uri.EscapeUriString (DateTime.Now.ToString())));
                         
                        if (cookiesOperation.Uri == null)
                            cookiesOperation.Uri = new Uri(Constants.ProviderId); 
                       
                        try
                        {
                            var roList = new List <HttpCookie>(cookiesOperation.Cookies); 
                            WebAccountManager.PushCookiesAsync(new Uri(Constants.ProviderId), roList ).AsTask().Wait();
                        } catch (Exception ex )
                        {
                            Trace.LogException(ex); 
                        }
                        cookiesOperation.ReportCompleted();
                        break;

                    default:
                        Debug.Assert(false, $"NotImplemented operation {operation.Kind} "); 
                        throw new NotImplementedException("Operation kind hasn't been implemented");
                }

            }
            catch (Exception e)
            {
                

                if (null != operation)
                {
                    IWebAccountProviderBaseReportOperation baseOperation = operation as IWebAccountProviderBaseReportOperation;
                    baseOperation.ReportError(new WebProviderError(
                                            0x80004005, //this is E_FAIL - not a good error to return, define your own good error codes
                                            "Exception during operation: " + e.Message));
                }
            }
            finally
            {
                if (serviceDeferral != null)
                {
                    serviceDeferral.Complete();
                }
            }
        }
    }
}
