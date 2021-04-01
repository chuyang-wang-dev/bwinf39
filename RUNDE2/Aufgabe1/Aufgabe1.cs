using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;

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
      Console.WriteLine($"Jetzige Konfiguration: {arg}\n");

      Stopwatch sw = new Stopwatch();
      Tuple<bool, int, List<int[]>> input = ReadInput();
      // Falls der eingegebene Datei-Name gueltig ist
      if (input.Item1)
      {
        // CancellationToken fuers Beenden der Threads
        var cToken = new CancellationTokenSource();
        var sToken = new CancellationTokenSource();

        // Eigene Simplex Methode 
        Tuple<bool, Dictionary<string, rat>> res = new Tuple<bool, Dictionary<string, rat>>(false, null);
        var simplexThread = new Thread(() => res = Solve(input.Item3, sToken.Token));

        CompleteSearch c = new CompleteSearch(input.Item3);
        var searchThread = new Thread(() => c.Process(cToken.Token));


        // Falls OrTools nicht explizit erlaubt ist, i.e. verboten ist
        // Kombination von CompleteSearch und Simplex
        if (!arg.UseGoogle)
        {
          simplexThread.Start();
          // Falls durch Kommando-Zeile-Argumente 
          // CompleteSearch nicht explizit verboten ist
          if (!arg.ForceSimplex)
            searchThread.Start();

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
                Thread.Sleep(500);

              // Falls die Zeitbeschraenkung ueberschritten wird
              Console.WriteLine($"\nTIME LIMIT EXCEEDED: {sw.ElapsedMilliseconds}ms");
              Console.WriteLine("Das koennte dazu fuehren, dass das ausgedurckte Ergebnis nicht optimal ist. ");
              // Falls Simplex ein besseres Ergebnis hat, durckt diese aus
              if (res.Item2["P"] >= c.HighestProfit)
                PrintResult(res.Item2, "Simplex");
              // und vice versa
              else
                PrintResult(c.GetResult(), "CompleteSearch");
            }
          }

          // Falls Complete Search hat das Problem zuerst geloest
          if (!arg.ForceSimplex && c.IsCompleted)
          {
            PrintResult(c.GetResult(), "CompleteSearch");
            // Die andere kann dann aufhoeren
            sToken.Cancel();
          }
          // Falls Simplex hat das Problem zuerst geloest
          else if (res.Item1)
          {
            cToken.Cancel();
            // Die andere kann dann aufhoeren
            PrintResult(res.Item2, "Simplex");
          }

        }
        // Falls Google Solver durch KommandoZeile-Argumente
        // explizit erlaubt wurde
        else
        {
          Dictionary<string, rat> result = LinearSolver.GoogleSolve(input.Item3);
          PrintResult(result, "OR-Tool (SCIP)");
        }
        Console.WriteLine($"Gesamt verwendete Zeit: {sw.ElapsedMilliseconds}");
      }
      else
      {
        Console.WriteLine("Die gegebene Datei-Namen ist nicht gueltig. Das Programm und die Datei muessen in demselben Ordner stehen. ");
      }

      Console.WriteLine("\r\nDrueck eine beliebige Taste zu schliessen...");
      Console.ReadKey();
    }

    // Liest die Test-Datei
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

    // Zeigt das Ergebnis in Console
    private static void PrintResult(Dictionary<string, rat> result, string source)
    {
      Console.WriteLine("\nDie beste Auswahl ist: ");
      Console.WriteLine("(1 bedeutet akzeptiert, 0 hingegen abgelehnt)");
      foreach (var kvp in result)
      {
        if (kvp.Key == "P") continue;
        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
      }
      Console.WriteLine("-------------------");
      Console.WriteLine("Zusammenfassung: ");
      Console.WriteLine($"\nAkzeptierte Anbieter: {string.Join(' ', result.Where(kvp => kvp.Value == 1).Select(kvp => kvp.Key[1..]))}");
      Console.WriteLine($"\nAbgelehnte Anbieter: {string.Join(' ', result.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key[1..]))}");
      Console.WriteLine($"\nDiese Auswahl besteht aus {result.Where(kvp => kvp.Value == 1).Count()} Anbietern. ");
      Console.WriteLine($"Die Mieteinnahme betraegt {result["P"]} Euro. ");
      Console.WriteLine($"\nDiese Antwort stammt aus: '{source}'");
    }

    // Loesen mit Simplex (b-a-c)
    public static Tuple<bool, Dictionary<string, rat>> Solve(List<int[]> data, CancellationToken cancelToken)
    {
      var res = GetConstraints(data);
      var linearSolver = new LinearSolver(res.Item1, res.Item2);
      linearSolver.Solve(cancelToken);
      // Falls Simplex fertig ist
      if (linearSolver.IsCompleted)
      {
        return new Tuple<bool, Dictionary<string, rat>>(true, linearSolver.BestSolution);
      }
      // Ansonsten, falls es eine relativ gute Loesung gibt, gibt diese zurueck
      // Oder 0
      else
      {
        return new Tuple<bool, Dictionary<string, rat>>(false,
          linearSolver.CurrentSolution ?? new Dictionary<string, rat>() {
          {"P", 0}}.Concat(
                      Enumerable
                      .Range(0, data.Count)
                      .ToDictionary(
                        i => $"x{i}",
                        _ => (rat)0))
                   .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        );
      }
    }

    // Bearbeitet die originelle Data und gibt sie als Beschraenkung zueurck
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

    
    private static CommandLineOptions ParseArgs(string[] args)
    {
      static void Fail(string[] kAv)
      {
        Console.WriteLine($"Gegebener Wert fuer das Kommandozeilen-Argument '{kAv[0]}' ist nicht gueltig. Vergleich Dokumentation. ");
      }

      int timeLimit = int.MaxValue;
      bool g = false;
      bool simplex = false;
      foreach (var arg in args)
      {
        string[] keyAndValue = arg.Trim().Split('=');
        switch (keyAndValue[0])
        {
          case "--time-limit":
            if (!int.TryParse(keyAndValue[1], out timeLimit))
              Fail(keyAndValue);
            break;
          case "--use-google":
            if (!bool.TryParse(keyAndValue[1], out g))
              Fail(keyAndValue);
            break;
          case "--force-simplex":
            if (!bool.TryParse(keyAndValue[1], out simplex))
              Fail(keyAndValue);
            break;
          default:
            Fail(keyAndValue);
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
        return $@"
        --time-limit={TimeLimit} 
        --use-google={UseGoogle} 
        --force-simplex={ForceSimplex}";
      }
    }

  }

  public static class IExtensions
  {
    // Gib den Flaecheninhalt bzw. die Mietannahme dieses Anbieters zurueck
    public static int GetSize(this int[] requestData)
    {
      return (requestData[1] - requestData[0]) * requestData[2];
    }
    public static bool Equal(this rat r1, rat r2)
    {
      return (r1 - r2).IsZero;
    }
  }
}
