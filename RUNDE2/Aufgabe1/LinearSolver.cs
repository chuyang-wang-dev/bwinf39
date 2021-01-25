using System;
using System.Collections.Generic;
using System.Linq;
using rat = Rationals.Rational;
using Aufgabe1.DataStructure;
using Google.OrTools.LinearSolver;
using System.Diagnostics;

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
      var c = new ConstraintWithMax(new List<LinearConstraint>(), originConstraints, objective);
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
          // BestSolution = c.Answer;
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
            List<LinearConstraint> n = new List<LinearConstraint>(c.Constraints)
            {
              new LinearConstraint(new string[] { pair.Key }, new rat[] { 1 }, 0, LinearConstraint.InequalityType.EqualTo)
            };
            toSearch.Add(new ConstraintWithMax(n, originConstraints, objective));
            n[^1] = new LinearConstraint(new string[] { pair.Key }, new rat[] { 1 }, 1, LinearConstraint.InequalityType.EqualTo);
            toSearch.Add(new ConstraintWithMax(n, originConstraints, objective));
          }
        }
      }
    }

    private class ConstraintWithMax : IComparable<ConstraintWithMax>
    {
      public rat Upperbound { get; }
      public rat Lowerbound { get; }
      public List<LinearConstraint> Constraints { get; }
      public Dictionary<string, rat> Answer { get; }
      public bool IsInfeasible;


      public ConstraintWithMax(List<LinearConstraint> constraints, LinearConstraint[] origin, Objective obj)
      {
        Constraints = new List<LinearConstraint>(constraints);

        Simplex s = new Simplex(origin.Concat(constraints).ToArray(), obj);
        s.Solve();
        if (s.Result == Simplex.ResultType.OPTIMAL)
        {
          s.Cut(10);
          // Constraints.AddRange();
          Answer = s.Answer;
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
          IsInfeasible = false;
        }
        else
        {
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
        Solver solver = Solver.CreateSolver("SCIP");
        var v1 = solver.MakeIntVar(0, 1, "v1");
        var v2 = solver.MakeIntVar(0, 1, "v2");
        var v3 = solver.MakeIntVar(0, 1, "v3");
        var v4 = solver.MakeIntVar(0, 1, "v4");
        var v5 = solver.MakeIntVar(0, 1, "v5");
        solver.Add(v1 * 1 + v2 * 3 + v3 * 3 + v4 * 1 <= 4);
        solver.Add(v4 * 1 + v5 * 1 <= 4);
        solver.Maximize(v1 * 1 + v2 * 3 + v3 * 3 + v4 * 1 + v5 * 2);
        solver.Solve();
        Console.WriteLine("Objective value = " + solver.Objective().Value());
        Console.WriteLine("v1 = " + v1.SolutionValue());
        Console.WriteLine("v2 = " + v2.SolutionValue());
        Console.WriteLine("v3 = " + v3.SolutionValue());
        Console.WriteLine("v4 = " + v4.SolutionValue());
        Console.WriteLine("v5 = " + v5.SolutionValue());

        var lcs = new LinearConstraint[] {
        new LinearConstraint(new string[] { "v1" }, new rat[] { 1}, 1, LinearConstraint.InequalityType.SmallerOrEqualTo),
        new LinearConstraint(new string[] { "v2" }, new rat[] { 1}, 1, LinearConstraint.InequalityType.EqualTo),
        new LinearConstraint(new string[] { "v2" }, new rat[] { 1}, 1, LinearConstraint.InequalityType.SmallerOrEqualTo),
        new LinearConstraint(new string[] { "v3" }, new rat[] { 1}, 0, LinearConstraint.InequalityType.EqualTo),
        new LinearConstraint(new string[] { "v3" }, new rat[] { 1}, 1, LinearConstraint.InequalityType.SmallerOrEqualTo),
        new LinearConstraint(new string[] { "v4" }, new rat[] { 1}, 1, LinearConstraint.InequalityType.SmallerOrEqualTo),
        new LinearConstraint(new string[] { "v5" }, new rat[] { 1}, 1, LinearConstraint.InequalityType.SmallerOrEqualTo),
        new LinearConstraint(new string[] { "v1", "v2", "v3", "v4" }, new rat[] { 1,3,3,1}, 4, LinearConstraint.InequalityType.SmallerOrEqualTo),
        new LinearConstraint(new string[] { "v4", "v5" }, new rat[] { 1,1}, 4, LinearConstraint.InequalityType.SmallerOrEqualTo),
      };
        var obj = new Objective(new string[] { "v1", "v2", "v3", "v4", "v5" }, new rat[] { 1, 3, 3, 1, 2 });
        var ls = new LinearSolver(lcs, obj);
        ls.Solve();
      }
    }
  }
  public class Simplex
  {
    public const int M = 100000;
    public ResultType Result { get; private set; }
    public Dictionary<string, rat> Answer { get; private set; }
    private static readonly Random rnd = new Random();


    private readonly List<List<rat>> left;
    private readonly List<string> entryVariableNames;
    private readonly Dictionary<string, int> namesToEntries;
    private readonly List<rat> right;
    private readonly List<string> rowPivotsNames;
    private readonly int userVarCount;
    private int slackCount;

    public Simplex(LinearConstraint[] LCs, Objective objective)
    {
      Result = ResultType.NOT_PROCESSED;
      Answer = new Dictionary<string, rat>();

      // All 0-1 Variables
      userVarCount = objective.Count;
      int colCount = userVarCount;
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
      //rat[rowCount][]
      left = new List<List<rat>>(rowCount);
      entryVariableNames = new List<string>(new string[colCount]);
      namesToEntries = new Dictionary<string, int>();
      right = new List<rat>(new rat[rowCount]);
      rowPivotsNames = new List<string>(new string[rowCount]);

      left.Add(new List<rat>(new rat[colCount]));
      for (int i = 0; i < objective.Count; i++)
      {
        left[0][i] = objective.LHSVariableCoefficients[i];
        entryVariableNames[i] = objective.LHSVariableNames[i];
        namesToEntries.Add(objective.LHSVariableNames[i], i);
      }
      right[0] = objective.RHSValue;
      rowPivotsNames[0] = objective.LHSVariableNames[0];


      // Fuer Constraints
      slackCount = 0; int artificialCount = 0;
      for (int row = 1; row <= LCs.Length; row++)
      {
        right[row] = LCs[row - 1].RHSValue;
        left.Add(new List<rat>(new rat[colCount]));
        for (int j = 0; j < LCs[row - 1].Count; j++)
        {
          if (namesToEntries.TryGetValue(LCs[row - 1].LHSVariableNames[j], out int entry))
          {
            left[row][entry] = LCs[row - 1].LHSVariableCoefficients[j];
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
          AddVar($"s{slackCount++}", ref colPtr);
        }
        else if (LCs[row - 1].Type == LinearConstraint.InequalityType.EqualTo)
        {
          AddRow(row, -1 * M, 0);
          AddVar($"a{artificialCount++}", ref colPtr);
        }
        else if (LCs[row - 1].Type == LinearConstraint.InequalityType.BiggerOrEqualTo)
        {
          // Surplus
          AddVar($"s{slackCount++}", ref colPtr);
          // Artificial
          AddRow(row, -1 * M, 0);
          AddVar($"a{artificialCount++}", ref colPtr);
        }

        void AddVar(string name, ref int entry)
        {
          namesToEntries.Add(name, entry);
          entryVariableNames[entry] = name;
          left[row][entry] = 1;
          rowPivotsNames[row] = name;
          entry++;
        }
      }
      if (!HasFeasibleSol()) Result = ResultType.NO_FEASIBLE_SOL;
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
        for (int i = 2; i < left[0].Count; i++)
        {
          if (left[0][c] > left[0][i]) // && !left[0][c].RoughlyEqualTo(left[0][i])
          {
            c = i;
            allCols.Clear();
            allCols.Add(i);
          }
          else if (left[0][c].Equal(left[0][i])) allCols.Add(i);
        }
        if (left[0][c] >= 0) return -1;
        return allCols[rnd.Next(allCols.Count)];
      }

      int FindSmallestRatioRow(int inCol)
      {
        int r = 1;
        while (left[r][inCol] <= 0) r++;
        for (int i = r + 1; i < left.Count; i++)
        {
          if (left[i][inCol] > 0 && right[i] / left[i][inCol] >= 0
              && right[r] / left[r][inCol] > right[i] / left[i][inCol])
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
        rowPivotsNames[row] = entryVariableNames[col];
        ReduceRow(row, col);
        for (int i = 0; i < left.Count; i++)
        {
          if (i == row) continue;
          if (!left[i][col].IsZero)
          {
            AddRow(row, left[i][col] * -1, i);
          }
        }
        col = FindMostNegativValCol();
      }
      if (IsFeasibleSol())
      {
        Result = ResultType.OPTIMAL;
        for (int i = 0; i < userVarCount; i++)
        {
          Answer.Add(entryVariableNames[i], 0);
        }
        for (int i = 0; i < rowPivotsNames.Count; i++)
        {
          if (Answer.ContainsKey(rowPivotsNames[i]))
            Answer[rowPivotsNames[i]] = right[i];
        }
      }
      // Should not really happen though
      else Result = ResultType.NO_FEASIBLE_SOL;
    }

    // Gomory Cut and reoptimize with dual method
    public LinearConstraint[] Cut(int n)
    {
      if (Result != ResultType.OPTIMAL)
      {
        Debug.WriteLine("Error: Cut without solved");
        return null;
      }

      List<LinearConstraint> rtr = new List<LinearConstraint>(n);
      for (int row = 1, count = 0; row < right.Count && count < n; row++)
      {
        if (right[row].FractionPart != 0)
        {
          while (right[row].WholePart == 0) AddRow(row, 1, row);
          count++;
          var newCons = new LinearConstraint(right[row].FractionPart * -1, LinearConstraint.InequalityType.SmallerOrEqualTo);
          // Turn from >= to <= by reversing sign (* -1)
          right.Add(right[row].FractionPart * -1);
          for (int i = 0; i < left.Count; i++) left[i].Add(0);
          left.Add(new List<rat>(new rat[left[0].Count]));
          for (int col = 1; col < left[0].Count - 1; col++)
          {
            // same as above
            left[^1][col] = left[row][col].FractionPart * -1;
            newCons.SetCoefficient(entryVariableNames[col], left[row][col].FractionPart * -1);
          }

          // new slack Var
          left[^1][^1] = 1;
          namesToEntries.Add($"s{slackCount}", left[0].Count - 1);
          entryVariableNames.Add($"s{slackCount}");
          rowPivotsNames.Add($"s{slackCount}");
          slackCount++;

          ReduceRow(row, namesToEntries[rowPivotsNames[row]]);

          // TODO: DUAL METHOD
          // COMPLETE IT
          DualSolve();

          if (!IsFeasibleSol())
          {
            Result = ResultType.NO_FEASIBLE_SOL;
          }

          for (int i = 0; i < userVarCount; i++)
          {
            Answer[entryVariableNames[i]] = 0;
          }
          for (int i = 0; i < rowPivotsNames.Count; i++)
          {
            if (Answer.ContainsKey(rowPivotsNames[i]))
              Answer[rowPivotsNames[i]] = right[i].CanonicalForm;
            if (right[i].CanonicalForm > 1 && rowPivotsNames[i][0] == 'x')
            {
              System.Console.WriteLine();
            }
          }

          rtr.Add(newCons);
          row = 1;
        }
      }

      return rtr.ToArray();
    }

    // Solve using dual 
    // bzw. transposed matrix
    private void DualSolve()
    {
      int GetMostNegativeRow()
      {
        List<int> rtr = new List<int>() { 1 };
        for (int r = 2; r < right.Count; r++)
        {
          if (right[r] < right[rtr[0]])
          {
            rtr.Clear();
            rtr.Add(r);
          }
          else if (right[r] == rtr[0]) rtr.Add(r);
        }
        if (rtr[0] >= 0) return -1;
        return rtr[rnd.Next(rtr.Count)];
      }

      int GetMinRatioCol(int r)
      {
        int col = 1;
        while (left[r][col] >= 0) col++;
        for (int c = col + 1; c < left.Count; c++)
        {
          if (left[r][c] < 0 && left[0][c] / left[r][c] <= 0)
          {
            if (left[0][c] / left[r][c] > left[0][col] / left[r][col])
              col = c;
          }
        }
        return col;
      }

      int row = GetMostNegativeRow();
      while (row != -1)
      {
        int col = GetMinRatioCol(row);
        rowPivotsNames[row] = entryVariableNames[col];
        ReduceRow(row, col);
        for (int i = 0; i < left.Count; i++)
        {
          if (i == row) continue;
          if (!left[i][col].IsZero)
          {
            AddRow(row, left[i][col] * -1, i);
          }
        }
        row = GetMostNegativeRow();
      }
    }

    private void ReduceRow(int row, int respectToCol)
    {
      rat divisor = left[row][respectToCol];
      for (int i = 0; i < left[0].Count; i++)
      {
        left[row][i] /= divisor;
        left[row][i] = left[row][i].CanonicalForm;
      }
      right[row] /= divisor;
      right[row] = right[row].CanonicalForm;
      left[row][respectToCol] = 1;
    }

    private void AddRow(int row, rat coefficient, int toRow)
    {
      right[toRow] += right[row] * coefficient;
      right[toRow] = right[toRow].CanonicalForm;
      for (int col = 0; col < left[0].Count; col++)
      {
        left[toRow][col] += left[row][col] * coefficient;
        left[toRow][col] = left[toRow][col].CanonicalForm;
      }
    }

    private bool HasFeasibleSol()
    {
      for (int i = 1; i < rowPivotsNames.Count; i++)
      {
        rat cf = left[i][namesToEntries[rowPivotsNames[i]]];
        if (right[i] / cf < 0)
        {
          return false;
        }
      }
      return true;
    }

    private bool IsFeasibleSol()
    {
      if (!HasFeasibleSol()) return false;
      for (int i = 1; i < rowPivotsNames.Count; i++)
      {
        if (rowPivotsNames[i][0] == 'a' && !right[i].IsZero) return false;
      }
      return true;
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