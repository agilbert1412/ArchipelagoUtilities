using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaitoKid.ArchipelagoUtilities.AssetDownloader
{
    public class NameCleaner
    {
        public string CleanName(string name)
        {
            if (name == null)
            {
                return string.Empty;
            }

            var charsToIgnore = new string[] { " ", "_", ":", "'" };
            name = name.ToLower();
            foreach (var charToIgnore in charsToIgnore)
            {
                name = name.Replace(charToIgnore, "");
            }

            return name;
        }
    }
}
