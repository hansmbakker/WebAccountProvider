using Saso.SampleProviders.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class ManageAccountPage : Page
    {
        WebAccount account;
        IWebAccountProviderOperation operation;
        CommonBaseOperation commonOperation;
        public ManageAccountPage()
        {
            this.InitializeComponent();
            this.Loaded += ManageAccountPage_Loaded;
        }


        private void ManageAccountPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (commonOperation.NeedsAccount && commonOperation.Account == null)
                commonOperation.ReportCompleted();

            userName.Text = commonOperation.Account.UserName;
            accountId.Text = commonOperation.Account.Id;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            operation = e.Parameter as IWebAccountProviderOperation;
            Debug.Assert(operation != null);
            commonOperation = new CommonBaseOperation(operation);

        }
        private async void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            await WebAccountManager.DeleteWebAccountAsync(commonOperation.Account);
            commonOperation.ReportCompleted();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {

            commonOperation.ReportCompleted();
            Frame frame = Window.Current.Content as Frame;
            if (frame != null && frame.CanGoBack)
            {
                frame.GoBack();
            }
            else
            {
                Windows.UI.Popups.MessageDialog dlg = new Windows.UI.Popups.MessageDialog("To exit, please close window on top right");
                dlg.ShowAsync();
            }
        }

        private void updateButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Implement update functionality 
            commonOperation.ReportCompleted();
        }

        private void retrieveCookiesButton_Click(object sender, RoutedEventArgs e)
        {
            WebAccountProviderRetrieveCookiesOperation retrieve = operation as WebAccountProviderRetrieveCookiesOperation;
            StringBuilder builder = new StringBuilder();
            foreach (var cookie in retrieve.Cookies)
            {
                builder.Append($"Name: {cookie.Name}, Domain: {cookie.Domain}, Value={cookie.Value}\n\r");
            }
            cookies.Text = builder.ToString();
        }

    }
}
