using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

using rat = Rationals.Rational;
using Aufgabe1.DataStructure;

namespace Aufgabe1.Drawing
{
  public class Painter
  {

    private static List<int> GetPossibleCols(List<int[]> chosen, int[][] grid, int i)
    {
      List<int> rtr = new List<int>();
      for (int c = 0; c < FlohmarktManagement.HEIGHT - chosen[i][2] + 1; c++)
      {
        if (Enumerable.Range(c, chosen[i][2]).All(c => Enumerable.Range(chosen[i][0] - FlohmarktManagement.START_TIME, chosen[i][1] - chosen[i][0]).All(r => grid[r][c] == -1)))
        {
          rtr.Add(c);
        }
      }
      return rtr;
    }

    public static void Paint(string path, Dictionary<string, rat> result, List<int[]> data)
    {
      List<int[]> chosen = data.Where(item => result[$"x{item[3]}"] == 1).ToList();
      QuickSort.Sort(chosen, 0, chosen.Count - 1);

      // Initiation
      int[][] grid = new int[FlohmarktManagement.INTERVAL_LENGTH][];
      for (int i = 0; i < grid.Length; i++)
      {
        grid[i] = Enumerable.Range(0, FlohmarktManagement.HEIGHT).Select(_ => -1).ToArray();
      }


      for (int i = 0; i < chosen.Count; i++)
      {
        List<int> possibleCols = GetPossibleCols(chosen, grid, i);

        // DEBUG
        if (possibleCols.Count == 0)
        {
          List<int> freeCol =
            Enumerable.Range(0, FlohmarktManagement.HEIGHT)
                      .Where(c => grid[chosen[i][0] - FlohmarktManagement.START_TIME][c] == -1)
                      .ToList();

          // Get longest consecutive cols
          int longestConsecutiveIdx = freeCol[0];
          int consecutiveLength = 1;
          for (int j = 0, currentStart = freeCol[0], currentLength = 1; j < freeCol.Count - 1; j++)
          {
            if (freeCol[j] + 1 == freeCol[j + 1])
            {
              currentLength++;
            }
            else
            {
              currentLength = 1;
              currentStart = freeCol[j + 1];
            }
            if (currentLength > consecutiveLength)
            {
              longestConsecutiveIdx = currentStart;
              consecutiveLength = currentLength;
            }
          }

          // Get col and row cut for free cols other than the longest consecutive
          for (int cI = 0; cI < freeCol.Count; cI++)
          {
            if (freeCol[cI] >= longestConsecutiveIdx && freeCol[cI] < longestConsecutiveIdx + consecutiveLength)
              continue;
            int objLeftIdx = grid[chosen[i][0] - FlohmarktManagement.START_TIME][freeCol[cI]];
            // Inset at left
            int insertAtCol = -1;
            // exclusive right border of the cut
            int cutAtCol = objLeftIdx == -1 ? freeCol[cI] + 1 : freeCol[cI] + chosen[objLeftIdx][2];
            // freeCol[cI] is the inclusive left border of the cut

            int startRow;
            if (objLeftIdx == -1)
            {
              startRow = chosen[i][0] - FlohmarktManagement.START_TIME;
            }
            else
            {
              startRow = chosen[objLeftIdx][0] - FlohmarktManagement.START_TIME;
            }

            // Cut AND dest row cut
            for (int r = startRow; r >= 0; r--)
            {
              // Check cut
              bool hasColCut = true;
              for (int R = chosen[i][0] - FlohmarktManagement.START_TIME; R >= r; R--)
              {
                if (grid[R][freeCol[cI]] == grid[R][freeCol[cI] - 1]
                  || (cutAtCol != FlohmarktManagement.HEIGHT
                  && grid[R][cutAtCol] == grid[R][cutAtCol - 1]))
                {
                  hasColCut = false;
                  break;
                }
              }
              if (!hasColCut) continue;

              // Check dest
              for (int c = longestConsecutiveIdx; c < consecutiveLength + longestConsecutiveIdx; c++)
              {
                bool HasRowCut = true;
                if (r != 0)
                {
                  for (int C = freeCol[cI]; C < c; C++)
                  {
                    if (grid[r][C] == grid[r - 1][C])
                    {
                      HasRowCut = false;
                      break;
                    }
                  }
                }
                if (!HasRowCut) continue;


                for (int R = chosen[i][0] - FlohmarktManagement.START_TIME - 1; R >= r; R--)
                {
                  if (grid[R][c] == grid[R][c - 1])
                  {
                    break;
                  }
                  if (R == r) insertAtCol = c;
                }

                // Test last col
                int lastCol = consecutiveLength + longestConsecutiveIdx - 1;
                if (insertAtCol == -1 && (r == 0 || grid[r][lastCol] == grid[r - 1][lastCol]))
                {
                  insertAtCol = lastCol + 1;
                }
              }

              if (insertAtCol != -1)
              {
                // DoInsert TODO
                InsertCol(insertAtCol, freeCol[cI], cutAtCol, r, grid);
                break;
              }
            }
          }
          possibleCols = GetPossibleCols(chosen, grid, i);
        }

        // Add current block
        int col = possibleCols
                    .OrderBy(a => GetBlank(a, chosen[i][0] - FlohmarktManagement.START_TIME, chosen[i][2], grid))
                    .First();
        if (possibleCols
                .Select(pC => GetBlank(pC, chosen[i][0] - FlohmarktManagement.START_TIME, chosen[i][2], grid))
                .All(n => n == 0))
        {
          col = possibleCols[0];
        }
        for (int row = chosen[i][0] - FlohmarktManagement.START_TIME;
              row < chosen[i][1] - FlohmarktManagement.START_TIME;
              row++)
        {
          for (int c = col; c < col + chosen[i][2]; c++)
          {
            grid[row][c] = chosen[i][3];
          }
        }


        DrawImage(grid); // DEBUG
      }


      DrawImage(grid);
    }

    // destCol: insert at the left side of this col
    // sourceCol: inclusive left border of the cut
    // endSourceCol: exclusive right border of the cut
    // cutRow: inclusive top border of the cut
    private static void InsertCol(int destCol, int sourceCol, int endSourceCol, int cutRow, int[][] grid)
    {
      int shiftDistance = endSourceCol - sourceCol;
      // initiation
      int[][] cut = new int[FlohmarktManagement.INTERVAL_LENGTH - cutRow][];
      for (int row = 0, i = cutRow; row < cut.Length; row++, i++)
      {
        cut[row] = new int[endSourceCol - sourceCol];
        for (int col = 0, j = sourceCol; col < shiftDistance; col++, j++)
        {
          cut[row][col] = grid[i][j];
        }
      }

      // Move the cols between dest and source
      for (int row = cutRow; row < FlohmarktManagement.INTERVAL_LENGTH; row++)
      {
        for (int col = sourceCol - 1; col >= destCol; col--)
        {
          grid[row][col + shiftDistance] = grid[row][col];
        }
      }

      // Insert the cached cols
      for (int row = 0, i = cutRow; row < cut.Length; row++, i++)
      {
        for (int col = 0, j = destCol; col < cut[0].Length; col++, j++)
        {
          grid[i][j] = cut[row][col];
        }
      }
    }



    private static int expCount = 0;
    public static void DrawImage(int[][] grid)
    {
      Image image = new Bitmap(600, 400);
      Graphics graph = Graphics.FromImage(image);

      graph.Clear(Color.Azure);

      Rectangle rect;
      Dictionary<int, Color> lut = new Dictionary<int, Color>
      {
        { -1, Color.White }
      };
      for (int r = 0; r < grid.Length; r++)
      {
        for (int c = 0; c < grid[0].Length; c++)
        {
          rect = new Rectangle(c * 100, r * 100, 100, 100);
          if (lut.TryGetValue(grid[r][c], out Color color))
          {
            graph.FillRectangle(new SolidBrush(color), rect);
            graph.DrawString($"{grid[r][c]}", new Font(FontFamily.GenericMonospace, 40), new SolidBrush(Color.Black), rect);
          }
          else
          {
            Color colour = GetRandomColor();
            lut.Add(grid[r][c], colour);
            graph.FillRectangle(new SolidBrush(colour), rect);
            graph.DrawString($"{grid[r][c]}", new Font(FontFamily.GenericMonospace, 40), new SolidBrush(Color.Black), rect);
          }

        }
      }

      image.Save($"F:\\repos\\BWI2020\\RUNDE2\\Aufgabe1\\graph{expCount++}.png", System.Drawing.Imaging.ImageFormat.Png);
      counter = 0;
    }

    private static int counter = 0;
    public static Color GetRandomColor()
    {
      if (++counter == GetColors().Count()) counter = 0;
      return GetColors().ElementAt(counter);
    }

    public static IEnumerable<Color> GetColors()
    {
      yield return Color.Aqua;
      yield return Color.MediumPurple;
      yield return Color.BlueViolet;
      yield return Color.Gold;
      yield return Color.Gray;
      yield return Color.LawnGreen;
      yield return Color.LightGoldenrodYellow;
      yield return Color.LightPink;
      yield return Color.Orange;
      yield return Color.NavajoWhite;
    }

    private static int GetBlank(int col, int startRow, int width, int[][] grid)
    {
      if (col + width > FlohmarktManagement.HEIGHT) throw new ArgumentException(nameof(col));
      int blankOfA = 0;
      for (int i = startRow - 1; i >= 0; i--)
      {
        for (int c = col; c < col + width; c++)
        {
          if (grid[i][c] == -1) blankOfA++;
          else break;
        }
      }
      return blankOfA;
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
              (arr[j][1] == arr[high][1] && arr[j][0] < arr[high][0]))
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