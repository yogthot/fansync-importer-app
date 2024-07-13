using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanSync.Ext
{
    public static class DictionaryExtensions
    {
        public static V GetDefault<K, V>(this Dictionary<K, V> dict, K key, V def = default)
        {
            if (dict.TryGetValue(key, out V value))
                return value;
            else
                return def;
        }

        public static bool SetOrRemove<K, V>(this Dictionary<K, V> dict, K key, V value)
        {
            if (value == null)
            {
                dict.Remove(key);
                return false;
            }
            else
            {
                dict[key] = value;
                return true;
            }
        }
        public static bool SetOrRemove<K>(this Dictionary<K, string> dict, K key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                dict.Remove(key);
                return false;
            }
            else
            {
                dict[key] = value;
                return true;
            }
        }
    }
}
