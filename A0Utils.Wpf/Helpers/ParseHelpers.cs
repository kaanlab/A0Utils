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
                return Result.Failure<DateTime>("Failed to parse date.");
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
                return Result.Failure<DateTime>("Failed to parse date.");
            }
        }
    }
}
