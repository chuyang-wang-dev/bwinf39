using System;
using System.Linq;
using System.Collections.Generic;

namespace Aufgabe1.Debug
{
  // DEBUG
  // DO NOT Submit this class
  // TEST ONLY
  public class RandomBlockGenerator
  {
    private readonly List<int[]> Result;
    public readonly int Seed;
    private static Random seedGiver = new Random();
    public RandomBlockGenerator(int startTime, int endTime, int maximumHeight, int number, int? seed = null)
    {
      Result = new List<int[]>(number);
      Random rng;

      if (seed != null)
      {
        Seed = (int)seed;
        seedGiver = new Random(Seed);
        rng = new Random(Seed);
      }
      else
      {
        Seed = seedGiver.Next();
        rng = new Random(Seed);
      }

      for (int i = 0; i < number; i++)
      {
        int[] current = new int[4];
        current[0] = rng.Next(startTime, endTime);
        current[1] = rng.Next(current[0] + 1, endTime + 1);
        current[2] = rng.Next(1, maximumHeight + 1);
        // AS id
        current[3] = i;
        Result.Add(current);
      }
      Console.WriteLine($"Using seed: {Seed}");
    }

    public List<int[]> GetResult()
    {
      return Result;
    }

    public void ToFile(string fileName)
    {

    }
  }


  // DEBUG
  // DO NOT Submit this class
  // TEST ONLY
  // USE FOR OBJECTS LESS THAN 20
  // O(2^n)
  public class BFFind
  {
    private readonly int[] table = new int[FlohmarktManagement.END_TIME - FlohmarktManagement.START_TIME];
    private int maximumProfit;
    private List<int[]> bestCombo;

    public BFFind(List<int[]> blocks)
    {
      maximumProfit = 0;
      bestCombo = new List<int[]>();

      IEnumerable<List<int[]>> combinations = Permutations(blocks);
      Console.WriteLine(combinations.Count());

      foreach (List<int[]> possible in combinations)
      {
        Check(possible);
      }
    }

    public int GetMaximum()
    {
      return maximumProfit;
    }

    public List<int[]> GetCombo()
    {
      return bestCombo;
    }

    private void Check(List<int[]> set)
    {
      int currentProfit = 0;
      for (int i = 0; i < table.Length; i++)
      {
        table[i] = 0;
      }

      foreach (var part in set)
      {
        for (int i = part[0] - FlohmarktManagement.START_TIME; i < part[1] - FlohmarktManagement.START_TIME; i++)
        {
          table[i] += part[2];
          currentProfit += part[2];
        }
      }
      if (Possible() && currentProfit > maximumProfit)
      {
        maximumProfit = currentProfit;
        bestCombo = set;
      }
    }

    private bool Possible()
    {
      foreach (int i in table)
      {
        if (i > FlohmarktManagement.HEIGHT) return false;
      }
      return true;
    }

    public static IEnumerable<List<T>> Permutations<T>(IEnumerable<T> source)
    {
      if (null == source)
        throw new ArgumentNullException(nameof(source));

      T[] data = source.ToArray();

      return Enumerable
        .Range(0, 1 << (data.Length))
        .Select(index => data
           .Where((v, i) => (index & (1 << i)) != 0)
           .ToList());
    }
  }

  // DEBUG
  public class CompleteSearch
  {
    public const int HEIGHT = 1000;
    public const int START_TIME = 8;
    public const int END_TIME = 18;
    public const int INTERVAL_LENGTH = END_TIME - START_TIME;
    
    private readonly List<int[]> demands;
    private readonly bool[] avaliable;
    private readonly Stack<int> currentDeleted;
    private readonly int[] currentMap;
    private readonly int highestValPossible;
    public List<int[]> BestCombination { get; private set; }
    public int HighestProfit { get; private set; }


    public int DEBUG_changedCount = 0;
    public int DEBUG_pruningCount = 0;

    public CompleteSearch(List<int[]> demands)
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