/// Author: Chuyang Wang, 2020
/// C# 8.0, .Net Core 3.1
/// To compile code, download .Net Core SDK 3.1 from https://dotnet.microsoft.com/download


using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace JuniorAufgabe1
{
  public static class PasswordGenerator
  {

    // Alle deutshen Konsonanten-Buchstaben
    private static readonly string[] _konsonanten = new string[] { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "q", "r", "s", "t", "v", "w", "x", "z" };

    // Konsonantencluster
    // Clusters wie "bb", "dd", "ll" usw. sind ausgeschlossen, da sie zur Verwirrung fuehren koennten
    // Clusters, die am Anfang stehen koennen:
    private static readonly string[] _startClusters = new string[] { "bl", "br", "ch", "dr", "fl", "fr", "gn", "gr", "kn", "kr", "pf", "pfl", "pfr", "pl", "pn", "pr", "ph", "phl", "phr", "sk", "skl", "skr", "sl", "sp", "sph", "spl", "spr", "st", "str", "sz", "sch", "schl", "schm", "schn", "schr", "schw", "tr", "zw", "ch", "gl", "kl", "sp", "sph", "sch", "scht", "schr", "tsch" };

    // Clusters, die am Ende eines Wortes stehen koennen
    private static readonly string[] _endClusters = new string[] { "ch", "pf", "ph", "sk", "skl", "sl", "sp", "sph", "st", "sch", "bst", "bt", "ch", "chs", "cht", "dm", "dt", "ft", "gd", "gs", "gt", "kt", "ks", "lb", "lch", "ld", "lf", "lg", "lk", "lm", "ln", "lp", "ls", "lsch", "lt", "lz", "mb", "md", "mp", "mpf", "mph", "ms", "msch", "mt", "nd", "nf", "nft", "ng", "ngs", "ngst", "nk", "nkt", "ns", "nsch", "nst", "nt", "nx", "nz", "ps", "psch", "pst", "pt", "sk", "sp", "sph", "st", "sch", "ts", "tsch", "tz", "tzt", "xt" };

    // Alle Konsonanten-Clusters
    private static readonly string[] _clusters = new string[] { "bl", "br", "ch", "chl", "dr", "dw", "fl", "fr", "gn", "gr", "kn", "kr", "pf", "pfl", "pfr", "pl", "pn", "pr", "ph", "phl", "phr", "sk", "skl", "skr", "sl", "sp", "sph", "spl", "spr", "st", "str", "sz", "sch", "schl", "schm", "schn", "schr", "schw", "tr", "wr", "zw", "bs", "bsch", "bst", "bt", "ch", "chs", "cht", "dm", "dt", "ft", "gd", "gl", "gs", "gt", "kt", "kl", "ks", "lb", "lch", "ld", "lf", "lg", "lk", "lm", "ln", "lp", "ls", "lsch", "lt", "lv", "lz", "mb", "md", "mn", "mp", "mpf", "mph", "mphl", "ms", "msch", "mt", "nd", "nf", "nft", "ng", "ngl", "ngs", "ngst", "nk", "nkt", "ns", "nsch", "nst", "nt", "nx", "nz", "ps", "psch", "pst", "pt", "sk", "sp", "sph", "st", "sch", "scht", "schr", "tm", "ts", "tsch", "tw", "tz", "tzt", "xt" };

    // Buchstaben, die nicht am Ende eines Wortes stehen duerfen
    private static readonly string[] _unendableChars = new string[] { "r", "l", "j", "v" };

    // Alle deutsche Vocale + Zweilauten
    // Deutsche Umlaut wie ae, oe, ue werden hier nicht betracht
    private static readonly string[] _vocale = new string[] { "a", "o", "i", "e", "u", "y", "au", "ai", "eu", "ei", "ui" };

    private static readonly RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();


    // Programmeingang
    // Es soll in der ersten Zeile zwei Intenger, A L,  gegeben werden, die durch ein Leerzeichen getrennt werden (vgl. Dokumentation)
    // A representiert die Anzahl der Testfaelle bzw. Anzahl der zu generierende Passwoerter. A ist eine positive ganze Zahl, die nicht groesser als 2,147,483,647 sein sollte. 
    // L representiert die Laenge jedes Passwortes. L ist eine positive ganze Zahl, die nicht groesser als 2,147,483,647 sein sollte. 
    // Jedoch ist es schon sehr sinnvoll, eine relative kleine Zahl fuer A oder L zu geben. Bspw. A = 10, L = 12
    public static void Main(string[] args)
    {
      string[] input = Console.ReadLine().Split(' ');
      int testCases = Convert.ToInt32(input[0]);
      int length = Convert.ToInt32(input[1]);

      for (int i = 0; i < testCases; i++)
      {
        Console.WriteLine(GeneratePassword(length));
      }
    }


    /// Methode, die ein Passwort mit gegebener Laenge generiert
    /// @param length: Eine ganze Zahl, die besagt, wie lange das zu generierende Passwort sein sollte.
    /// @return string: Das generierte Passwort 
    public static string GeneratePassword(int length)
    {
      // Check given argument
      if (length < 1) throw new ArgumentException("Password must contain at least one character!", nameof(length));

      List<string> password = new List<string>();

      byte[] randomNumber = new byte[1];

      // Rechnen, wie viele Nummer in diesem Passwort sein darf
      int maximalNumberInPassword = (int)(length / 16 + 0.5);
      // Speichert, wie viele Nummer schon in dem Passwort zugefuegt sind
      int numberInPasswordCount = 0;
      // Zufallsnummer, die entscheidet, ob eine Nummer zugefuegt werden soll, sofern die anderen Bedingungen erfuellt sind
      byte[] canCreateNumber = new byte[1];


      while (password.GetStringLength() < length)
      {
        // Generiert eine aus der kryptographischen Sicht gesehen sichere Zufallsnummer
        // Also eine Zufallsnummer, die hier als reale Zufallsnummer gesehen werden kann
        rngCsp.GetBytes(randomNumber);
        rngCsp.GetBytes(canCreateNumber);

        // Test ob hier eine Nummer sein koennte
        if ((canCreateNumber[0] % 7 == 1) && maximalNumberInPassword > numberInPasswordCount && password.IsNumberFollowable())
        {
          // Regel 1: Nummer duerfen maximal 99 sein und 0 darf nicht vor irgendeiner beliebigen Nummer stehen
          password.Add((randomNumber[0] % 100).ToString());
          numberInPasswordCount++;
        }

        // Zufallsnummer mod Laenge der Moeglichkeit, so beschraenkt man den Bereich der generierten Zufallsnummer
        string stringToAdd = password.GetFollowableChars()[randomNumber[0] % password.GetFollowableChars().Length];

        // Falls es dann zu lange waere, fang direkt die naechste Schleife an
        if (password.GetStringLength() + stringToAdd.Length > length) continue;
        else if (password.GetStringLength() + stringToAdd.Length == length)
        {
          if (_clusters.Except(_endClusters).Contains(stringToAdd)) continue;
          if (_unendableChars.Contains((password + stringToAdd).Substring(length - 1, 1))) continue;
        }

        // Fuegt das neue Teil des Passworts hinzu
        password.Add(stringToAdd);
      }

      // Am Ende gibt man das generierte Passwort zurueck
      return password.ListToString().Substring(0, length);
    }


    /// Extension Methode, die die Laenge aller string dieser List<string> zurueckgibt
    /// @param List<string> sL
    /// @return int
    private static int GetStringLength(this List<string> sL)
    {
      int length = 0;
      foreach (string s in sL)
      {
        length += s.Length;
      }
      return length;
    }


    /// Extension Methode, die eine List<string> als eine gasamte string zurueckgibt
    /// @param List<string> sL: Eine Liste von string
    /// @return string
    private static string ListToString(this List<string> sL)
    {
      string str = "";
      foreach (string s in sL)
      {
        str += s;
      }
      return str;
    }


    /// Extension Methode, die ein Feld zurueckgibt, in dem alle moegliche Konbination von Buchstaben steht
    /// @param string s: Das vorher bereits generierte Teil-Passwort in Form von List<string>
    /// @return string[]: Ein Feld, das alle moegliche Konbinationen von Buchstaben beinhaltet
    private static string[] GetFollowableChars(this List<string> sL)
    {
      if (sL.Count == 0) return _startClusters.Union(_konsonanten).ToArray();
      if (Char.IsNumber(sL.Last().ToCharArray()[0])) return _vocale.Union(_startClusters).Union(_konsonanten).ToArray();
      if (_konsonanten.Union(_clusters).Contains(sL.Last())) return _vocale;
      if (sL.Last().Length > 1 && _vocale.Contains(sL.Last())) return _konsonanten.Union(_clusters).ToArray();
      switch (sL.Last().ToCharArray()[sL.Last().Length - 1])
      {
        case 'a':
          return ((new string[] { "u", "i" }).Union(_konsonanten).Union(_clusters)).ToArray();
        case 'o':
          return ((new string[] { "u" }).Union(_konsonanten).Union(_clusters)).ToArray();
        case 'e':
          return ((new string[] { "u", "i" }).Union(_konsonanten).Union(_clusters)).ToArray();
        case 'i':
          return _konsonanten.Union(_clusters).ToArray();
        case 'u':
          return ((new string[] { "i" }).Union(_konsonanten).Union(_clusters)).ToArray();
        case 'y':
          return _konsonanten.Union(_clusters).ToArray();
        default:
          System.Diagnostics.Debug.WriteLine(sL.ListToString());
          throw new ArgumentException("The given character is not implemented!", nameof(sL));
      }
    }

    /// Extension Methode, die zeight, ob jetzt eine Nummer kommen darf
    /// @param List<string> sL
    /// @return bool
    private static bool IsNumberFollowable(this List<string> sL)
    {
      if (sL.Count == 0) return true;
      if (sL.Count < 2) return false;
      bool isVK = _konsonanten.Union(_clusters).Contains(sL.Last()) && _vocale.Contains(sL[^2]);
      bool isKV = _vocale.Contains(sL.Last()) && _konsonanten.Union(_clusters).Contains(sL[^2]);
      bool isEndWithIllegalChar = _unendableChars.Contains(sL.Last()[^1].ToString());
      return sL.Count > 1 && (isVK || isKV) && !isEndWithIllegalChar;
    }
  }
}