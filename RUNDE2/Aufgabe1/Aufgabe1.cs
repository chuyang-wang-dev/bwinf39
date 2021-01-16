using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Google.OrTools.LinearSolver;

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
      Stopwatch sw = new Stopwatch();
      Tuple<bool, int, List<int[]>> input = ReadInput();
      sw.Start();
      if (input.Item1)
      {
        // new List<int[]>() {new int[]{10,11,1,0}, new int[]{10,11,3,1}, new int[]{10,11,3,2}, new int[]{11,12,1,3}, new int[]{10,12,1,4}}
        Solve(input.Item3, new List<Tuple<int, bool>>());
        sw.Stop();
        var cs = new CompleteSearch(input.Item3);
        cs.Process1();
        cs.PrintResult();
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

    private static void Solve(List<int[]> data, List<Tuple<int, bool>> extraConstraints)
    {
      Solver solver = Solver.CreateSolver("SCIP");

      // Initialize Xn Variablen
      List<Variable> Xn = new List<Variable>();
      for (int i = 0; i < data.Count; i++)
      {
        Xn.Add(solver.MakeIntVar(0, 1, $"X{i}"));
      }

      // Constraints
      List<Constraint> constraints = new List<Constraint>();
      for (int i = 0; i < INTERVAL_LENGTH; i++)
      {
        var c = solver.MakeConstraint(0, HEIGHT, $"Ccol{i}");
        var colItems = GetItemsInCol(i, data);
        foreach (var item in colItems)
        {
          c.SetCoefficient(Xn[item], data[item][2]);
        }
        constraints.Add(c);
      }
      for (int i = 0; i < extraConstraints.Count; i++)
      {
        int val = extraConstraints[i].Item2 ? 1 : 0;
        var c = solver.MakeConstraint(val, val, $"EC{i}");
        c.SetCoefficient(Xn[extraConstraints[i].Item1], 1);
        constraints.Add(c);
      }

      var objective = solver.Objective();
      for (int i = 0; i < data.Count; i++)
      {
        objective.SetCoefficient(Xn[i], data[i].GetSize());
      }
      objective.SetMaximization();

      if (solver.Solve() == Solver.ResultStatus.OPTIMAL)
      {
        System.Console.WriteLine(solver.Objective().Value());
      }
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
      return Math.Abs(d % 1) <= (Double.Epsilon * 100);
    }
  }
}