using System;
using System.Collections.Generic;
using System.Linq;
using rat = Rationals.Rational;
using Aufgabe1.DataStructure;
using Google.OrTools.LinearSolver;
using System.Diagnostics;
using System.IO;

namespace Aufgabe1.LinearProgramming
{
  public class LinearSolver
  {
    private readonly LinearConstraint[] originConstraints;
    private readonly Objective objective;
    private readonly Heap<ConstraintWithMax> toSearch;
    private rat globalLowerBound;

    public Dictionary<string, rat> BestSolution;


    public LinearSolver(LinearConstraint[] LCs, Objective objective)
    {
      originConstraints = LCs;
      this.objective = objective;
      toSearch = new Heap<ConstraintWithMax>(50);
      globalLowerBound = 0;
    }

    public void Solve()
    {
      var c = new ConstraintWithMax(originConstraints, objective);
      SolveOne(c);
      while (toSearch.Count != 0)
      {
        SolveOne(toSearch.Pop());
      }
    }

    private void SolveOne(ConstraintWithMax c)
    {
      if (!c.IsInfeasible)
      {
        if (globalLowerBound > c.Upperbound) return;
        if (globalLowerBound < c.Lowerbound)
        {
          globalLowerBound = c.Lowerbound;
        }

        // int sol found
        if (c.Lowerbound == c.Upperbound)
        {
          toSearch.Clear();
          BestSolution = new Dictionary<string, rat>();
          foreach (var kvp in c.Answer)
          {
            BestSolution.Add(kvp.Key, kvp.Value.CanonicalForm);
          }
          return;
        }

        foreach (KeyValuePair<string, rat> pair in c.Answer)
        {
          if (pair.Key.Equals(objective.LHSVariableNames[0])) continue;
          if (!(pair.Value.FractionPart == 0))
          {
            LinearConstraint zero = new LinearConstraint(new string[] { pair.Key }, new rat[] { 1 }, 0, LinearConstraint.InequalityType.SmallerOrEqualTo);
            toSearch.Add(new ConstraintWithMax(c.Tableau, zero, objective));
            LinearConstraint one = new LinearConstraint(new string[] { pair.Key }, new rat[] { -1 }, -1, LinearConstraint.InequalityType.SmallerOrEqualTo);
            toSearch.Add(new ConstraintWithMax(c.Tableau, one, objective));
          }
        }
      }
    }

    private class ConstraintWithMax : IComparable<ConstraintWithMax>
    {
      public rat Upperbound { get; }
      public rat Lowerbound { get; }
      public Dictionary<string, rat> Answer { get; }
      public bool IsInfeasible;
      public Simplex Tableau { get; }

      public ConstraintWithMax(LinearConstraint[] LCs, Objective obj)
      {
        Simplex s = new Simplex(LCs, obj);
        s.Solve();
        if (s.Result == Simplex.ResultType.NO_FEASIBLE_SOL) throw new Exception();
        s.Cut(10);
        Answer = s.Answer;
        Tableau = s;
        rat maximalVal = s.Answer[obj.LHSVariableNames[0]];
        rat lowerBound = 0;
        for (int i = 1; i < s.Answer.Count; i++)
        {
          rat ans = s.Answer[obj.LHSVariableNames[i]];
          if (ans.FractionPart == 0)
            lowerBound += obj.LHSVariableCoefficients[i] * s.Answer[obj.LHSVariableNames[i]];
        }
        Upperbound = maximalVal;
        Lowerbound = lowerBound * -1;
        if (s.Result != Simplex.ResultType.OPTIMAL)
        {
          IsInfeasible = true;
        }
      }


      public ConstraintWithMax(Simplex old, LinearConstraint lc, Objective obj)
      {
        Simplex s = new Simplex(old);
        s.AddConstraint(lc);
        s.DualSolve();
        if (s.Result == Simplex.ResultType.OPTIMAL)
        {
          s.Cut(5);
          Answer = s.Answer;
          Tableau = s;
          rat maximalVal = s.Answer[obj.LHSVariableNames[0]];
          rat lowerBound = 0;
          for (int i = 1; i < s.Answer.Count; i++)
          {
            rat ans = s.Answer[obj.LHSVariableNames[i]];
            if (ans.FractionPart == 0)
              lowerBound += obj.LHSVariableCoefficients[i] * s.Answer[obj.LHSVariableNames[i]];
          }
          Upperbound = maximalVal;
          Lowerbound = lowerBound * -1;
          if (s.Result != Simplex.ResultType.OPTIMAL)
          {
            IsInfeasible = true;
          }
        }
        else
        {
          Upperbound = -1;
          IsInfeasible = true;
        }
      }

      public int CompareTo(ConstraintWithMax that)
      {
        if (Upperbound > that.Upperbound) return 1;
        else if (Upperbound == that.Upperbound)
        {
          if (Lowerbound > that.Lowerbound) return 1;
          else if (Lowerbound == that.Lowerbound) return 0;
          else return -1;
        }
        else return -1;
      }
    }

    public static class Tester
    {
      public static void GoogleSolve(List<int[]> data)
      {
        static int[] GetItemsInCol(int c, List<int[]> data)
        {
          return Enumerable.Range(0, data.Count).
            Where(idx => data[idx][0] <= c + FlohmarktManagement.START_TIME && data[idx][1] > c + FlohmarktManagement.START_TIME).
            ToArray();
        }

        // Google Solver
        Solver solver = Solver.CreateSolver("SCIP");

        // Initialize Xn Variablen
        List<Variable> Xn = new List<Variable>();
        for (int i = 0; i < data.Count; i++)
        {
          Xn.Add(solver.MakeIntVar(0, 1, $"X{i}"));
        }

        // Constraints
        List<Constraint> constraintsG = new List<Constraint>();
        for (int i = 0; i < FlohmarktManagement.INTERVAL_LENGTH; i++)
        {
          var c = solver.MakeConstraint(0, FlohmarktManagement.HEIGHT, $"Ccol{i}");
          var colItems = GetItemsInCol(i, data);
          foreach (var item in colItems)
          {
            c.SetCoefficient(Xn[item], data[item][2]);
          }
          constraintsG.Add(c);
        }

        var objective = solver.Objective();
        for (int i = 0; i < data.Count; i++)
        {
          objective.SetCoefficient(Xn[i], data[i].GetSize());
        }
        objective.SetMaximization();

        if (solver.Solve() == Solver.ResultStatus.OPTIMAL)
        {
          for (int i = 0; i < Xn.Count; i++)
          {
            System.Console.WriteLine($"X{i}: " + Xn[i].SolutionValue());
          }
          System.Console.WriteLine(solver.Objective().Value());
        }
      }

      public static void Test()
      {
        //   Solver solver = Solver.CreateSolver("SCIP");
        //   var v1 = solver.MakeIntVar(0, 1, "v1");
        //   var v2 = solver.MakeIntVar(0, 1, "v2");
        //   var v3 = solver.MakeIntVar(0, 1, "v3");
        //   var v4 = solver.MakeIntVar(0, 1, "v4");
        //   var v5 = solver.MakeIntVar(0, 1, "v5");
        //   solver.Add(v1 * 1 + v2 * 3 + v3 * 3 + v4 * 1 <= 4);
        //   solver.Add(v4 * 1 + v5 * 1 <= 4);
        //   solver.Maximize(v1 * 1 + v2 * 3 + v3 * 3 + v4 * 1 + v5 * 2);
        //   solver.Solve();
        //   Console.WriteLine("Objective value = " + solver.Objective().Value());
        //   Console.WriteLine("v1 = " + v1.SolutionValue());
        //   Console.WriteLine("v2 = " + v2.SolutionValue());
        //   Console.WriteLine("v3 = " + v3.SolutionValue());
        //   Console.WriteLine("v4 = " + v4.SolutionValue());
        //   Console.WriteLine("v5 = " + v5.SolutionValue());

        //   var lcs = new LinearConstraint[] {
        //   new LinearConstraint(new string[] { "v1" }, new rat[] { 1}, 1, LinearConstraint.InequalityType.SmallerOrEqualTo),
        //   new LinearConstraint(new string[] { "v2" }, new rat[] { 1}, 1, LinearConstraint.InequalityType.SmallerOrEqualTo),
        //   new LinearConstraint(new string[] { "v3" }, new rat[] { 1}, 1, LinearConstraint.InequalityType.SmallerOrEqualTo),
        //   new LinearConstraint(new string[] { "v4" }, new rat[] { 1}, 1, LinearConstraint.InequalityType.SmallerOrEqualTo),
        //   new LinearConstraint(new string[] { "v5" }, new rat[] { 1}, 1, LinearConstraint.InequalityType.SmallerOrEqualTo),
        //   new LinearConstraint(new string[] { "v1", "v2", "v3", "v4" }, new rat[] { 1,3,3,1}, 4, LinearConstraint.InequalityType.SmallerOrEqualTo),
        //   new LinearConstraint(new string[] { "v4", "v5" }, new rat[] { 1,1}, 4, LinearConstraint.InequalityType.SmallerOrEqualTo),
        // };
        //   var obj = new Objective(new string[] { "v1", "v2", "v3", "v4", "v5" }, new rat[] { 1, 3, 3, 1, 2 });
        //   var ls = new LinearSolver(lcs, obj);
        //}
        var lcs = new LinearConstraint[] {
        new LinearConstraint(new string[] {"v1", "v2"}, new rat[] {-5,4}, 0, LinearConstraint.InequalityType.SmallerOrEqualTo),
        new LinearConstraint(new string[] {"v1", "v2"}, new rat[] {5,2}, 15, LinearConstraint.InequalityType.SmallerOrEqualTo)
        };
        var o = new Objective(new string[] { "v1", "v2" }, new rat[] { 1, 1 });
        var ls = new LinearSolver(lcs, o);
        ls.Solve();
      }
    }
  }
  public class Simplex
  {
    public const int M = 100000;
    public const int CUT_D_LIMIT = 300;
    public ResultType Result { get; private set; }
    public Dictionary<string, rat> Answer { get; private set; }
    private static readonly Random rnd = new Random();


    public List<List<rat>> Left { get; }
    public List<string> EntryVariableNames { get; }
    public Dictionary<string, int> NamesToEntries { get; }
    public List<rat> Right { get; }
    public List<string> RowPivotsNames { get; }
    public int UserVarCount { get; }
    public int SlackCount { get; private set; }

    public Simplex(LinearConstraint[] LCs, Objective objective)
    {
      Result = ResultType.NOT_PROCESSED;
      Answer = new Dictionary<string, rat>();

      // All 0-1 Variables
      UserVarCount = objective.Count;
      int colCount = UserVarCount;
      // Slack, Surplus and Artificial Variables
      foreach (LinearConstraint lc in LCs)
      {
        switch (lc.Type)
        {
          // Surplus + Artificial
          case LinearConstraint.InequalityType.BiggerOrEqualTo:
            colCount += 2;
            break;
          // Artificial
          case LinearConstraint.InequalityType.EqualTo:
            colCount += 1;
            break;
          // Slack
          case LinearConstraint.InequalityType.SmallerOrEqualTo:
            colCount += 1;
            break;
        }
      }
      // Constraints + objective func
      int rowCount = LCs.Length + 1;
      // Ptr (col entry) fuer slack, surplus and artificial vars
      int colPtr = objective.Count;
      Left = new List<List<rat>>(rowCount);
      EntryVariableNames = new List<string>(new string[colCount]);
      NamesToEntries = new Dictionary<string, int>();
      Right = new List<rat>(new rat[rowCount]);
      RowPivotsNames = new List<string>(new string[rowCount]);

      Left.Add(new List<rat>(new rat[colCount]));
      for (int i = 0; i < objective.Count; i++)
      {
        Left[0][i] = objective.LHSVariableCoefficients[i];
        EntryVariableNames[i] = objective.LHSVariableNames[i];
        NamesToEntries.Add(objective.LHSVariableNames[i], i);
      }
      Right[0] = objective.RHSValue;
      RowPivotsNames[0] = objective.LHSVariableNames[0];


      // Fuer Constraints
      SlackCount = 0; int artificialCount = 0;
      for (int row = 1; row <= LCs.Length; row++)
      {
        Right[row] = LCs[row - 1].RHSValue;
        Left.Add(new List<rat>(new rat[colCount]));
        for (int j = 0; j < LCs[row - 1].Count; j++)
        {
          if (NamesToEntries.TryGetValue(LCs[row - 1].LHSVariableNames[j], out int entry))
          {
            Left[row][entry] = LCs[row - 1].LHSVariableCoefficients[j];
          }
          else
          {
            // Should not really happen because already added by objective func
            // Let see and test
            throw new Exception();
          }
        }

        // Add Slack, Surplus and Artificial Variables 
        // Vgl. Big M Method
        if (LCs[row - 1].Type == LinearConstraint.InequalityType.SmallerOrEqualTo)
        {
          AddVar($"s{SlackCount++}", ref colPtr);
        }
        else if (LCs[row - 1].Type == LinearConstraint.InequalityType.EqualTo)
        {
          AddRow(row, -1 * M, 0);
          AddVar($"a{artificialCount++}", ref colPtr);
        }
        else if (LCs[row - 1].Type == LinearConstraint.InequalityType.BiggerOrEqualTo)
        {
          // Surplus
          AddVar($"s{SlackCount++}", ref colPtr);
          // Artificial
          AddRow(row, -1 * M, 0);
          AddVar($"a{artificialCount++}", ref colPtr);
        }

        void AddVar(string name, ref int entry)
        {
          NamesToEntries.Add(name, entry);
          EntryVariableNames[entry] = name;
          Left[row][entry] = 1;
          RowPivotsNames[row] = name;
          entry++;
        }
      }
      if (!HasFeasibleSol()) Result = ResultType.NO_FEASIBLE_SOL;
    }

    public Simplex(Simplex s)
    {
      Left = new List<List<rat>>();
      for (int i = 0; i < s.Left.Count; i++)
      {
        Left.Add(new List<rat>(s.Left[i]));
      }
      Right = new List<rat>(s.Right);
      UserVarCount = s.UserVarCount;
      SlackCount = s.SlackCount;
      RowPivotsNames = new List<string>(s.RowPivotsNames);
      EntryVariableNames = new List<string>(s.EntryVariableNames);
      NamesToEntries = s.NamesToEntries.ToDictionary(entry => entry.Key, entry => entry.Value);
    }

    public void AddConstraint(LinearConstraint lc)
    {
      if (lc.Type != LinearConstraint.InequalityType.SmallerOrEqualTo)
        throw new NotImplementedException();

      if (lc.LHSVariableCoefficients.Count == 0 && lc.RHSValue.IsZero) return;

      Right.Add(lc.RHSValue);
      for (int i = 0; i < Left.Count; i++) Left[i].Add(0);
      Left.Add(new List<rat>(new rat[Left[0].Count]));
      for (int i = 0; i < lc.LHSVariableCoefficients.Count; i++)
      {
        Left[^1][NamesToEntries[lc.LHSVariableNames[i]]] = lc.LHSVariableCoefficients[i];
      }

      // new slack Var
      Left[^1][^1] = 1;
      NamesToEntries.Add($"s{SlackCount}", Left[0].Count - 1);
      EntryVariableNames.Add($"s{SlackCount}");
      RowPivotsNames.Add($"s{SlackCount}");
      SlackCount++;

      for (int i = 1; i < Left[0].Count - 1; i++)
      {
        if (Left[^1][i] > 0 && RowPivotsNames.Contains(EntryVariableNames[i]))
        {
          AddRow(
            RowPivotsNames.FindIndex(n => n == EntryVariableNames[i]),
            -1 * Left[^1][i],
            Left.Count - 1);
        }
      }
    }

    public void Solve()
    {
      if (Result == ResultType.NO_FEASIBLE_SOL) return;

      int FindMostNegativValCol()
      {
        int c = 1;
        List<int> allCols = new List<int>
        {
          c
        };
        for (int i = 2; i < Left[0].Count; i++)
        {
          if (Left[0][c] > Left[0][i])
          {
            c = i;
            allCols.Clear();
            allCols.Add(i);
          }
          else if (Left[0][c].Equal(Left[0][i])) allCols.Add(i);
        }
        if (Left[0][c] >= 0) return -1;
        return allCols[rnd.Next(allCols.Count)];
      }

      int FindSmallestRatioRow(int inCol)
      {
        int r = 1;
        while (Left[r][inCol] <= 0) r++;
        for (int i = r + 1; i < Left.Count; i++)
        {
          if (Left[i][inCol] > 0 && Right[i] / Left[i][inCol] >= 0
              && Right[r] / Left[r][inCol] > Right[i] / Left[i][inCol])
          {
            r = i;
          }
        }
        return r;
      }

      int col = FindMostNegativValCol();
      while (col != -1)
      {
        int row = FindSmallestRatioRow(col);
        RowPivotsNames[row] = EntryVariableNames[col];
        ReduceRow(row, col);
        for (int i = 0; i < Left.Count; i++)
        {
          if (i == row) continue;
          if (!Left[i][col].IsZero)
          {
            AddRow(row, Left[i][col] * -1, i);
          }
        }
        col = FindMostNegativValCol();
      }
      if (IsFeasibleSol())
      {
        Result = ResultType.OPTIMAL;
        SetAnswer();
      }
      // Should not really happen though
      else Result = ResultType.NO_FEASIBLE_SOL;
    }

    // Gomory Cut and reoptimize with dual method
    public void Cut(int n)
    {
      if (!IsFeasibleSol())
      {
        Debug.WriteLine("Error: Cut with non-feasible solution");
        return;
      }

      for (int count = 0; count < n; count++)
      {
        int r = 1;
        while (Right[r].FractionPart == 0)
        {
          r++;
          if (r == Right.Count) return;

        }
        for (int row = 2; row < Right.Count; row++)
        {
          if (Right[row].FractionPart != 0)
          {
            if (Right[row].Denominator > CUT_D_LIMIT) continue;
            if (Right[r].FractionPart < Right[row].FractionPart) r = row;
          }
        }
        // Turn from >= to <= by reversing sign (* -1)
        LinearConstraint newLc = new LinearConstraint(Right[r].FractionPart * -1, LinearConstraint.InequalityType.SmallerOrEqualTo);
        for (int col = 1; col < Left[0].Count; col++)
        {
          newLc.SetCoefficient(EntryVariableNames[col], Left[r][col].FractionPart * -1);
        }
        AddConstraint(newLc);
        DualSolve();
      }
    }

    private void SetAnswer()
    {
      if (Result != ResultType.OPTIMAL) throw new Exception();
      Answer = new Dictionary<string, rat>();
      for (int i = 0; i < UserVarCount; i++)
      {
        Answer[EntryVariableNames[i]] = 0;
      }
      for (int i = 0; i < RowPivotsNames.Count; i++)
      {
        if (Answer.ContainsKey(RowPivotsNames[i]))
          Answer[RowPivotsNames[i]] = Right[i].CanonicalForm;

        if (Right[i].CanonicalForm > 1 && RowPivotsNames[i][0] == 'x')
        {
          System.Console.WriteLine();
        }
      }
    }

    // Solve using dual 
    // bzw. transposed matrix
    public void DualSolve()
    {
      int GetMostNegativeRow()
      {
        List<int> rtr = new List<int>() { 1 };
        for (int r = 2; r < Right.Count; r++)
        {
          if (Right[r] < Right[rtr[0]])
          {
            rtr.Clear();
            rtr.Add(r);
          }
          else if (Right[r].Equal(Right[rtr[0]])) rtr.Add(r);
        }
        if (Right[rtr[0]] >= 0) return -1;
        return rtr[rnd.Next(rtr.Count)];
      }

      int GetMinRatioCol(int r)
      {
        int col = 1;
        while (Left[r][col] >= 0)
        {
          col++;
          if (col >= Left[r].Count) return -1;
        }
        for (int c = col + 1; c < Left[0].Count; c++)
        {
          if (Left[r][c] < 0) 
          {
            if (!(Left[0][c] / Left[r][c] <= 0))
            {
              throw new Exception();
            }
            if (Left[0][c] / Left[r][c] > Left[0][col] / Left[r][col])
              col = c;
          }
        }
        return col;
      }

      int row = GetMostNegativeRow();
      while (row != -1)
      {
        int col = GetMinRatioCol(row);
        if (col == -1)
        {
          Result = ResultType.NO_FEASIBLE_SOL;
          return;
        }
        RowPivotsNames[row] = EntryVariableNames[col];
        ReduceRow(row, col);
        for (int i = 0; i < Left.Count; i++)
        {
          if (i == row) continue;
          if (!Left[i][col].IsZero)
          {
            AddRow(row, Left[i][col] * -1, i);
          }
        }
        row = GetMostNegativeRow();
      }

      // Solve();
      if (IsFeasibleSol()) SetAnswer();
      else
      {
        Result = ResultType.NO_FEASIBLE_SOL;
      }
    }

    private void ReduceRow(int row, int respectToCol)
    {
      rat divisor = Left[row][respectToCol].CanonicalForm;
      for (int i = 0; i < Left[0].Count; i++)
      {
        Left[row][i] /= divisor;
        Left[row][i] = Left[row][i].CanonicalForm;
      }
      Right[row] /= divisor;
      Right[row] = Right[row].CanonicalForm;
    }

    private void AddRow(int row, rat coefficient, int toRow)
    {
      Right[toRow] = Right[toRow] + Right[row] * coefficient;
      Right[toRow] = Right[toRow].CanonicalForm;
      for (int col = 0; col < Left[0].Count; col++)
      {
        Left[toRow][col] = Left[toRow][col] + Left[row][col] * coefficient;
        Left[toRow][col] = Left[toRow][col].CanonicalForm;
      }
    }

    public bool HasFeasibleSol()
    {
      for (int i = 1; i < RowPivotsNames.Count; i++)
      {
        if (Right[i] < 0)
        {
          return false;
        }
      }
      return true;
    }

    public bool IsFeasibleSol()
    {
      if (!HasFeasibleSol()) return false;
      for (int i = 1; i < RowPivotsNames.Count; i++)
      {
        if (RowPivotsNames[i][0] == 'a' && !Right[i].IsZero) return false;
      }
      return true;
    }

    // TODO: DEBUG
    public void ToFile()
    {
      string path = "F:\\repos\\JWI2020\\RUNDE2\\Aufgabe1\\t.txt";
      File.AppendAllText(path, "\r\n\r\n");
      File.AppendAllText(path, ToString());
    }

    public override string ToString()
    {
      static string GetSpace(int c)
      {
        if (c < 0) return "";
        return string.Join("", Enumerable.Range(0, c).Select(c => " "));
      }

      string SEPERATOR = ", ";
      int MAX_WIDTH = 8;

      string result = "--" + GetSpace(MAX_WIDTH - 2) + SEPERATOR;
      result += string.Join(SEPERATOR, EntryVariableNames.Select(s => s + GetSpace(MAX_WIDTH - s.Length)));
      result += " | RH ";

      for (int i = 0; i < Left.Count; i++)
      {
        result += "\r\n";
        result += RowPivotsNames[i] + GetSpace(MAX_WIDTH - RowPivotsNames[i].Length) + SEPERATOR;
        result += string.Join(SEPERATOR, Left[i].Select(r => r + GetSpace(MAX_WIDTH - r.ToString().Length)));
        result += $" | {Right[i]}";
      }

      return result;
    }

    public enum ResultType
    {
      OPTIMAL,
      NO_FEASIBLE_SOL,
      // NOT IMPLEMENTED
      TIME_LIMIT_EXCEEDED,
      NOT_PROCESSED
    }
  }

  public abstract class LinearExpression
  {
    public List<string> LHSVariableNames { get; protected set; }
    public List<rat> LHSVariableCoefficients { get; protected set; }
    public rat RHSValue { get; protected set; }
    public int Count { get => LHSVariableNames.Count; }

    public void SetCoefficient(string variableName, rat coefficient)
    {
      if (coefficient.IsZero) return;
      LHSVariableNames.Add(variableName);
      LHSVariableCoefficients.Add(coefficient);
    }
  }

  public class LinearConstraint : LinearExpression
  {
    public enum InequalityType
    {
      BiggerOrEqualTo,
      SmallerOrEqualTo,
      EqualTo
    }

    public InequalityType Type { get; }

    public LinearConstraint(string[] names, rat[] cfs, rat equalsTo, InequalityType inequalityType)
    {
      if (names.Length != cfs.Length) throw new ArgumentException(nameof(names));
      Type = inequalityType;
      LHSVariableNames = new List<string>(names);
      LHSVariableCoefficients = new List<rat>(cfs);
      RHSValue = equalsTo;
    }

    public LinearConstraint(rat equalsTo, InequalityType inequalityType)
    {
      Type = inequalityType;
      RHSValue = equalsTo;
      LHSVariableNames = new List<string>();
      LHSVariableCoefficients = new List<rat>();
    }
  }

  // Angenommen, Maximazation
  public class Objective : LinearExpression
  {
    public Objective(string[] names, rat[] cfs)
    {
      if (names.Length != cfs.Length) throw new ArgumentException(nameof(names));
      LHSVariableNames = new List<string> { "P" };
      LHSVariableNames.AddRange(names);
      LHSVariableCoefficients = new List<rat>() { 1 };
      LHSVariableCoefficients.AddRange(cfs.Select(cf => cf * -1));
      RHSValue = 0;
    }
  }
}