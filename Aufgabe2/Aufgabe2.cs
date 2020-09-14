/// Author: Chuyang Wang, 2020
/// C# 8.0, .Net Core 3.1
/// To compile code, download .Net Core SDK 3.1 from https://dotnet.microsoft.com/download


using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Aufgabe2
{
  public static class DreiecksPuzzlesLoeser
  {
    public static void Main(string[] args)
    {
      Console.WriteLine("Bitte Name der Test-Datei eingeben...");
      string testFilePath = Console.ReadLine();

      if (File.Exists(testFilePath))
      {
        using (StreamReader sr = File.OpenText(testFilePath))
        {
          // Einlesen der Datei
          int figures = Convert.ToInt32(sr.ReadLine());
          int partCount = Convert.ToInt32(sr.ReadLine());

          Part[] allParts = new Part[partCount];
          for (int line = 0; line < partCount; line++)
          {
            string[] sides = sr.ReadLine().Split(' ');
            allParts[line] = new Part(Convert.ToInt32(sides[0]), Convert.ToInt32(sides[1]), Convert.ToInt32(sides[2]));
          }


          Solve(ref allParts);
        }
      }
    }

    private static void Solve(ref Part[] allParts)
    {
      for (int i1 = 0; i1 < 7; i1++)
      {
        for (int i2 = i1 + 1; i2 < 8; i2++)
        {
          for (int i3 = i2 + 1; i3 < 9; i3++)
          {
            List<PartCollection> p1Pairs = allParts[i1].GetAroundParts(allParts);
            List<PartCollection> p2Pairs = allParts[i2].GetAroundParts(allParts);
            List<PartCollection> p3Pairs = allParts[i3].GetAroundParts(allParts);

            PartCollection[] ans = GetPossibleGraph(p1Pairs.ToArray(), p2Pairs.ToArray(), p3Pairs.ToArray());
            if (ans != null)
            {
              Console.Write(ans);
            }
          }
        }
      }
    }

    private static PartCollection[] GetPossibleGraph(PartCollection[] pC1, PartCollection[] pC2, PartCollection[] pC3)
    {
      foreach (var p1 in pC1)
      {
        foreach (var p2 in pC2)
        {
          foreach (var p3 in pC3)
          {
            if (p1.Center.IsInAroundParts(p2) || p1.Center.IsInAroundParts(p3) || p2.Center.IsInAroundParts(p1) || p2.Center.IsInAroundParts(p3) || p3.Center.IsInAroundParts(p1) || p3.Center.IsInAroundParts(p2)) continue;
            if (PartCollection.CanBuildGraph(p1, p2, p3))
            {
              Console.Write(new PartCollection[3] { p1, p2, p3 });
              //return new PartCollection[3] { p1, p2, p3 };
            }
          }
        }
      }
      return null;
    }

    private static bool IsAfterAnother(this int[] sides, int first, int second)
    {
      for (int i = 0; i < 3; i++)
      {
        if (sides[i] == first)
        {
          if (sides[i == 2 ? 0 : i + 1] == second) return true;
          else return false;
        }
      }
      throw new Exception();
    }

    private class PartCollection
    {
      private Part _center;
      private Dictionary<int, Part> _partsAround;
      private Part[] _partsAroundL = new Part[3];
      private Dictionary<int, int> _connectionFromCenter;
      public Part Center
      {
        get { return _center; }
        private set { }
      }
      public Dictionary<int, Part> PartsAround
      {
        get { return _partsAround; }
        private set { }
      }
      public Dictionary<int, int> ConnectionFromCenter
      {
        get { return _connectionFromCenter; }
      }
      public Part[] PartsAroundL
      {
        get { return _partsAroundL; }
      }

      public PartCollection(Part center, Dictionary<int, Part> partsAround, Dictionary<int, int> connections)
      {
        _center = center;
        _partsAround = partsAround;
        _connectionFromCenter = connections;
        for (int i = 0; i < 3; i++)
        {
          _partsAroundL[i] = partsAround[i];
        }
      }

      public bool HasOnlyOneSharedPartWith(PartCollection pC, out Part sharedPart, out Tuple<int, int> connectionSides)
      {
        sharedPart = null;
        connectionSides = null;
        byte sharedPartCount = 0;
        for (int i = 0; i < 3; i++)
        {
          for (int j = 0; j < 3; j++)
          {
            if (this._partsAround[i].Equals(pC.PartsAround[j]) && this._connectionFromCenter[i] != pC.ConnectionFromCenter[j])
            {
              sharedPartCount++;
              sharedPart = this._partsAround[i];
              connectionSides = new Tuple<int, int>(_connectionFromCenter[i], pC.ConnectionFromCenter[j]);
            }
          }
        }
        return sharedPartCount == 1;
      }

      public static bool CanBuildGraph(PartCollection pC1, PartCollection pC2, PartCollection pC3)
      {
        if (pC1.PartsAroundL.Union(pC2.PartsAroundL).Union(pC3.PartsAroundL).ToArray().Length + 3 != 9) return false;
        Part biPart1, biPart2, biPart3;
        Tuple<int, int> connectionSides1, connectionSides2, connectionSides3;
        if (pC1.HasOnlyOneSharedPartWith(pC2, out biPart1, out connectionSides1) && pC1.HasOnlyOneSharedPartWith(pC3, out biPart2, out connectionSides2) && pC2.HasOnlyOneSharedPartWith(pC3, out biPart3, out connectionSides3))
        {
          if (!biPart1.Equals(biPart2) && !biPart1.Equals(biPart3) && !biPart2.Equals(biPart3))
          {
            // Test if possible without spiegeln
            bool b1, b2;
            b1 = connectionSides1.Item1 - connectionSides1.Item2 == -1 || connectionSides1.Item1 - connectionSides1.Item2 == 2;
            b2 = connectionSides2.Item1 - connectionSides2.Item2 == -1 || connectionSides2.Item1 - connectionSides2.Item2 == 2;
            if ((b1 && b2) || (!b1 && !b2)) return false;
            b1 = connectionSides3.Item1 - connectionSides3.Item2 == -1 || connectionSides3.Item1 - connectionSides3.Item2 == 2;
            b2 = connectionSides2.Item1 - connectionSides2.Item2 == -1 || connectionSides2.Item1 - connectionSides2.Item2 == 2;
            if ((b1 && b2) || (!b1 && !b2)) return false;
            b1 = connectionSides1.Item1 - connectionSides1.Item2 == -1 || connectionSides1.Item1 - connectionSides2.Item2 == 2;
            b2 = connectionSides3.Item1 - connectionSides3.Item2 == -1 || connectionSides3.Item1 - connectionSides3.Item2 == 2;
            if ((b1 && b2) || (!b1 && !b2)) return false;
            return true;
          }
        }
        return false;
      }
    }
    private class Part
    {
      private int[] _sides;
      private int[] _connected;

      public int[] Sides
      {
        get { return _sides; }
      }
      public int[] Connected
      {
        get { return _connected; }
      }

      public Part(int side1, int side2, int side3)
      {
        _sides = new int[] { side1, side2, side3 };
        _connected = new int[] { -1, -1, -1 };
      }

      public List<PartCollection> GetAroundParts(Part[] allParts)
      {
        List<PartCollection> possibilities = new List<PartCollection>();
        Part[] otherParts;
        List<Part> temp = new List<Part>();
        for (int i = 0; i < 9; i++)
        {
          if (!allParts[i].Equals(this)) temp.Add(allParts[i]);
        }
        while (temp.Count < 8) temp.Add(this);
        otherParts = temp.ToArray();
        for (int i1 = 0; i1 < 6; i1++)
        {
          for (int i2 = i1 + 1; i2 < 7; i2++)
          {
            for (int i3 = i2 + 2; i3 < 8; i3++)
            {
              Action releaseAll = () =>
              {
                this.ReleaseConnection();
                otherParts[i1].ReleaseConnection();
                otherParts[i2].ReleaseConnection();
                otherParts[i3].ReleaseConnection();
              };

              Tuple<int, int>[] indexes = new Tuple<int, int>[3];
              if (this.TryPair(otherParts[i1], out indexes[0]) && this.TryPair(otherParts[i2], out indexes[1]) && this.TryPair(otherParts[i3], out indexes[2]))
              {
                var dic = new Dictionary<int, Part>();
                var concDic = new Dictionary<int, int>();
                dic.Add(indexes[0].Item1, otherParts[i1]);
                dic.Add(indexes[1].Item1, otherParts[i2]);
                dic.Add(indexes[2].Item1, otherParts[i3]);
                concDic.Add(indexes[0].Item1, indexes[0].Item2);
                concDic.Add(indexes[1].Item1, indexes[1].Item2);
                concDic.Add(indexes[2].Item1, indexes[2].Item2);
                possibilities.Add(new PartCollection(this, dic, concDic));
              }

              releaseAll();

              if (this.TryPair(otherParts[i2], out indexes[0]) && this.TryPair(otherParts[i3], out indexes[1]) && this.TryPair(otherParts[i1], out indexes[2]))
              {
                var dic = new Dictionary<int, Part>();
                var concDic = new Dictionary<int, int>();
                dic.Add(indexes[2].Item1, otherParts[i1]);
                dic.Add(indexes[0].Item1, otherParts[i2]);
                dic.Add(indexes[1].Item1, otherParts[i3]);
                concDic.Add(indexes[0].Item1, indexes[0].Item2);
                concDic.Add(indexes[1].Item1, indexes[1].Item2);
                concDic.Add(indexes[2].Item1, indexes[2].Item2);
                possibilities.Add(new PartCollection(this, dic, concDic));
              }

              releaseAll();

              if (this.TryPair(otherParts[i2], out indexes[0]) && this.TryPair(otherParts[i1], out indexes[1]) && this.TryPair(otherParts[i3], out indexes[2]))
              {
                var dic = new Dictionary<int, Part>();
                var concDic = new Dictionary<int, int>();
                dic.Add(indexes[1].Item1, otherParts[i1]);
                dic.Add(indexes[0].Item1, otherParts[i2]);
                dic.Add(indexes[2].Item1, otherParts[i3]);
                concDic.Add(indexes[0].Item1, indexes[0].Item2);
                concDic.Add(indexes[1].Item1, indexes[1].Item2);
                concDic.Add(indexes[2].Item1, indexes[2].Item2);
                possibilities.Add(new PartCollection(this, dic, concDic));
              }

              releaseAll();

              if (this.TryPair(otherParts[i1], out indexes[0]) && this.TryPair(otherParts[i3], out indexes[1]) && this.TryPair(otherParts[i2], out indexes[2]))
              {
                var dic = new Dictionary<int, Part>();
                var concDic = new Dictionary<int, int>();
                dic.Add(indexes[0].Item1, otherParts[i1]);
                dic.Add(indexes[2].Item1, otherParts[i2]);
                dic.Add(indexes[1].Item1, otherParts[i3]);
                concDic.Add(indexes[0].Item1, indexes[0].Item2);
                concDic.Add(indexes[1].Item1, indexes[1].Item2);
                concDic.Add(indexes[2].Item1, indexes[2].Item2);
                possibilities.Add(new PartCollection(this, dic, concDic));
              }

              releaseAll();

              if (this.TryPair(otherParts[i3], out indexes[0]) && this.TryPair(otherParts[i1], out indexes[1]) && this.TryPair(otherParts[i2], out indexes[2]))
              {
                var dic = new Dictionary<int, Part>();
                var concDic = new Dictionary<int, int>();
                dic.Add(indexes[1].Item1, otherParts[i1]);
                dic.Add(indexes[2].Item1, otherParts[i2]);
                dic.Add(indexes[0].Item1, otherParts[i3]);
                concDic.Add(indexes[0].Item1, indexes[0].Item2);
                concDic.Add(indexes[1].Item1, indexes[1].Item2);
                concDic.Add(indexes[2].Item1, indexes[2].Item2);
                possibilities.Add(new PartCollection(this, dic, concDic));
              }

              releaseAll();

              
              if (this.TryPair(otherParts[i3], out indexes[0]) && this.TryPair(otherParts[i2], out indexes[1]) && this.TryPair(otherParts[i1], out indexes[2]))
              {
                var dic = new Dictionary<int, Part>();
                var concDic = new Dictionary<int, int>();
                dic.Add(indexes[2].Item1, otherParts[i1]);
                dic.Add(indexes[1].Item1, otherParts[i2]);
                dic.Add(indexes[0].Item1, otherParts[i3]);
                concDic.Add(indexes[0].Item1, indexes[0].Item2);
                concDic.Add(indexes[1].Item1, indexes[1].Item2);
                concDic.Add(indexes[2].Item1, indexes[2].Item2);
                possibilities.Add(new PartCollection(this, dic, concDic));
              }

              releaseAll();
            }
          }
        }
        return possibilities;
      }

      public bool TryPair(Part anotherPart, out Tuple<int, int> indexes)
      {
        for (int i = 0; i < 3; i++)
        {
          if (_connected[i] == -1)
          {
            for (int j = 0; j < 3; j++)
            {
              if (anotherPart.Connected[j] == -1)
              {
                if (_sides[i] + anotherPart.Sides[j] == 0)
                {
                  indexes = new Tuple<int, int>(i, j);
                  _connected[i] = j;
                  anotherPart.SetConnected(j, i);
                  return true;
                }
              }
            }
          }
        }
        indexes = null;
        return false;
      }

      public void SetConnected(int thisNode, int to)
      {
        _connected[thisNode] = to;
      }

      public void ReleaseConnection()
      {
        _connected = new int[] { -1, -1, -1 };
      }

      public bool IsInAroundParts(PartCollection pC)
      {
        for (int i = 0; i < 3; i++)
        {
          if (pC.PartsAroundL[i].Equals(this)) return true;
        }
        return false;
      }

      public override int GetHashCode()
      {
        unchecked
        {
          int hash = 4357;

          hash = hash * 6113 + _sides[0].GetHashCode();
          hash = hash * 101 + _sides[1].GetHashCode();
          hash = hash * 307 + _sides[2].GetHashCode();
          return hash;
        }
      }

      public override bool Equals(object obj)
      {
        if (obj == null || GetType() != obj.GetType())
        {
          return false;
        }

        return obj.GetHashCode() == this.GetHashCode();
      }

    }
  }
}
