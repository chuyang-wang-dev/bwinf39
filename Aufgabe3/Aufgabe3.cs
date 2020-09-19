using System;
using System.IO;

namespace Aufgabe3
{
  public class Turnier
  {
    public static void Main(string[] args)
    {
      RNGProvider rng = new RNGProvider();
      Console.WriteLine("Bitte Name der Test-Datei eingeben...");
      string testFilePath = Console.ReadLine();

      if (File.Exists(testFilePath))
      {
        using (StreamReader sr = File.OpenText(testFilePath))
        {
          int playerCount = Convert.ToInt32(sr.ReadLine());

          // int[i] = Spielstaerke der SpielerIn i+1
          int[] playerStrength = new int[playerCount];
          for (int i = 0; i < playerCount; i++)
          {
            playerStrength[i] = Convert.ToInt32(sr.ReadLine());
          }

          int bestPlayerNum;
          double procent;

          KOx1 koX1 = new KOx1(playerStrength, rng, new int[] {
            0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
          });
          koX1.PlayGameNth(1000000);
          procent = koX1.GetWinningProcentOfBestPlayer(out bestPlayerNum);
          Console.WriteLine($"{bestPlayerNum}: {procent}");

          KOx5 koX5 = new KOx5(playerStrength, rng, new int[] {
            0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
          });
          koX5.PlayGameNth(1000000);
          procent = koX5.GetWinningProcentOfBestPlayer(out bestPlayerNum);
          Console.WriteLine($"{bestPlayerNum}: {procent}");

          Liga liga = new Liga(playerStrength, rng);
          liga.PlayGameNth(1000000);
          procent = liga.GetWinningProcentOfBestPlayer(out bestPlayerNum);
          Console.WriteLine($"{bestPlayerNum}: {procent}");


        }
      }
      else
      {
        Console.WriteLine("Die gegebene Datei-Namen ist nicht gueltig. Das Programm und die Datei muessen in demselben Ordner stehen. ");
      }
      Console.ReadKey();
    }

    private class Liga : GameModel
    {
      public Liga(int[] playerStrengths, RNGProvider rngProvider)
      {
        _playerStrengths = playerStrengths;
        _winTimesTotal = new int[playerStrengths.Length];
        // Set default val to 0
        Array.Clear(_winTimesTotal, 0, _winTimesTotal.Length);
        _rngProvider = rngProvider;
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
            if (_rngProvider.GetIntBetween(1, _playerStrengths[againstPlayerNum] + _playerStrengths[playerNumber]) <= _playerStrengths[playerNumber])
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
      public KOx1(int[] playerStrengths, RNGProvider rngProvider, int[] playPlan)
      {
        _playerStrengths = playerStrengths;
        _winTimesTotal = new int[playerStrengths.Length];
        // Set default val to 0
        Array.Clear(_winTimesTotal, 0, _winTimesTotal.Length);
        _rngProvider = rngProvider;
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
        Node<int> treeRoot = new CompleteBinaryTreeBuilder<int>(initData).Root;
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
        if (_rngProvider.GetIntBetween(1, _playerStrengths[player1.Data] + _playerStrengths[player2.Data]) <= _playerStrengths[player1.Data])
        {
          return player1.Data;
        }
        else return player2.Data;
      }
    }

    private class KOx5 : GameModel
    {
      private int[] _playPlan;
      public KOx5(int[] playerStrengths, RNGProvider rngProvider, int[] playPlan)
      {
        _playerStrengths = playerStrengths;
        _winTimesTotal = new int[playerStrengths.Length];
        // Set default val to 0
        Array.Clear(_winTimesTotal, 0, _winTimesTotal.Length);
        _rngProvider = rngProvider;
        _playPlan = playPlan;
      }
      public override void PlayGameOnce()
      {
        // Initialize the tree
        int[] initData = new int[_playPlan.Length * 2 - 1];
        for (int i = 0; i < initData.Length; i++) initData[i] = -1;
        _playPlan.CopyTo(initData, _playPlan.Length - 1);
        // Stores the play plan
        Node<int> treeRoot = new CompleteBinaryTreeBuilder<int>(initData).Root;
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
          if (_rngProvider.GetIntBetween(1, _playerStrengths[player1.Data] + _playerStrengths[player2.Data]) <= _playerStrengths[player1.Data]) winningTimeCount[0]++;
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
      protected RNGProvider _rngProvider;
      public abstract void PlayGameOnce();
      public abstract void PlayGameNth(int n);
      // overload
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
      public double GetWinningProcentOfBestPlayerO(out int bestPlayerNumber)
      {
        bestPlayerNumber = -1;
        int mostWinTimes = -1, totalTimes = 0;
        for (int i = 0; i < _winTimesTotal.Length; i++)
        {
          totalTimes += _winTimesTotal[i];
          if (_winTimesTotal[i] > mostWinTimes)
          {
            mostWinTimes = _winTimesTotal[i];
            bestPlayerNumber = i;
          }
          else if (_winTimesTotal[i] == mostWinTimes)
          {
            bestPlayerNumber = bestPlayerNumber < i ? bestPlayerNumber : i;
          }
        }
        return (double)mostWinTimes / (double)totalTimes;
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

    private class CompleteBinaryTreeBuilder<T>
    {
      public Node<T> Root { get; }
      public CompleteBinaryTreeBuilder(params T[] dataArray)
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

    private class RNGProvider
    {
      private System.Random rng = new System.Random();

      // i1 and i2 are inclusive
      public int GetIntBetween(int i1, int i2)
      {
        if (i1 < 0 || i1 > i2) throw new ArgumentException(nameof(i1));
        return rng.Next(i1, i2 + 1);
      }
    }
  }
}
