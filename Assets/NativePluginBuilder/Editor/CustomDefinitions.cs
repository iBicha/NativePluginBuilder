using System;
using System.Collections.Generic;

namespace iBicha
{
    [Serializable]
    public class CustomDefinitions : SerializableDictionary<string, string>
    {
        public string ToDefinitionString(string separator = " ")
        {
            List<string> validDefinitions = new List<string>();
            for (int i = 0; i < Count; i++)
            {
                string key = this[i].Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    string value = this[key].Trim();
                    if (!string.IsNullOrEmpty(value))
                    {
                        validDefinitions.Add(string.Format("{0}={1}", key, value));
                    }
                    else
                    {
                        validDefinitions.Add(string.Format("{0}", key));
                    }
                }
            }
            return string.Join(separator, validDefinitions.ToArray());
        }
    }
}
