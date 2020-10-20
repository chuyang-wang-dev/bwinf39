/// Author: Chuyang Wang, 2020
/// C# 8.0, .Net Core 3.1
/// To compile code, download .Net Core SDK 3.1 from https://dotnet.microsoft.com/download


using System;
using System.IO;
using System.Linq;

namespace Aufgabe3
{
  public static class Turnier
  {
    private const int REPEAT = 1000000;
    private static System.Random rng = new System.Random();

    // Zufallszahlengenerator
    // i1 and i2 are inclusive
    private static int GetIntBetween(int i1, int i2)
    {
      if (i1 < 0 || i1 > i2) throw new ArgumentException(nameof(i1));
      return rng.Next(i1, i2 + 1);
    }

    // Erweiterungsmethode, die testet, 
    // ob der gegebenen Spielplan gueltig ist
    private static bool IsValid(this int[] spielPlan)
    {
      if (spielPlan.Distinct().Count() < spielPlan.Length) return false;
      for (int i = 0; i < spielPlan.Length; i++)
      {
        if (spielPlan[i] < 0 || spielPlan[i] >= spielPlan.Length) return false;
      }
      return true;
    }

    public static void Main(string[] args)
    {
      Console.WriteLine("Bitte Name der Test-Datei eingeben...");
      string testFilePath = Console.ReadLine();

      if (File.Exists(testFilePath))
      {
        using (StreamReader sr = File.OpenText(testFilePath))
        {
          // Einlesen von Daten
          int playerCount = Convert.ToInt32(sr.ReadLine());

          // int[i] = Spielstaerke der SpielerIn i+1
          int[] playerStrength = new int[playerCount];
          for (int i = 0; i < playerCount; i++)
          {
            playerStrength[i] = Convert.ToInt32(sr.ReadLine());
          }

          // Einlesen von Spielplan
          int[] spielPlan = new int[playerCount];
          while (!spielPlan.IsValid())
          {
            Console.WriteLine("Geben Sie einen Spielplan ein...");
            Console.Write("Es kann wie Folgendes aussehen: ");
            Console.Write(playerCount == 8 ? "1,2,3,4,5,6,7,8\r\n" : "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16\r\n");
            string[] temp = Console.ReadLine().Split(',');
            for (int i = 0; i < temp.Length; i++)
            {
              if (!Int32.TryParse(temp[i], out spielPlan[i]))
              {
                Console.WriteLine("ggb. Spielplan ungueltig. Bitte erneurt eingeben.");
                break;
              }
              else
              {
                // Index ist 1 kleiner als die richtige Spielnummer
                // denn der faengt mit 0 an
                spielPlan[i]--;
              }
            }
          }

          // Das Turnier in den 3 Spielformen durchfuehren
          int bestPlayerNum;
          double procent;

          KOx1 koX1 = new KOx1(playerStrength, spielPlan);
          koX1.PlayGameNth(REPEAT);
          procent = koX1.GetWinningProcentOfBestPlayer(out bestPlayerNum);
          // Index ist 1 kleiner als die richtige Spielnummer
          // also bestPlayerNum + 1 , 
          // sodass anstatt des Indexes die Spielernummer gezeigt werden kann
          Console.WriteLine($"KOx1: {bestPlayerNum + 1} - {procent}");

          KOx5 koX5 = new KOx5(playerStrength, spielPlan);
          koX5.PlayGameNth(REPEAT);
          procent = koX5.GetWinningProcentOfBestPlayer(out bestPlayerNum);
          Console.WriteLine($"KOx5: {bestPlayerNum + 1} - {procent}");

          Liga liga = new Liga(playerStrength);
          liga.PlayGameNth(REPEAT);
          procent = liga.GetWinningProcentOfBestPlayer(out bestPlayerNum);
          Console.WriteLine($"Liga: {bestPlayerNum + 1} - {procent}");
        }
      }
      else
      {
        Console.WriteLine("Die gegebene Datei-Namen ist nicht gueltig. Das Programm und die Datei muessen in demselben Ordner stehen. ");
      }
      Console.WriteLine("\r\nDrueck eine beliebige Taste zu schliessen...");
      Console.ReadKey();
    }

    private class Liga : GameModel
    {
      public Liga(int[] playerStrengths)
      {
        _playerStrengths = playerStrengths;
        _winTimesTotal = new int[playerStrengths.Length];
        // Set default val to 0
        Array.Clear(_winTimesTotal, 0, _winTimesTotal.Length);
      }

      public override void PlayGameNth(int n)
      {
        for (int i = 0; i < n; i++)
        {
          PlayGameOnce();
        }
      }

      public override void PlayGameOnce()
      {
        int[] winTimes = new int[_playerStrengths.Length];
        // Set default val to 0
        Array.Clear(winTimes, 0, winTimes.Length);
        // Array index = the player number - 1
        for (int playerNumber = 0; playerNumber < _playerStrengths.Length - 1; playerNumber++)
        {
          for (int againstPlayerNum = playerNumber + 1; againstPlayerNum < _playerStrengths.Length; againstPlayerNum++)
          {
            if (GetIntBetween(1, _playerStrengths[againstPlayerNum] + _playerStrengths[playerNumber]) <= _playerStrengths[playerNumber])
            {
              // 1. Player wins
              winTimes[playerNumber] += 1;
            }
            else
            {
              // 2. Player wins
              winTimes[againstPlayerNum] += 1;
            }
          }
        }
        int highstScorePlayer = -1;
        int highstScore = -1;
        for (int i = 0; i < winTimes.Length; i++)
        {
          if (winTimes[i] > highstScore)
          {
            highstScorePlayer = i;
            highstScore = winTimes[i];
          }
          else if (winTimes[i] == highstScore)
          {
            highstScorePlayer = highstScorePlayer < i ? highstScorePlayer : i;
          }
        }
        _winTimesTotal[highstScorePlayer] += 1;
      }
    }


    private class KOx1 : GameModel
    {
      private int[] _playPlan;
      public KOx1(int[] playerStrengths, int[] playPlan)
      {
        _playerStrengths = playerStrengths;
        _winTimesTotal = new int[playerStrengths.Length];
        // Set default val to 0
        Array.Clear(_winTimesTotal, 0, _winTimesTotal.Length);
        _playPlan = playPlan;
      }

      public override void PlayGameNth(int n)
      {
        for (int i = 0; i < n; i++)
        {
          PlayGameOnce();
        }
      }

      public override void PlayGameOnce()
      {
        // Initialize the tree
        int[] initData = new int[_playPlan.Length * 2 - 1];
        for (int i = 0; i < initData.Length; i++) initData[i] = -1;
        _playPlan.CopyTo(initData, _playPlan.Length - 1);
        // Stores the play plan
        Node<int> treeRoot = new BinaryTreeBuilder<int>(initData).Root;
        GetGameResult(treeRoot);
        _winTimesTotal[treeRoot.Data] += 1;
      }

      private void GetGameResult(Node<int> currentNode)
      {
        if (currentNode.RightChild != null)
        {
          GetGameResult(currentNode.LeftChild);
          GetGameResult(currentNode.RightChild);
          currentNode.Data = GetWinner(currentNode.LeftChild, currentNode.RightChild);
        }
        else
        {
          currentNode.Parent.Data = GetWinner(currentNode.Parent.LeftChild, currentNode.Parent.RightChild);
        }
      }

      private int GetWinner(Node<int> player1, Node<int> player2)
      {
        if (GetIntBetween(1, _playerStrengths[player1.Data] + _playerStrengths[player2.Data]) <= _playerStrengths[player1.Data])
        {
          return player1.Data;
        }
        else return player2.Data;
      }
    }

    private class KOx5 : GameModel
    {
      private int[] _playPlan;
      public KOx5(int[] playerStrengths, int[] playPlan)
      {
        _playerStrengths = playerStrengths;
        _winTimesTotal = new int[playerStrengths.Length];
        // Set default val to 0
        Array.Clear(_winTimesTotal, 0, _winTimesTotal.Length);
        _playPlan = playPlan;
      }
      public override void PlayGameOnce()
      {
        // Initialize the tree
        int[] initData = new int[_playPlan.Length * 2 - 1];
        for (int i = 0; i < initData.Length; i++) initData[i] = -1;
        _playPlan.CopyTo(initData, _playPlan.Length - 1);
        // Stores the play plan
        Node<int> treeRoot = new BinaryTreeBuilder<int>(initData).Root;
        GetGameResult(treeRoot);
        _winTimesTotal[treeRoot.Data] += 1;
      }

      private void GetGameResult(Node<int> currentNode)
      {
        if (currentNode.RightChild != null)
        {
          GetGameResult(currentNode.LeftChild);
          GetGameResult(currentNode.RightChild);
          currentNode.Data = GetWinner(currentNode.LeftChild, currentNode.RightChild);
        }
        else
        {
          currentNode.Parent.Data = GetWinner(currentNode.Parent.LeftChild, currentNode.Parent.RightChild);
        }
      }
      private int GetWinner(Node<int> player1, Node<int> player2)
      {
        int[] winningTimeCount = new int[2];
        Array.Clear(winningTimeCount, 0, 2);
        for (int i = 0; i < 5; i++)
        {
          if (GetIntBetween(1, _playerStrengths[player1.Data] + _playerStrengths[player2.Data]) <= _playerStrengths[player1.Data]) winningTimeCount[0]++;
          else winningTimeCount[1]++;
        }
        return winningTimeCount[0] > winningTimeCount[1] ? player1.Data : player2.Data;
      }

      public override void PlayGameNth(int n)
      {
        for (int i = 0; i < n; i++)
        {
          PlayGameOnce();
        }
      }
    }

    private abstract class GameModel
    {
      protected int[] _winTimesTotal;
      protected int[] _playerStrengths;
      public abstract void PlayGameOnce();
      public abstract void PlayGameNth(int n);
      public double GetWinningProcentOfBestPlayer(out int bestPlayerNumber)
      {
        bestPlayerNumber = 0;
        for (int i = 0; i < _playerStrengths.Length; i++)
        {
          if (_playerStrengths[i] > _playerStrengths[bestPlayerNumber]) bestPlayerNumber = i;
        }

        int totalTimes = 0;
        foreach (var i in _winTimesTotal)
        {
          totalTimes += i;
        }
        return (double)_winTimesTotal[bestPlayerNumber] / (double)totalTimes;
      }
    }

    private class Node<T>
    {
      public T Data { get; set; }
      public Node<T> LeftChild { get; set; }
      public Node<T> RightChild { get; set; }
      public Node<T> Parent { get; set; }

      public Node(T data)
      {
        this.Data = data;
      }

    }

    private class BinaryTreeBuilder<T>
    {
      public Node<T> Root { get; }
      public BinaryTreeBuilder(params T[] dataArray)
      {
        Root = build(dataArray, 0, null);
      }

      private Node<T> build(T[] arr, int currentIndex, Node<T> parentNode)
      {
        if (currentIndex < arr.Length)
        {
          Node<T> currentNode = new Node<T>(arr[currentIndex]);
          currentNode.Parent = parentNode;
          currentNode.LeftChild = build(arr, currentIndex * 2 + 1, currentNode);
          currentNode.RightChild = build(arr, currentIndex * 2 + 2, currentNode);
          return currentNode;
        }
        return null;
      }
    }
  }
}
