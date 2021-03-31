using System;
using System.Collections.Generic;

namespace Aufgabe1.DataStructure
{
  // System.Collection.Generic.SortedSet hat irgendwie doch nicht funktioniert
  public class Heap<E> where E : IComparable<E>
  {
    private readonly List<E> heapLst;
    private int size;
    public int Count { get => size; }
    public E Top { get => heapLst[1]; }

    public Heap(int initSize)
    {
      heapLst = new List<E>(initSize + 1)
      {
        default
      };
      size = 0;
    }

    public void Add(E data)
    {
      size++;
      heapLst.Add(data);

      for (int k = size; k > 1; k /= 2)
      {
        if (heapLst[k / 2].CompareTo(heapLst[k]) < 0)
          Swap(k / 2, k);
        else
          break;
      }
    }

    private void Swap(int i1, int i2)
    {
      E d = heapLst[i1];
      heapLst[i1] = heapLst[i2];
      heapLst[i2] = d;
    }

    private void Heapity(int pos)
    {
      if (IsLeaf(pos))
        return;

      if (heapLst[pos].CompareTo(heapLst[pos * 2]) < 0
          || (heapLst.Count > pos * 2 + 1 && heapLst[pos].CompareTo(heapLst[pos * 2 + 1]) < 0))
      {
        if (size < pos * 2 + 1)
        {
          Swap(pos, pos * 2);
          Heapity(pos * 2);
        }
        else if (heapLst[pos * 2].CompareTo(heapLst[pos * 2 + 1]) < 0)
        {
          Swap(pos, pos * 2 + 1);
          Heapity(pos * 2 + 1);
        }
        else
        {
          Swap(pos, pos * 2);
          Heapity(pos * 2);
        }
      }
    }

    private bool IsLeaf(int pos)
    {
      return pos > (size / 2);
    }

    public E Pop()
    {
      E data = Top;
      heapLst[1] = heapLst[size];
      heapLst.RemoveAt(size);
      size--;
      Heapity(1);
      return data;
    }

    public void Clear()
    {
      size = 0;
      heapLst.Clear();
      heapLst.Add(default);
    }
  }


  public class UnionFind
  {
    private readonly int[] ids;
    private readonly int[] size;

    public UnionFind(int N)
    {
      ids = new int[N];
      size = new int[N];

      for (int i = 0; i < N; i++)
      {
        ids[i] = i;
        size[i] = 1;
      }
    }

    public int Find(int id)
    {
      int root = ids[id];
      if (id == root)
        return root;
      root = Find(root);
      ids[id] = root;
      return root;
    }

    public bool IsConnected(int id1, int id2)
    {
      return Find(id1) == Find(id2);
    }

    public void Union(int id1, int id2)
    {
      int r1 = Find(id1);
      int r2 = Find(id2);
      if (r1 == r2)
        return;

      if (size[r1] > size[r2])
      {
        ids[r2] = r1;
        size[r1] += size[r2];
      }
      else
      {
        ids[r1] = r2;
        size[r2] += size[r1];
      }
    }

    public static int GetIndexByRowAndCol(int row, int col)
    {
      return FlohmarktManagement.INTERVAL_LENGTH * row - FlohmarktManagement.HEIGHT + col - 1;
    }
  }
}