using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Aufgabe1
{
  public class Aufgabe1Stats
  {
    private const int MAXIMUM_HEIGHT = 40;
    public List<double> UsedTimeEachBF { get; private set; }
    public List<double> UsedTimeEachTR { get; private set; }
    public Aufgabe1Stats()
    {
      UsedTimeEachBF = new List<double>();
      UsedTimeEachTR = new List<double>();
    }

    public void StartNth(int n)
    {
      Stopwatch sw = new Stopwatch();

      for (int i = 0; i < n; i++)
      {
        RandomBlockGenerator rbg = new RandomBlockGenerator(FlohmarktManagement.START_TIME, FlohmarktManagement.END_TIME, MAXIMUM_HEIGHT, 15);
        List<int[]> testData = rbg.GetResult();

        sw.Start();
        FlohmarktManagement calc = new FlohmarktManagement(testData);
        calc.Process();
        Console.WriteLine($"DP: {calc.HighestProfit}");
        sw.Stop();
        UsedTimeEachTR.Add(sw.ElapsedMilliseconds);
        sw.Reset();
        Console.WriteLine($"DP: {new BFFind(testData).GetMaximum()}");

        Console.WriteLine("----------------");
      }
    }

    // Cf. Commit bcdbef
    /*
    public AnalysisResult StartAnalysis(int startN, int endN, int interval, int overtime = 10000, int testFor = 10)
    {
      List<List<double>> runtimes = new List<List<double>>();
      for (int n = startN; n < endN; n += interval)
      {
        runtimes.Add(new List<double>());
        for (int j = 0; j < testFor; j++)
        {
          List<int[]> testData = new RandomBlockGenerator(FlohmarktManagement.START_TIME, FlohmarktManagement.END_TIME, MAXIMUM_HEIGHT, n).GetResult();
          FlohmarktManagement calc = new FlohmarktManagement(testData);
          calc.ProcessWithTimeout(overtime);
          Console.WriteLine(calc.HighestProfit);
          runtimes[^1].Add(calc.UsedTime);
        }
      }
      AnalysisResult result = new AnalysisResult(startN, endN, interval, overtime, runtimes);
      return result;
    }
*/

    public class AnalysisResult
    {
      public List<double> Averanges { get; private set; }
      public List<double> SDiviations { get; private set; }
      public int Timeout { get; private set; }
      public AnalysisResult(int start, int end, int interval, int timeout, List<List<double>> runtimes)
      {
        Timeout = timeout;
        Averanges = new List<double>();
        SDiviations = new List<double>();
        for (int n = start, i = 0; n < end; n += interval, i++)
        {
          Averanges.Add(Aufgabe1Stats.GetStandardDeviation(runtimes[i]));
          SDiviations.Add(Aufgabe1Stats.GetStandardDeviation(runtimes[i]));
        }
      }
    }

    public static double GetStandardDeviation(List<double> doubleList)
    {
      if (doubleList.Count == 0) return 0;
      double average = doubleList.Average();
      double sumOfDerivation = 0;
      foreach (double value in doubleList)
      {
        sumOfDerivation += (value) * (value);
      }
      double sumOfDerivationAverage = sumOfDerivation / (doubleList.Count - 1);
      return Math.Sqrt(sumOfDerivationAverage - (average * average));
    }

    public static double GetAverange(List<double> doubleList)
    {
      if (doubleList.Count <= 0) return 0;
      return doubleList.Average();
    }
  }
}