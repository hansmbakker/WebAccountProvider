using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Provider;
using Windows.Security.Credentials;

namespace Saso.SampleProviders.Helpers
{
    public class CommonBaseOperation
    {
        IWebAccountProviderOperation baseOperation; 

        public CommonBaseOperation ( IWebAccountProviderOperation baseOperationParam  )
        {
            this.baseOperation = baseOperationParam;
        }
        public WebAccount Account
        {
            get
            {
                switch (baseOperation.Kind)
                {
                    case WebAccountProviderOperationKind.ManageAccount:
                        return ((WebAccountProviderManageAccountOperation)baseOperation).WebAccount;
                        break;
                    case WebAccountProviderOperationKind.DeleteAccount:
                        return ((WebAccountProviderDeleteAccountOperation)baseOperation).WebAccount;
                        break;
                    // case WebAccountProviderOperationKind.RetrieveCookies :
                    //  return ((WebAccountProviderRetrieveCookiesOperation)operation).;
                    default:
                        Debug.Assert(false);
                        break; 
                }
                return null;
            }
        }

        public void ReportCompleted ()
        {
            switch (baseOperation.Kind)
            {
                case WebAccountProviderOperationKind.ManageAccount:
                    ((WebAccountProviderManageAccountOperation)baseOperation).ReportCompleted();
                    break;
                case WebAccountProviderOperationKind.DeleteAccount:
                     ((WebAccountProviderDeleteAccountOperation)baseOperation).ReportCompleted ();
                    break;
                case WebAccountProviderOperationKind.SignOutAccount:
                    ((WebAccountProviderSignOutAccountOperation)baseOperation).ReportCompleted();
                    break; 
                default:
                    Debug.Assert(false);
                    break; 
            }
        }
         
        public void RetrieveCookies  ()
        {
           
        }
        public bool NeedsAccount
        {
            get
            {
                return baseOperation.Kind == WebAccountProviderOperationKind.DeleteAccount ||
                       baseOperation.Kind == WebAccountProviderOperationKind.ManageAccount ||
                       baseOperation.Kind == WebAccountProviderOperationKind.SignOutAccount 
                       ;
            }
        }

        public void Delete ( )
        {
                            
            AccountManager.Current.RemoveAccount(Account.Id);            
            WebAccountManager.DeleteWebAccountAsync(Account).AsTask().Wait();

        }

        public void SignOut ()
        {
            Account.SignOutAsync().AsTask().Wait(); 
        }
    }
}
