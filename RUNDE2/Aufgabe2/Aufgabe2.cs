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
      // Falls das Einlesen von Daten erfolgreich ist
      if (data.Item1)
      {
        Matrix matrix = data.Item6;
        matrix.Sort();
        matrix.EliminateAll();

        #region  Analysiere Data
        // Spalte (Obstsorte), die von der Aufgabe gefragt sind
        int[] entriesToFind = data.Item3.Select(s => data.Item4[s]).ToArray();
        List<Row> resultRowsWithConflict = new List<Row>();
        // Bezieht sich auf resultRowsWithConflict
        // Speichert den Index, falls diese Zeile nicht nur Wunschsorten,
        // Sondern auch andere nicht gewuenschte Sorten enthalten
        List<int> conflictIndex = new List<int>();
        for (int i = 0; i < entriesToFind.Length; i++)
        {
          // Um Duplikate zu vermeiden
          if (resultRowsWithConflict.All(e => e.Left[entriesToFind[i]] == 0))
          {
            // Speichert die Zeile, in denen 
            // das Element in dieser Spalte nicht null ist
            resultRowsWithConflict.Add(matrix.FindNonZeroRow(entriesToFind[i]));
            // Testet, ob diese Zeile nicht gewuenschte Obstsorte enthaelt
            for (int j = 0; j < resultRowsWithConflict[^1].Count; j++)
            {
              if (resultRowsWithConflict[^1].Left[j] != 0 && !entriesToFind.Contains(j))
              {
                conflictIndex.Add(resultRowsWithConflict.Count - 1);
                // Eine Nicht-Uebereinstimmung reicht
                break;
              }
            }
          }
        }

        // ResC enthaelt auch nicht-gewuenschte Obstsorten
        Row resC = Row.Condense(resultRowsWithConflict.ToArray());
        // ResO enthaelt nur Wunschsorten
        // Jedoch koennte es sein, 
        // dass nicht alle Wunschsorten vorhanden sind
        Row resO = Row.Condense(Enumerable.
          Range(0, resultRowsWithConflict.Count).
          // wo dieser Index nicht in conflictIndex vorhanden ist
          Where(idx => !conflictIndex.Contains(idx)).
          Select(i => resultRowsWithConflict[i]).
          ToArray());
        // Schuesseln von ResC
        List<int> schuesselWithConflict = resC.GetRHSNonZeroEntries();
        // Schuesseln von ResO
        List<int> schuesselWithoutConflict = resO.GetRHSNonZeroEntries();
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
          Console.WriteLine(string.Join(",", schuesselWithConflict.Select(s => s + 1)));
          Console.WriteLine("");
          Console.WriteLine("Schuesseln, die besucht werden muessen, um NUR gewuenschte Obst zu bekommen (aber moeglicherweise kann man nicht alle gewuenschten Obstsorten bekommen): ");
        }
        Console.WriteLine(string.Join(",", schuesselWithoutConflict.Select(s => s + 1)));

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

    // Fordert den Nutzer auf, einen Dateinamen einzugeben, liest diese,
    // und parst in eine Matrix
    // RETURN: 
    // bool Item1 - ob die Datei erfolgreich eingelesen ist
    // int Item2 - Anzahl der verfuegbaren Obstsorten 
    // string[] Item3 - Wunschsorten
    // Dictionary<string, int> Item4 
    // string[] Item5 
    // Matrix Item6 - Matrix wie in Loesungsidee beschrieben
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
          // implizierte Zeile (mit 1s)
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

      // Loest die Matrix
      // O(n^3)
      public void EliminateAll()
      {
        for (int i = 0; i < rows[0].Count; i++) // O(n)
          Eliminate(i); // O(n^2*n) = O(n^3)
      }

      // Versucht, ein Pivotelement in dieser Spalte zu finden
      // Erfolgt es, wird alle andere Elemente eliminiert
      // Sonst return
      // O(n^2)
      public void Eliminate(int col)
      {
        int pivotRow = FindPivotRow(col); // O(n)
        if (pivotRow == -1)
        {
          return;
        }

        bool isUnique = true;
        for (int r = 0; r < rows.Count; r++) // O(n)
        {
          if (r == pivotRow) continue;
          if (rows[r].Left[col] != 0)
          {
            // Eliminiert andere Elemente dieser Spalte
            rows[r].SubtractFrom(rows[pivotRow]); // O(n)
            isUnique = false;
          }
        } // O(n^2)

        if (!isUnique)
        {
          List<Row> rs = new List<Row>();
          for (int i = 0; i < rows.Count; i++) // O(n)
          {
            rs.AddRange(Row.Extract(rows[i])); // O(n)
          } // O(n^2)
          rows = rs;
          Distinct(); // O(nlogn)
          Sort(); // O(n^2)
        }
      }

      // Entfernt Duplikate und die Null-Zeile
      // O(nlogn)
      public void Distinct()
      {
        rows = rows.Where(e => !e.IsZero).
          GroupBy(r => r.Left.GetHashCode()).
          Select(e => e.First()).
          ToList();
      }

      // Bubble sort
      // Sortiert nach Anzahl von Nulls jeder Zeile
      // O(n^2)
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

      // Versucht, das Pivotelement dieser Spalte zu finden
      // Die Pivotzeile wird zurueckgegeben
      // Falls es nicht moeglich ist, dann gib -1 zurueck
      // O(n)
      private int FindPivotRow(int col)
      {
        for (int i = 0; i < rows.Count; i++)
        {
          if (rows[i].Left[col] != 0 && rows[i].Left.LeadingZeros == col)
            return i;
        }
        return -1;
      }

      // Nachdem die Matrix bereits in reduzierte ZSF ist
      // Findet das Element dieser Spalte, welches nicht null ist
      // RETURN:
      // Die ganze Zeile dieses Elements
      public Row FindNonZeroRow(int col)
      {
        if (col < 0 || col >= rows[0].Left.Count) throw new ArgumentException(nameof(col));
        for (int i = 0; i < rows.Count; i++)
        {
          if (rows[i].Left[col] != 0) return rows[i];
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
      // True falls alle Elemente dieser Zeile sind null
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

      // Die jetzige Zeile (Minuend) wird von der gegebene Zeile (Subtrahenden) subtrahiert
      // Die Differenz wird der neue Wert des jetzigen Objekts
      public void SubtractFrom(Row r)
      {
        left.SubtractFrom(r.Left);
        right.SubtractFrom(r.Right);
      }

      // Teilt die gegebene Zeile in positive und negative Zeilen
      // Die negative Zeile wird zu positiv umgewandelt
      // indem *-1 durchgefuehrt wird
      // BSP: Eingabe: {1,0,0,-1,1} => {0,1,1,0,-1}
      //      Ausgabe: [{1,0,0,0,1} => {0,1,1,0,0}, 
      //                {0,0,0,1,0} => {0,0,0,0,1}]              
      // O(n)
      public static Row[] Extract(Row r)
      {
        sbyte[] leftPos, leftNeg, rightPos, rightNeg;
        leftPos = new sbyte[r.Left.Count];
        leftNeg = new sbyte[r.Left.Count];
        rightPos = new sbyte[r.Left.Count];
        rightNeg = new sbyte[r.Left.Count];
        for (int i = 0; i < r.Left.Count; i++) // O(n)
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

      // Fuegt alle Zeilen zu einer Einzelnen
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

      // Parameter:
      // string[] entryToName - Das entspricht Item5 aus ReadInput()
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

      public override string ToString()
      {
        return $"{{ {left} => {right} }}";
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

      // O(n)
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
