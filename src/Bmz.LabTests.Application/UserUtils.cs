namespace Bmz.LabTests.Application;

public static partial class UserUtils
{
    public static string Transliterate(string text)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["а"] = "a", ["б"] = "b", ["в"] = "v", ["г"] = "g", ["д"] = "d", ["е"] = "e", ["ё"] = "yo",
            ["ж"] = "zh", ["з"] = "z", ["и"] = "i", ["й"] = "j", ["к"] = "k", ["л"] = "l", ["м"] = "m",
            ["н"] = "n", ["о"] = "o", ["п"] = "p", ["р"] = "r", ["с"] = "s", ["т"] = "t", ["у"] = "u",
            ["ф"] = "f", ["х"] = "kh", ["ц"] = "ts", ["ч"] = "ch", ["ш"] = "sh", ["щ"] = "shch",
            ["ъ"] = "", ["ы"] = "y", ["ь"] = "", ["э"] = "e", ["ю"] = "yu", ["я"] = "ya",
            ["А"] = "A", ["Б"] = "B", ["В"] = "V", ["Г"] = "G", ["Д"] = "D", ["Е"] = "E", ["Ё"] = "Yo",
            ["Ж"] = "Zh", ["З"] = "Z", ["И"] = "I", ["Й"] = "J", ["К"] = "K", ["Л"] = "L", ["М"] = "M",
            ["Н"] = "N", ["О"] = "O", ["П"] = "P", ["Р"] = "R", ["С"] = "S", ["Т"] = "T", ["У"] = "U",
            ["Ф"] = "F", ["Х"] = "Kh", ["Ц"] = "Ts", ["Ч"] = "Ch", ["Ш"] = "Sh", ["Щ"] = "Shch",
            ["Ъ"] = "", ["Ы"] = "Y", ["Ь"] = "", ["Э"] = "E", ["Ю"] = "Yu", ["Я"] = "Ya"
        };

        var transliterated = string.Concat(text.Select(c => map.TryGetValue(c.ToString(), out var replacement) ? replacement : c.ToString()));
        return System.Text.RegularExpressions.Regex.Replace(transliterated.Replace(" ", "."), "\\s+", "");
    }

    public static string GeneratePassword(int length = 10)
    {
        const string charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";
        var random = Random.Shared;
        var password = new char[length];
        for (var i = 0; i < length; i++)
        {
            password[i] = charset[random.Next(charset.Length)];
        }
        return new string(password);
    }
}
