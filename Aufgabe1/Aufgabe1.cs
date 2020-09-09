/// Author: Chuyang Wang, 2020
/// C# 8.0, .Net Core 3.1
/// To compile code, download .Net Core SDK 3.1 from https://dotnet.microsoft.com/download


using System;
using System.IO;
using System.Collections.Generic;

namespace Aufgabe1
{
  public static class WoerterAufraeumer
  {
    public static void Main(string[] args)
    {
      Console.WriteLine("Bitte Name der Test-Datei eingeben...");
      string testFilePath = Console.ReadLine();

      if (File.Exists(testFilePath))
      {
        using (StreamReader sr = File.OpenText(testFilePath))
        {
          string[] raetsel = (sr.ReadLine()).Split(' ');
          string[] woerter = (sr.ReadLine()).Split(' ');

          Dictionary<int, int[]> lookupTableRW = new Dictionary<int, int[]>();
          List<int[]> possiblePairs = new List<int[]>();

          for (int rPos = 0; rPos < raetsel.Length; rPos++)
          {
            List<int> possibleWPosForR = new List<int>();
            for (int wPos = 0; wPos < woerter.Length; wPos++)
            {
              if (raetsel[rPos].GetRealStringLength() == woerter[wPos].Length)
              {
                for (int i = 0; i < woerter[wPos].Length; i++)
                {
                  if (raetsel[rPos][i].CompareTo(woerter[wPos][i]) == 0)
                  {
                    possibleWPosForR.Add(wPos);
                    break;
                  }
                  // Falls nicht gleich wie das Wort aber auch nicht _
                  // Dann kann man das ausschliessen
                  else if (raetsel[rPos][i].CompareTo('_') != 0) break;
                  // Falls diese Raetsel nur aus _ besteht
                  if (i == woerter[wPos].Length - 1) possibleWPosForR.Add(wPos);
                }
              }
            }
            lookupTableRW.Add(rPos, possibleWPosForR.ToArray());
            Console.WriteLine(lookupTableRW[rPos].ArrayToString());
          }

          string[] answer = new string[raetsel.Length];
          List<int> usedWoerter = new List<int>();
          while (usedWoerter.Count < woerter.Length)
          {
            for (int i = 0; i < raetsel.Length; i++)
            {
              int[] possibilities = lookupTableRW[i];

              List<int> restlicheWPos = new List<int>();
              for (int j = 0; j < possibilities.Length; j++)
              {
                if (!usedWoerter.Contains(possibilities[j]))
                {
                  restlicheWPos.Add(possibilities[j]);
                }
              }
              lookupTableRW[i] = restlicheWPos.ToArray();

              bool singleOrAllSameWord = true;
              for (int j = 0; j < possibilities.Length - 1; j++)
              {
                if (woerter[possibilities[j]].CompareTo(woerter[possibilities[j + 1]]) != 0) singleOrAllSameWord = false;
              }
              if (possibilities.Length > 0 && singleOrAllSameWord && !usedWoerter.Contains(possibilities[0]))
              {
                if (raetsel[i].HasPunctuation()) answer[i] = woerter[possibilities[0]] + raetsel[i][^1];
                else answer[i] = woerter[possibilities[0]];
                usedWoerter.Add(possibilities[0]);
              }
            }
          }

          Console.WriteLine(String.Join(" ", answer));
          Console.ReadKey();
        }
      }
      else
      {
        Console.WriteLine("Die gegebene Datei-Namen ist nicht gueltig. Das Programm und die Datei muessen in demselben Ordner stehen. ");
        Console.ReadKey();
      }
    }


    private static int GetRealStringLength(this string s)
    {
      int length = 0;
      foreach (char c in s.ToCharArray())
      {
        if (Char.IsLetter(c) || '_'.CompareTo(c) == 0) length++;
      }
      return length;
    }


    private static bool HasPunctuation(this string s)
    {
      foreach (char c in s.ToCharArray())
      {
        if (!Char.IsLetter(c) && '_'.CompareTo(c) != 0) return true;
      }
      return false;
    }

    private static string ArrayToString(this int[] i)
    {
      return String.Join(' ', i);
    }
  }
}
