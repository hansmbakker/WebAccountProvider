using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

        private void DebugPrint (string s)
        {

        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            var provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(WebProviderId);

            WebTokenRequestPromptType tokenType = WebTokenRequestPromptType.Default;
            WebTokenRequest webTokenRequest = new WebTokenRequest(
                                                    provider,
                                                    "all",
                                                    "", tokenType);
            var  webTokenRequestResult = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(webTokenRequest);


            switch (webTokenRequestResult.ResponseStatus)
            {
                case WebTokenRequestStatus.Success:
                    DebugPrint("Web Token retrieved successfully");                      
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
    }
}
