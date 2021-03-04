using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using Aufgabe1.Drawing;
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
      var arg = ParseArgs(args);
      Stopwatch sw = new Stopwatch();
      Tuple<bool, int, List<int[]>> input = ReadInput();
      if (input.Item1)
      {
        var cToken = new CancellationTokenSource();
        var sToken = new CancellationTokenSource();

        // selbst implementierte Simplex Methode 
        Tuple<bool, Dictionary<string, rat>> res = new Tuple<bool, Dictionary<string, rat>>(false, null);
        var simplexThread = new Thread(() => res = Solve(input.Item3, sToken.Token));

        CompleteSearch c = new CompleteSearch(input.Item3);
        var searchThread = new Thread(() => c.Process(cToken.Token));

        // Falls OrTools nicht explizit erlaubt ist
        // Kombination von CompleteSearch und Simplex
        if (!arg.UseGoogle)
        {
          simplexThread.Start();
          // Falls durch Kommando-Zeile-Argumente 
          // CompleteSearch nicht explizit verboten wird
          if (!arg.ForceSimplex)
          {
            searchThread.Start();
          }
          // Messen von Zeit
          sw.Start();
          while ((arg.ForceSimplex || searchThread.IsAlive) && simplexThread.IsAlive)
          {
            Thread.Sleep(100);
            if (sw.ElapsedMilliseconds > arg.TimeLimit)
            {
              cToken.Cancel();
              sToken.Cancel();
              sw.Stop();
              // Warten auf die Threads aufzuhoeren
              while ((!arg.ForceSimplex && searchThread.IsAlive) || simplexThread.IsAlive)
                Thread.Sleep(300);
              System.Console.WriteLine($"TIME LIMIT EXCEEDED: {sw.ElapsedMilliseconds}ms");
              if (res.Item2["P"] > c.HighestProfit)
              {
                // TODO
                System.Console.WriteLine(res.Item2["P"]);
              }
              else
              {
                // TODO AUSGABE
                System.Console.WriteLine(c.HighestProfit);
              }
            }
          }
          if (!arg.ForceSimplex && c.IsCompleted)
          {
            c.PrintResult();
            sToken.Cancel();
          }
          else if (res.Item1)
          {
            cToken.Cancel();
            PrintResult(res.Item2);
          }

        }
        // Falls Google Solver durch KommandoZeile-Argumente
        // explizit erlaubt wurde
        else
        {
          Dictionary<string, rat> result = LinearSolver.GoogleSolve(input.Item3);
          PrintResult(result);
          Painter.Paint("", result, input.Item3);
        }
      }
      else
      {
        Console.WriteLine("Die gegebene Datei-Namen ist nicht gueltig. Das Programm und die Datei muessen in demselben Ordner stehen. ");
      }
      Console.WriteLine(sw.ElapsedMilliseconds);
      Console.ReadKey();
    }

    private static CommandLineOptions ParseArgs(string[] args)
    {
      int timeLimit = Int32.MaxValue;
      bool g = false;
      bool simplex = false;
      foreach (var arg in args)
      {
        string[] keyAndValue = arg.Trim().Split('=');
        switch (keyAndValue[0])
        {
          case "--time-limit":
            if (!Int32.TryParse(keyAndValue[1], out timeLimit))
            {
              System.Console.WriteLine($"Gegebener Wert fuer das Kommandozeilen-Argument '{keyAndValue[0]}' ist nicht gueltig. Vergleich Dokumentation. ");
            }
            break;
          case "--use-google":
            if (!bool.TryParse(keyAndValue[1], out g))
            {
              System.Console.WriteLine($"Gegebener Wert fuer das Kommandozeilen-Argument '{keyAndValue[0]}' ist nicht gueltig. Vergleich Dokumentation. ");
            }
            break;
          case "--force-simplex":
            if (!bool.TryParse(keyAndValue[1], out simplex))
            {
              System.Console.WriteLine($"Gegebener Wert fuer das Kommandozeilen-Argument '{keyAndValue[0]}' ist nicht gueltig. Vergleich Dokumentation. ");
            }
            break;
          default:
            System.Console.WriteLine($"Gegebene Kommandozeilen-Argument '{keyAndValue[0]}' ist nicht gueltig. Vergleich Dokumentation. ");
            break;
        }
      }
      return new CommandLineOptions(timeLimit, g, simplex);
    }

    private readonly struct CommandLineOptions
    {
      public CommandLineOptions(int timeLimit = Int32.MaxValue,
                                bool useGoogle = false,
                                bool forceSimplex = false)
      {
        TimeLimit = timeLimit;
        UseGoogle = useGoogle;
        ForceSimplex = forceSimplex;
      }

      public readonly int TimeLimit { get; }
      public readonly bool UseGoogle { get; }
      public readonly bool ForceSimplex { get; }

      public override string ToString()
      {
        return $@"--time-limit={TimeLimit} 
        --use-google={UseGoogle} 
        --force-simplex={ForceSimplex}";
      }
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

    private static void PrintResult(Dictionary<string, rat> result)
    {
      Console.WriteLine("Die beste Auswahl ist: ");
      foreach (var kvp in result)
      {
        if (kvp.Key == "P") continue;
        System.Console.WriteLine($"{kvp.Key}: {kvp.Value}");
      }
      Console.WriteLine("-------------------");
      Console.WriteLine("Zusammenfassung: ");
      Console.WriteLine($"Diese Auswahl besteht aus {result.Where(kvp => kvp.Value == 1).Count()} Anbietern. ");
      Console.WriteLine($"Die Mieteinnahme betraegt {result["P"]} Euro. ");
    }

    public static Tuple<bool, Dictionary<string, rat>> Solve(List<int[]> data, CancellationToken cancelToken)
    {
      var res = GetConstraints(data);
      var linearSolver = new LinearSolver(res.Item1, res.Item2);
      linearSolver.Solve(cancelToken);
      if (linearSolver.IsCompleted)
      {
        return new Tuple<bool, Dictionary<string, rat>>(true, linearSolver.BestSolution);
      }
      else
      {
        return new Tuple<bool, Dictionary<string, rat>>(false,
          linearSolver.CurrentSolution ?? new Dictionary<string, rat>() {
          {"P", 0}
        });
      }
    }

    private static Tuple<LinearConstraint[], Objective> GetConstraints(List<int[]> data)
    {
      List<LinearConstraint> constraints = new List<LinearConstraint>();
      for (int i = 0; i < INTERVAL_LENGTH; i++)
      {
        var c = new LinearConstraint(HEIGHT, LinearConstraint.InequalityType.SmallerOrEqualTo);
        var colItems = GetItemsInCol(i, data);
        foreach (var item in colItems)
        {
          c.SetCoefficient($"x{item}", data[item][2]);
        }
        constraints.Add(c);
      }
      for (int i = 0; i < data.Count; i++)
      {
        constraints.Add(new LinearConstraint(new string[] { $"x{i}" }, new rat[] { 1 }, 1, LinearConstraint.InequalityType.SmallerOrEqualTo));
      }


      var objective = new Objective(Enumerable.Range(0, data.Count)
                                                                .Select(i => $"x{i}")
                                                                .ToArray(),
                                                      Enumerable.Range(0, data.Count)
                                                      .Select(i => (rat)data[i].GetSize())
                                                      .ToArray());
      return new Tuple<LinearConstraint[], Objective>(constraints.ToArray(), objective);
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

    public static rat Copy(this rat r)
    {
      if (r.Denominator.Equals(0)) return 0;
      return new rat(r.Numerator, r.Denominator);
    }
  }
}