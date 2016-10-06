# WebAccountProvider Sample

[09/29/2016. Early work in progress]

In today's connected world where we constantly connect to great online services (like Facebook, Outlook.com, OneDrive, etc.), authentication is a frequent, yet necessary day-to-day task.  

There is some god infrastructure in-place (such as OAuth, Single Sign-On) to decrease the recurrent overhead for users who need to always be authenticated against a service.  Windows 8 introduced 
[WebAuthenticationBroker](https://msdn.microsoft.com/en-us/library/windows/apps/windows.security.authentication.web.webauthenticationbroker.aspx) to ease single sign-on for experiences on Windows. 

Windows 10 introduced [WebAccountManager](https://msdn.microsoft.com/en-us/library/windows/apps/windows.security.authentication.web.provider.webaccountmanager.aspx), which offers more flexible infrastructure around managing web accounts with system-wide integration (in Settings -> Email & App accounts), as well as communication between online properties (e.g. outlook.com or facebook.com) and their app counterparts (e.g. the Mail or Facbook App).  

Recently, I needed to share state between a website and an app, so I looked into the feature. I found the feature incredibly powerful, yet very undocumented.  I am not the ultimate expert for it*, but the feature was not that hard to code, so i hope the sample code included in this repo can bootstrap anyone looking to provide a single sign-on experience with WebAccountManager. 


*For more context on WebAccountManager and advise from real experts, here are the resources I know of:

- [Build 2015 Single Sign-on talk](https://channel9.msdn.com/Events/Build/2015/2-709)


- Official [WebAccountManager](https://github.com/Microsoft/Windows-universal-samples/tree/master/Samples/WebAccountManagement) sample, which shows you how to consume providers, but it misses the intricate how to write a [WebAccountProvider](https://msdn.microsoft.com/en-us/library/windows/apps/windows.security.credentials.webaccountprovider.aspx), which is what this sample demonstrates. 



##Running the sample
To run the client sample:
1. Open Saso.SampleProvider.sln 
2. Compile all projects 
3. Deploy Saso.SampleProvider and Saso.SampleProvider.Client 
4. Run the .Client app 
5. Click the "Invoke Login UI" button.  
This will show you the login UI from the provider (it is in Saso.SampleProvider.SampleUI.RequestTokenPage.xaml). 
Note: if you see the XAML, you will see more UI elements, the sample client app passes a parameter called "simpleUI" that hides options in the login page. Read all the implementation details below if you want to try the full UI.  
   
6. Once you have signed in once in the client, you can click on the "Retrieve Token Silently" button from the client app.  If this was a different app, since the account provider logged in at system level, the second app could retrieve the token silently even without logging in.
7. Finally, if you want to try calling from a website into the provider, navigate to https://paxwaptest.azurewebsites.net
When you arrive, you should see no cookies. 
Click submit to call into the provider. This shall create cookies for the site. 
Refresh the site and you should see the cookies now! You have the system's AdvertiserId. 
Notes: 
-To pass information from the website into the provider, just include it in the query string.
-This is a test server, pending when you read this, it might be taken down. If you can't find it, you will need to use your own website, which means reconfiguring the provider in the _package.appxmanifest_ declaration, the _Constants.cs_ file for provider, _MainPage.xaml.cs_ for the client, and the _default.html_ file for the website. Global search in Visual studio is your friend.       

Those are the steps, let's now walk through how it works. 

## Implementation details 

To leverage the WebAccountManager infrastructure, you just need to follow these steps: : 

1. Register your app  as a Web Account Provider.   
   You can do this in your apps package.appxmanifest. The entry point is a background task that will handle most silent operations.  
   <Extensions>
       <uap:Extension Category="windows.webAccountProvider">
             <uap:WebAccountProvider Url="https://paxwaptest.azurewebsites.net/" BackgroundEntryPoint="Saso.SampleProvider.BackgroundService.MainTask" />
           </uap:Extension>     
   </Extensions>

2. Implement the background task service to handle the different kinds of [WebAccountProviderOperationKind](https://msdn.microsoft.com/en-us/library/windows/apps/windows.security.authentication.web.provider.webaccountprovideroperationkind.aspx) actions. The critical ones for the background task are RequestToken, GetTokenSilently, DeleteAccount, and RetrieveCookies.  The other operations such as AddAccount are not handled in the background task but in the app itself, via our next step.  

   The background service has few operations, but it should also manage your user account's lifecycle (such as authentication, retrieving, storing and securing tokens, etc.). My sample background task is Saso.SampleProvider.BackgroundService.MainTask and it does a naive, insecure and slightly incomplete implementation of account lifecycle management, but it does show all the different parameters and way to interact with WebAccountManager infrastructure as well as the right order for operations that are sequential - like adding an account into the system.  
   ​

3. Within your app, listen for the App's OnActivated event and handle the ActivationKind.WebAccountProvider scenario.  This is complementary to prior step, and here you should focus on the ones that need to show user interface -- such as AddAccount, ManageAccount-. 

Those are (in an over simplified nutshell) the steps required to implement an account provider.  



The next item to explain from our sample is how to leverage the WebAccountManager infrastructure to call into a provider.  There are many ways to call into a provider. Here i will call three:

1. Saso.SampleProvider.Client shows how to leverage WebAccountManager to locate a provider and call it showing UI and/or silently.  Try the sample and play with the infrastructure to see how WAM is launching the provider app (when UI is needed) or calling into the background service. 
2. You can also call into a WAP provider from within your app itself.  Imagine you are the Mail app, and need integration w/ live.com, within your app, you can use WAM infrastructure to handle your authentication, or pushing cookies to the web. 
3. You can also call from the web (IE and Edge) using the tbauth:// protocol handler. By just invoking it with your provider's id, the background service is called and you can pass information from the web into your provider using the query string parameters for the call.  You can also return information by pushing cookies to the web. 


TIP: The easiest way to see it all in action is to step through the debugger. Here is one (of many ways) to do it: 


1. Right-click on Saso.Provider project and select _Properties_ menu item.  
2. Click on the _Debug_ tab
3. Check the _Do not launch, but debug my code when it starts_ option
4. Press _F5_ or _Debug->Start Debugging_  
5. Set break points in Saso.SampleProvider's App.xaml.cs constructor and the _Run_ method in Saso.SampleProvider.BackgroundService.MainTask 

