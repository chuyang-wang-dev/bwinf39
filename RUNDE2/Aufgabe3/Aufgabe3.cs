using System;
using System.IO;
using System.Linq;

namespace Aufgabe3
{
  public class EisbudenSolver
  {
    public static void Main(string[] args)
    {
      System.Console.WriteLine(Debug.Bruteforce.Validate(new Circle(20, new int[] { 1, 2, 7, 9, 11, 12, 13, 14, 17, 19 }), new int[] { 1, 8, 12 }));
      Debug.Test.m();
      /*

      Console.OutputEncoding = System.Text.Encoding.UTF8;
      if (TryReadInput(out Circle data))
      {
        System.Console.WriteLine(Debug.Bruteforce.Validate(data, new int[] { 1, 5, 14 }));
      }

      Console.WriteLine("\r\nDrueck eine beliebige Taste zu schliessen...");
      */
      Console.ReadKey();
    }

    public static bool TryReadInput(out Circle data)
    {
      data = null;
      Console.WriteLine("Bitte Name der Test-Datei eingeben...");
      string testFilePath = Console.ReadLine();

      if (File.Exists(testFilePath))
      {
        using (StreamReader sr = File.OpenText(testFilePath))
        {
          int[] l1 = sr.ReadLine().Trim().Split(' ').Select(s => Convert.ToInt32(s)).ToArray();
          int u = l1[0], houseCount = l1[1];
          int[] houseIndeces = sr.ReadLine().Trim().Split(' ').Select(s => Convert.ToInt32(s)).ToArray();
          data = new Circle(u, houseIndeces);
          return true;
        }
      }
      else
      {
        Console.WriteLine("Die gegebene Datei-Namen ist nicht gueltig. Das Programm und die Datei muessen in demselben Ordner stehen. ");
        return false;
      }
    }
  }



  public class Circle
  {
    public int Circumference { get; }
    private readonly bool[] hasHouses;
    public int[] HouseNumbers { get; }
    public bool this[int index]
    {
      get
      {
        if (Math.Abs(index) > (Circumference - 1)) throw new IndexOutOfRangeException();
        if (index >= 0) return hasHouses[index];
        else return hasHouses[Circumference + index];
      }
    }
    public int Length { get; }
    public Circle(int circumference, int[] housePositions)
    {
      if (circumference <= 0) throw new ArgumentException(nameof(circumference));
      if (housePositions.Length <= 0 || housePositions.Length > circumference) throw new ArgumentException(nameof(housePositions));
      Circumference = circumference;
      hasHouses = Enumerable.Range(0, circumference).
        Select(idx => housePositions.Contains(idx)).
        ToArray();
      Length = housePositions.Length;
      HouseNumbers = housePositions;
    }

    public int[] GetAllLengthsTo(int pos)
    {
      int[] rtr = new int[Length];
      for (int i = 0; i < Length; i++)
      {
        rtr[i] = GetLengthTo(HouseNumbers[i], pos);
      }
      return rtr;
    }

    public int GetLengthTo(int house, int pos)
    {
      int temp = Math.Abs(house - pos);
      return temp > Circumference / 2 ? Circumference - temp : temp;
    }

  }
}
