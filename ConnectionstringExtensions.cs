using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PasswordFlagger
{
    public static class ConnectionstringExtensions
    {
        public static Dictionary<string, string> ConvertConnectionstringToDictionary(string connectionstring)
        {
            var matches = Regex.Matches(connectionstring, @"\s*(?<key>[^;=]+)\s*=\s*((?<value>[^'][^;]*)|'(?<value>[^']*)')").Cast<Match>().ToList();
            var dir = new Dictionary<string, string>();

            foreach (Match m in matches)
            {
                var key = m.Groups["key"].Value;
                if (!dir.ContainsKey(key)) {
                    dir.Add(key, m.Groups["value"].Value);
                } 
            }

            return dir;
        }
    }
}
