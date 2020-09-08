using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PasswordGenerator
{
  public static class PasswordGenerator
  {

    // Alle deutshen Konsonanten-Buchstaben
    private static readonly string[] _konsonanten = new string[] { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "q", "r", "s", "t", "v", "w", "x", "z" };

    // Konsonantencluster
    // Clusters wie "bb", "dd", "ll" usw. sind ausgeschlossen, da sie zur Verwirrung fuehren koennten
    // Clusters, die am Anfang stehen koennen:
    private static readonly string[] _startClusters = new string[] { "bl", "br", "ch", "dr", "dw", "fl", "fr", "gn", "gr", "kn", "kr", "pf", "pfl", "pfr", "pl", "pn", "pr", "ph", "phl", "phr", "sk", "skl", "skr", "sl", "sp", "sph", "spl", "spr", "st", "str", "sz", "sch", "schl", "schm", "schn", "schr", "schw", "tr", "zw", "ch", "gl", "kl", "sp", "sph", "sch", "scht", "schr", "tsch" };

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

      int maximalNumberInPassword = (int)(length / 8 + 0.5);
      int numberInPasswordCount = 0;
      byte[] createNumberLimit = new byte[1];
      rngCsp.GetBytes(createNumberLimit);

      List<string> password = new List<string>();

      byte[] randomNumber = new byte[1];

      while (password.GetStringLength() < length)
      {
        // Generiert eine aus der kryptographischen Sicht gesehen sichere Zufallsnummer
        // Also eine Zufallsnummer, die hier als reale Zufallsnummer gesehen werden kann
        rngCsp.GetBytes(randomNumber);

        // Test ob hier eine Nummer sein koennte
        if ((createNumberLimit[0] % 2 == 1) && maximalNumberInPassword > numberInPasswordCount)
        {
          if (password.GetStringLength() == 0 || !_konsonanten.Contains(password.ListToString().Substring(password.GetStringLength() - 1)))
          {
            if ((password.ListToString() + randomNumber[0].ToString()).Length <= length)
            {
              password.Add((randomNumber[0] % 100).ToString());
              rngCsp.GetBytes(createNumberLimit);
              numberInPasswordCount++;
            }
          }
        }

        // Zufallsnummer mod Laenge der Moeglichkeit, so beschraenkt man den Bereich der generierten Zufallsnummer
        string stringToAdd = password.ListToString().getFollowableChars()[randomNumber[0] % password.ListToString().getFollowableChars().Length];
        if (password.GetStringLength() + stringToAdd.Length > length)
        {
          continue;
        }
        else if (password.GetStringLength() + stringToAdd.Length == length)
        {
          if (_clusters.Except(_endClusters).Contains(stringToAdd)) continue;
          if (_unendableChars.Contains((password + stringToAdd).Substring(length - 1, 1))) continue;
        }
        password.Add(stringToAdd);
      }

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
    /// @param string s: Das vorher bereits generierte Teil-Passwort
    /// @return string[]: Ein Feld, das alle moegliche Konbinationen von Buchstaben beinhaltet
    private static string[] getFollowableChars(this string s)
    {
      if (s.Length == 0)
      {
        return (_vocale.Union(_konsonanten).Union(_startClusters)).ToArray();
      }
      else if (s.Length >= 2)
      {
        if (_vocale.Contains(s.Substring(s.Length - 2)) || (_vocale.Contains(s.Substring(s.Length - 1)) && _vocale.Contains(s.Substring(s.Length - 2)[0].ToString()))) return _konsonanten.Union(_clusters).ToArray();
      }

      char lastCharacter = s.Substring(s.Length - 1).ToCharArray()[0];
      if (Char.IsNumber(lastCharacter)) return (_vocale.Union(_konsonanten).Union(_startClusters)).ToArray();
      if (_konsonanten.Contains(lastCharacter.ToString())) return _vocale;
      switch (lastCharacter)
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
          System.Diagnostics.Debug.WriteLine(s);
          throw new ArgumentException("The given character is not implemented!", nameof(s));
      }
    }

    private static string[] GetFollowableChars(this List<string> sL) {

    }
  }
}