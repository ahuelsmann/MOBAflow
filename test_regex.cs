using System;
using System.Text.RegularExpressions;
class Program {
    static void Main() {
        string svg = ""<svg stroke=\""currentColor\""></svg>"";
        string color = ""#FFFFFF"";
        string colorValuePattern = @""currentColor|black|#000|#000000|rgb\(0,\s*0,\s*0\)"";
        
        var result = Regex.Replace(
            svg,
            $@""(?<name>stroke|fill)=[\""'](?<value>{colorValuePattern})[\""']"",
            match => $""{match.Groups[\""name\""].Value}=\""{color}\"""",
            RegexOptions.IgnoreCase);
            
        Console.WriteLine(result);
    }
}
