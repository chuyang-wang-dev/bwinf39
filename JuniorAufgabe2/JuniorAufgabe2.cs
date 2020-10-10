/// Author: Chuyang Wang, 2020
/// C# 8.0, .Net Core 3.1
/// To compile code, download .Net Core SDK 3.1 from https://dotnet.microsoft.com/download


using System;
using System.IO;

namespace JuniorAufgabe2
{
  public static class BaulWuerfeCounter
  {
    private const char HUEGEL_ZEICHEN = 'X';

    public static void Main(string[] args)
    {
      int spezialBaulwuerfenCount = 0;

      Console.WriteLine("Bitte Name der Test-Datei eingeben...");
      string testFilePath = Console.ReadLine();

      if (File.Exists(testFilePath))
      {
        using (StreamReader sr = File.OpenText(testFilePath))
        {
          int b = Convert.ToInt32(sr.ReadLine());
          int h = Convert.ToInt32(sr.ReadLine());

          char[,] map = new char[h, b];
          bool[,] besucht = new bool[h, b];

          // Einlesen der Daten
          for (int i = 0; i < h; i++)
          {
            char[] lineChars = sr.ReadLine().ToCharArray();
            for (int j = 0; j < b; j++)
            {
              map[i, j] = lineChars[j];
              besucht[i, j] = false;
            }
          }

          for (int i = 0; i < h; i++)
          {
            for (int j = 0; j < b; j++)
            {
              // Falls es schon ganz am Rand steht und ist unmoeglich, eine Baulwuerfenbau zu bilden
              if (i + 3 >= h || j + 2 >= b)
              {
                // Dann ueberspringt man das Planquadrat direkt, ohne zu pruefen
                continue;
              }

              // Falls diese Quadrat ist noch nicht besucht und auf diesem Quadrat
              // gibt es ein X bzw. Huegel
              if (!besucht[i, j] && map[i, j].IstHuegel())
              {
                // Testen, ob es sich von solchen speziellen Baulwuerfen handelt
                bool istSpezial = map[i, j + 1].IstHuegel() && map[i, j + 2].IstHuegel() && map[i + 1, j].IstHuegel() && map[i + 1, j + 2].IstHuegel() && map[i + 2, j].IstHuegel() && map[i + 2, j + 2].IstHuegel() && map[i + 3, j].IstHuegel() && map[i + 3, j + 1].IstHuegel() && map[i + 3, j + 2].IstHuegel();
                // Testet, ob die beiden Planquadraten im Zentrum leer sind
                // sodass es genau den Muster eines Baulwurfsbaues bildet
                bool centerIstLeer = !(map[i+1, j+1].IstHuegel() || map[i+2, j+1].IstHuegel());
                // Testet, ob die Huegel des Baues schon Teil eines anderen Baues ist  
                bool sindBesucht = besucht[i, j + 1] || besucht[i, j + 2] || besucht[i + 1, j] || besucht[i + 1, j + 2] || besucht[i + 2, j] || besucht[i + 2, j + 2] || besucht[i + 3, j] || besucht[i + 3, j + 1] || besucht[i + 3, j + 2];
                // Falls es der Fall ist
                if (istSpezial && !sindBesucht && centerIstLeer)
                {
                  // Erhoeht man den Counter und markiert
                  // alle Huegel des Baues als besucht
                  spezialBaulwuerfenCount++;
                  besucht[i, j] = true;
                  besucht[i, j + 1] = true;
                  besucht[i, j + 2] = true;
                  besucht[i + 1, j] = true;
                  besucht[i + 1, j + 2] = true;
                  besucht[i + 2, j] = true;
                  besucht[i + 2, j + 2] = true;
                  besucht[i + 3, j] = true;
                  besucht[i + 3, j + 1] = true;
                  besucht[i + 3, j + 2] = true;
                }
                // Falls es aber nicht der Fall ist
                else
                {
                  // markiert man nur das jetzigen Quadrat als besucht
                  besucht[i, j] = true;
                }
              }
            }
          }
        }
        Console.WriteLine($"Es gibt insgesamt {spezialBaulwuerfenCount} Baulwurfsbaue.");
      }
      else
      {
        Console.WriteLine("Die gegebene Datei-Namen ist nicht gueltig. Das Programm und die Datei muessen in demselben Ordner stehen. ");
      }
      Console.WriteLine("Drueck eine beliebige Taste zu schliessen...");
      Console.ReadKey();
    }

    private static bool IstHuegel(this char mapKoordinate)
    {
      return mapKoordinate.CompareTo(HUEGEL_ZEICHEN) == 0;
    }

  }
}