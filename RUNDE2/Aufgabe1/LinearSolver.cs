using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;

using Aufgabe1.DataStructure;
using rat = Rationals.Rational;

// Wird NUR genutzt wenn --use-google=true explizit gesetzt wird
using Google.OrTools.LinearSolver;

namespace Aufgabe1.LinearProgramming
{
  // Branch and Cut
  public class LinearSolver
  {
    // Beschraenkung/Ungleichungen
    private readonly LinearConstraint[] originConstraints;
    // Die Zielfunktion
    private readonly Objective objective;
    private readonly Heap<ConstraintWithMax> toSearch;
    // Das jetzige beste Ergebnis
    // Ist ein UpperBound kleiner als diesen Wert
    // wird es nicht mehr in zwei Zweigen geteilt
    private rat globalLowerBound;
    // Die Loesung
    public Dictionary<string, rat> BestSolution { get; private set; }
    // Die temporale beste Loesung, entspricht dem globalLowerBound
    public Dictionary<string, rat> CurrentSolution { get; private set; }
    // Falls der Algorithmus durch CancellationToken unterbrochen wird 
    // anstatt selbst damit fertig ist, dann false
    public bool IsCompleted { get; private set; }

    public LinearSolver(LinearConstraint[] LCs, Objective objective)
    {
      originConstraints = LCs;
      this.objective = objective;
      toSearch = new Heap<ConstraintWithMax>(50);
      globalLowerBound = 0;
    }

    // Loest die LP durch Branch-And-Cut
    public void Solve(CancellationToken cancelToken)
    {
      var c = new ConstraintWithMax(originConstraints, objective, cancelToken);
      // Teilt das jetzige Ergebnis in zwei Zweigen
      // sofern die Werte nicht alle ganzzaehlig sind
      SolveOne(c, cancelToken);
      // Solange es weitere Zweige zum Rechnen gibt
      while (toSearch.Count != 0)
      {
        SolveOne(toSearch.Pop(), cancelToken);
        // Wird das Suchen wegen TimeLimit unterbrochen, dann
        if (cancelToken.IsCancellationRequested)
        {
          IsCompleted = false;
          return;
        }
      }

      if (cancelToken.IsCancellationRequested)
        IsCompleted = false;
      else
        IsCompleted = true;
    }

    // Unterteilt das jetzige Ergebnic c in Unterzweigen
    // Fuer die Variablen, die nicht ganzzahlig sind
    private void SolveOne(ConstraintWithMax c, CancellationToken cancelToken)
    {
      if (cancelToken.IsCancellationRequested)
      {
        IsCompleted = false;
        return;
      }
      if (!c.IsInfeasible)
      {
        // Falls das jetzige Ergebis schlechter als die Untergrenze, 
        // dann muss es nicht in Unterzweigen mehr geteilt werden
        if (globalLowerBound > c.Upperbound) return;
        // Falls das Lowerbound des jetizgen Ergebnises besser
        // als das Gespeicherte, dann ersetz das Gespeicherte
        // durch das Jetzige
        if (globalLowerBound < c.Lowerbound)
        {
          globalLowerBound = c.Lowerbound;
          // Und update das jetizge beste Loesung
          CurrentSolution = new Dictionary<string, rat>();
          foreach (var kvp in c.Answer)
          {
            CurrentSolution.Add(kvp.Key, kvp.Value.WholePart);
            CurrentSolution["P"] = c.Lowerbound;
          }
        }

        // fall eine ganzzahlige Loesung (alle Variablen ganzzahlig) gefunden
        // dann kann man die ganze Suchreihe loeschen
        // denn dort wird die Ergebnis nach Upperbound sortiert
        // d.h. dieses Ergebnis 'c' hat den hoechstwahrscheinlichen Wert
        // innerhalb aller Moeglichkeiten
        if (c.Lowerbound.Equal(c.Upperbound))
        {
          toSearch.Clear();
          // Die endgueltige Loesung
          BestSolution = new Dictionary<string, rat>();
          foreach (var kvp in c.Answer)
          {
            BestSolution.Add(kvp.Key, kvp.Value.CanonicalForm);
          }
          return;
        }

        // Falls das jetizge Ergebnis nicht-ganzzahlige Variablen enthaelt
        // werden jede nicht-ganzzahlige Variable in zwei Zweigen unterteilt
        // naemlich x_i = 0 oder x_i = 1
        foreach (var pair in c.Answer)
        {
          // der Zielwert 'P' kann uebersprungen werden
          if (pair.Key.Equals(objective.LHSVariableNames[0])) continue;
          if (!(pair.Value.FractionPart == 0))
          {
            LinearConstraint zero = new LinearConstraint(new string[] { pair.Key }, new rat[] { 1 }, 0, LinearConstraint.InequalityType.SmallerOrEqualTo);
            toSearch.Add(new ConstraintWithMax(c.Tableau, zero, objective, cancelToken));
            LinearConstraint one = new LinearConstraint(new string[] { pair.Key }, new rat[] { -1 }, -1, LinearConstraint.InequalityType.SmallerOrEqualTo);
            toSearch.Add(new ConstraintWithMax(c.Tableau, one, objective, cancelToken));
          }
        }
      }
    }

    // Loesen des ILP mithilfe von SCIP integiert in Google OrTools
    // Diese Methode wird NUR aufgerufen, wenn
    // --use-google=true gesetzt wird
    public static Dictionary<string, rat> GoogleSolve(List<int[]> data)
    {
      static int[] GetItemsInCol(int c, List<int[]> data)
      {
        return Enumerable.Range(0, data.Count).
          Where(idx => data[idx][0] <= c + FlohmarktManagement.START_TIME && data[idx][1] > c + FlohmarktManagement.START_TIME).
          ToArray();
      }

      // Google Solver
      Solver solver = Solver.CreateSolver("SCIP");

      // legt Xn ganzzahlige Variablen an
      // deren Wert zwischen 0 und 1 liegt
      List<Variable> Xn = new List<Variable>();
      for (int i = 0; i < data.Count; i++)
      {
        Xn.Add(solver.MakeIntVar(0, 1, $"x{i}"));
      }

      // Ungleichungen, also muessen die gasamte vergebene Laenge 
      // kleiner als FlohmarktManagement.HEIGHT, i.e. 1000, sein
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

      // legt die Zielfunktion
      var objective = solver.Objective();
      for (int i = 0; i < data.Count; i++)
      {
        objective.SetCoefficient(Xn[i], data[i].GetSize());
      }
      objective.SetMaximization();

      // Falls eine optimale Loesung gefunden wird
      Dictionary<string, rat> result = new Dictionary<string, rat>();
      if (solver.Solve() == Solver.ResultStatus.OPTIMAL)
      {
        for (int i = 0; i < Xn.Count; i++)
        {
          result.Add($"x{i}", rat.Approximate(Xn[i].SolutionValue(), tolerance: 0.001d));
        }
        result.Add($"P", rat.Approximate(solver.Objective().Value(), tolerance: 0.001d));
        return result;
      }
      // Falls die Eingabe richtig ist
      // dann sollte das nicht passieren
      // denn 0 ist mindestens eine gueltige Loesung
      else
      {
        Console.WriteLine("SCIP Solver hat einen unerwarteten Fehler gehabt. Falls es erneut auftreten, setzen Sie --use-goole=false");
        Console.WriteLine("\r\nDrueck eine beliebige Taste...");
        Console.ReadKey();
        throw new Exception("LinearProgramming.LinearSolver.GoogleSolve");
      }
    }

    // Klasse zum Speichern von geloeste Simplex Tableau
    private class ConstraintWithMax : IComparable<ConstraintWithMax>
    {
      public rat Upperbound { get; }
      public rat Lowerbound { get; }
      // Das jetizge Ergebnis
      public Dictionary<string, rat> Answer { get; }
      public bool IsInfeasible;
      // Das jetizge Tableau
      public Simplex Tableau { get; }

      public ConstraintWithMax(LinearConstraint[] LCs, Objective obj, CancellationToken cancelToken)
      {
        Simplex s = new Simplex(LCs, obj);
        // Loest die LP-Relaxition mit Simplex
        s.Solve();
        if (s.Result == Simplex.ResultType.NO_FEASIBLE_SOL) // Sollte echt nicht passieren
          throw new InvalidOperationException("Ergebnis existiert nur in optimalem Fall");
        // Gomory Cut
        s.Cut(7, cancelToken);
        Answer = s.Answer;
        Tableau = s;

        // Rechnet Upper- und Lowerbound
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
          IsInfeasible = true;
      }

      // Neuloesen mit einer weiterer Beschraenkung
      public ConstraintWithMax(Simplex old, LinearConstraint lc, Objective obj, CancellationToken cancelToken)
      {
        Simplex s = new Simplex(old);
        // Addiert das neu ergaenzte Ungleichung
        // Also, wenn man x_i <= 0 oder -x_i <= -1 gesetzt hat
        // diese ist identisch wie x_1 = 0 oder = 1 zu setzen
        s.AddConstraint(lc);
        // Reoptimieren mit Dual Methode 
        s.DualSolve(cancelToken);

        if (s.Result == Simplex.ResultType.OPTIMAL)
        {
          // Gomory Cut
          s.Cut(5, cancelToken);
          if (cancelToken.IsCancellationRequested) return;
          Answer = s.Answer;
          Tableau = s;

          // Rechnet Upper- und Lowerbound
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
            IsInfeasible = true;
        }
        else
        {
          Upperbound = -1;
          IsInfeasible = true;
        }
      }

      // Das Objekt ist 'groesser', 
      // wenn sein Upperbound groesser ist
      // ist es gleich, dann vergleicht man den Lowerbound
      public int CompareTo(ConstraintWithMax that)
      {
        if (Upperbound > that.Upperbound) return 1;
        else if (Upperbound.Equal(that.Upperbound))
        {
          if (Lowerbound > that.Lowerbound) return 1;
          else if (Lowerbound.Equal(that.Lowerbound)) return 0;
          else return -1;
        }
        else return -1;
      }
    }
  }

  // einfach Simplex
  // Big M Methode, in diesem Kontext wird dies jedoch
  // nicht gebraucht
  public class Simplex
  {
    // Fuer BigM Methode
    // Hier wird es NICHT gebraucht
    public const int M = 100000;
    // Wenn die abgeschnittene Flaeche zu klein ist,
    // dann mach es lieber nicht
    // Grenzwert:
    public const int CUT_D_LIMIT = 150;
    // NOT_PROCESSED | OPTIMAL | NO_FEASIBLE_SOL
    public ResultType Result { get; private set; }
    // Die Loesung
    public Dictionary<string, rat> Answer { get; private set; }


    public List<List<rat>> Left { get; }
    public List<string> ColumnVariableNames { get; }
    public Dictionary<string, int> NamesToEntries { get; }
    public List<rat> Right { get; }
    public List<string> RowBasicVariableNames { get; }
    // Speichert, wie viele Entscheidungsvariablen es gibt
    // Also die Anzahl von Variablen, die nicht Slack sind
    public int UserVarCount { get; }
    // Also die Anzahl von Schlupfenvariablen
    public int SlackCount { get; private set; }

    public Simplex(LinearConstraint[] LCs, Objective objective)
    {
      Result = ResultType.NOT_PROCESSED;
      Answer = new Dictionary<string, rat>();

      // Alle 0-1 Variablen
      UserVarCount = objective.Count;
      int colCount = UserVarCount;
      // Anzahl von Slack, Surplus und Artificial Variables
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
          // Theoretisch wird nur diese verwendet
          // das obige zwei dienen nur zur Vollstaendigkeit
          case LinearConstraint.InequalityType.SmallerOrEqualTo:
            colCount += 1;
            break;
        }
      }
      // Constraints + Zielfunktion
      int rowCount = LCs.Length + 1;
      // Ptr (col entry) fuer slack, surplus und artificial vars
      int colPtr = objective.Count;
      Left = new List<List<rat>>(rowCount);
      ColumnVariableNames = new List<string>(new string[colCount]);
      NamesToEntries = new Dictionary<string, int>();
      Right = new List<rat>(new rat[rowCount]);
      RowBasicVariableNames = new List<string>(new string[rowCount]);

      Left.Add(new List<rat>(new rat[colCount]));
      for (int i = 0; i < objective.Count; i++)
      {
        Left[0][i] = objective.LHSVariableCoefficients[i];
        ColumnVariableNames[i] = objective.LHSVariableNames[i];
        NamesToEntries.Add(objective.LHSVariableNames[i], i);
      }
      Right[0] = objective.RHSValue;
      RowBasicVariableNames[0] = objective.LHSVariableNames[0];


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
        }

        // Add Slack, Surplus and Artificial Variables 
        // Vgl. Big M Method
        // Eben sind EqualTo und BiggerOrEqualTo nicht noetig
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

        // Funktion, um eine neue Variable in den Matrix zu setzen
        void AddVar(string name, ref int entry)
        {
          NamesToEntries.Add(name, entry);
          ColumnVariableNames[entry] = name;
          Left[row][entry] = 1;
          RowBasicVariableNames[row] = name;
          entry++;
        }
      }
      if (!HasFeasibleSol()) Result = ResultType.NO_FEASIBLE_SOL;
    }

    // Kopiert von einem vorhandenen Objekt
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
      RowBasicVariableNames = new List<string>(s.RowBasicVariableNames);
      ColumnVariableNames = new List<string>(s.ColumnVariableNames);
      NamesToEntries = s.NamesToEntries.ToDictionary(entry => entry.Key, entry => entry.Value);
    }

    // Loesen mit traditionellem Simplex
    public void Solve()
    {
      if (Result == ResultType.NO_FEASIBLE_SOL) return;

      // i.e. Spalte, deren 'reduced cost' am niedrigsten und negativ ist
      int FindEnteringVariable()
      {
        int c = 1;
        List<int> allCols = new List<int> { c };
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
        return allCols[0];
      }

      // i.e. Zeile mit kleinstem Quotient in dieser Spalte
      int FindLeavingVariable(int inCol)
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

      int col = FindEnteringVariable();
      // Solange es noch negative Werte in P Zeile gibt
      while (col != -1)
      {
        int row = FindLeavingVariable(col);
        RowBasicVariableNames[row] = ColumnVariableNames[col];
        ReduceRow(row, col);
        for (int i = 0; i < Left.Count; i++)
        {
          if (i == row) continue;
          // Eliminiert diese Variable in anderen Zeilen
          if (!Left[i][col].IsZero)
            AddRow(row, Left[i][col] * -1, i);
        }
        // Such nach einer neuen Variable
        // deren reduced cost negativ ist
        col = FindEnteringVariable();
      }

      if (IsFeasibleSol())
      {
        Result = ResultType.OPTIMAL;
        SetAnswer();
      }
      // Unter diesem Kontext sollte nicht passieren
      else Result = ResultType.NO_FEASIBLE_SOL;
    }


    // Dual Methode zur Reoptimierung 
    // Nachdem eine neue Beschraenkung eingefuegt wurde
    public void DualSolve(CancellationToken cancelToken)
    {
      // Zeile mit kleinsten (und nagativ) reduced cost
      // Sie ist in diesem Fall Right[]
      // Denn der Matrix wird transposed
      int FindEnteringVariable()
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
        return rtr[0];
      }

      // Spalte mit niedrigsten Betrag von Quotient
      int FindLeavingVariable(int r)
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
            if (Left[0][c] / Left[r][c] > Left[0][col] / Left[r][col])
              col = c;
          }
        }
        return col;
      }

      int row = FindEnteringVariable();
      // Solange es noch weiter reduziert werden
      while (row != -1 && !cancelToken.IsCancellationRequested)
      {
        int col = FindLeavingVariable(row);
        if (col == -1)
        {
          Result = ResultType.NO_FEASIBLE_SOL;
          return;
        }
        RowBasicVariableNames[row] = ColumnVariableNames[col];
        ReduceRow(row, col);
        for (int i = 0; i < Left.Count; i++)
        {
          if (i == row) continue;
          if (!Left[i][col].IsZero)
            AddRow(row, Left[i][col] * -1, i);
        }
        row = FindEnteringVariable();
      }

      if (IsFeasibleSol())
        SetAnswer();
      else
        Result = ResultType.NO_FEASIBLE_SOL;
    }

    // Fuegt eine neue Beschraekung
    // DualSolve() sollte aufgerufen werden um das zu reoptimieren
    public void AddConstraint(LinearConstraint lc)
    {
      if (lc.Type != LinearConstraint.InequalityType.SmallerOrEqualTo)
        throw new NotImplementedException(nameof(lc));

      // Falls die Ungleichung 0<=0 ist, dann braucht man es nicht einzusetzen
      if (lc.LHSVariableCoefficients.Count == 0 && lc.RHSValue.IsZero) return;

      Right.Add(lc.RHSValue);
      for (int i = 0; i < Left.Count; i++) Left[i].Add(0);
      Left.Add(new List<rat>(new rat[Left[0].Count]));
      for (int i = 0; i < lc.LHSVariableCoefficients.Count; i++)
      {
        Left[^1][NamesToEntries[lc.LHSVariableNames[i]]] = lc.LHSVariableCoefficients[i];
      }

      // neue slack Var
      Left[^1][^1] = 1;
      NamesToEntries.Add($"s{SlackCount}", Left[0].Count - 1);
      ColumnVariableNames.Add($"s{SlackCount}");
      RowBasicVariableNames.Add($"s{SlackCount}");
      SlackCount++;

      // Falls manche neue eingesetzte Koeffiziente Basic ist
      // muessen sie dementsprechen in dieser neu eingefuegte Zeile
      // eliminiert werden
      for (int i = 1; i < Left[0].Count - 1; i++)
      {
        if (Left[^1][i] > 0 && RowBasicVariableNames.Contains(ColumnVariableNames[i]))
        {
          AddRow(
            RowBasicVariableNames.FindIndex(n => n == ColumnVariableNames[i]),
            -1 * Left[^1][i],
            Left.Count - 1);
        }
      }
    }

    // Gomory Cut und reoptimieren mit Dual Methode
    public void Cut(int n, CancellationToken cancelToken)
    {
      if (!IsFeasibleSol())
      {
        System.Diagnostics.Debug.WriteLine("Error: Cut with non-feasible solution");
        return;
      }

      // Iteriert man durch die Zeilen und
      // sucht nach nicht-ganzzahlige Variablen, 
      // deren Bruchteil am groessten ist, 
      // um moeglich groesse Flaeche abzuschneiden
      for (int count = 0; count < n && !cancelToken.IsCancellationRequested; count++)
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
            // Falls die abgeschnittene Flaeche zu klein ist
            // dann sollte das nicht durchgefuehrt werden
            if (Right[row].Denominator > CUT_D_LIMIT) continue;
            if (Right[r].FractionPart < Right[row].FractionPart) r = row;
          }
        }

        // Aendert von >= zu <= 
        // indem man (* -1) auf beiden Seiten durchfuehrt
        LinearConstraint newLc = new LinearConstraint(Right[r].FractionPart * -1, LinearConstraint.InequalityType.SmallerOrEqualTo);
        for (int col = 1; col < Left[0].Count; col++)
          newLc.SetCoefficient(ColumnVariableNames[col], Left[r][col].FractionPart * -1);

        AddConstraint(newLc);
        DualSolve(cancelToken);
      }
    }

    // Liest die Loesung von dem Tableau
    private void SetAnswer()
    {
      if (Result != ResultType.OPTIMAL) throw new Exception();
      Answer = new Dictionary<string, rat>();
      for (int i = 0; i < UserVarCount; i++)
      {
        Answer[ColumnVariableNames[i]] = 0;
      }
      for (int i = 0; i < RowBasicVariableNames.Count; i++)
      {
        if (Answer.ContainsKey(RowBasicVariableNames[i]))
          Answer[RowBasicVariableNames[i]] = Right[i].CanonicalForm;

        if (Right[i].CanonicalForm > 1 && RowBasicVariableNames[i][0] == 'x')
        {
          Console.WriteLine();
        }
      }
    }

    // Zeilenoperation: R_row = R_row / respectToCol
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

    // Zeilenoperation: R_toRow = R_row * coefficient + R_toRow
    private void AddRow(int row, rat coefficient, int toRow)
    {
      if (row == toRow && coefficient.Equal(-1))
        throw new InvalidOperationException("Row operation causing row being eliminated to zero");
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
      for (int i = 1; i < RowBasicVariableNames.Count; i++)
      {
        // Da alle Variablen groesser oder gleich 0 sind
        // ist es unmoeglich, dass irgendwelche kleiner als 0 ist
        // in diesem Fall hat das Problem keine Loesung
        if (Right[i] < 0)
          return false;
      }
      return true;
    }

    public bool IsFeasibleSol()
    {
      if (!HasFeasibleSol()) return false;
      for (int i = 1; i < RowBasicVariableNames.Count; i++)
      {
        // Gibt es Artificial Variablen, die nicht gleich 0 sind
        // dann ist das Ergebnis 'infeasible'
        if (RowBasicVariableNames[i][0] == 'a' && !Right[i].IsZero) return false;
      }
      return true;
    }

    // DEBUG ONLY
    public void ToFile()
    {
      string path = "F:\\repos\\JWI2020\\RUNDE2\\Aufgabe1\\t.txt";
      File.AppendAllText(path, "\r\n\r\n");
      File.AppendAllText(path, ToString());
    }

    // Auch nur fuer Debug
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
      result += string.Join(SEPERATOR, ColumnVariableNames.Select(s => s + GetSpace(MAX_WIDTH - s.Length)));
      result += " | RH ";

      for (int i = 0; i < Left.Count; i++)
      {
        result += "\r\n";
        result += RowBasicVariableNames[i] + GetSpace(MAX_WIDTH - RowBasicVariableNames[i].Length) + SEPERATOR;
        result += string.Join(SEPERATOR, Left[i].Select(r => r + GetSpace(MAX_WIDTH - r.ToString().Length)));
        result += $" | {Right[i]}";
      }

      return result;
    }

    public enum ResultType
    {
      OPTIMAL,
      NO_FEASIBLE_SOL,
      // NICHT IMPLEMENTIERT
      // Wird wahrscheinlich auch nicht implementiert
      TIME_LIMIT_EXCEEDED,
      NOT_PROCESSED
    }
  }

  // LinearExpression ist, emmm, Linear Expression
  // Als Basisklasse fuer die Ungleichungen und die Zielfunktion
  public abstract class LinearExpression
  {
    public List<string> LHSVariableNames { get; protected set; }
    public List<rat> LHSVariableCoefficients { get; protected set; }
    // Auf der rechten Seite findet man nur Konstanten (also nur Zahlen)
    public rat RHSValue { get; protected set; }
    // Anzahl von Variablen auf der linken Seite
    public int Count { get => LHSVariableNames.Count; }

    public void SetCoefficient(string variableName, rat coefficient)
    {
      if (coefficient.IsZero) return;
      LHSVariableNames.Add(variableName);
      LHSVariableCoefficients.Add(coefficient);
    }
  }

  // Lineare Ungleichung
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

  // Angenommen fuer Maximazation
  // Die Zielfunktion
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
