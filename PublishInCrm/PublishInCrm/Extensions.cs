using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CemYabansu.PublishInCrm
{
    public static class Extensions
    {
        public static T GetValue<T>(this Dictionary<string, T> dictionary, string key)
        {
            string[] keyVariations = new string[] {
                key,
                key.ToLower(),
                key.ToLowerInvariant(),
                key.ToUpper(),
                key.ToUpperInvariant()
            };

            foreach (string keyVariation in keyVariations)
            {
                if (dictionary.ContainsKey(keyVariation))
                {
                    return dictionary[keyVariation];
                }
            }

            return default(T);
        }
    }
}
