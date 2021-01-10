using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Aufgabe1
{
  public class FlohmarktManagement
  {
    public const int HEIGHT = 20;
    public const int START_TIME = 8;
    public const int END_TIME = 18;
    public const int INTERVAL_LENGTH = END_TIME - START_TIME;
    public static void Main(string[] args)
    {
      Stopwatch sw = new Stopwatch();
      Tuple<bool, int, List<int[]>> input = ReadInput();
      sw.Start();
      if (input.Item1)
      {
        FlohmarktManagement fm = new FlohmarktManagement(input.Item3);
        fm.Process();
        fm.PrintResult();
        sw.Stop();
      }
      else
      {
        Console.WriteLine("Die gegebene Datei-Namen ist nicht gueltig. Das Programm und die Datei muessen in demselben Ordner stehen. ");
      }
      Console.WriteLine(sw.ElapsedMilliseconds);
      Console.ReadKey();
    }


    // IsSuccess, demandCount, data
    private static Tuple<bool, int, List<int[]>> ReadInput()
    {
      Console.WriteLine("Bitte Name der Test-Datei eingeben...");
      string testFilePath = Console.ReadLine();

      if (File.Exists(testFilePath))
      {
        using (StreamReader sr = File.OpenText(testFilePath))
        {
          int demandCount = Convert.ToInt32(sr.ReadLine().Trim());
          List<int[]> data = new List<int[]>();
          for (int i = 0; i < demandCount; i++)
          {
            data.Add(sr.ReadLine().Trim().
              Split(' ').
              Select(s => Convert.ToInt32(s.Trim())).
              Append(i).  // arr[3]: Als ID des Anbieters, um zu unterscheiden
              ToArray());
          }
          return new Tuple<bool, int, List<int[]>>(true, demandCount, data);
        }
      }
      else return new Tuple<bool, int, List<int[]>>(false, -1, null);
    }


    private readonly List<int[]> demands;
    private readonly bool[] avaliable;
    private readonly Stack<int> currentDeleted;
    private readonly int[] currentMap;
    private readonly int highestValPossible;
    public List<int[]> BestCombination { get; private set; }
    public int HighestProfit { get; private set; }


    public int DEBUG_changedCount = 0;
    public int DEBUG_pruningCount = 0;

    public FlohmarktManagement(List<int[]> demands)
    {
      this.demands = new List<int[]>(demands);
      currentDeleted = new Stack<int>();
      avaliable = new bool[demands.Count];
      for (int i = 0; i < demands.Count; i++) avaliable[i] = true;
      currentMap = new int[INTERVAL_LENGTH];

      HighestProfit = 0;

      // Initialize the map
      for (int c = 0, cCount = 0; c < INTERVAL_LENGTH; c++, cCount = 0)
      {
        for (int i = 0; i < demands.Count; i++)
        {
          if (avaliable[i] && demands[i][0] <= c + START_TIME && demands[i][1] > c + START_TIME)
          {
            cCount += demands[i][2];
          }
        }
        currentMap[c] = cCount;
      }
      highestValPossible = CalcMaximumProfit();
    }

    public void PrintResult()
    {
      if (BestCombination != null)
      {
        Console.WriteLine("Die beste Auswahl ist (Hier werden die betroffenen Anbieter gezeigt wie das Format der Eingabe): ");
        foreach (int[] item in BestCombination) Console.WriteLine(
          string.Join(" ", Enumerable.
            Range(0, 3).
            Select(i => item[i])));
        Console.WriteLine("-------------------");
        Console.WriteLine("Zusammenfassung: ");
        Console.WriteLine($"Diese Auswahl besteht aus {BestCombination.Count} Anbietern. ");
        Console.WriteLine($"Die Mieteinnahme betraegt {HighestProfit} Euro. ");
      }
      else throw new InvalidOperationException("Data is not processed yet. ");
    }

    public void Process()
    {
      BestCombination = null;

      for (int i = highestValPossible - 1; i > 0; i -= (int)Math.Ceiling(100d / (double)demands.Count))
      {
        HighestProfit = i;
        RecRemove(FindFirstConflictCol(0));

        if (BestCombination != null) break;
      }
    }

    public void Process1()
    {
      HighestProfit = 0;
      RecRemove(FindFirstConflictCol(0));
    }

    private void RecRemove(Tuple<int, List<int[]>> firstConflict)
    {

      int max = CalcMaximumProfit();
      // If smaller or equal the current highest, do pruning
      if (max <= HighestProfit)
      {
        // Pop the last deleted obj and set it to avaliable
        Restore();
        DEBUG_pruningCount++;
        return;
      }

      // If compatible at this stage
      if (firstConflict.Item1 < 0)
      {
        // Update highest profit
        HighestProfit = max;
        BestCombination = GetCurrentCombination();

        // Pop the last deleted obj and set it to avaliable
        Restore();
        DEBUG_changedCount++;
        return;
      }

      foreach (int[] conflictObj in firstConflict.Item2)
      {
        if (HighestProfit == highestValPossible) return;
        int conflictIndex = demands.FindIndex(item => item[3] == conflictObj[3]);
        Delete(conflictIndex);

        RecRemove(FindFirstConflictCol(firstConflict.Item1));
      }
      Restore();
    }

    private void Delete(int index)
    {
      currentDeleted.Push(index);
      avaliable[index] = false;

      for (int i = demands[index][0]; i < demands[index][1]; i++)
      {
        currentMap[i - START_TIME] -= demands[index][2];
      }
    }

    private void Restore()
    {
      if (currentDeleted.Count == 0) return;
      int index = currentDeleted.Pop();
      avaliable[index] = true;

      for (int i = demands[index][0]; i < demands[index][1]; i++)
      {
        currentMap[i - START_TIME] += demands[index][2];
      }
    }

    // Col starts with index 0; Search starts with col 0
    private Tuple<int, List<int[]>> FindFirstConflictCol(int startCol = 0)
    {
      List<int[]> currentCol;
      // Traverse the cols
      for (int c = startCol; c < INTERVAL_LENGTH; c++)
      {
        if (currentMap[c] <= HEIGHT) continue;
        currentCol = new List<int[]>();
        // Traverse the demand list
        for (int i = 0; i < demands.Count; i++)
        {
          if (avaliable[i] && demands[i][0] <= c + START_TIME && demands[i][1] > c + START_TIME)
          {
            currentCol.Add(demands[i]);
          }
        }
        return new Tuple<int, List<int[]>>(c, currentCol);
      }
      return new Tuple<int, List<int[]>>(-1, null);
    }

    private int CalcMaximumProfit()
    {
      int count = 0;
      for (int i = 0; i < INTERVAL_LENGTH; i++)
      {
        count += currentMap[i] > HEIGHT ? HEIGHT : currentMap[i];
      }
      return count;
    }

    private List<int[]> GetCurrentCombination()
    {
      return Enumerable.
        Range(0, demands.Count).
        Where(i => avaliable[i]).
        Select(i => demands[i]).
        ToList();
    }

    private static class QuickSort
    {
      public static void Sort(int[] arr, int low, int high)
      {
        if (high <= low) return;

        int pIndex = Partition(arr, low, high);
        Sort(arr, low, pIndex - 1);
        Sort(arr, pIndex + 1, high);
      }

      private static int Partition(int[] arr, int low, int high)
      {
        int i = low - 1;
        for (int j = low; j < high; j++)
        {
          if (arr[j] < arr[high])
          {
            Swap(arr, ++i, j);
          }
        }
        Swap(arr, ++i, high);
        return i;
      }

      private static void Swap(int[] arr, int i1, int i2)
      {
        if (i1 == i2) return;
        int temp = arr[i1];
        arr[i1] = arr[i2];
        arr[i2] = temp;
      }
    }
  }
}