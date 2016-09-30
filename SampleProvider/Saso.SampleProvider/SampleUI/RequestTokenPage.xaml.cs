using Saso.SampleProviders.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Authentication.Web.Provider;
using Windows.Security.Credentials;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Saso.SampleProvider.SampleUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RequestTokenPage : Page
    {
        const int MaxFailedAttempts = 5; 

        WebAccountProviderRequestTokenOperation _operation;
        WebAccount _requestedAccount;

        int _failedAttempts; 
        public RequestTokenPage()
        {
            this.InitializeComponent();
            this.Loaded += RequestTokenPage_Loaded;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _operation = e.Parameter as WebAccountProviderRequestTokenOperation;

            if (_operation == null)
            {
                submitButton.IsEnabled = false;
                errorMessage.Text = Constants.FormatError(Constants.UnexpectedOperation, Constants.NoToken);
            }
            else
            {
                if (_operation.ProviderRequest.WebAccounts.Count > 0)
                {

                    _requestedAccount = _operation.ProviderRequest.WebAccounts[0];
                    userName.Text = _requestedAccount.UserName;
                    
                    WebAccount account = null;
                    account = await WebAccountManager.GetPerUserFromPerAppAccountAsync(_requestedAccount);
                    Debug.Assert(account != null);
                    // TODO: You could return error if the account is not found. 
                    // For the sample we let them add the account again. 
                }

                // This is a hack for demo purposes to show simplest UI 
                if ( _operation.ProviderRequest.ClientRequest.Properties.Keys.Contains("UI"))
                {
                    string ui = _operation.ProviderRequest.ClientRequest.Properties["UI"]; 
                    if (ui == "Simplest")
                    {
                        for (int x = 8; x < 20; x++)
                            RootGrid.RowDefinitions[x].Height = new GridLength(0); 
                    }
                }
            }
        }


        private void RequestTokenPage_Loaded(object sender, RoutedEventArgs e)
        {

            userName.Text = "userName";
            accountId.Text = "accountId";
            perUserPerAppId.Text = "appToUserId"; 
        }


        private IAsyncAction SetViewPropeties(  WebAccount account, Uri appCallbackUri , WebAccountClientViewType viewType )
        {             
            WebAccountClientView view = null;             
            if ( viewType == WebAccountClientViewType.IdAndProperties )
            {
                view = new WebAccountClientView(WebAccountClientViewType.IdAndProperties, appCallbackUri );

            }
            else
            {
                view = new WebAccountClientView(WebAccountClientViewType.IdOnly, appCallbackUri);
            } 

            return Windows.Security.Authentication.Web.Provider.WebAccountManager.SetViewAsync( account , view);
        }

        private async void submitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(userName.Text) &&
                 !string.IsNullOrEmpty(accountId.Text)
               )
            {
                if (_requestedAccount == null)
                {
                    WebAccountScope scope = WebAccountScope.PerUser;
                  
                    if (chkPerUserPerAppScope.IsChecked.Value)
                    {
                        scope = WebAccountScope.PerApplication;
                    }

                    if (string.IsNullOrEmpty(accountId.Text))
                        accountId.Text = userName.Text;

                    var account = await AddWebAccount(userName.Text, accountId.Text, _operation.ProviderRequest.ClientRequest.Properties, 
                        chkPerUserScope.IsChecked.Value,  chkPerUserPerAppScope.IsChecked.Value ,                           
                         perUserPerAppId.Text);

                    if ( chkPerUserPerAppScope.IsChecked.Value )
                    {
                        WebAccountClientViewType viewType = WebAccountClientViewType.IdOnly;
                        if (chkFullView.IsChecked.Value)
                            viewType = WebAccountClientViewType.IdAndProperties;

                        await SetViewPropeties(account, _operation.ProviderRequest.ApplicationCallbackUri, viewType);
                    } 
                     
                    //TODO: is there more 
                    if (account != null)
                        _requestedAccount = account;


                }
                //TODO: when does this happen? 
                else
                    Debug.Assert(false);

                if (_requestedAccount != null)
                {
                    var tokenResponse = await CreateTokenResponse(_requestedAccount);
                    _operation.ProviderResponses.Add(tokenResponse);
                    _operation.CacheExpirationTime = DateTimeOffset.Now.AddDays(1);
                    _operation.ReportCompleted();
                }
                else
                {
                    _operation.ReportError(CreateProviderError(Constants.ErrorCodes.AddAccountFailed));
                }
            }
            else
            {
                ++_failedAttempts;
                if (_failedAttempts < MaxFailedAttempts)
                {
                    errorMessage.Text = Constants.FormatError
                    (Constants.UserNameAccountIdRequired, MaxFailedAttempts - _failedAttempts);
                }
                else
                {
                    _operation.ReportError(CreateProviderError(Constants.ErrorCodes.InvalidPassword));
                }
            }
        }



        private async Task<WebAccount> AddWebAccount ( 
                string userName, string accountId, IDictionary<string, string> properties  , 
                bool perUserScope , bool perAppScope , string perUserPerAppId )
        {
             
            if (string.IsNullOrEmpty(accountId))
            {
                //TODO:                 
                accountId = userName;
            }
            

            ReadOnlyDictionary<string, string> ro = new ReadOnlyDictionary<string, string>( properties);


            WebAccount newAccount = null ;


            if ( perUserScope )
            // if (perUserScope && !perAppScope )
            {
                newAccount = await WebAccountManager.AddWebAccountAsync(accountId, userName, ro, WebAccountScope.PerUser );
            }


            if (perAppScope && perUserScope)
                newAccount = await WebAccountManager.AddWebAccountAsync( "perapp" + accountId, userName, ro, WebAccountScope.PerApplication,
                    newAccount.Id);
            else if (perAppScope)
                newAccount = await WebAccountManager.AddWebAccountAsync(accountId, userName, ro, WebAccountScope.PerApplication, 
                    perUserPerAppId); 
            

            string localPerUserPerAppid= null ; 
            if ( perUserScope && perAppScope /* && !string.IsNullOrEmpty(perUserPerAppId)*/ )
            {
                WebAccount perUserPerAppAccount = await WebAccountManager.GetPerUserFromPerAppAccountAsync(newAccount);
                if (perUserPerAppAccount != null)
                {
                    localPerUserPerAppid = perUserPerAppAccount.Id;
                    newAccount = perUserPerAppAccount;
                } 
            }

            AccountManager.Current.AddAccount(new Account() { Id = accountId, UserName = userName , PerUserPerAppId = localPerUserPerAppid });


            return newAccount; 
        }
        private WebProviderError CreateProviderError (  Constants.ErrorCodes errorCode   )
        {
            string message = Constants.UnknownError ; 
            switch ( errorCode )
            {
                case Constants.ErrorCodes.InvalidPassword:
                    message = Constants.InvalidPassword; 
                    break;
                case Constants.ErrorCodes.Unknown:                    
                    break;

                case Constants.ErrorCodes.AddAccountFailed:
                    message = Constants.AddAccountFailed;
                    break; 

                default:
                    Debug.Assert(false);
                    break; 
            }
            return new WebProviderError( (uint)errorCode , message ); 
        }

        private async Task<WebProviderTokenResponse> CreateTokenResponse(  WebAccount account )
        {
            if (account == null)
                throw new InvalidOperationException(Constants.AccountRequired);
             
            Account act =  AccountManager.Current.Find (account.Id);
            Debug.Assert(act != null);            
             
            if (act.Token == null || string.IsNullOrEmpty(act.Token.Value))
            {  
                string token = string.Format(Guid.NewGuid().ToString());
                act.Token = new Token() { Value = token, ExpirationDate = DateTime.Now.AddDays(1) };
            }
            else
            {
                act.Token.ExpirationDate =  DateTime.Now.AddDays(1) ;
            }                     
            AccountManager.Current.UpdateAccount(act); 
           
            return new WebProviderTokenResponse( new WebTokenResponse( act.Token.Value , account )); 
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {

        }

     
    }
}
