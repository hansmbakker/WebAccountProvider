
using Saso.SampleProviders.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Authentication.Web.Provider;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.UserProfile;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Trace = Saso.SampleProviders.Helpers.Trace;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Saso.SampleProvider
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {       
        

        bool _isCallbackRegistered = false;

        public MainPage()
        {
            this.InitializeComponent();
            this.ProvidersComboBox.ItemsSource = Provider.All;
            this.Loaded += MainPage_Loaded;

            Trace.AddListener(DebugPrintListener); 
        }

        void DebugPrintListener ( string message )
        {
           Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
           {
               DebugPrint(message); 
           }); 
        }

        private async Task PopulateAccounts ( )
        {
            var accounts = await WebAccountManager.FindAllProviderWebAccountsAsync();
            AccountsComboBox.ItemsSource = accounts;
            if (accounts.Count <= 0)
            {
                UseAccount.IsEnabled = false;
                UseAccount.IsChecked = false;
            }
            else
            {
                UseAccount.IsEnabled = true;
            }

            foreach ( var acct in accounts )
            {
                DebugPrint($"Acct:{acct.Id},User{acct.UserName}"); 
            }
        }


        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            for ( int x = 0;  x < Provider.All.Count; x++ )
            {
                if ( Provider.All[x].Name == "Saso" )
                {
                    ProvidersComboBox.SelectedIndex = x; 
                    break; 
                }
            }

            await PopulateAccounts(); 

        }


        // TODO: If register Account Control callback in OnNavigatedTo, the App cannot be launched
        //       on Phone VM, though it works on desktop. Still in investigation. For now, move the
        //       callback registration when the Account Control button is clicked.
        //
        //       Need to wait for the account control code bug fix: Bug 2791786
        //
        //protected override void OnNavigatedTo(NavigationEventArgs e)
        //{
        //    AccountsSettingsPane.GetForCurrentView().AccountCommandsRequested += OnAccountCommandsRequested;
        //}

        //protected override void OnNavigatedFrom(NavigationEventArgs e)
        //{
        //    AccountsSettingsPane.GetForCurrentView().AccountCommandsRequested -= OnAccountCommandsRequested;
        //}


        public void DebugPrint(String Trace)
        {
            DebugArea.Text = DateTime.Now.ToString("h:mm:ss tt ") + Trace + "\r\n" + DebugArea.Text;
        }


        public void SetProviderUI ( Provider p )
        {            
            WebAccountProvider.Text = p.Id;
            Scope.Text = p.Scope ?? "" ;
            ClientId.Text = p.ClientId ?? "" ;     
        }

        private async void SelectYourProvider(object sender, RoutedEventArgs e)
        {
            await ValidateProviderSelection(); 
        }


        WebAccountProvider currentWebAccountProvider; 
        private async Task ValidateProviderSelection()
        {
            if (ProvidersComboBox.SelectedItem != null)
            {
                var targetProvider =  ProvidersComboBox.SelectedItem as Provider;
                var targetWebAccountProvider = await WebAuthenticationCoreManager.FindAccountProviderAsync(targetProvider.Id);

                if (targetWebAccountProvider != null)
                {
                    SetProviderUI(targetProvider);
                    currentWebAccountProvider = targetWebAccountProvider; 
                } 
                else
                {
                    ProvidersComboBox.SelectedIndex = -1;
                    DebugPrint("Error: The provider was not found in this system"); 
                }
            }           
        }

        private void RequestTokenAsync_Click(object sender, RoutedEventArgs e)
        {
            if (WebAccountProvider.Text == "")
                DebugPrint("please input WebAccountProvider");
            if (Scope.Text == "")
                DebugPrint("please input Scope");
            if (ClientId.Text == "")
                DebugPrint("please input ClientID");

            AuthenticateWithRequestToken(
                WebAccountProvider.Text,
                Authority.Text,
                Scope.Text,
                ClientId.Text/*, true*/,
                TokenBindingTarget.Text,
                RequestProperties.Text);
        }


        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (WebAccountProvider.Text == "")
                DebugPrint("please input WebAccountProvider");
            if (Scope.Text == "")
                DebugPrint("please input Scope");
            if (ClientId.Text == "")
                DebugPrint("please input ClientID");

            AuthenticateWithRequestToken(
                WebAccountProvider.Text,
                Authority.Text,
                Scope.Text,
                ClientId.Text,
                TokenBindingTarget.Text,
                RequestProperties.Text);
        }


        private void AccountControl_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            // TODO: move the callback registration to OnNavigatedTo            
            try
            {
                if (!_isCallbackRegistered)
                {
                    AccountsSettingsPane.GetForCurrentView().AccountCommandsRequested += OnAccountCommandsRequested;
                    _isCallbackRegistered = true;
                }

                DebugPrint("Launching AccountSettingsPane...");
                AccountsSettingsPane.Show();
            }
            catch (Exception ex)
            {
                DebugPrint("AccountControl_Click exception: " + ex.Message);
            }

            ((Button)sender).IsEnabled = true;
        }

        private async void OnAccountCommandsRequested(
            AccountsSettingsPane sender,
            AccountsSettingsPaneCommandsRequestedEventArgs e)
        {
            var deferral = e.GetDeferral();

            // This scenario only lets the user have one account at a time.
            // If there already is an account, we do not include a provider in the list
            // This will prevent the add account button from showing up.
 
            if (HasAccountStored())
            {
                await AddWebAccountToPane(e);
            }
            else
            {
                await AddWebAccountProvidersToPane(e);
            }

            deferral.Complete();
        }

        private async Task AddWebAccountProvidersToPane(AccountsSettingsPaneCommandsRequestedEventArgs e)
        {
            try
            {
                foreach (var provider in Provider.All)
                {
                    WebAccountProvider webAccountProvider = await WebAuthenticationCoreManager.FindAccountProviderAsync(provider.Id);
                    Debug.Assert(webAccountProvider != null); 
                    if (webAccountProvider != null)
                    {
                        WebAccountProviderCommand command = new WebAccountProviderCommand(webAccountProvider, WebAccountProviderCommandInvoked);
                        e.WebAccountProviderCommands.Add(command);
                    }                     
                }
            }
            catch (Exception ex)
            {
                DebugPrint("AddWebAccountProvidersToPane exception: " + ex.Message);
            }
        }

        private async Task AddWebAccountToPane(AccountsSettingsPaneCommandsRequestedEventArgs e)
        {

            var taskAccounts = WebAccountManager.FindAllProviderWebAccountsAsync().AsTask<IReadOnlyList<WebAccount>>();
            taskAccounts.Wait();
            var accounts = taskAccounts.Result;
            foreach ( WebAccount account in accounts )
            {
                WebAccountCommand command = new WebAccountCommand(account, WebAccountCommandInvoked, SupportedWebAccountActions.Remove);
                e.WebAccountCommands.Add(command);
            }
        }

        private async Task<WebAccount> GetWebAccount()
        {
            String accountID = ApplicationData.Current.LocalSettings.Values[Constants.StoredAccountIdKey] as String;
            String providerID = ApplicationData.Current.LocalSettings.Values[Constants.StoredProviderIdKey] as String;

            WebAccount account = null;

            try
            {
                WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(providerID);

                account = await WebAuthenticationCoreManager.FindAccountAsync(provider, accountID);

                // The account has been deleted if FindAccountAsync returns null
                if (account == null)
                {
                    DebugPrint("cannot find the account, which might be removed from Settings.");
                    RemoveAccountData();
                }
            }
            catch (Exception ex)
            {
                DebugPrint("GetWebAccount exception: " + ex.Message);
            }

            return account;
        }

        private void RemoveAccountData()
        {
            ApplicationData.Current.LocalSettings.Values.Remove(Constants.StoredAccountIdKey);
            ApplicationData.Current.LocalSettings.Values.Remove(Constants.StoredProviderIdKey);
        }


        private async void WebAccountProviderCommandInvoked(WebAccountProviderCommand command)
        {
            DebugPrint("Entering WebAccountProviderCommandInvoked ...");

            var provider = command.WebAccountProvider;
            string scope = string.Empty;
            string clientId = string.Empty;

            

            foreach (var p in Provider.All)
            {
                if (p.Id == provider.Id)
                {
                    scope = p.Scope;
                    clientId = p.ClientId; 
                }
            }
            await AuthenticateWithRequestToken(provider,  scope , clientId, "");
        }


        private async void WebAccountCommandInvoked(WebAccountCommand command, WebAccountInvokedArgs args)
        {
            if (args.Action == WebAccountAction.Remove)
            {
                DebugPrint("Removing account");

                try
                {
                    if (command.WebAccount != null)
                    {
                        await command.WebAccount.SignOutAsync();
                        await WebAccountManager.DeleteWebAccountAsync(command.WebAccount);
                    } 
                } catch (Exception ex )
                {
                    Trace.LogException(ex); 
                }

            }
        }

        

        private bool HasAccountStored()
        {
            //TODO: should we get this from our local repo?   Not for a sample, but yes for real apps
            var taskAccounts = WebAccountManager.FindAllProviderWebAccountsAsync().AsTask<IReadOnlyList<WebAccount>>();
            taskAccounts.Wait();
            var accounts = taskAccounts.Result;
            return accounts.Count > 0;  
        }

         

        private void AddRequestProperties(String RequestProperties, WebTokenRequest webTokenRequest)
        {
            if (String.IsNullOrEmpty(RequestProperties))
                return;

            char[] sep = { ',' };
            string[] props = RequestProperties.Split(sep);
            if (props.Length == 0)
                return;

            foreach (string prop in props)
            {
                webTokenRequest.Properties.Add(prop, prop);
            }
        }

        public async Task AuthenticateWithRequestToken(WebAccountProvider Provider, String Scope, String ClientID, String RequestProperties)
        {
            DebugPrint("Entering AuthenticateWithRequestToken ...");

            try
            {
                WebTokenRequest webTokenRequest = new WebTokenRequest(
                                                        Provider,
                                                        Scope,
                                                        ClientID);
                DebugPrint("Call RequestTokenAsync");
                WebTokenRequestResult webTokenRequestResult = await WebAuthenticationCoreManager.RequestTokenAsync(webTokenRequest);
                HandleResult(webTokenRequestResult, true); // store returned account locally
            }
            catch (Exception ex)
            {
                DebugPrint("RequestToken failed: " + ex.Message);
            }
        }

        

        public async void AuthenticateWithRequestToken(
            String WebAccountProviderID,
            String Authority,
            String Scope,
            String ClientID,
            String TokenBindingTarget,
            String RequestProperties)
        {
            try
            {
                //
                //create webTokenRequest with ProviderID, Scope, ClientId
                //
                DebugPrint("creating the webAccountProviderID");

                WebAccountProvider provider = null;
                if (String.IsNullOrEmpty(Authority))
                {
                    DebugPrint("calling FindAccountProviderAsync...");
                    provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(WebAccountProviderID);
                }
                else
                {
                    DebugPrint("calling FindAccountProviderWithAuthorityAsync...");
                    provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                                WebAccountProviderID,
                                Authority);
                }

                DebugPrint("Provider Id: " + provider.Id);
                DebugPrint("Provider DisplayName: " + provider.DisplayName);

                WebTokenRequestPromptType prompt = ForceUiCheckBox.IsChecked.Value ?
                                                   WebTokenRequestPromptType.ForceAuthentication : WebTokenRequestPromptType.Default;

                WebTokenRequest webTokenRequest = new WebTokenRequest(
                                                    provider,
                                                    Scope,
                                                    ClientID,
                                                    prompt);

                //
                // add properties to the tokenrequest if the IDP plugin requires
                //

                AddRequestProperties(RequestProperties, webTokenRequest);

                DebugPrint("Call RequestTokenAsync");
                WebTokenRequestResult webTokenRequestResult = await WebAuthenticationCoreManager.RequestTokenAsync(webTokenRequest);
                HandleResult(webTokenRequestResult);
            }
            catch (Exception ex)
            {
                DebugPrint("RequestToken failed: " + ex.Message);
            }
        }

        private async void ProcessWebTokenResponse(WebTokenResponse tokenResponse, bool storeAccount = false)
        {
            try
            {
                string authToken = tokenResponse.Token;
                // Do something with the token 
                DebugPrint("The token returned : " + authToken);

                for (int i = 0; i < tokenResponse.Properties.Count; i++)
                {
                    string msg = string.Format("---ResponseProperties[{0}].key=[{1}], value=[{2}]",
                        i, tokenResponse.Properties.ElementAt(i).Key, tokenResponse.Properties.ElementAt(i).Value);
                    DebugPrint(msg);
                }

                WebAccount account = tokenResponse.WebAccount;
                if (account != null)
                {
                    DebugPrint("tokenResponse.WebAccount is not null.");

                    string msg = string.Format("---- AccounInfo. Id={0}, UserName={1}",
                        account.Id,
                        account.UserName);

                    DebugPrint(msg);

                    for (int i = 0; i < account.Properties.Count; i++)
                    {
                        string tmpmsg = string.Format("---WebAccount.Properties[{0}].key=[{1}], value=[{2}]",
                            i, account.Properties.ElementAt(i).Key, account.Properties.ElementAt(i).Value);
                        DebugPrint(tmpmsg);
                    }

                    DebugPrint("webaccount.GetPictureAsync");
                    IRandomAccessStream picStream = await account.GetPictureAsync(WebAccountPictureSize.Size1080x1080);
                    DebugPrint("webaccount.GetPictureAsync successful");

                    string picStreamSize = string.Format("---WebAccount picture stream size is: {0}", picStream.Size);
                    DebugPrint(picStreamSize);
              
                }
                else
                {
                    DebugPrint("tokenResponse.WebAccount is null.");
                }
            }
            catch (Exception ex)
            {
                DebugPrint("Exeption during ProcessWebTokenResponse: " + ex.Message);
            }
        }


        public void HandleResult(WebTokenRequestResult webTokenRequestResult, bool addAccount = false)
        {
            switch (webTokenRequestResult.ResponseStatus)
            {
                case WebTokenRequestStatus.Success:

                    DebugPrint("RequestTokenAsync succeeds");
                    try
                    {
                        WebTokenResponse tokenResponse = webTokenRequestResult.ResponseData[0];
                        ProcessWebTokenResponse(tokenResponse, addAccount);
                    }
                    catch (Exception ex)
                    {
                        DebugPrint("Exeption during ResponseData: " + ex.Message);
                    }
                    break;


                case WebTokenRequestStatus.UserCancel:

                    // Handle user cancel by resuming pre-login screen 
                    DebugPrint("User cancelled the authentication");
                    break;


                case WebTokenRequestStatus.AccountProviderNotAvailable:

                    // fall back to WebAuthenticationBroker  
                    DebugPrint("WebTokenRequestStatus.AccountProviderNotAvailable");
                    break;

                case WebTokenRequestStatus.ProviderError:
                    DebugPrint(string.Format("Error: 0x{0:X08} message: {1}", webTokenRequestResult.ResponseError.ErrorCode, webTokenRequestResult.ResponseError.ErrorMessage));
                    break;

                case WebTokenRequestStatus.UserInteractionRequired:
                    DebugPrint("WebTokenRequestStatus.UserInteractionRequired");

                    if (webTokenRequestResult.ResponseError != null)
                    {
                        DebugPrint(string.Format("Error: 0x{0:X08} message: {1}", webTokenRequestResult.ResponseError.ErrorCode, webTokenRequestResult.ResponseError.ErrorMessage));
                    }
                    break;

                default:
                    DebugPrint("Unhandled webTokenRequestResult.ResponseStatus: " + webTokenRequestResult.ResponseStatus);
                    break;
            }
        }


        private void ConnectSilently_Click(object sender, RoutedEventArgs e)
        {
            if (WebAccountProvider.Text == "")
                DebugPrint("please input WebAccountProvider");
            if (Scope.Text == "")
                DebugPrint("please input Scope");
            if (ClientId.Text == "")
                DebugPrint("please input ClientID");

            AuthenticateWithGetTokenSilently(
                WebAccountProvider.Text,
                Authority.Text,
                Scope.Text,
                ClientId.Text,
                TokenBindingTarget.Text,
                RequestProperties.Text);
        }

        private async void AuthenticateWithGetTokenSilently(
            String WebAccountProviderID,
            String authority,
            String Scope,
            String ClientID,
            String TokenBindingTarget,
            String RequestProperties)
        {
            try
            {
                //
                //create webTokenRequest with ProviderID, Scope, ClientId
                //
                DebugPrint("creating the webAccountProviderID");
                WebAccountProvider provider = null;
                if (String.IsNullOrEmpty(authority))
                {
                    DebugPrint("calling FindAccountProviderAsync...");
                    provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(WebAccountProviderID);
                }
                else
                {
                    DebugPrint("calling FindAccountProviderWithAuthorityAsync...");
                    provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                                WebAccountProviderID,
                                Authority.Text);
                }

                WebTokenRequestPromptType tokenType = WebTokenRequestPromptType.Default;

                if (ForceUiCheckBox.IsChecked.Value)
                    tokenType = WebTokenRequestPromptType.ForceAuthentication; 

                WebTokenRequest webTokenRequest = new WebTokenRequest(
                                                        provider,
                                                        Scope,
                                                        ClientID, tokenType);

                //
                //adding properties to the tokenrequest if the IDP plugin requires
                DebugPrint("Adding properties to TokenRequest");
                webTokenRequest.Properties.Add("facebook_properties", "additional_properties");

                AddRequestProperties(RequestProperties, webTokenRequest);

                DebugPrint("Call GetTokenSilently");
                WebTokenRequestResult result;
                bool useAccount = UseAccount.IsChecked.Value;
                WebAccount account = AccountsComboBox.SelectedItem as WebAccount;                   
                if (useAccount && account != null ) 
                    result = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(webTokenRequest, account );
                else 
                    result = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(webTokenRequest);

                HandleResult(result);

            }
            catch (Exception ex)
            {
                DebugPrint("GetTokenSilently failed: " + ex.Message);
            }
        }

        private async void RefreshAccounts_Click(object sender, RoutedEventArgs e)
        {
            await PopulateAccounts(); 
        }

        private async void FindAccount_Click(object sender, RoutedEventArgs e)
        {
            if ( string.IsNullOrEmpty ( AccountId.Text ))
            {
                DebugPrint("Error: Account Id is required");
                return; 
            }

            if (currentWebAccountProvider != null)
            {
                var account = await WebAuthenticationCoreManager.FindAccountAsync(currentWebAccountProvider, AccountId.Text);
                if (account != null)
                {
                    DebugPrint($"Found. Acct: {account.Id},UserName{account.UserName}");
                }
                else
                {
                    DebugPrint("Account not found");

                }
            }
            else
                DebugPrint("Error: You must first select a provider");             
        }
 
        private async void PushCookies_Click(object sender, RoutedEventArgs e)
        {            
            List<HttpCookie> cookies = new List<HttpCookie>();
            HttpCookie adIdCookie = CookieManager.GetAdIdCookie ( );
            adIdCookie.Value = "UpdatedManually"; 

            cookies.Add(adIdCookie);
            HttpCookie debugCookie = CookieManager.MakeCookie(Constants.DebugCookieKey, CookieManager.GetDomain(Constants.ProviderId), Constants.DefaultCookiePath, DateTime.Now.ToString() + "- from Client" );
            cookies.Add(debugCookie);
            
            try
            {  
                await WebAccountManager.PushCookiesAsync( new Uri (currentWebAccountProvider.Id) , cookies);
                DebugPrint("Cookie(s) added"); 
            }
            catch ( Exception ex )
            {
                DebugPrint($"Error: {ex.Message}\n{ex.StackTrace}"); 
            }
        }

        private async void GetCookies_Click(object sender, RoutedEventArgs e)
        {
            // you should not call PullCookiesAsync. It is restricted to AAD provider. It requires   <uap:Capability Name="userPrincipalName" />            
           //  await WebAccountManager.PullCookiesAsync(currentWebAccountProvider.Id, /* Package.Current.Id.FamilyName */ pFn );

            try
            {
                HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();                 
                var cookieManager = filter.CookieManager;
                
                HttpCookieCollection cookieCollection = cookieManager.GetCookies(new Uri(currentWebAccountProvider.Id));
                int i = 0;
                DebugPrint($"Retrieved {cookieCollection.Count} cookies");

               
                foreach (HttpCookie cookie in cookieCollection)
                {
                    i++;
                    Debug.WriteLine("\n\ni=" + i);
                    Debug.WriteLine("\nName=" + cookie.Name);
                    Debug.WriteLine("\nValue=" + cookie.Value);
                    Debug.WriteLine("\nDomain=" + cookie.Domain);
                    Debug.WriteLine("\nPath=" + cookie.Path);
                    Debug.WriteLine("\nHttpOnly=" + cookie.HttpOnly);
                    Debug.WriteLine("\nSecure=" + cookie.Secure);
                    Debug.WriteLine("\nExpires=" + cookie.Expires.ToString());
                    DebugPrint($"{cookie.Name}={cookie.Value}, ");        
                }
            }
            catch (Exception ex)
            {
                Trace.LogException(ex); 
            }
       }
    }
}
