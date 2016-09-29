using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Storage;

namespace Saso.SampleProviders.Helpers
{

    public class Account
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string PerUserPerAppId { get; set; } 

        public Token Token { get; set ; } 
    }
    public class Token 
    {       
        public string Value  { get; set; }
        public DateTime ExpirationDate { get; set; } 
    } 



    public class AccountManager 
    {
        // TODO: this is a super naive implementation for local usage only ;) 
        // performance, security, etc.were not considered. It is a simple sample. 
        /// <summary>
        ///  
        /// </summary>
        private static List<Account> _accounts;
        private static bool _isLoaded = false;
        private static AccountManager _instance = null ; 

        public static AccountManager Current
        {
            get
            {
                if ( _instance == null )
                {
                    _instance = new AccountManager();
                    _instance.LoadAccounts(); 
                }
                return _instance; 

            }
        }

        private AccountManager()
        {
            _accounts = new List<Account>();            
        }
      
       
        public bool LoadAccounts ()
        {
            if (_isLoaded)
                return true; 
            try
            {                 
                lock (_instance)
                {
                    var fileTask = Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(Constants.AccountsFileName).AsTask<StorageFile>(); 
                    fileTask.Wait(); 
                    if ( fileTask.IsCompleted && !fileTask.IsFaulted )
                    {
                        var file = fileTask.Result;

                        var readTask = FileIO.ReadTextAsync(file).AsTask<string>();
                        readTask.Wait();
                        var accountsAsString = readTask.Result; 
                        var accounts = JsonConvert.DeserializeObject<List<Account>>(accountsAsString);
                        if (accounts != null && accounts.Count > 0)
                        {
                            _accounts = accounts;
                            _isLoaded = true;
                        }
                    }
                }                 
            }
            catch (Exception ex)
            {
                Trace.LogException(ex);  
            }
            return _isLoaded ; 
        }

        private void SaveAccounts()
        {
            LoadAccounts(); 
            try
            {
                lock ( _instance )
                {  
                    string accountsAsString = JsonConvert.SerializeObject(_accounts);
                    var fileTask = Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(Constants.AccountsFileName,
                        CreationCollisionOption.ReplaceExisting).AsTask<StorageFile>(); 

                    fileTask.Wait();
                    Windows.Storage.StorageFile file = fileTask.Result;
                    var writeTask = FileIO.WriteTextAsync(file, accountsAsString).AsTask();
                    writeTask.Wait();
                }
            } 
            catch (Exception ex)
            {
                Trace.LogException(ex);
                // let this trickle.. 
                throw ex;  
            }
        }

        
        public bool HasAccounts 
        {
            get {
                LoadAccounts(); 
                return _accounts.Count > 0 ;                
            }
        }

        public void AddAccount ( Account newAccount )
        {
            LoadAccounts(); 
            _accounts.Add(newAccount);
            SaveAccounts(); 
        }

        public void UpdateAccount ( Account account )
        {
            /// We do nothing since we assume it is all same instances in memory 
            /// We do ahve to save to disk.. 
              SaveAccounts(); 
        }

        public void RemoveAccount ( string accountId )
        {
            LoadAccounts();
            Account account = null; 
            foreach ( Account a in _accounts )
            {
                if ( a.Id == accountId )
                {
                    account = a; 
                }
            }

            if (account != null)
            {
                _accounts.Remove(account);
                SaveAccounts();
            }             
        }

        public Account Find ( string id )
        {
            LoadAccounts(); 
            foreach (Account a in _accounts)
            {
                if (a.Id == id)
                    return a ;
            }
            throw new InvalidOperationException(Constants.FormatError ( Constants.UnexpectedOperation, Constants.AccountRequired )) ;   
        }

        public Token GetToken ( WebAccount account )
        {
            LoadAccounts();
            foreach ( Account a in _accounts )
            {
                if (a.Id == account.Id )
                    return a.Token; 
            }
            return null;   
        }                
    }


#if FALSE  

     public class AccountManager 
    {
        // TODO: this is a super naive implementation for local usage only ;) 
        // performance, security, etc.were not considered. It is a simple sample. 
        /// <summary>
        ///  
        /// </summary>
        private static List<Account> _accounts;
        private static bool _isLoaded = false;
        static AccountManager()
        {
            _accounts = new List<Account>();
            //LoadAccounts(); 
             
            //TODO: not waiting to load accounts as that would deadlock here. 
        }
      
       
        public async static Task<bool> LoadAccounts ()
        {
            if (_isLoaded)
                return true; 
            try
            { 
                string accountsAsString = "";
                Windows.Storage.StorageFile file = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(Constants.AccountsFileName);
                accountsAsString = await FileIO.ReadTextAsync(file);                 
                var accounts = JsonConvert.DeserializeObject<List<Account>>(accountsAsString);
                if (accounts != null && accounts.Count > 0)
                {
                    _accounts = accounts;
                    _isLoaded = true;
                } 
#if DEBUG
                else
                    Debug.Assert(false);
#endif

                return true; 
            }
            catch (Exception ex)
            {
                Trace.LogException(ex);  
            }
            return false; 
        }

        private async static Task SaveAccounts()
        {
            await LoadAccounts(); 
            try
            {
                string accountsAsString = JsonConvert.SerializeObject(_accounts);
                Windows.Storage.StorageFile file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(Constants.AccountsFileName, 
                    CreationCollisionOption.ReplaceExisting); 
                 await FileIO.WriteTextAsync(file, accountsAsString);
            } 
            catch (Exception ex)
            {
                Trace.LogException(ex);
                // let this trickle.. 
                throw ex;  
            }
        }

        
        public static bool HasAccounts 
        {
            get {

                return _accounts.Count > 0 ; 
               
            }
        }

        public async static Task AddAccountAsync ( Account newAccount )
        {
            await LoadAccounts(); 
            _accounts.Add(newAccount);
            await SaveAccounts(); 
        }

        public static Task UpdateAccountAsync ( Account account )
        {
            /// We do nothing since we assume it is all same instances in memory 
            /// We do ahve to save to disk.. 
            return SaveAccounts(); 
        }

        public static async Task RemoveAccountAsync ( string accountId )
        {
            await LoadAccounts();
            Account account = null; 
            foreach ( Account a in _accounts )
            {
                if ( a.Id == accountId )
                {
                    account = a; 
                }
            }

            if (account != null)
            {
                _accounts.Remove(account);
                await SaveAccounts();
            }
             
        }

        public async static Task<Account> FindAsync  ( string id )
        {
            await LoadAccounts(); 
            foreach (Account a in _accounts)
            {
                if (a.Id == id)
                    return a ;
            }
            throw new InvalidOperationException(Constants.FormatError ( Constants.UnexpectedOperation, Constants.AccountRequired )) ;   
        }

        public static async Task<Token> GetTokenAsync ( WebAccount account )
        {
            await LoadAccounts();
            foreach ( Account a in _accounts )
            {
                if (a.Id == account.Id )
                    return a.Token; 
            }
            return null;   
        }        
        
    }
#endif 
}
