using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aufgabe1
{
  public class FlohmarktManagement
  {
    // TODO: Delete Debug variables before submit
    public const int DEBUG_demandCount = 500;

    public const int HEIGHT = 1000;
    public const int START_TIME = 0;
    public const int END_TIME = 10;
    public const int INTERVAL_LENGTH = END_TIME - START_TIME;
    public static void Main(string[] args)
    {
      Analyse();
      /*
      RandomBlockGenerator rbg = new RandomBlockGenerator(FlohmarktManagement.START_TIME, FlohmarktManagement.END_TIME, 40, FlohmarktManagement.DEBUG_demandCount);
      List<int[]> testData = rbg.GetResult();

      FlohmarktManagement calc = new FlohmarktManagement(testData);
      calc.FindRelativSol();
      */
      Console.ReadKey();
    }

    private static void Analyse()
    {
      Aufgabe1Stats stats = new Aufgabe1Stats();
      var a = stats.StartAnalysis(10, 1010, 50, 20000, 5);
      /*
      Console.WriteLine("");
      Console.WriteLine("Bruteforce Avg:");
      Console.WriteLine($"{stats.GetAverange(stats.UsedTimeEachBF)}");
      Console.WriteLine("Bruteforce S Diviation:");
      Console.WriteLine($"{stats.GetStandardDeviation(stats.UsedTimeEachBF)}");

      Console.WriteLine($"------------------");

      Console.WriteLine("Tree Avg:");
      Console.WriteLine($"{stats.GetAverange(stats.UsedTimeEachTR)}");
      Console.WriteLine("Tree S Diviation:");
      Console.WriteLine($"{stats.GetStandardDeviation(stats.UsedTimeEachTR)}");

            var rbg = new RandomBlockGenerator(FlohmarktManagement.START_TIME, FlohmarktManagement.END_TIME, FlohmarktManagement.HEIGHT, FlohmarktManagement.DEBUG_demandCount, 401882730);
            List<int[]> testData = rbg.GetResult();

            var bruteForce = new BFFind(testData);
            Console.WriteLine($"BF: {bruteForce.GetMaximum()}");

            FlohmarktManagement calc = new FlohmarktManagement(testData);
            calc.Process();
            Console.WriteLine($"DP: {calc.HighestProfit}");

      */
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
    public long UsedTime { get; private set; }
    private int timeLimit = Int32.MaxValue;
    public bool Finished { get; private set; }
    private Stopwatch sw;

    public FlohmarktManagement(List<int[]> demands)
    {
      this.demands = new List<int[]>();
      foreach (int[] item in demands)
      {
        // Deep Copy
        this.demands.Add((int[])item.Clone());
      }
      //QuickSort.Sort(this.demands, 0, demands.Count - 1);
      currentDeleted = new Stack<int>();
      avaliable = new bool[demands.Count];
      for (int i = 0; i < demands.Count; i++) avaliable[i] = true;
      currentMap = new int[INTERVAL_LENGTH];

      BestCombination = new List<int[]>();
      HighestProfit = 0;

      // Initialize the map
      for (int c = 0, cCount = 0; c < INTERVAL_LENGTH; c++, cCount = 0)
      {
        for (int i = 0; i < demands.Count; i++)
        {
          if (avaliable[i] && demands[i][0] <= c && demands[i][1] > c)
          {
            cCount += demands[i][2];
          }
        }
        currentMap[c] = cCount;
      }
      highestValPossible = CalcMaximumProfit();

      sw = new Stopwatch();
    }

    public void FindRelativSol()
    {
      Finished = true;
      ProcessWithTimeout(60000);
      if (!Finished)
      {
        timeLimit = 30000;
        UsedTime = 0;
        for (int low = 0, high = highestValPossible, searching = (low + high) / 2;
             low < high-1 && !Finished;
            searching = (low + high + 1) / 2, UsedTime = 0, sw.Reset())
        {
          Finished = true;
          HighestProfit = searching;
          sw.Start();
          RecRemove(FindFirstConflictCol(0));

          if (HighestProfit > searching) low = HighestProfit;
          else high = searching;
        }

      }
    }

    public void ProcessWithTimeout(int timeout)
    {
      UsedTime = 0;
      timeLimit = timeout;
      sw.Start();
      Process();
      sw.Stop();
      sw.Reset();
    }

    public void Process()
    {
      BestCombination = null;

      for (int i = highestValPossible - 1; i > 0; i -= (int)Math.Ceiling(100d / (double)demands.Count))
      {
        HighestProfit = i;
        RecRemove(FindFirstConflictCol(0));

        if (BestCombination != null) break;
        if (timeLimit < UsedTime)
        {
          HighestProfit = -1;
          return;
        }
      }
    }

    private void RecRemove(Tuple<int, List<int[]>> firstConflict)
    {
      UsedTime = sw.ElapsedMilliseconds;

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
        if (timeLimit < UsedTime)
        {
          Finished = false;
          Restore();
          return;
        }
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
        currentMap[i] -= demands[index][2];
      }
    }

    private void Restore()
    {
      if (currentDeleted.Count == 0) return;
      int index = currentDeleted.Pop();
      avaliable[index] = true;

      for (int i = demands[index][0]; i < demands[index][1]; i++)
      {
        currentMap[i] += demands[index][2];
      }
    }

    // Col starts with index 0; Search starts with col 0
    private Tuple<int, List<int[]>> FindFirstConflictCol(int startCol = 0)
    {
      List<int[]> currentCol;
      // Traverse the cols
      for (int c = startCol, count = 0; c < INTERVAL_LENGTH; c++, count = 0)
      {
        currentCol = new List<int[]>();
        if (currentMap[c] <= HEIGHT) continue;
        // Traverse the demand list
        // TODO: Binary Search to improve performance
        for (int i = 0; i < demands.Count; i++)
        {
          if (avaliable[i] && demands[i][0] <= c && demands[i][1] > c)
          {
            currentCol.Add(demands[i]);
            count += demands[i][2];
          }
        }
        if (count > HEIGHT)
        {
          return new Tuple<int, List<int[]>>(c, currentCol);
        }
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
      List<int[]> l = new List<int[]>();
      for (int i = 0; i < demands.Count; i++)
      {
        if (avaliable[i]) l.Add(demands[i]);
      }
      return l;
    }

    // Angonommen, die eingegebenen Daten ist nicht nach End_time sortiert
    private static class QuickSort
    {
      public static void Sort(List<int[]> arr, int low, int high)
      {
        if (high <= low) return;

        int pIndex = Partition(arr, low, high, (a, b) => a[1] - b[1], (a, b) => a[2] * (a[1] - a[0]) - b[2] * (b[1] - b[0]));
        Sort(arr, low, pIndex - 1);
        Sort(arr, pIndex + 1, high);
      }

      private static int Partition(List<int[]> arr, int low, int high, Comparison<int[]> cp1, Comparison<int[]> cp2)
      {
        int i = low - 1;
        for (int j = low; j < high; j++)
        {
          if (cp1(arr[j], arr[high]) < 0 || (cp1(arr[j], arr[high]) == 0 && cp2(arr[j], arr[high]) > 0))
          {
            Swap(arr, ++i, j);
          }
        }
        Swap(arr, ++i, high);
        return i;
      }

      public static void SortBySizeD(List<int[]> arr, int low, int high)
      {
        if (high <= low) return;

        int pIndex = Partition(arr, low, high, (a, b) => b[2] * (b[1] - b[0]) - a[2] * (a[1] - a[0]), (a, b) => 0);
        SortBySizeD(arr, low, pIndex - 1);
        SortBySizeD(arr, pIndex + 1, high);
      }

      private static void Swap(List<int[]> arr, int i1, int i2)
      {
        if (i1 == i2) return;
        int[] temp = arr[i1];
        arr[i1] = arr[i2];
        arr[i2] = temp;
      }
    }
  }

  public static class IListExtensions
  {
    // Using binary search to insert 
    // Time complexity: O(nlogn)
    public static void InsertWithOrder<T>(this IList<T> l, T val, Comparison<T> cp)
    {
      int left = 0, right = l.Count - 1;
      while (right > left)
      {
        int checkPointIdx = (right + left) / 2;
        int res = cp(l[checkPointIdx], val);
        if (res == 0)
        {
          l.Insert(checkPointIdx, val);
          return;
        }
        else if (res > 0)
        {
          right = checkPointIdx - 1;
        }
        else
        {
          left = checkPointIdx + 1;
        }
      }
      l.Insert(left, val);
    }
  }

}