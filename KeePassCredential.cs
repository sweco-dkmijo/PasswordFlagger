using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace PasswordFlagger
{
    public class KeePassCredential : IStatusObject
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public bool IsCompleted { get; set; }

        public event EventHandler ObjectCompleted;

        public string GetName()
        {
            return Name;
        }

        public async Task<string> GetPassword()
        {
            string baseUrl = ConfigurationManager.AppSettings["amtpwd_url"];

            string url = baseUrl + $"/api/v6/rest/Entries/{Id}/password";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Add("Authorization", KeePass.Bearer_token);

            using (WebResponse response = await request.GetResponseAsync())
            {
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    string resPassword = streamReader.ReadToEnd();
                    resPassword = resPassword.Replace("\"", "");
                    IsCompleted = true;
                    ObjectCompleted.Invoke(this, new EventArgs());
                    if (resPassword.Equals(string.Empty))
                        return null;
                    return resPassword;
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}