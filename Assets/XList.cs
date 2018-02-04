using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;

/// List with more Customization & flexibility

[Serializable]
public class XList<T> {
   
    // Must never be assigned as null
    public T[] buffer = new T[8];
    public int Count = 0;

    [DebuggerHidden]
    [DebuggerStepThrough]
    public IEnumerator<T> GetEnumerator ()
    {
        if (buffer != null)
        {
            for (int i = 0; i < Count; ++i)
            {
                yield return buffer[i];
            }
        }
    }

    [DebuggerHidden]
    public T this[int i]
    {
        get { return buffer[i]; }
        set { buffer[i] = value; }
    }

    public void AddEmpty (int count)
    {
        AllocateMore(Count + count);
        Count += count;
    }

    void AllocateMore ()
    {
        T[] newList = new T[Mathf.Max(buffer.Length << 1, 32)];
        if (Count > 0) buffer.CopyTo(newList, 0);
        buffer = newList;
    }

    void AllocateMore(int target) 
    {
        if(buffer.Length >= target)
             return;
        var next = Mathf.ClosestPowerOfTwo(target);
        target = next == target * 2 ? target : next;
        T[] newList = new T[target];
        if (buffer != null) {
            buffer.CopyTo(newList, 0);
        }
        buffer = newList;
    }

    void Trim ()
    {
        if (Count > 0)
        {
            if (Count < buffer.Length)
            {
                T[] newList = new T[Count];
                for (int i = 0; i < Count; ++i) newList[i] = buffer[i];
                buffer = newList;
            }
        }
        else buffer = new T[0];
    }

    public void Clear () { Count = 0; }

    public void Release () { Count = 0; buffer = null; }

    public void Add (T item)
    {
        if (Count == buffer.Length) AllocateMore();
        buffer[Count++] = item;
    }

    public void AddRange(T[] range)
    {
        if (range == null || range.Length == 0)
            return;
        var length = range.Length;
        AllocateMore(Count + length);
        Array.Copy(range, 0, buffer, Count, length);
        Count += length;
    }

    public void AddRange(List<T> range)
    {
        if (range == null || range.Count == 0)
            return;
        var length = range.Count;
        AllocateMore(length);
        range.CopyTo(0, buffer, Count, length);
        Count += length;
    }

    public void AddRange(XList<T> range)
    {
        if (range == null || range.Count == 0)
            return; 
        var length = range.Count;
        AllocateMore(length);
        Array.Copy(range.buffer, 0, buffer, Count, length);
        Count += length;
    }



    public void Insert (int index, T item)
    {
        if (Count == buffer.Length) AllocateMore();

        if (index > -1 && index < Count)
        {
            for (int i = Count; i > index; --i) buffer[i] = buffer[i - 1];
            buffer[index] = item;
            ++Count;
        }
        else Add(item);
    }

    public bool Contains (T item)
    {
        if (buffer == null) return false;
        for (int i = 0; i < Count; ++i) if (buffer[i].Equals(item)) return true;
        return false;
    }

    public int IndexOf (T item)
    {
        if (buffer == null) return -1;
        for (int i = 0; i < Count; ++i) if (buffer[i].Equals(item)) return i;
        return -1;
    }

    /* public bool Remove (T item)
    {
        if (buffer != null)
        {
            EqualityComparer<T> comp = EqualityComparer<T>.Default;

            for (int i = 0; i < Count; ++i)
            {
                if (comp.Equals(buffer[i], item))
                {
                    --Count;
                    buffer[i] = default(T);
                    for (int b = i; b < Count; ++b) buffer[b] = buffer[b + 1];
                    buffer[Count] = default(T);
                    return true;
                }
            }
        }
        return false;
    } */

    public void RemoveAt (int index)
    {
        if (index > -1 && index < Count)
        {
            Array.Copy(buffer, index + 1, buffer, index, Count - index);
            Count--;
            // buffer[index] = default(T);
           // for (int b = index; b < Count; ++b) buffer[b] = buffer[b + 1];
           // buffer[Count] = default(T);
        }
    }

    public void RemoveRange (int index, int count)
    {
        var end = index + count; 
        if (end < Count)
        {
            Count--;
            Array.Copy(buffer, end, buffer, index, Count - end);
        }
    }

    public T Pop ()
    {
        if (Count != 0)
        {
            T val = buffer[--Count];
//            buffer[Count] = default(T);
            return val;
        }
        return default(T);
    }

    public T[] ToArray () { Trim(); return buffer; }


    static List<T> _singleton = new List<T>();

    public static List<T> list {
        get {
            _singleton.Clear();
            return _singleton;   
        }
    }

    static XList<T> _Xsingleton = new XList<T>();

    public static XList<T> xlist {
        get {
            _Xsingleton.Clear();
            return _Xsingleton;   
        }
    }

}