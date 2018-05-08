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
    public class SerializableDictionary<T, Y> : IEnumerable<KeyValuePair<T, Y>>
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

        public T GetKey(int index)
        {
            return keys[index];
        }

        public Y GetValue(int index)
        {
            return values[index];
        }

        public List<T> Keys => keys;
        public List<Y> Values => values;
        

        private class Enumerator : IEnumerator<KeyValuePair<T, Y>>
        {
            private SerializableDictionary<T, Y> _dictionary;
            private int _index = -1;
            public Enumerator(SerializableDictionary<T, Y> dictionary)
            {
                _dictionary = dictionary;
            }

            public void Dispose()
            {
                _dictionary = null;
            }

            public bool MoveNext()
            {
                _index++;
                return _index < _dictionary.Count;
            }

            public void Reset()
            {
                _index = -1;
            }

            public KeyValuePair<T, Y> Current => new KeyValuePair<T, Y>(_dictionary.GetKey(_index), _dictionary.GetValue(_index));

            object IEnumerator.Current => Current;
        }


        public IEnumerator<KeyValuePair<T, Y>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
