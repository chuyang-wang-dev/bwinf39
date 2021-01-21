using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Aufgabe1.LinearProgramming;
using rat = Rationals.Rational;

namespace Aufgabe1
{
  public class FlohmarktManagement
  {
    public const int HEIGHT = 1000;
    public const int START_TIME = 8;
    public const int END_TIME = 18;
    public const int INTERVAL_LENGTH = END_TIME - START_TIME;
    public static void Main(string[] args)
    {
      LinearSolver.Tester.Test();
      Stopwatch sw = new Stopwatch();
      Tuple<bool, int, List<int[]>> input = ReadInput();
      sw.Start();
      if (input.Item1)
      {
        Solve(input.Item3);
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

    public static void Solve(List<int[]> data)
    {
      LinearSolver.Tester.GoogleSolve(data);

      List<LinearConstraint> constraints = new List<LinearConstraint>();
      for (int i = 0; i < INTERVAL_LENGTH; i++)
      {
        var c = new LinearConstraint(HEIGHT, LinearProgramming.LinearConstraint.InequalityType.SmallerOrEqualTo);
        var colItems = GetItemsInCol(i, data);
        foreach (var item in colItems)
        {
          c.SetCoefficient($"x{item}", data[item][2]);
        }
        constraints.Add(c);
      }
      for (int i = 0; i < data.Count; i++)
      {
        constraints.Add(new LinearConstraint(new string[] { $"x{i}" }, new rat[] { 1 }, 1, LinearProgramming.LinearConstraint.InequalityType.SmallerOrEqualTo));
      }


      var objective = new Objective(Enumerable.Range(0, data.Count)
                                                                .Select(i => $"x{i}")
                                                                .ToArray(),
                                                      Enumerable.Range(0, data.Count)
                                                                .Select(i => (rat)data[i].GetSize())
                                                                .ToArray());

      var linearSolver = new LinearSolver(constraints.ToArray(), objective);
      linearSolver.Solve();
      System.Console.WriteLine(string.Join("\r\n", linearSolver.BestSolution.Select(kvp => kvp.Key + ": " + kvp.Value)));
    }

    private static int[] GetItemsInCol(int c, List<int[]> data)
    {
      return Enumerable.Range(0, data.Count).
        Where(idx => data[idx][0] <= c + START_TIME && data[idx][1] > c + START_TIME).
        ToArray();
    }
  }

  public static class IExtensions
  {
    public static int GetSize(this int[] requestData)
    {
      return (requestData[1] - requestData[0]) * requestData[2];
    }

    public static bool IsInt(this double d)
    {
      return Math.Abs(d % 1) <= (Double.Epsilon * 1E8);
    }

    public static bool RoughlyEqualTo(this double d1, double d2)
    {
      double epsilon = Math.Max(Math.Abs(d1), Math.Abs(d2)) * 1E-14;
      return Math.Abs(d1 - d2) <= epsilon;
    }

    public static bool IsInt(this decimal d)
    {
      return Math.Abs(Math.Round(d, 10) - d) <= d * (decimal)1E-10;
    }

    public static bool RoughlyEqualTo(this decimal d1, decimal d2)
    {
      decimal epsilon = Math.Max(Math.Abs(d1), Math.Abs(d2)) * (decimal)1E-14;
      return Math.Abs(d1 - d2) <= epsilon;
    }

    public static bool Equal(this rat r1, rat r2)
    {
      return (r1 - r2).IsZero;
    }
  }
}