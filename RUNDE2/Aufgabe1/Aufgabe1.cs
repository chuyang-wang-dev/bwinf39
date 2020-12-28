using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Aufgabe1
{
  public class FlohmarktManagement
  {
    // TODO: Delete Debug variables before submit
    public const int DEBUG_demandCount = 1500;

    public const int HEIGHT = 1000;
    public const int START_TIME = 0;
    public const int END_TIME = 10;
    public const int INTERVAL_LENGTH = END_TIME - START_TIME;
    public static void Main(string[] args)
    {

      Aufgabe1Stats stats = new Aufgabe1Stats();
      var a = stats.StartAnalysis(10, 1010, 50, 20000, 15);
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

      Console.ReadKey();
    }

    private readonly List<int[]> demands;
    private readonly bool[] avaliable;
    private readonly Stack<int> currentDeleted;
    private readonly int[] currentMap;
    private int highestValPossible, relativHighest;
    public List<int[]> BestCombination { get; private set; }
    public int HighestProfit { get; private set; }


    public int DEBUG_changedCount = 0;
    public int DEBUG_pruningCount = 0;
    public long UsedTime { get; private set; }
    private int timeLimit = Int32.MaxValue;
    public bool Finished { get; private set; }
    private Stopwatch sw = new Stopwatch();

    public FlohmarktManagement(List<int[]> demands)
    {
      this.demands = new List<int[]>();
      foreach (int[] item in demands)
      {
        // Deep Copy
        this.demands.Add((int[])item.Clone());
      }
      QuickSort.Sort(this.demands, 0, demands.Count - 1);
      currentDeleted = new Stack<int>();
      avaliable = new bool[demands.Count];
      for (int i = 0; i < demands.Count; i++) avaliable[i] = true;
      currentMap = new int[INTERVAL_LENGTH];

      BestCombination = new List<int[]>();
      HighestProfit = 0;
      relativHighest = 0;
    }

    // Angenommen, demands ist bereits nach Endzeit und Groesse sortiert
    // TODO: Optimized the start val sothat the algorithm runs faster
    private void FindRelativOptimizedSol()
    {
      BestCombination = new List<int[]>();
      Tuple<int, int[][]>[] dpChart = new Tuple<int, int[][]>[1 + demands.Count];
      dpChart[0] = new Tuple<int, int[][]>(0, new int[0][]);
      {
        bool included = false;
        for (int i = 0; i < demands.Count; i++, included = false)
        {
          BestCombination.Clear();
          for (int j = i; j >= 0; j--)
          {
            BestCombination.Add(demands[i]);
            BestCombination.AddRange(dpChart[j].Item2);
            if (AreCompatible(BestCombination) && CalcProfit(BestCombination) > dpChart[i].Item1)
            {
              dpChart[i + 1] = new Tuple<int, int[][]>(CalcProfit(BestCombination), BestCombination.ToArray());
              included = true;
              break;
            }
            BestCombination.Clear();
          }
          if (!included) dpChart[i + 1] = new Tuple<int, int[][]>(dpChart[i].Item1, dpChart[i].Item2);
        }
      }
      BestCombination = dpChart[demands.Count].Item2.ToList();
      HighestProfit = CalcProfit(BestCombination);
    }

    public void ProcessWithTimeout(int timeout)
    {
      UsedTime = 0;
      timeLimit = timeout;
      sw.Start();
      Process();
      sw.Stop();
    }

    public void Process()
    {
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

      //FindRelativOptimizedSol();
      BestCombination = null;
      highestValPossible = CalcMaximumProfit();

      for (int i = highestValPossible - 1; i > 0; i -= (int)Math.Ceiling(100d / (double)demands.Count))
      {
        HighestProfit = i;

        Tuple<int, List<int[]>> fc = FindFirstConflictCol(0);
        RecRemove(fc);

        if (BestCombination != null) break;
      }
    }

    private void RecRemove(Tuple<int, List<int[]>> firstConflict)
    {
      UsedTime = sw.ElapsedMilliseconds;
      if (timeLimit < UsedTime)
      {
        Finished = false;
        return;
      }


      int max = CalcMaximumProfit();
      // If smaller or equal the current highest, do pruning
      if (max <= HighestProfit)
      {
        if (currentDeleted.Count > 0)
        {
          relativHighest = relativHighest > max ? relativHighest : max;
          // Pop the last deleted obj and set it to avaliable
          Restore();
          DEBUG_pruningCount++;
        }
        return;
      }

      // If compatible at this stage
      if (firstConflict.Item1 < 0)
      {
        // Update highest profit
        HighestProfit = max;
        BestCombination = GetCurrentConbination();

        // Pop the last deleted obj and set it to avaliable
        if (currentDeleted.Count > 0) Restore();
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

      if (currentDeleted.Count > 0)
      {
        Restore();
      }
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


    private int old1_CalcMaximumProfit()
    {
      int count = 0;
      for (int c = 0, cCount = 0; c < INTERVAL_LENGTH; c++, cCount = 0)
      {
        for (int i = 0; i < demands.Count; i++)
        {
          if (avaliable[i] && demands[i][0] <= c && demands[i][1] > c)
          {
            cCount += demands[i][2];
          }
        }
        count += cCount > HEIGHT ? HEIGHT : cCount;
      }
      return count;
    }

    private List<int[]> GetCurrentConbination()
    {
      List<int[]> l = new List<int[]>();
      for (int i = 0; i < demands.Count; i++)
      {
        if (avaliable[i]) l.Add(demands[i]);
      }
      return l;
    }

    private static List<int[]> DeepCopy(List<int[]> l)
    {
      List<int[]> rtr = new List<int[]>();
      foreach (int[] item in l)
      {
        rtr.Add(item);
      }
      return rtr;
    }

    private static bool AreCompatible(List<int[]> l)
    {
      for (int c = 0, count = 0; c < INTERVAL_LENGTH; c++, count = 0)
      {
        for (int i = 0; i < l.Count; i++)
        {
          if (l[i][0] <= c && l[i][1] > c)
          {
            count += l[i][2];
          }
        }
        if (count > HEIGHT) return false;
      }
      return true;
    }

    private static int CalcProfit(List<int[]> l)
    {
      int c = 0;
      foreach (int[] item in l)
      {
        c += item[2] * (item[1] - item[0]);
      }
      return c;
    }

    private static int[] AddElementToSortedArr(int[] arr, int val)
    {
      if (arr.Length == 0) return new int[] { val };
      int[] rtr = new int[arr.Length + 1];
      for (int i = 0, j = 0; i < arr.Length + 1; i++)
      {
        if (val < arr[j]) rtr[i++] = val;
        if (i < arr.Length + 1) rtr[i] = arr[j];
      }
      return rtr;
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