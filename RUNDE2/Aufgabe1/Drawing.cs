using System;
using System.Collections.Generic;
using System.Linq;

using rat = Rationals.Rational;

namespace Aufgabe1.Drawing
{
  public class Painter
  {
    public static void Paint(string path, Dictionary<string, rat> result, List<int[]> data)
    {
      List<int[]> chosen = data.Where(item => result[$"x{item[3]}"] == 1).ToList();
      QuickSort.Sort(chosen, 0, chosen.Count - 1);
      int[][] grid = new int[FlohmarktManagement.INTERVAL_LENGTH][];
      for (int i = 0; i < grid.Length; i++)
      {
        grid[i] = Enumerable.Range(0, FlohmarktManagement.HEIGHT).Select(_ => -1).ToArray();
      }

      

    }

    private static class QuickSort
    {
      public static void Sort(List<int[]> arr, int low, int high)
      {
        if (high <= low) return;

        int pIndex = Partition(arr, low, high);
        Sort(arr, low, pIndex - 1);
        Sort(arr, pIndex + 1, high);
      }

      private static int Partition(List<int[]> arr, int low, int high)
      {
        int i = low - 1;
        for (int j = low; j < high; j++)
        {
          if (arr[j][1] < arr[high][1] ||
            (arr[j][1] == arr[high][1] && arr[j][2] < arr[high][2]))
          {
            Swap(arr, ++i, j);
          }
        }
        Swap(arr, ++i, high);
        return i;
      }

      private static void Swap(List<int[]> arr, int i1, int i2)
      {
        if (i1 == i2) return;
        int[] temp = arr[i1];
        arr[i1] = arr[i2];
        arr[i2] = temp;
      }
    }
  }
}