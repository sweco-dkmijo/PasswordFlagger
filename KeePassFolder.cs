using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PasswordFlagger
{
    public class KeePassFolder : IStatusRepporter
    {
        public KeePassFolder[] Children { get; set; }
        public KeePassCredential[] Credentials { get; set; }
        public string Name { get; set; }

        public event EventHandler ProcessStarted;

        public ICollection<IStatusObject> GetStatusObjects()
        {
            List<IStatusObject> statusObjects = new List<IStatusObject>();
            foreach (KeePassCredential credential in Credentials)
                statusObjects.Add(credential);
            foreach (KeePassFolder folder in Children)
                statusObjects.AddRange(folder.GetStatusObjects());
            return statusObjects;
        }

        public override string ToString()
        {
            string str = "\n|___" + Name + "";

            foreach (KeePassCredential credential in Credentials)
            {
                str += "\n|  -Crederntial: " + credential.ToString() + "";
            }

            foreach (KeePassFolder folder in Children)
            {
                str += "\n|  " + folder.ToString().Replace("\n", "\n|  ");
            }
            return str;
        }

        internal async Task<string[]> GetCredentialsRecursive()
        {
            ProcessStarted?.Invoke(this, new EventArgs());

            List<Task<string>> credentialTasks = new List<Task<string>>();
            foreach (KeePassCredential credential in Credentials)
                credentialTasks.Add(credential.GetPassword());

            await Task.Yield();
            List<Task<string[]>> credentialsFromFolderTasks = new List<Task<string[]>>();
            foreach (KeePassFolder folder in Children)
                credentialsFromFolderTasks.Add(folder.GetCredentialsRecursive());

            List<string> credentials = new List<string>();
            foreach (Task<string> cred in credentialTasks)
                credentials.Add(await cred);
            foreach (Task<string[]> creds in credentialsFromFolderTasks)
                credentials.AddRange(await creds);

            credentials.RemoveAll(x => x == null);

            return credentials.ToArray();
        }
    }
}