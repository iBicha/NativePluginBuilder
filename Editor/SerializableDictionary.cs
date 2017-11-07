using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace iBicha
{
    [Serializable]
    public class SerializableDictionary<T, Y>
    {
        [SerializeField] private List<T> keys;
        [SerializeField] private List<Y> values;

        public SerializableDictionary()
        {
            keys = new List<T>();
            values = new List<Y>();
        }

        public void Add(T key, Y value)
        {
            keys.Add(key);
            values.Add(value);
        }

        public void Insert(T key, Y value, int index)
        {
            keys.Insert(index, key);
            values.Insert(index, value);
        }

        public bool Remove(T key)
        {
            if (!keys.Contains(key))
            {
                return false;
            }
            return RemoveAt(keys.IndexOf(key));
        }

        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException();
            }
            keys.RemoveAt(index);
            values.RemoveAt(index);
            return true;
        }

        public int Count
        {
            get
            {
                if (keys.Count != values.Count)
                {
                    throw new IndexOutOfRangeException("Keys.Count != Values.Count");
                }
                return keys.Count;
            }
        }

        public T this[int i]
        {
            get
            {
                return keys[i];
            }
            set
            {
                keys[i] = value;
            }
        }

        public Y this[T key]
        {
            get
            {
                if (!keys.Contains(key))
                {
                    return default(Y);
                }
                int index = keys.IndexOf(key);
                return values[index];
            }
            set
            {
                if (!keys.Contains(key))
                {
                    return;
                }
                int index = keys.IndexOf(key);
                values[index] = value;
            }
        }
    }
}
