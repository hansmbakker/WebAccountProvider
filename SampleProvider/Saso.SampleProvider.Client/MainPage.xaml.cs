using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Saso.SampleProvider.Client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string WebProviderId = "https://paxwaptest.azurewebsites.net";
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void UpdateResults (string s)
        {
            this.ResultsTextBox.Text += s; 
        }
        private void ClearResults()  
        { 
            this.ResultsTextBox.Text = "" ; 
        } 
        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            await RetrieveToken(true); 
        }

        private async void RetrieveSilently_Click(object sender, RoutedEventArgs e)
        {
            RetrieveToken(false); 
        }

        private async Task<bool> RetrieveToken (bool showUi)
        {
            ClearResults(); 
            try
            {
                var provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(WebProviderId);
                if (provider == null)
                {
                    UpdateResults($"Provider for {WebProviderId} is not installed. Please ensure it has been deployed");
                    return false;
                } 

                WebTokenRequestPromptType tokenType = WebTokenRequestPromptType.Default;
                WebTokenRequest webTokenRequest = new WebTokenRequest(
                                                        provider,
                                                        "all",
                                                        "", tokenType);

                webTokenRequest.Properties.Add("UI", "Simplest"); 

                WebTokenRequestResult webTokenRequestResult = null;
                if (showUi)
                    webTokenRequestResult= await WebAuthenticationCoreManager.RequestTokenAsync(webTokenRequest);
                else
                    webTokenRequestResult = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(webTokenRequest);


                switch (webTokenRequestResult.ResponseStatus)
                {
                    case WebTokenRequestStatus.Success:
                        UpdateResults("Web Token retrieved successfully\n");
                        int count = 0; 
                        foreach (var result in webTokenRequestResult.ResponseData)
                        {
                            UpdateResults($"Token {count++} = {result.Token} \n");
                        } 
                        break;
                    case WebTokenRequestStatus.UserCancel:
                        // Handle user cancel by resuming pre-login screen 
                        UpdateResults("User cancelled the authentication");
                        break;

                    case WebTokenRequestStatus.AccountProviderNotAvailable:

                        // fall back to WebAuthenticationBroker  
                        UpdateResults("WebTokenRequestStatus.AccountProviderNotAvailable");
                        break;

                    case WebTokenRequestStatus.ProviderError:
                        UpdateResults(string.Format("Error: 0x{0:X08} message: {1}", webTokenRequestResult.ResponseError.ErrorCode, webTokenRequestResult.ResponseError.ErrorMessage));
                        break;

                    case WebTokenRequestStatus.UserInteractionRequired:
                        UpdateResults("WebTokenRequestStatus.UserInteractionRequired");

                        if (webTokenRequestResult.ResponseError != null)
                        {
                            UpdateResults(string.Format("Error: 0x{0:X08} message: {1}", webTokenRequestResult.ResponseError.ErrorCode, webTokenRequestResult.ResponseError.ErrorMessage));
                        }
                        break;

                    default:
                        UpdateResults("Unhandled webTokenRequestResult.ResponseStatus: " + webTokenRequestResult.ResponseStatus);
                        break;
                }
            } catch (Exception ex )
            {

            }
            return false;  
        }
    }
}
