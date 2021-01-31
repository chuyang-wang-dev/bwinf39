using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Aufgabe1.LinearProgramming;

namespace Aufgabe1
{
  public class CompleteSearch
  {
    private readonly List<int[]> demands;
    private readonly bool[] avaliable;
    private readonly Stack<int> currentDeleted;
    private readonly int[] currentMap;
    private readonly int highestValPossible;
    public List<int[]> BestCombination { get; private set; }
    public int HighestProfit { get; private set; }
    public bool IsCompleted { get; private set; }


    public int DEBUG_changedCount = 0;
    public int DEBUG_pruningCount = 0;

    public CompleteSearch(List<int[]> demands)
    {
      this.demands = new List<int[]>(demands);
      currentDeleted = new Stack<int>();
      avaliable = new bool[demands.Count];
      for (int i = 0; i < demands.Count; i++) avaliable[i] = true;
      currentMap = new int[FlohmarktManagement.INTERVAL_LENGTH];

      HighestProfit = 0;

      // Initialize the map
      for (int c = 0, cCount = 0; c < FlohmarktManagement.INTERVAL_LENGTH; c++, cCount = 0)
      {
        for (int i = 0; i < demands.Count; i++)
        {
          if (avaliable[i] && demands[i][0] <= c + FlohmarktManagement.START_TIME && demands[i][1] > c + FlohmarktManagement.START_TIME)
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

    public void Process(CancellationToken cancelToken)
    {
      if (demands.Count < 50) {
        ProcessFromZero(cancelToken);
        return;
      }

      BestCombination = null;

      for (int i = highestValPossible - 1; i > 0; i -= (int)Math.Ceiling(100d / (double)demands.Count))
      {
        HighestProfit = i;
        RecRemove(FindFirstConflictCol(0), cancelToken);

        if (BestCombination != null) {
          IsCompleted = true;
          break;
        }
        else if (cancelToken.IsCancellationRequested) {
          IsCompleted = false;
          HighestProfit = 0;
          break;
        }
      }
    }

    public void ProcessFromZero(CancellationToken cancelToken) {
      IsCompleted = true;
      HighestProfit = 0;
      RecRemove(FindFirstConflictCol(0), cancelToken);
    }

    private void RecRemove(Tuple<int, List<int[]>> firstConflict, CancellationToken cancelToken)
    {
      if (cancelToken.IsCancellationRequested)
      {
        Restore();
        IsCompleted = false;
        return;
      }

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

        RecRemove(FindFirstConflictCol(firstConflict.Item1), cancelToken);
      }
      Restore();
    }

    private void Delete(int index)
    {
      currentDeleted.Push(index);
      avaliable[index] = false;

      for (int i = demands[index][0]; i < demands[index][1]; i++)
      {
        currentMap[i - FlohmarktManagement.START_TIME] -= demands[index][2];
      }
    }

    private void Restore()
    {
      if (currentDeleted.Count == 0) return;
      int index = currentDeleted.Pop();
      avaliable[index] = true;

      for (int i = demands[index][0]; i < demands[index][1]; i++)
      {
        currentMap[i - FlohmarktManagement.START_TIME] += demands[index][2];
      }
    }

    // Col starts with index 0; Search starts with col 0
    private Tuple<int, List<int[]>> FindFirstConflictCol(int startCol = 0)
    {
      List<int[]> currentCol;
      // Traverse the cols
      for (int c = startCol; c < FlohmarktManagement.INTERVAL_LENGTH; c++)
      {
        if (currentMap[c] <= FlohmarktManagement.HEIGHT) continue;
        currentCol = Enumerable.Range(0, demands.Count)
          .Where(idx => avaliable[idx]
          && demands[idx][0] <= c + FlohmarktManagement.START_TIME
          && demands[idx][1] > c + FlohmarktManagement.START_TIME)
          .Select(i => demands[i])
          .ToList();
        return new Tuple<int, List<int[]>>(c, currentCol);
      }
      return new Tuple<int, List<int[]>>(-1, null);
    }

    private int CalcMaximumProfit()
    {
      int count = 0;
      for (int i = 0; i < FlohmarktManagement.INTERVAL_LENGTH; i++)
      {
        count += currentMap[i] > FlohmarktManagement.HEIGHT ? FlohmarktManagement.HEIGHT : currentMap[i];
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
  }
}