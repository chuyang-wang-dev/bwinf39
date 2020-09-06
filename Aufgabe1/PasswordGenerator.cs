using System;
using System.Linq;
using System.Security.Cryptography;

namespace PasswordGenerator
{
  public static class PasswordGenerator
  {

    // Alle Konsonanten 
    private static readonly string[] _konsonanten = new string[] { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "q", "r", "s", "t", "v", "w", "x", "z" };
    // Konsonantencluster
    // Clusters wie "bb", "dd", "ll" usw. sind ausgeschlossen, da sie zur Verwirrung fuehren koennten
    // Clusters, die am Anfang stehen koennen:
    private static readonly string[] _startClusters = new string[] { "bl", "br", "ch", "dr", "dw", "fl", "fr", "gn", "gr", "kn", "kr", "pf", "pfl", "pfr", "pl", "pn", "pr", "ph", "phl", "phr", "sk", "skl", "skr", "sl", "sp", "sph", "spl", "spr", "st", "str", "sz", "sch", "schl", "schm", "schn", "schr", "schw", "tr", "zw", "ch", "gl", "kl", "sp", "sph", "sch", "scht", "schr", "tsch" };
    private static readonly string[] _endClusters = new string[] { "ch", "pf", "ph", "sk", "skl", "sl", "sp", "sph", "st", "sz", "sch", "bst", "bt", "ch", "chs", "cht", "dm", "dt", "ft", "gd", "gs", "gt", "kt", "ks", "lb", "lch", "ld", "lf", "lg", "lk", "lm", "ln", "lp", "ls", "lsch", "lt", "lz", "mb", "md", "mp", "mpf", "mph", "ms", "msch", "mt", "nd", "nf", "nft", "ng", "ngs", "ngst", "nk", "nkt", "ns", "nsch", "nst", "nt", "nx", "nz", "ps", "psch", "pst", "pt", "sk", "sp", "sph", "st", "sch", "ts", "tsch", "tz", "tzt", "xt" };
    // Alle Clusters
    private static readonly string[] _clusters = new string[] { "bl", "br", "ch", "chl", "dr", "dw", "fl", "fr", "gn", "gr", "kn", "kr", "pf", "pfl", "pfr", "pl", "pn", "pr", "ph", "phl", "phr", "sk", "skl", "skr", "sl", "sp", "sph", "spl", "spr", "st", "str", "sz", "sch", "schl", "schm", "schn", "schr", "schw", "tr", "wr", "zw", "bs", "bsch", "bst", "bt", "ch", "chs", "cht", "dm", "dt", "ft", "gd", "gl", "gs", "gt", "kt", "kl", "ks", "lb", "lch", "ld", "lf", "lg", "lk", "lm", "ln", "lp", "ls", "lsch", "lt", "lv", "lz", "mb", "md", "mn", "mp", "mpf", "mph", "mphl", "ms", "msch", "mt", "nd", "nf", "nft", "ng", "ngl", "ngs", "ngst", "nk", "nkt", "ns", "nsch", "nst", "nt", "nx", "nz", "ps", "psch", "pst", "pt", "sk", "sp", "sph", "st", "sch", "scht", "schr", "tm", "ts", "tsch", "tw", "tz", "tzt", "xt" };
    private static readonly string[] _unendableChars = new string[] { "r", "l", "j", "v" };

    private static readonly char[] _allChars = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

    // Alle deutsche Vocale + Zweilauten
    // Deutsche Umlaut werden hier nicht betracht
    private static readonly string[] _vocale = new string[] { "a", "o", "i", "e", "u", "y", "au", "ai", "eu", "ei", "ui" };

    private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();


    public static string GeneratePassword(int length)
    {
      // Check given argument
      if (length < 1) throw new ArgumentException("Password must contain at least one character!", nameof(length));

      int maximalNumberInPassword = (int)(length / 8 + 0.5);
      int numberInPasswordCount = 0;
      byte[] createNumberLimit = new byte[1];
      rngCsp.GetBytes(createNumberLimit);

      string password = "";
      byte[] randomNumber = new byte[1];

      while (password.Length < length)
      {
        // Generiert eine aus der kryptographischen Sicht gesehen sichere Zufallsnummer
        // Also eine Zufallsnummer, die hier als reale Zufallsnummer gesehen werden kann
        rngCsp.GetBytes(randomNumber);

        // Test ob hier eine Nummer sein koennte
        if (randomNumber[0] > createNumberLimit[0] && maximalNumberInPassword > numberInPasswordCount)
        {
          if (password.Length == 0 || !_konsonanten.Contains(password.Substring(password.Length - 1)))
          {
            if ((password + randomNumber[0].ToString()).Length <= length)
            {
              password += (randomNumber[0] % 100).ToString();
              rngCsp.GetBytes(createNumberLimit);
              numberInPasswordCount++;
            }
          }
        }

        // Zufallsnummer mod Laenge der Moeglichkeit, so beschraenkt man den Bereich der generierten Zufallsnummer
        string stringToAdd = password.getFollowableChars()[randomNumber[0] % password.getFollowableChars().Length];
        if (password.Length + stringToAdd.Length > length)
        {
          continue;
        }
        else if (password.Length + stringToAdd.Length == length)
        {
          if (_clusters.Except(_endClusters).Contains(stringToAdd)) continue;
          if (_unendableChars.Contains((password + stringToAdd).Substring(length - 1, 1))) continue;
        }
        password += stringToAdd;
      }

      return password.Substring(0, length);
    }


    /// @param length: Eine ganze Zahl, die besagt, wie lange das zu generierende Passwort sein sollte.
    /// @return string: Das generierte Passwort 
    public static string GeneratePassword1(int length)
    {
      // Check given argument
      if (length < 1) throw new ArgumentException("Password must contain at least one character!", nameof(length));

      char[] password = new char[length];
      byte[] randomNumber = new byte[1];
      rngCsp.GetBytes(randomNumber);
      password[0] = _allChars[randomNumber[0] % 26];
      for (int i = 0; i < length - 1; i++)
      {
        // Generiert eine aus der kryptographischen Sicht gesehen sichere Zufallsnummer
        // Also eine Zufallsnummer, die hier als reale Zufallsnummer gesehen werden kann
        rngCsp.GetBytes(randomNumber);
        // Zufallsnummer mod Laenge der Moeglichkeit, so beschraenkt man den Bereich der generierten Zufallsnummer
        password[i + 1] = password[i].getFollowableChars()[randomNumber[0] % password[i].getFollowableChars().Length];
      }

      return new string(password);
    }


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
          Console.WriteLine(s);
          throw new ArgumentException("The given character is not implemented!", nameof(s));
      }
    }

    private static char[] getFollowableChars(this char c)
    {
      switch (c)
      {
        case 'a':
          return new char[] { 'b', 'c', 'd', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'z' };
        case 'b':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y', };
        case 'c':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        case 'd':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        case 'e':
          return new char[] { 'b', 'c', 'd', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'z' };
        case 'f':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y', };
        case 'g':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y', };
        case 'h':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        case 'i':
          return new char[] { 'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'z' };
        case 'j':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        case 'k':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y', 't', };
        case 'l':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        case 'm':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        case 'n':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        case 'o':
          return new char[] { 'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'z' };
        case 'p':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y', 'r' };
        case 'q':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        case 'r':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        case 's':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y', };
        case 't':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        case 'u':
          return new char[] { 'b', 'c', 'd', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'z' };
        case 'v':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        case 'w':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        case 'x':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        case 'y':
          return new char[] { 'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'z' };
        case 'z':
          return new char[] { 'a', 'o', 'e', 'u', 'i', 'y' };
        default:
          Console.WriteLine(c);
          throw new ArgumentException("The given character is not implemented!", nameof(c));
      }
    }

  }
}