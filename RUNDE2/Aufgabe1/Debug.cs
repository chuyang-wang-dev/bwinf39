using System;
using System.Linq;
using System.Collections.Generic;

namespace Aufgabe1
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
}