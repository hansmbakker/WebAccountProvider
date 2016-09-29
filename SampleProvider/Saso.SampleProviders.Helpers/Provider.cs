using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saso.SampleProviders.Helpers
{
    public class Provider
    {
        public Provider (string name , string id )
        {
            Id = id;
            Name = name; 

            DomainClientId = String.Empty;
            ClientId = string.Empty;
            Scope = String.Empty;
            Authority = string.Empty; 
        }
        public string Id { get; internal set;  }
        public string DomainClientId { get; internal set;  }

        public string ClientId { get; internal set;  }
        public string Scope { get; internal set;  }

        public string Authority { get; internal set; }

        public string Name { get; internal set;  }


        public static List<Provider> All
        {
            get
            {
                return _providers; 
            }
        }



        static Provider ()
        {

            _providers = new List<Provider>()
                {
                    new Provider ( "Microsoft Account" ,  "https://login.live.com" ) { ClientId = "none" , Scope ="service::spaces.live.com::mbi_ssl" },
                    new Provider ( "Active Directory" , "https://login.windows.net") { DomainClientId = "1443cfbe-782c-4972-9350-471a3ea95acf" , Scope ="" },
                    new Provider (  Constants.ProviderName,  Constants.ProviderId  )  { ClientId="SampleProviderApp", Scope="all" }  
                }; 
        }
        private static List<Provider> _providers = null; 
    }
     

    

     

    //const string MicrosoftProviderId = "https://login.microsoft.com";
    //const string AADAuthority = "organizations";

    //const string ContosoProviderId = "https://www.contoso.com";

    //const string StoredAccountIdKey = "accountId";
    //const string StoredProviderIdKey = "providerid";
}
