using CSharpFunctionalExtensions;
using System;
using System.Text.RegularExpressions;

namespace A0Utils.Wpf.Helpers
{
    public static class ParseHelpers
    {
        public static Result<DateTime> FindA0LicenseExp(string data)
        {
            Match match = Regex.Match(data, @"\[А0\]\s*Поддержка до:\s*(\d{2}\.\d{2}\.\d{4})");

            if (match.Success)
            {
                if (DateTime.TryParse(match.Groups[1].Value, out DateTime result))
                {
                    return result;
                }
                else
                {
                    return Result.Failure<DateTime>("Failed to parse date.");
                }
            }
            else
            {
                return default;
            }
        }

        public static Result<DateTime> FindPIRLicenseExp(string data)
        {
            Match match = Regex.Match(data, @"\[ПИР\]\s*Поддержка до:\s*(\d{2}\.\d{2}\.\d{4})");

            if (match.Success)
            {
                if (DateTime.TryParse(match.Groups[1].Value, out DateTime result))
                {
                    return result;
                }
                else
                {
                    return Result.Failure<DateTime>("Failed to parse date.");
                }
            }
            else
            {
                return default;
            }
        }

        public static Result<string> FindLicenseType(string[] data)
        {
            string pattern = @"Sentinel(?:\s+HL)?";
            Regex regex = new Regex(pattern, RegexOptions.Compiled);

            foreach (string line in data)
            {
                Match match = regex.Match(line);
                if (match.Success)
                {
                        return Result.Success(match.Value);
                }
            }

            return Result.Failure<string>("Failed to parse license type.");
        }

        public static string FindNsi(string[] data, string template)
        {
            string pattern = @"^\s*" + Regex.Escape(template);
            Regex regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Multiline);

            foreach (string line in data)
            {
                Match match = regex.Match(line);
                if (match.Success)
                {
                    return line;
                }
            }

            return string.Empty;
        }

        public static string FindPrices(string[] data, string template)
        {
            string pattern = $"Справочник цен\\s+\"({Regex.Escape(template)})\"";
            Regex regex = new Regex(pattern, RegexOptions.Compiled);

            foreach (string line in data)
            {
                Match match = regex.Match(line);
                if (match.Success)
                {
                    // Return only the captured group (i.e. the text inside the quotes)
                    return match.Groups[1].Value;
                }
            }
            return string.Empty;
        }
    }
}
