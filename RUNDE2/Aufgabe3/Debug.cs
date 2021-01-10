using System;
using System.Collections.Generic;
using System.Linq;

namespace Aufgabe3.Debug
{
  public class Test
  {
    public static void m()
    {
      List<Circle> l = new List<Circle>();
      RandomCircleGenerator rcg = new RandomCircleGenerator();
      for (int i = 0; i < 50; i++)
      {
        Circle data = rcg.Next(20, 10);
        int[] ans = Bruteforce.old_Solve(data);
        Console.WriteLine(ans != null ? string.Join(",", ans) : "No Ans");
        if (ans == null)
        {
          l.Add(data);
        }
      }
    }
  }

  public class RandomCircleGenerator
  {
    private Random rng = new Random();

    public Circle Next(int u, int houseCount)
    {
      int[] housePos = new int[houseCount];
      for (int i = 0; i < houseCount; i++)
      {
        int j = rng.Next(0, u);
        while (housePos.Contains(j))
        {
          j = rng.Next(0, u);
        }
        housePos[i] = j;
      }
      return new Circle(u, housePos);
    }
  }
  public class Bruteforce
  {
    public static bool Validate(Circle c, int[] pos)
    {
      for (int i = 0; i < c.Circumference - 2; i++)
      {
        for (int ii = 1; ii < c.Circumference - 1; ii++)
        {
          for (int iii = 2; iii < c.Circumference; iii++)
          {
            if (WillChange(c, pos, new int[] { i, ii, iii }))
            {
              return false;
            }
          }
        }
      }

      return true;
    }

    public static int[] Solve(Circle c)
    {
      for (int j = 0; j < c.Length - 2; j++)
      {
        for (int jj = 1; jj < c.Length - 1; jj++)
        {
          for (int jjj = 2; jjj < c.Length; jjj++)
          {
            int[] bestPos = new int[3] { c.HouseNumbers[j], c.HouseNumbers[jj], c.HouseNumbers[jjj] };

            if (Validate(c, bestPos)) return bestPos;
          }
        }
      }
      return null;
    }

    public static int[] old_Solve(Circle c)
    {
      for (int j = 0; j < c.Circumference - 2; j++)
      {
        for (int jj = 1; jj < c.Circumference - 1; jj++)
        {
          for (int jjj = 2; jjj < c.Circumference; jjj++)
          {
            int[] bestPos = new int[] { j, jj, jjj };

            if (Validate(c, bestPos)) return bestPos;
          }
        }
      }
      return null;
    }

    private static bool WillChange(Circle c, int[] oldPos, int[] newPos)
    {
      static int Min(int i1, int i2, int i3)
      {
        return Math.Min(i1, Math.Min(i2, i3));
      }
      // Index of array contianing house positions => length to nearest buden
      int[] oldLength = new int[c.Length];
      int[] newLength = new int[c.Length];

      int[][] allOldLths = new int[3][];
      for (int i = 0; i < allOldLths.Length; i++) allOldLths[i] = c.GetAllLengthsTo(oldPos[i]);
      oldLength = Enumerable.Range(0, c.Length).
        Select(idx => Min(allOldLths[0][idx], allOldLths[1][idx], allOldLths[2][idx])).
        ToArray();
      int[][] allNewLths = new int[3][];
      for (int i = 0; i < allNewLths.Length; i++) allNewLths[i] = c.GetAllLengthsTo(newPos[i]);
      newLength = Enumerable.Range(0, c.Length).
        Select(idx => Min(allNewLths[0][idx], allNewLths[1][idx], allNewLths[2][idx])).
        ToArray();

      int improvedCount = 0;
      for (int i = 0; i < c.Length; i++)
      {
        if (oldLength[i] > newLength[i]) improvedCount++;
      }
      return improvedCount > c.Length / 2;
    }
  }
}