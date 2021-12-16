using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace PasswordFlagger
{
    public class KeePass
    {
        private KeePassFolder rootFolder;
        private static List<string> entryIdsList;
        private static object lockObj = new object();

        private static string bearer_token;
        private static int token_expires_in;
        private static DateTime tokenFetched_UTCtimestamp;
        public KeePassFolder RootFolder
        {
            get
            {
                if (rootFolder == null)
                    rootFolder = GetRootFolder();
                return rootFolder;
            }
        }


        private static string baseUrl
        {
            get
            {
                lock (lockObj)
                {
                    return ConfigurationManager.AppSettings["amtpwd_url"];
                }
            }
        }

        public static string Bearer_token
        {
            get
            {
                lock (lockObj)
                    return bearer_token;
            }
            set
            {
                lock (lockObj)
                    bearer_token = value;
            }
        }
        private static int Token_expires_in
        {
            get
            {
                lock (lockObj)
                    return token_expires_in;
            }
            set
            {
                lock (lockObj)
                    token_expires_in = value;
            }
        }
        private static DateTime TokenFetched_UTCtimestamp
        {
            get
            {
                lock (lockObj)
                    return tokenFetched_UTCtimestamp;
            }
            set
            {
                lock (lockObj)
                    tokenFetched_UTCtimestamp = value;
            }
        }

        private KeePassFolder GetRootFolder()
        {
            string foldersEndpoint = baseUrl + "/api/v5/rest/folders";

            HttpWebRequest request = WebRequest.Create(foldersEndpoint) as HttpWebRequest;
            request.Method = "GET";
            request.ContentType = "application/json";
            if (Bearer_token == null)
                GetNewAuthToken();
            request.Headers.Add("Authorization", Bearer_token);

            KeePassFolder folder;
            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    string jsonResponseText = streamReader.ReadToEnd();
                    folder = JsonConvert.DeserializeObject<KeePassFolder>(jsonResponseText);
                }
            }

            return folder;
        }

        internal string[] GetCredentialsArray()
        {
            string[] credentials = RootFolder.GetCredentialsRecursive().Result;
            return credentials;
        }

        private static void GetNewAuthToken()
        {
            string url = baseUrl + ConfigurationManager.AppSettings["amtpwd_Auth"];

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
            outgoingQueryString.Add("grant_type", "password");
            outgoingQueryString.Add("username", ConfigurationManager.AppSettings["keepass_username"]);
            outgoingQueryString.Add("password", ConfigurationManager.AppSettings["keepass_password"]);
            byte[] postBytes = new ASCIIEncoding().GetBytes(outgoingQueryString.ToString());

            Stream postStream = request.GetRequestStream();
            postStream.Write(postBytes, 0, postBytes.Length);
            postStream.Flush();
            postStream.Close();

            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    string jsonResponseText = streamReader.ReadToEnd();
                    dynamic res = JsonConvert.DeserializeObject(jsonResponseText);

                    Bearer_token = "bearer " + (string)res.access_token;
                    Token_expires_in = (int)res.expires_in;
                    TokenFetched_UTCtimestamp = DateTime.UtcNow;
                }
            }
        }

        public static List<string> GetAllCredentials()
        {
            GetAndStoreAllEntryIds();
            var list = new List<string>();

            foreach (var entryId in entryIdsList){
                string pass = GetCredential(entryId);
                pass = pass.Trim();
                if(!string.IsNullOrEmpty(pass))
                    list.Add(pass);
            }

            return list;
        }

        private static bool CheckIfAuthTokenIsValid()
        {
            if (Bearer_token == null)
                return false;

            //Check if token will expire within half an hour.
            int tokenExpireWithHalfHourBuffer = Token_expires_in + 1800;
            var utcNowMinusSecondsForTokenExpire = DateTime.UtcNow.AddSeconds(-tokenExpireWithHalfHourBuffer);
            return utcNowMinusSecondsForTokenExpire < TokenFetched_UTCtimestamp;
        }

        private static void GetAndStoreAllEntryIds()
        {
            if (CheckIfAuthTokenIsValid() == false){
                GetNewAuthToken();
            }

            string folderGuid1 = ConfigurationManager.AppSettings["amtpwd_parentfolderid_drift"];
            string folderGuid2 = ConfigurationManager.AppSettings["amtpwd_parentfolderid_test"];

            string url1 = baseUrl + string.Format(ConfigurationManager.AppSettings["amtpwd_folders"], folderGuid1);
            string url2 = baseUrl + string.Format(ConfigurationManager.AppSettings["amtpwd_folders"], folderGuid2);
            string[] uri = new string[] { url1, url2 };
            entryIdsList = new List<string>();

            foreach (var url in uri)
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Headers.Add("Authorization", Bearer_token);

                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        dynamic jsonResponseText = streamReader.ReadToEnd();
                        ParentFolderData parentFolder = JsonConvert.DeserializeObject<ParentFolderData>(jsonResponseText);

                        foreach (ChildFolderData c in parentFolder.Children)
                        {
                            foreach (Entry e in c.Credentials)
                            {
                                if(!entryIdsList.Contains(e.Id))
                                    entryIdsList.Add(e.Id);
                            }
                        }
                    }
                }
            }
        }

        private static string GetCredential(string keePassentryId)
        {
            if (CheckIfAuthTokenIsValid() == false)
                GetNewAuthToken();

            string url = baseUrl + ConfigurationManager.AppSettings["amtpwd_password-entries"];
            string urlFormatted = string.Format(url, keePassentryId);

            var request = (HttpWebRequest)WebRequest.Create(urlFormatted);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Add("Authorization", Bearer_token);

            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    string resPassword = streamReader.ReadToEnd();
                    resPassword = resPassword.Replace("\"", "");
                    return resPassword;
                }
            }
        }

        private class ParentFolderData
        {
            public List<ChildFolderData> Children { get; set; } //Children = subfolders with credentials
        }

        //For parsing json
        private class ChildFolderData
        {
            public List<Entry> Credentials { get; set; }
        }
        //For parsing json And caching     
        private class Entry
        {
            public Entry(string username, string id)
            {
                this.Id = id;
                this.Username = username;
            }

            public string Name { get; set; }
            public string Id { get; set; }
            public string Username { get; set; }
        }
    }
}