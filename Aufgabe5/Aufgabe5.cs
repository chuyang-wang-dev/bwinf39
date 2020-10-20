/// Author: Chuyang Wang, 2020
/// C# 8.0, .Net Core 3.1
/// To compile code, download .Net Core SDK 3.1 from https://dotnet.microsoft.com/download


using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Aufgabe5
{
  public static class WichtelnSortierment
  {
    public static void Main(string[] args)
    {
      Console.WriteLine("Bitte Name der Test-Datei eingeben...");
      string testFilePath = Console.ReadLine();

      if (File.Exists(testFilePath))
      {
        using (StreamReader sr = File.OpenText(testFilePath))
        {
          # region Einlesen und Speichern von Beispiel-Daten
          int studentCount = Convert.ToInt32(sr.ReadLine());

          List<Person> students = new List<Person>(studentCount);
          for (int i = 0; i < studentCount; i++)
          {
            string[] line = sr.ReadLine().Trim().Split(' ');
            // Speichert, welche Geschenke sich dieser Schueler wuenscht
            int[] wishesWithIndexI = new int[3];
            for (int j = 0, wishI = 0; j < line.Length; j++)
            {
              // Die Eingabedateien sind offenbar nicht nur mit einem Leerzeichen getrennt
              // bspw. kommt 3  1 10 vor
              // Deshalb wird geprueft, nur wenn es wirklich eine Zahl enthaelt, wird dies dann gespeichert
              // -1, weil der Index mit 0 anfaengt
              if (!string.IsNullOrWhiteSpace(line[j])) wishesWithIndexI[wishI++] = Convert.ToInt32(line[j].Trim()) - 1;
            }
            students.Add(new Person(i, wishesWithIndexI));
          }
          # endregion 

          // key: Gegenstandnummer
          // val: die Person, die diesen Gegenstand bekommen hat
          Dictionary<int, int> giftsTo = new Dictionary<int, int>();

          // Prueft fuer jede Reihe (also der 1. 2. und 3. Wunsch)
          // welche der Wuensche erfuellt werden koennen
          // und wessen Wunsch genau erfuellt werden soll
          // Die erfuellte Wuensche werden in die Zuordnungstabelle giftsTo gespeichert
          processCol(0, ref students, ref giftsTo, GetAllGivenGiftInCol(0, students));
          processCol(1, ref students, ref giftsTo, giftsTo.Keys.ToArray());
          processCol(2, ref students, ref giftsTo, giftsTo.Keys.ToArray());


          // key: die Person, die diesen Gegenstand bekommen hat
          // val: Gegenstandnummer
          Dictionary<int, int> personWithGift = new Dictionary<int, int>();

          # region Teste, welche Geschenke noch nicht gegeben sind und verteilen die
          for (int i = 0; i < studentCount; i++)
          {
            int personIndex;
            if (giftsTo.TryGetValue(i, out personIndex)) personWithGift.Add(personIndex, i);
            else
            {
              for (int j = 0; j < students.Count; j++)
              {
                // Falls der Schueler nachher keinen gewuenschten Gegenstand bekommen hat
                // wird ein noch nicht gegebener Gegenstand ihm gegeben
                if (students[j].GotGift == -1)
                {
                  students[j].GotGift = i;
                  giftsTo.Add(i, students[j].Index);
                  personWithGift.Add(students[j].Index, i);
                  break;
                }
              }
            }
          }
          # endregion

          # region Ausgabe des Ergebnises
          for (int i = 0; i < studentCount; i++)
          {
            // Ausgabe: S G
            // S ist die Nummer des/r SchuelerIn,
            // G ist die Nummer der Gegenstand, der an dem/der SchuelerIn S gegeben wird
            // Bsp: 1 3
            // Das heisst, der/die Schueler 1 bekommt den Gegenstand 3
            Console.WriteLine($"{i + 1} {personWithGift[i] + 1}");
          }
          # endregion
        }
      }
      else
      {
        Console.WriteLine("Die gegebene Datei-Namen ist nicht gueltig. Das Programm und die Datei muessen in demselben Ordner stehen. ");
      }
      Console.WriteLine("\r\nDrueck eine beliebige Taste zu schliessen...");
      Console.ReadKey();
    }


    // Prueft fur die gegebenen Reihe col,
    // welche der Wuensche erfuellt werden koennen
    // und zu wem die Geschenke gegeben werden sollen
    // int col: die Reihe zu bearbeiten, 
    // also ob man den 1. , 2. oder 3. Wunsch pruefen sollte
    // giftsTo: Zuordnungstabelle, vgl. Dokumentation
    // givenGifts: Die Gegenstaende, die bereits an den 
    // anderen Schuelern gegeben wurden
    private static void processCol(int col, ref List<Person> students, ref Dictionary<int, int> giftsTo, int[] givenGifts)
    {
      int studentCount = students.Count;

      # region Teste die Wuensche, die nur einmal in dieser Reihe erscheinen
      for (int i = 0; i < studentCount; i++)
      {
        int[] allPersonWithSameWish = GetAllPersonIndexWithSameGiftWish(students[i].Wishes[col], students, col);
        // Sofern ein Wunsch nur einmal in der Reihe erscheint
        // (die Wuensche von den anderen, deren Wunsch bereits erfuellt wurde, 
        // wird hier nicht mehr betrachtet)
        // wird der gewuenschte Gegenstand diesem Schueler gegeben. 
        if (allPersonWithSameWish.Length == 1 && allPersonWithSameWish[0] == i)
        {
          giftsTo.Add(students[i].Wishes[col].GiftWish, students[i].Index);
          students[i].GotGift = students[i].Wishes[col].GiftWish;
          students[i].SetAllWishToUnable();
        }
      }
      // Testet, ob die gegebenen Geschenke auch 
      // von den anderen (mit niedriger Prioritaet) gewuenscht wurden
      // Ist so, aendert diese Wuensche (von den anderen Schuelern) als nicht erfuellbar
      for (int idx = 0; idx < studentCount; idx++)
      {
        students[idx].CheckAllWishesAvaliblility(givenGifts.Union(giftsTo.Keys).ToArray(), col + 1);
      }
      # endregion


      # region Testet die Wuensche, die mehrmals in dieser Reihe erscheinen
      for (int i = 0; i < studentCount; i++)
      {
        // Hier wird die Wahrscheinlichkeit gerechnet, 
        // ob der Wunsch noch spaeter erfuellt werden kann
        // Der Wunsch mit der niedrigsten Wahrscheinlichkeit
        // Wird erfuellt
        if (students[i].Wishes[col].Avaliable)
        {
          int[] allPersonWithSameWish = GetAllPersonIndexWithSameGiftWish(students[i].Wishes[col], students, col);
          double lowestScore = double.MaxValue;
          int personIndexWithLowestScore = -1;

          for (int j = 0; j < allPersonWithSameWish.Length; j++)
          {
            if (students[allPersonWithSameWish[j]].Calculate(students, col) < lowestScore)
            {
              lowestScore = students[allPersonWithSameWish[j]].Calculate(students, col);
              personIndexWithLowestScore = allPersonWithSameWish[j];
            }
          }

          giftsTo.Add(students[i].Wishes[col].GiftWish, personIndexWithLowestScore);
          students[personIndexWithLowestScore].GotGift = students[i].Wishes[col].GiftWish;
          students[personIndexWithLowestScore].SetAllWishToUnable();

          // Testet, ob die gegebenen Geschenke auch 
          // von den anderen (mit niedriger Prioritaet) gewuenscht wurden
          // Ist so, aendert diese Wuensche als nicht erfuellbar
          for (int idx = 0; idx < studentCount; idx++)
          {
            students[idx].CheckAllWishesAvaliblility(giftsTo.Keys.ToArray(), col);
          }
        }
      }
      # endregion
    }


    // Methode, die zurueckgibt, welche Schueler 
    // der ggb. Gegenstand "giftWish" als ihre col-1's Wunsch erwuenscht
    private static int[] GetAllPersonIndexWithSameGiftWish(Wish giftWish, List<Person> students, int col)
    {
      List<int> persons = new List<int>();
      for (int i = 0; i < students.Count; i++)
      {
        if (students[i].Wishes[col].GiftWish == giftWish.GiftWish && students[i].Wishes[col].Avaliable) persons.Add(students[i].Index);
      }
      return persons.ToArray();
    }


    // Methode, die zurueckgibt,
    // welche Gegenstaende in dieser Reihe gewuenscht sind
    private static int[] GetAllGivenGiftInCol(int col, List<Person> students)
    {
      List<int> giftNums = new List<int>();
      for (int i = 0; i < students.Count; i++)
      {
        giftNums.Add(students[i].Wishes[col].GiftWish);
      }
      return giftNums.Distinct().ToArray();
    }


    // Erweiterungsmethode, die bestimmt, 
    // ob die ggb. val in die ggb. arr ist
    private static bool Contains(this int[] arr, int val)
    {
      foreach (int i in arr)
      {
        if (i == val) return true;
      }
      return false;
    }


    // Klasse, die ein Wunsch eines Schuelers repraesentiert
    private class Wish
    {
      public int GiftWish
      {
        get;
      }

      // Ob dieser Wunsch erfuellt werden kann
      // false, wenn bspw. ein anderer Schueler das Geschenk schon bekommt
      // Oder, dass ein anderer Wunsch des Schuelers bereits erfuellt ist,
      // und damit sind alle andere Wuensche des Schuelers nicht erfuellbar
      public bool Avaliable
      {
        get;
        set;
      }

      public Wish(int wish, int col)
      {
        Avaliable = true;
        GiftWish = wish;
      }

      public static Wish[] ConvertToWishes(int[] wishesOfOnePerson)
      {
        return new Wish[] { new Wish(wishesOfOnePerson[0], 0), new Wish(wishesOfOnePerson[1], 1), new Wish(wishesOfOnePerson[2], 2) };
      }
    }


    // Klasse, die ein Schueler repraesetiert
    private class Person
    {
      public int Index
      {
        get;
      }

      // Die drei Wuensche des Schuelers
      public Wish[] Wishes
      {
        get;
      }

      // Der Gegenstand, das dem Schueler gegeben wird
      public int GotGift
      {
        get; set;
      }

      public Person(int index, int[] wishes)
      {
        Index = index;
        Wishes = Wish.ConvertToWishes(wishes);
        GotGift = -1;
      }

      public void SetAllWishToUnable()
      {
        for (int i = 0; i < 3; i++) this.Wishes[i].Avaliable = false;
      }


      // Diese Methode aendert das Attribut "Avaliable" 
      // von den Wuenschen des Schluelers zu false,
      // wenn diese gewuenschte Geschenke schon gegeben sind,
      // welche durch int[] givenGift gegeben wird
      public void CheckAllWishesAvaliblility(int[] givenGift, int startCol)
      {
        for (int i = startCol; i < 3; i++)
        {
          if (givenGift.Contains(Wishes[i].GiftWish)) Wishes[i].Avaliable = false;
        }
      }


      // Hier wird gerechnet, wie hoch die Wahrscheinlichkeit ist,
      // dass die andere Wuensche dieses Schluelers (also die 2. und 3. Wuensche)
      // spaeter noch erfuellt werden koennen.
      // Ausserdem wird geprueft, ob diese anderen Wuensche des Schluelers 
      // auch von den anderen Schluelern erwuenscht sind.
      // Ist es der Fall, dann ist die Wahrscheinlichkeit,
      // dass diese Wuensche des Schuelers nicht erfuellt werden koennen,
      // dementsprechend hoch, denn die koennen auch von den Anderen genommen werden.
      // Je hoeher diese Wahrscheinlichkeit ist, dass der 2. bzw. 3. Wunsch
      // des Schluelers spaeter erfuellt werden kann,
      // desto groesser wird die zurueckgegebene Zahl "score".
      // Die konkrekte Rechenformel befindet sich in der Dokumentation.
      // Selbstverstaendlich wird der 2. Wuensche der anderen Schueler nicht mehr betrachtet,
      // wenn es schon ueber den 2. Wunsch bestimmen wird
      public double Calculate(List<Person> students, int col)
      {
        double score1 = 0;
        double score2 = 0;

        // Also wird dieser If-Block nicht durchgefuehrt,
        // wenn jetzt ueber den 2. Wunsch bestimmt werden soll
        // denn es wird schon in processCol gemacht
        // und hier ist es unnoetig
        if (col == 0 && this.Wishes[1].Avaliable)
        {
          score1 = 10000d / (double)GetAllPersonIndexWithSameGiftWish(this.Wishes[1], students, 1).Length;
          int appearTimesIn2 = GetAllPersonIndexWithSameGiftWish(this.Wishes[1], students, 2).Length;
          score1 = score1 - appearTimesIn2 > 1 ? score1 - appearTimesIn2 : 1;
        }

        if (col < 2 && this.Wishes[2].Avaliable)
        {
          score2 = 100d / (double)GetAllPersonIndexWithSameGiftWish(this.Wishes[2], students, 2).Length;
          int appearTimesIn1 = GetAllPersonIndexWithSameGiftWish(this.Wishes[2], students, 1).Length;
          score2 = score2 - 10 * appearTimesIn1 > 1 ? score2 - 10 * appearTimesIn1 : 1;
        }

        // Dementsprechend wird fuer den letzten Wunsch gar nichts gerechnet
        // weil es sich kein Sinn mehr ergibt
        // Falls es in der 3. Reihe noch mehrere Moeglichkeiten gibt,
        // waehlt man dann eins zufaellig aus
        // Das beeinflusst nicht, wie gut diese Verteilung ist
        // (denn alle der 3. Wunsch ist und nur einer erfuellt werden kann)

        return score1 + score2;
      }
    }
  }
}
