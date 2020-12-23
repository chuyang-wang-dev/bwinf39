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
      // Setzt die Encoding zu UTF8 fuer deutsche Buchstaben
      Console.OutputEncoding = System.Text.Encoding.UTF8;

      Console.WriteLine("Bitte Name der Test-Datei eingeben...");
      string testFilePath = Console.ReadLine();

      if (File.Exists(testFilePath))
      {
        using (StreamReader sr = File.OpenText(testFilePath))
        {
          string[] raetsel = (sr.ReadLine()).Split(' ');
          string[] woerter = (sr.ReadLine()).Split(' ');

          // speichert, welche moegliche Woerter zu 
          // dem ggb. Lueckentext passen koennen
          // Index des Lueckentextes ---> alle moegliche passende ggb. Woerter
          // ex. 3 ---> [1, 3, 4]
          Dictionary<int, int[]> raetselZuWoertern = new Dictionary<int, int[]>();

          // Man geht die Raetsel durch
          for (int rPos = 0; rPos < raetsel.Length; rPos++)
          {
            // temperale Liste, die speichert
            // welche Woerter zu diesem Lueckentext passen
            List<int> possibleWPosForR = new List<int>();
            // Man geht dann die Woerter durch
            for (int wPos = 0; wPos < woerter.Length; wPos++)
            {
              // Nur wenn der Raetsel und das Wort gleich lang sind
              // ist es moeglich, dass dieses Wort auch dem Lueckentext passt
              if (raetsel[rPos].GetRealStringLength() == woerter[wPos].Length)
              {
                // In diesem Fall wird jeder Buchstabe des Wortes geprueft
                for (int i = 0; i < woerter[wPos].Length; i++)
                {
                  // Falls ein Buchstabe sowohl in dem Lueckentext als auch im Wort stimmt
                  if (raetsel[rPos][i].CompareTo(woerter[wPos][i]) == 0)
                  {
                    // Wird dieses Wort als eine Moeglichkeit fuer diesen Lueckentext angesehen
                    possibleWPosForR.Add(wPos);
                    break;
                  }
                  // Falls ein Buchstabe im Lueckentext diesem Wort widerspricht
                  // ex. Lueckentext: __f__, Wort: Birne
                  // der 3. Buchstabe f != r
                  // kann man das Wort ausschliessen
                  else if (raetsel[rPos][i].CompareTo('_') != 0) break;
                  // Falls dieser Lueckentext nur aus _ besteht
                  // Dann wird diese auch als eine Moeglichkeit angesehen
                  if (i == woerter[wPos].Length - 1) possibleWPosForR.Add(wPos);
                }
              }
            }
            // alle Moeglichkeiten werden zu diesem Lueckentext zugeordnet
            raetselZuWoertern.Add(rPos, possibleWPosForR.ToArray());
          }

          // die Array answer speichert die richtige Antwort als string
          string[] answer = new string[raetsel.Length];
          // speichert die Indexe der genutzten Woerter
          List<int> usedWoerter = new List<int>();
          // Waehrend nicht alle Woerter genutzt wurden
          // bzw. noch weitere Woerter nicht in dem Lueckentext zugeordnet wurden
          while (usedWoerter.Count < woerter.Length)
          {
            // Man geht der Lueckentext nochmal durch
            for (int i = 0; i < raetsel.Length; i++)
            {
              int[] alteMoeglichkeiten = raetselZuWoertern[i];

              // Speichert, wie viele Moeglichkeit noch uebrig bleiben
              List<int> restlicheWPos = new List<int>();
              // Fuer alle Moeglichkeiten
              for (int j = 0; j < alteMoeglichkeiten.Length; j++)
              {
                // Falls diese Index nicht genutzt wurde
                if (!usedWoerter.Contains(alteMoeglichkeiten[j]))
                {
                  // bleibt dieses immer als eine Moeglichkeit
                  restlicheWPos.Add(alteMoeglichkeiten[j]);
                }
              }
              // Update die Moeglichkeiten fuer den Lueckentext
              raetselZuWoertern[i] = restlicheWPos.ToArray();


              // Falls es nur eine Moeglichkeit uebrigbleibt
              // oder alle restliche moegliche Woerter gleich sind
              bool singleOrAllSameWord = true;
              for (int j = 0; j < restlicheWPos.Count - 1; j++)
              {
                if (woerter[restlicheWPos[j]].CompareTo(woerter[restlicheWPos[j + 1]]) != 0) singleOrAllSameWord = false;
              }

              // Falls es fuer diese Lueckentext noch moegliche Woerter gibt
              // und es nur eine Moeglichkeit bleibt (singleOrAllSameWord)
              if (restlicheWPos.Count > 0 && singleOrAllSameWord)
              {
                // Falls es Satzzeichen gibt, fuegt es noch auch in die Antwort hinzu
                if (raetsel[i].HasPunctuation()) answer[i] = woerter[restlicheWPos[0]] + raetsel[i][^1];
                // Ansonsten nur das Wort
                else answer[i] = woerter[restlicheWPos[0]];
                // Makiert das Wort als genutzt
                usedWoerter.Add(restlicheWPos[0]);
              }
            }
          }

          // die Array answer wird zu ein string umgewandelt und
          // in Console gezeigt
          Console.WriteLine(String.Join(" ", answer));
        }
      }
      else
      {
        Console.WriteLine("Die gegebene Datei-Namen ist nicht gueltig. Das Programm und die Datei muessen in demselben Ordner stehen. ");
      }

      Console.WriteLine("\r\nDrueck eine beliebige Taste zu schliessen...");
      Console.ReadKey();
    }


    private static int GetRealStringLength(this string s)
    {
      int length = 0;
      foreach (char c in s.ToCharArray())
      {
        // Nur Buchstaben oder _ zaehlt als eine Laenge
        // Also sind Satzzeichen kein richtige Laenge
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
  }
}
