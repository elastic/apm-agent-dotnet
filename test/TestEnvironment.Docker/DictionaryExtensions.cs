using System.Collections.Generic;
using System.Linq;

namespace TestEnvironment.Docker
{
    public static class DictionaryExtensions
    {
        public static IDictionary<T1, T2> MergeDictionaries<T1, T2>(this IDictionary<T1, T2> dictionary, IDictionary<T1, T2> other)
        {
            if (other is null)
            {
                return dictionary;
            }

            var nonExistentEnvironmentVariables = other.Where(e => !dictionary.ContainsKey(e.Key));
            return dictionary.Concat(nonExistentEnvironmentVariables).ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }
}
