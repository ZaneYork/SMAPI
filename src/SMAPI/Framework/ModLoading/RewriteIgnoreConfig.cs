using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewModdingAPI.SMAPI.Framework.ModLoading
{
    class RewriteIgnoreConfig
    {
        public Dictionary<string, string> Ignore { get; set; } = new Dictionary<string, string>();
    }
}
