using Saso.SampleProviders.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
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

namespace Saso.SampleProvider
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }


        public static void RegisterBackgroundTask()
        {
            var bgTaskName = "DefaultSignInAccountChangeTask";

            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == bgTaskName)
                {
                    task.Value.Unregister(true);
                }
            }

            var builder = new BackgroundTaskBuilder();
            builder.Name = bgTaskName;
            builder.TaskEntryPoint = "Background.DefaultSignInAccountChangeTask";
            builder.SetTrigger(new SystemTrigger(SystemTriggerType.DefaultSignInAccountChange, false));
            BackgroundTaskRegistration bgTaskReg = builder.Register();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }


        protected override void OnActivated(Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.WebAccountProvider)
            {
                Trace.Log("Activated with WebAccountProvider request");
                var e = args as WebAccountProviderActivatedEventArgs;            
                Trace.Log( string.Format( "Activation Type = {0}", e.GetType().ToString()));
                OnWebAccountProvider(e);
            }
        }

        private void OnWebAccountProvider(WebAccountProviderActivatedEventArgs args)
        {
            try
            {
                Frame rootFrame = Window.Current.Content as Frame;

                // Do not repeat app initialization when the Window already has content,
                // just ensure that the window is active
                if (rootFrame == null)
                {
                    // Create a Frame to act as the navigation context and navigate to the first page
                    rootFrame = new Frame();
                    // Set the default language
                    rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];
                    rootFrame.NavigationFailed += OnNavigationFailed;

                    if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                    {
                        //TODO: Load state from previously suspended application
                    }
                    // Place the frame in the current Window
                    Window.Current.Content = rootFrame;
                }

                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    Trace.Log ("DBG: rootFrame.Content is null");
                    Window.Current.Content = rootFrame;
                }

                var baseOperation = args.Operation;

                switch (baseOperation.Kind)
                {
                    case WebAccountProviderOperationKind.RequestToken:
                        {
                            Trace.Log ("DBG: operation.Option=RequestToken.");
                            var requestOperation = baseOperation as WebAccountProviderRequestTokenOperation;
                            rootFrame.Navigate(typeof(SampleUI.RequestTokenPage), requestOperation);
                        }
                        break;

                    case WebAccountProviderOperationKind.AddAccount:
                        {
                            //the UI required to trigger this will only be available in the next Windows release
                            Trace.Log ("DBG: operation.Option=AddAccount.");
                            var addAccountoperation = baseOperation as WebAccountProviderAddAccountOperation;
                            rootFrame.Navigate(typeof(SampleUI.AddAccountPage), addAccountoperation);
                        }
                        break;

                    case WebAccountProviderOperationKind.DeleteAccount:
                    case WebAccountProviderOperationKind.ManageAccount:
                    case WebAccountProviderOperationKind.RetrieveCookies:
                        {                                                         
                            //var manageAccountoperation = baseOperation as WebAccountProviderManageAccountOperation;                             
                            //WebAccount accountToManage = manageAccountoperation.WebAccount;
                           
                            rootFrame.Navigate(typeof(SampleUI.ManageAccountPage), baseOperation);
                        }
                        break;                    
                    default:
                        Trace.Error("DBG: unkown UI operation kind");
                        base.OnActivated(args);
                        break;
                }

                // Ensure the current window is active
                Window.Current.Activate();
            }
            catch (Exception e)
            {
                Trace.LogException (e);
                throw e;
            }
        }
    
    }
}
