/// Author: Chuyang Wang, 2020
/// C# 8.0, .Net Core 3.1
/// To compile code, download .Net Core SDK 3.1 from https://dotnet.microsoft.com/download


using System;
using System.IO;
using System.Collections.Generic;

namespace Aufgabe2
{
  public static class DreiecksPuzzlesLoeser
  {
    public static void Main(string[] args)
    {
      Console.WriteLine("Bitte Name der Test-Datei eingeben...");
      string testFilePath = Console.ReadLine();

      if (File.Exists(testFilePath))
      {
        using (StreamReader sr = File.OpenText(testFilePath))
        {
          // Einlesen der Datei
          int figuren = Convert.ToInt32(sr.ReadLine());
          int teileAnzahl = Convert.ToInt32(sr.ReadLine());

          Teil[] teile = new Teil[teileAnzahl];
          for (int zeile = 0; zeile < teileAnzahl; zeile++)
          {
            string[] teileDerZeile = sr.ReadLine().Split(' ');
            teile[zeile] = new Teil(Convert.ToInt32(teileDerZeile[0]), Convert.ToInt32(teileDerZeile[1]), Convert.ToInt32(teileDerZeile[2]));
          }

          //


        }
      }
      else
      {
        Console.WriteLine("Die gegebene Datei-Namen ist nicht gueltig. Das Programm und die Datei muessen in demselben Ordner stehen. ");
      }
      Console.ReadKey();
    }

    private static List<Teil[]> GetPossiblePairsInThird(this Teil node, Teil[] tree)
    {
      List<Teil[]> possibilities = new List<Teil[]>();


    }

    private class Teil
    {
      private int[] _seiten;
      public int[] Seiten
      {
        get { return _seiten; }
      }

      public Teil(int seite1, int seite2, int seite3)
      {
        _seiten = new int[3] { seite1, seite2, seite3 };
      }


    }
  }
}
