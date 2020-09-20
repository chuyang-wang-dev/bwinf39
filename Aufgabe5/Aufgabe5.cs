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
    private static bool Contains(this int[] arr, int val)
    {
      foreach (int i in arr)
      {
        if (i == val) return true;
      }
      return false;
    }

    public static void Main(string[] args)
    {
      Console.WriteLine("Bitte Name der Test-Datei eingeben...");
      string testFilePath = Console.ReadLine();

      if (File.Exists(testFilePath))
      {
        using (StreamReader sr = File.OpenText(testFilePath))
        {
          int studentCount = Convert.ToInt32(sr.ReadLine());

          List<Person> students = new List<Person>(studentCount);
          for (int i = 0; i < studentCount; i++)
          {
            string[] line = sr.ReadLine().Trim().Split(' ');
            int[] wishesWithIndexI = new int[3];
            for (int j = 0, wishI = 0; j < line.Length; j++)
            {
              // Die Eingabedateien sind offenbar nicht nur mit einem Leerzeichen getrennt
              // bspw. kommt 3  1 10 vor
              // Deshalb wird geprueft, nur wenn es wirklich eine Zahl enthaelt, wird dies dann gespeichert
              if (!string.IsNullOrWhiteSpace(line[j])) wishesWithIndexI[wishI++] = Convert.ToInt32(line[j].Trim()) - 1;
            }
            students.Add(new Person(i, wishesWithIndexI));
          }

          int[] givenGiftsFromCol0 = GetAllGivenGiftInCol(0, students, new List<Wish>());

          // key: gift number
          // val: person who has it as index
          Dictionary<int, int> giftsTo = new Dictionary<int, int>();

          // Inspect the 0. col and take the indentical gift wish
          processCol(0, ref students, ref giftsTo, givenGiftsFromCol0);
          processCol(1, ref students, ref giftsTo, giftsTo.Keys.ToArray());
          processCol(2, ref students, ref giftsTo, giftsTo.Keys.ToArray());

          Dictionary<int, int> personWithGift = new Dictionary<int, int>();
          for (int i = 0; i < studentCount; i++)
          {
            int personIndex;
            if (giftsTo.TryGetValue(i, out personIndex)) personWithGift.Add(personIndex, i);
            else
            {
              for (int j = 0; j < students.Count; j++)
              {
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

          for (int i = 0; i < studentCount; i++)
          {
            // Ausgabe: S G
            // S ist die Nummer des/r SchuelerIn,
            // G ist die Nummer der Gegenstand, der an dem/der SchuelerIn S gegeben wird
            // Bsp: 1 3
            // Das heisst, der/die Schueler 1 bekommt den Gegenstand 3
            Console.WriteLine($"{i + 1} {personWithGift[i] + 1}");
          }
        }
      }
      else
      {
        Console.WriteLine("Die gegebene Datei-Namen ist nicht gueltig. Das Programm und die Datei muessen in demselben Ordner stehen. ");
      }
      Console.ReadKey();
    }

    private static void processCol(int col, ref List<Person> students, ref Dictionary<int, int> giftsTo, int[] givenGifts)
    {
      int studentCount = students.Count;
      for (int i = 0; i < studentCount; i++)
      {
        int[] allPersonWithSameWish = GetAllPersonIndexWithSameGiftWish(students[i].Wishes[col], students, col);
        if (allPersonWithSameWish.Length == 1 && allPersonWithSameWish[0] == i)
        {
          giftsTo.Add(students[i].Wishes[col].GiftWish, students[i].Index);
          students[i].GotGift = students[i].Wishes[col].GiftWish;
          students[i].SetAllWishToUnable();
        }
      }
      // change the avaliable attribute of all person's wishes
      for (int idx = 0; idx < studentCount; idx++)
      {
        students[idx].CheckAllWishesAvaliblility(givenGifts.Union(giftsTo.Keys).ToArray(), col + 1);
      }

      // Check the gift option that comes more than once
      for (int i = 0; i < studentCount; i++)
      {
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
          // change the avaliable attribute of all person's wishes
          for (int idx = 0; idx < studentCount; idx++)
          {
            students[idx].CheckAllWishesAvaliblility(giftsTo.Keys.ToArray(), col);
          }
        }
      }
    }

    private static int[] GetAllPersonIndexWithSameGiftWish(Wish giftWish, List<Person> students, int col)
    {
      List<int> persons = new List<int>();
      for (int i = 0; i < students.Count; i++)
      {
        if (students[i].Wishes[col].GiftWish == giftWish.GiftWish && students[i].Wishes[col].Avaliable) persons.Add(students[i].Index);
      }
      return persons.ToArray();
    }

    //private static int[] GetAllSameGiftWishWithPersonIndex(Person person, List<Person> students, int col)


    private static int[] GetAllGivenGiftInCol(int col, List<Person> students, List<Wish> except)
    {
      List<int> giftNums = new List<int>();
      for (int i = 0; i < students.Count; i++)
      {
        if (!giftNums.Contains(students[i].Wishes[col].GiftWish) && !except.Contains(students[i].Wishes[col])) giftNums.Add(students[i].Wishes[col].GiftWish);
      }
      return giftNums.ToArray();
    }

    private class Wish
    {
      public int GiftWish
      {
        get;
      }

      public bool Avaliable
      {
        get;
        set;
      }

      public int Col
      {
        get;
      }

      public Wish(int person, int wish, int col)
      {
        Avaliable = true;
        GiftWish = wish;
        Col = col;
      }

      public static Wish[] ConvertToWishes(int[] wishesOfOnePerson, int personIndex)
      {
        return new Wish[] { new Wish(personIndex, wishesOfOnePerson[0], 0), new Wish(personIndex, wishesOfOnePerson[1], 1), new Wish(personIndex, wishesOfOnePerson[2], 2) };
      }
    }

    private class Person
    {
      public int Index
      {
        get;
      }

      public Wish[] Wishes
      {
        get;
      }

      public Wish Wish1
      {
        get { return Wishes[0]; }
      }

      public Wish Wish2
      {
        get { return Wishes[1]; }
      }

      public Wish Wish3
      {
        get { return Wishes[2]; }
      }

      public int GotGift
      {
        get; set;
      }

      public Person(int index, int[] wishes)
      {
        Index = index;
        Wishes = Wish.ConvertToWishes(wishes, index);
        GotGift = -1;
      }

      public void SetAllWishToUnable()
      {
        for (int i = 0; i < 3; i++) this.Wishes[i].Avaliable = false;
      }

      public void SetAvaliableOfWish(int wishCol, bool avaliable = false)
      {
        Wishes[wishCol].Avaliable = avaliable;
      }

      // If any wish is in the given array, change its status to unavaliable
      public void CheckAllWishesAvaliblility(int[] givenGift, int startCol)
      {
        for (int i = startCol; i < 3; i++)
        {
          if (givenGift.Contains(Wishes[i].GiftWish)) Wishes[i].Avaliable = false;
        }
      }
      public double Calculate(List<Person> students, int col)
      {
        double score1 = 0;
        double score2 = 0;

        if (col == 0 && this.Wishes[1].Avaliable)
        {
          score1 = (double)10000 / (double)GetAllPersonIndexWithSameGiftWish(this.Wishes[1], students, 1).Length;
          score1 -= score1 - GetAllPersonIndexWithSameGiftWish(this.Wishes[1], students, 2).Length > 0 ? GetAllPersonIndexWithSameGiftWish(this.Wishes[1], students, 2).Length : score1;
        }
        if (col < 2 && this.Wishes[2].Avaliable)
        {
          score2 = (double)100 / (double)GetAllPersonIndexWithSameGiftWish(this.Wishes[2], students, 2).Length;
          int appearTimesIn1 = GetAllPersonIndexWithSameGiftWish(this.Wishes[2], students, 1).Length;
          score2 = score2 - 10 * appearTimesIn1 > 1 ? score2 - 10 * appearTimesIn1 : 1;
        }
        return score1 + score2;
      }
    }
  }
}
