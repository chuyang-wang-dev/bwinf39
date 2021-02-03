using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aufgabe2
{
  public static class ObstspiessSolver
  {
    public static void Main(string[] args)
    {
      Console.OutputEncoding = System.Text.Encoding.UTF8;

      Tuple<bool, int, string[], Dictionary<string, int>, string[], Matrix> data = ReadInput();
      if (data.Item1)
      {
        Matrix matrix = data.Item6;
        matrix.Sort();
        matrix.EliminateAll();

        #region  Format Data to be readable
        int[] entriesToFind = data.Item3.Select(s => data.Item4[s]).ToArray();
        List<int> conflictIndex = new List<int>();
        List<Row> resultRowsWithConflict = new List<Row>();
        for (int i = 0; i < entriesToFind.Length; i++)
        {
          if (!resultRowsWithConflict.Any(e => e.Left[entriesToFind[i]] != 0))
          {
            resultRowsWithConflict.Add(matrix.FindRow(entriesToFind[i]));
            for (int j = 0; j < resultRowsWithConflict[^1].Count; j++)
            {
              if (resultRowsWithConflict[^1].Left[j] != 0 && !entriesToFind.Contains(j))
              {
                conflictIndex.Add(resultRowsWithConflict.Count - 1);
                break;
              }
            }
          }
        }

        Row resC = Row.Condense(resultRowsWithConflict.ToArray());
        Row resO = Row.Condense(Enumerable.
          Range(0, resultRowsWithConflict.Count).
          Where(idx => !conflictIndex.Contains(idx)).
          Select(i => resultRowsWithConflict[i]).
          ToArray());
        List<int> schlangeWithConflict = resC.GetRHSNonZeroEntries();
        List<int> schlangeWithoutConflict = resO.GetRHSNonZeroEntries();
        #endregion

        #region OutPut
        bool eindeutig = conflictIndex.Count == 0;

        Console.WriteLine("");
        Console.WriteLine($"Gesucht: {string.Join(",", data.Item3)}");
        Console.WriteLine("");

        if (eindeutig)
        {
          Console.Write("Eine eindeutige Antwort wurde gefunden: ");
        }
        else
        {
          Console.WriteLine("Keine eindeutige Antwort konnte gefunden werden: ");
          Console.WriteLine("Schuesseln, die besucht werden muessen, um ALLE gewuenschten Obst zu bekommen (aber moeglicherweise kann man auch weitere unerwuenschte Obstsorte bekommen): ");
          Console.WriteLine(string.Join(",", schlangeWithConflict.Select(s => s + 1)));
          Console.WriteLine("");
          Console.WriteLine("Schuesseln, die besucht werden muessen, um NUR gewuenschte Obst zu bekommen (aber moeglicherweise kann man nicht alle gewuenschten Obstsorten bekommen): ");
        }
        Console.WriteLine(string.Join(",", schlangeWithoutConflict.Select(s => s + 1)));

        Console.WriteLine("\r\n----------------");
        Console.WriteLine("Weitere Einzelheiten: ");
        if (!eindeutig) Console.WriteLine("(Zeilen, in denen keine eindeutige Antwort fuer die gesuchten Obstsorten gefunden werden kann, werden mit \"**\" am Anfang der Zeile markiert)\r\n");
        for (int i = 0; i < resultRowsWithConflict.Count; i++)
        {
          if (conflictIndex.Contains(i)) Console.Write("**");
          Console.WriteLine(resultRowsWithConflict[i].ToReadableFormat(data.Item5));
        }
        #endregion
      }
      else
      {
        Console.WriteLine("Die gegebene Datei-Namen ist nicht gueltig. Das Programm und die Datei muessen in demselben Ordner stehen. ");
      }

      Console.WriteLine("\r\nDrueck eine beliebige Taste zu schliessen...");
      Console.ReadKey();
    }

    // IsSuccess, obstCount, toFind, nameToEntry, entryToName, data
    private static Tuple<bool, int, string[], Dictionary<string, int>, string[], Matrix> ReadInput()
    {
      Console.WriteLine("Bitte Name der Test-Datei eingeben...");
      string testFilePath = Console.ReadLine();

      if (File.Exists(testFilePath))
      {
        using (StreamReader sr = File.OpenText(testFilePath))
        {
          int obstCount = Convert.ToInt32(sr.ReadLine());
          string[] toFind = sr.ReadLine().Trim().Split(' ');
          int lines = Convert.ToInt32(sr.ReadLine());

          Dictionary<string, int> nameToEntry = new Dictionary<string, int>();
          string[] entryToName = new string[obstCount];
          Row[] rows = new Row[lines + 1];


          for (int i = 0, entry = 0; i < lines; i++)
          {
            int[] nums = sr.ReadLine().Trim().Split().Select(s => Convert.ToInt32(s)).ToArray();
            string[] names = sr.ReadLine().Trim().Split();
            sbyte[] LHS = new sbyte[obstCount];
            sbyte[] RHS = new sbyte[obstCount];
            for (int j = 0; j < nums.Length; j++)
            {
              if (!nameToEntry.ContainsKey(names[j]))
              {
                nameToEntry.Add(names[j], (sbyte)entry);
                entryToName[entry] = names[j];
                ++entry;
              }
              LHS[nameToEntry[names[j]]] = 1;
              RHS[nums[j] - 1] = 1;
            }
            rows[i] = new Row(new Vector(LHS), new Vector(RHS));
          }
          // Add implied information
          rows[^1] = new Row(new Vector(Enumerable.Range(0, obstCount).Select(_ => (sbyte)1)), new Vector(Enumerable.Range(0, obstCount).Select(_ => (sbyte)1)));
          Matrix data = new Matrix(rows);
          for (int j = 0; j < toFind.Length; j++)
          {
            if (!nameToEntry.ContainsKey(toFind[j]))
            {
              entryToName[nameToEntry.Count] = toFind[j];
              nameToEntry.Add(toFind[j], nameToEntry.Count);
            }
          }
          return new Tuple<bool, int, string[], Dictionary<string, int>, string[], Matrix>(true, obstCount, toFind, nameToEntry, entryToName, data);
        }
      }
      else return new Tuple<bool, int, string[], Dictionary<string, int>, string[], Matrix>(false, -1, null, null, null, null);
    }



    private class Matrix
    {
      private List<Row> rows;
      public Matrix(Row[] rows)
      {
        if (rows.Length == 0) throw new ArgumentException(nameof(rows));
        this.rows = new List<Row>();
        this.rows.AddRange(rows);
      }

      public void EliminateAll()
      {
        for (int i = 0; i < rows[0].Count; i++)
        {
          Eliminate(i);
        }
      }

      public void Eliminate(int col)
      {
        bool isUnique = true;
        int pivotRow = FindPivotRow(col);
        if (pivotRow == -1)
        {
          return;
        }
        for (int r = 0; r < rows.Count; r++)
        {
          if (r == pivotRow) continue;
          if (rows[r].Left[col] != 0)
          {
            rows[r].SubtractFrom(rows[pivotRow]);
            isUnique = false;
          }
        }
        if (!isUnique)
        {
          List<Row> rs = new List<Row>();
          for (int i = 0; i < rows.Count; i++)
          {
            rs.AddRange(Row.Extract(rows[i]));
          }
          rows = rs;
          Distinct();
          Sort();
        }
      }

      // Make list distinct AND remove 0:0 row
      public void Distinct()
      {
        rows = rows.Where(e => !e.IsZero).
          GroupBy(r => r.Left.GetHashCode()).
          Select(e => e.First()).
          ToList();
      }

      // Bubble sort
      // Brings matrix to row echolon form
      public void Sort()
      {
        static void Swap(List<Row> arr, int i1, int i2)
        {
          Row temp = arr[i1];
          arr[i1] = arr[i2];
          arr[i2] = temp;
        }

        for (int i = 0; i < rows.Count - 1; i++)
        {
          for (int j = 0; j < rows.Count - 1 - i; j++)
          {
            if (rows[j].GetLeftLeadingZeros() > rows[j + 1].GetLeftLeadingZeros()) Swap(rows, j, j + 1);
          }
        }
      }

      private int FindPivotRow(int col)
      {
        for (int i = 0; i < rows.Count; i++)
        {
          if (rows[i].Left[col] != 0 && rows[i].Left.LeadingZeros == col) return i;
        }
        return -1;
      }

      public Row FindRow(int colEntry)
      {
        if (colEntry < 0 || colEntry >= rows[0].Left.Count) throw new ArgumentException(nameof(colEntry));
        for (int i = 0; i < rows.Count; i++)
        {
          if (rows[i].Left[colEntry] != 0) return rows[i];
        }
        throw new Exception("Should not happen. At Matrix.FindRow");
      }

      public override string ToString()
      {
        return string.Join("\r\n", rows);
      }
    }



    private class Row
    {
      private readonly Vector left;
      private readonly Vector right;
      public Vector Left { get => left; }
      public Vector Right { get => right; }
      public int Count { get => left.Count; }
      public bool IsZero
      {
        get
        {
          for (int i = 0; i < left.Count; i++)
          {
            if (left[i] != 0) return false;
          }
          return true;
        }
      }

      public Row(Vector left, Vector right)
      {
        if (left.Count != right.Count) throw new ArgumentException("Dimension unequal");
        this.left = left;
        this.right = right;
      }

      public int GetLeftLeadingZeros()
      {
        return left.LeadingZeros;
      }

      public void SubtractFrom(Row r)
      {
        left.SubtractFrom(r.Left);
        right.SubtractFrom(r.Right);
      }

      public static Row[] Extract(Row r)
      {
        sbyte[] leftPos, leftNeg, rightPos, rightNeg;
        leftPos = new sbyte[r.Left.Count];
        leftNeg = new sbyte[r.Left.Count];
        rightPos = new sbyte[r.Left.Count];
        rightNeg = new sbyte[r.Left.Count];
        for (int i = 0; i < r.Left.Count; i++)
        {
          if (r.Left[i] > 0)
          {
            leftPos[i] = 1;
            leftNeg[i] = 0;
          }
          else if (r.Left[i] < 0)
          {
            leftPos[i] = 0;
            leftNeg[i] = 1;
          }
          else
          {
            leftPos[i] = 0;
            leftNeg[i] = 0;
          }
        }

        for (int i = 0; i < r.Right.Count; i++)
        {
          if (r.Right[i] > 0)
          {
            rightPos[i] = 1;
            rightNeg[i] = 0;
          }
          else if (r.Right[i] < 0)
          {
            rightPos[i] = 0;
            rightNeg[i] = 1;
          }
          else
          {
            rightPos[i] = 0;
            rightNeg[i] = 0;
          }
        }

        return new Row[] { new Row(new Vector(leftPos), new Vector(rightPos)), new Row(new Vector(leftNeg), new Vector(rightNeg)) };
      }

      public static Row Condense(Row[] rows)
      {
        if (rows.Length == 0) throw new ArgumentException(nameof(rows));
        Row rtr = new Row(
          new Vector(Enumerable.Range(0, rows[0].Left.Count).Select(_ => (sbyte)0)),
          new Vector(Enumerable.Range(0, rows[0].Left.Count).Select(_ => (sbyte)0)));
        for (int i = 0; i < rows.Length; i++)
        {
          rtr.Left.AddFrom(rows[i].Left);
          rtr.Right.AddFrom(rows[i].Right);
        }
        return rtr;
      }

      public List<int> GetRHSNonZeroEntries()
      {
        List<int> rtr = new List<int>();
        for (int colEntry = 0; colEntry < Count; colEntry++)
        {
          if (Right[colEntry] != 0) rtr.Add(colEntry);
        }
        return rtr;
      }

      public override string ToString()
      {
        return $"{{ {left} => {right} }}";
      }

      public string ToReadableFormat(string[] entryToName)
      {
        string[] obst = Enumerable.
          Range(0, Count).
          Where(entry => Left[entry] != 0).
          Select(idx => entryToName[idx]).ToArray();
        int[] schuesseln = Enumerable.
          Range(0, Count).
          Where(idx => Right[idx] != 0).
          Select(i => i + 1).ToArray();
        return $"{string.Join(", ", obst)} => {string.Join(", ", schuesseln)}";
      }
    }

    private class Vector : IEnumerable<sbyte>, IEquatable<Vector>
    {
      private readonly sbyte[] data;
      public int Count
      {
        get
        {
          return data.Length;
        }
      }
      public int LeadingZeros
      {
        get
        {
          for (int j = 0; j < data.Length; j++)
          {
            if (data[j] != 0) return j;
          }
          return data.Length;
        }
      }
      public sbyte this[int i]
      {
        get { return data[i]; }
        set { data[i] = value; }
      }

      public Vector(IEnumerable<sbyte> data)
      {
        if (data.Count() == 0) throw new ArgumentException(nameof(data));
        this.data = data.ToArray();
      }

      public Vector(sbyte[] data)
      {
        if (data.Count() == 0) throw new ArgumentException(nameof(data));
        this.data = data.ToArray();
      }

      public void AddFrom(Vector v)
      {
        for (int i = 0; i < data.Length; i++)
        {
          data[i] += v[i];
        }
      }

      public void SubtractFrom(Vector v)
      {
        for (int i = 0; i < data.Length; i++)
        {
          data[i] -= v[i];
        }
      }

      public IEnumerator<sbyte> GetEnumerator()
      {
        return ((IEnumerable<sbyte>)this.data).GetEnumerator();
      }

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
      {
        return GetEnumerator();
      }

      public bool Equals(Vector v)
      {
        return this.GetHashCode() == v.GetHashCode();
      }

      public override bool Equals(object o)
      {
        if (o is Vector v) return this.Equals(v);
        else return false;
      }

      public override int GetHashCode()
      {
        unchecked
        {
          int hash = 21;
          for (int i = 0; i < data.Length; i++)
          {
            hash = hash * 23 + data[i].GetHashCode();
          }
          return hash;
        }
      }

      public static bool operator ==(Vector v1, Vector v2)
      {
        return v1.Equals(v2);
      }

      public static bool operator !=(Vector v1, Vector v2)
      {
        return !v1.Equals(v2);
      }

      public override string ToString()
      {
        return $"[{string.Join(',', data)}]";
      }

    }
  }
}
