using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

using rat = Rationals.Rational;

namespace Aufgabe1
{
  // Vollstaendige Suche mit Backtracking
  // Hier wird das Problem als ein groesses Rechteck
  // mit eine Hoehe von 10 und eine Breite von 1000
  // in den man viele kleine Rechtecke einsetzt, 
  // abstrahiert. Vgl. Dokumentation
  public class CompleteSearch
  {
    // Speichert die Data aller Anbieter
    private readonly List<int[]> demands;
    // Der Index dieses Arrays entspricht dem von demands
    // speichert, ob der Anbieter dieses Indexs 
    // 'logisch' geloescht oder nicht
    private readonly bool[] avaliable;
    // Speichert die Index von Anbietern, die 'logisch' geloescht sind
    // Sie sind immer noch in demands, jedoch sind 
    // ihre avaliabe[i] zu false gesetzt worden
    private readonly Stack<int> currentDeleted;
    // Ein Array mit der Laenge von die Zeitspannen
    // In diesem Fall FlohmarktManagement.HEIGHT, i.e. 10 (8-18 Uhr)
    // In dem Array wird gespeichert, wie viele Plaetze 
    // in dieser Zeitspanne verwendet worden sind
    private readonly int[] currentMap;
    // Hoechstmoeglicher Gewinn 
    private readonly int highestValPossible;
    // Beste Auswahl
    public List<int[]> BestCombination { get; private set; }
    // Der Gewinn mit der besten Auswahl, vgl. BestCombination
    public int HighestProfit { get; private set; }
    // Falls der Algorithmus durch CancellationToken unterbrochen wird 
    // anstatt selbst damit fertig ist, dann false
    public bool IsCompleted { get; private set; }


    // ---DEBUGGING---
    public int DEBUG_changedCount = 0;
    public int DEBUG_pruningCount = 0;
    // ---------------

    public CompleteSearch(List<int[]> demands)
    {
      this.demands = new List<int[]>(demands);
      currentDeleted = new Stack<int>();
      avaliable = new bool[demands.Count];
      for (int i = 0; i < demands.Count; i++) avaliable[i] = true;
      currentMap = new int[FlohmarktManagement.INTERVAL_LENGTH];

      HighestProfit = 0;

      // Initialize the map
      for (int c = 0, cCount = 0; c < FlohmarktManagement.INTERVAL_LENGTH; c++, cCount = 0)
      {
        for (int i = 0; i < demands.Count; i++)
        {
          if (avaliable[i] && demands[i][0] <= c + FlohmarktManagement.START_TIME && demands[i][1] > c + FlohmarktManagement.START_TIME)
          {
            cCount += demands[i][2];
          }
        }
        currentMap[c] = cCount;
      }
      highestValPossible = CalcCurrentMaximumProfit();
    }

    // Gib das Ergebnis als Dictionary zurueck in Form
    // wie in LinearSolver, sodass sie identisch ausgegen koennen
    public Dictionary<string, rat> GetResult()
    {
      if (null == BestCombination)
        throw new InvalidOperationException("Data is not processed yet. ");
      Dictionary<string, rat> result = Enumerable.
        Range(0, demands.Count).
        ToDictionary(
        idx => $"x{idx}",
        idx => BestCombination.Exists(item => item[3] == idx) ? 1 : (rat)0);
      result.Add("P", HighestProfit);
      return result;
    }

    // Suche nach einer optimalen Loesung
    public void Process(CancellationToken cancelToken)
    {
      // Fuer kleine Data kann man direkt von 0 anfangen
      // Somit ist es wahrscheinlich etwas schneller 
      if (demands.Count < 50)
      {
        IsCompleted = true;
        HighestProfit = 0;
        RecRemove(0, FindFirstConflictCol(0), cancelToken);
        return;
      }

      // Ansonsten faengt man vom Oben an
      BestCombination = null;
      for (int i = highestValPossible - 1; i > 0; i--)
      {
        HighestProfit = i;
        RecRemove(0, FindFirstConflictCol(0), cancelToken);

        // Solange, bis eine optimale Loesung gefunden wird
        if (BestCombination != null)
        {
          IsCompleted = true;
          break;
        }
        // Oder wenn IsCancellationRequested
        else if (cancelToken.IsCancellationRequested)
        {
          IsCompleted = false;
          HighestProfit = 0;
          break;
        }
      }
    }

    // Iterieren alle moegliche Kombination rekursiv
    // Falls der jetzige hoechste Gewinn niedriger als 'lower bound' ist
    // dann wird diesen Zweig abgeschnitten
    private void RecRemove(int startIndex, Tuple<int, List<int[]>> conflicts, CancellationToken cancelToken)
    {
      // Falls es am Anfang die Bedingungen bereits erfuellt
      // i. e. flohmarkt6.txt und flohmarkt7.txt
      if (conflicts.Item1 == -1)
      {
        HighestProfit = CalcCurrentMaximumProfit();
        BestCombination = GetCurrentCombination();
        return;
      }
      // Iteration
      for (int i = startIndex; i < conflicts.Item2.Count; i++)
      {
        // Falls der Algorithmus durch CancellationToken unterbrochen wird
        // dann direkt zurueck
        if (cancelToken.IsCancellationRequested)
        {
          Restore();
          IsCompleted = false;
          break;
        }

        if (HighestProfit == highestValPossible) return;
        int conflictIndex = demands.FindIndex(item => item[3] == conflicts.Item2[i][3]);
        // Loescht zunaechst den Anbieter
        Delete(conflictIndex);

        // Der jetizge hoechste Gewinn
        int max = CalcCurrentMaximumProfit();
        // Fall kleiner oder gleich als den jetizgen globalen Wert
        // Dann kann man diesen Zweig abschneiden
        if (max <= HighestProfit)
        {
          // Pop the last deleted obj and set it to avaliable
          Restore();
          DEBUG_pruningCount++;
          continue;
        }

        // Ansonsten ist der moegliche Gewinn immer noch 
        // hoeher als die globale Untergrenze, dann
        // such danach, in welcher Spalte es noch Konflikte gibt
        int nextCol = -1;
        for (int c = conflicts.Item1; c < FlohmarktManagement.INTERVAL_LENGTH; c++)
        {
          if (currentMap[c] > FlohmarktManagement.HEIGHT)
          {
            nextCol = c;
            break;
          }
        }

        // Falls es hier keinen Konflikt mehr gibt
        if (nextCol == -1)
        {
          // Update den hoechsten Gewinn und die Kombination
          HighestProfit = max;
          BestCombination = GetCurrentCombination();

          // Pop the last deleted obj and set it to avaliable
          Restore();
          DEBUG_changedCount++;
          // return;
        }
        // Falls der Konflikt dieser Spalte geloest ist
        // Dann geht man zur naechsten Spalte
        else if (nextCol != conflicts.Item1)
        {
          RecRemove(0, FindFirstConflictCol(nextCol), cancelToken);
        }
        // Ansonsten bleibt man in dieser Spalte
        // und faengt mit dem naechsten Anbieter an
        else
        {
          RecRemove(i + 1, conflicts, cancelToken);
        }
      }
      // Ist dieser Unterzweig vollstaendig gesucht
      // geht man zurueck zum oberen Zweig
      Restore();
    }

    // diese Anbieter 'logisch' loeschen
    private void Delete(int index)
    {
      currentDeleted.Push(index);
      avaliable[index] = false;

      for (int i = demands[index][0]; i < demands[index][1]; i++)
      {
        currentMap[i - FlohmarktManagement.START_TIME] -= demands[index][2];
      }
    }

    // Die Anbieter, die zuletzt durch Delete() geloescht wurde,
    // wiederherstellen
    private void Restore()
    {
      if (currentDeleted.Count == 0) return;
      int index = currentDeleted.Pop();
      avaliable[index] = true;

      for (int i = demands[index][0]; i < demands[index][1]; i++)
      {
        currentMap[i - FlohmarktManagement.START_TIME] += demands[index][2];
      }
    }

    // Col startet mit Index 0; Search startet eben mit col 0
    // RETURN: Tuple<int, List<int[]>>: (SpalteZahl, Anfragen dieser Spalte)
    private Tuple<int, List<int[]>> FindFirstConflictCol(int startCol = 0)
    {
      List<int[]> currentCol;
      // iteriert ueber Spalten
      for (int c = startCol; c < FlohmarktManagement.INTERVAL_LENGTH; c++)
      {
        // Falls die Summe der Laenge aller Anfragen kleiner 
        // als HEIGHT, i.e. 1000, dann passt diese Spalte
        if (currentMap[c] <= FlohmarktManagement.HEIGHT) continue;
        // Ansonsten gib alle Anfragen dieser Spalte zurueck
        currentCol = Enumerable.Range(0, demands.Count)
          .Where(idx => avaliable[idx]
          && demands[idx][0] <= c + FlohmarktManagement.START_TIME
          && demands[idx][1] > c + FlohmarktManagement.START_TIME)
          .Select(i => demands[i])
          .ToList();
        return new Tuple<int, List<int[]>>(c, currentCol);
      }
      // Falls es in allen Spalten passt
      // dann gib -1 als Spalte zurueck
      return new Tuple<int, List<int[]>>(-1, null);
    }

    // Der jetzige hoechstmoegliche Gewinn
    // Falls in manchen Spalten ueber 1000 sind, dann 
    // zaehlen die als 1000
    private int CalcCurrentMaximumProfit()
    {
      int count = 0;
      for (int i = 0; i < FlohmarktManagement.INTERVAL_LENGTH; i++)
      {
        count += currentMap[i] > FlohmarktManagement.HEIGHT ? FlohmarktManagement.HEIGHT : currentMap[i];
      }
      return count;
    }

    // Gib alle Anbieter, die nicht durch Delete()
    // 'geloescht' sind, zurueck
    private List<int[]> GetCurrentCombination()
    {
      return Enumerable.
        Range(0, demands.Count).
        Where(i => avaliable[i]).
        Select(i => demands[i]).
        ToList();
    }
  }
}
