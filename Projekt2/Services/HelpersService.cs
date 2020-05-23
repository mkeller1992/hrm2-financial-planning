using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Services
{
    public class HelpersService
    {
        public string FormatDecimal(decimal? number) {

            if (number == null) 
            {
                return "0";
            }
            return $"{number:n0}";
        }

        public string FormatPercentage(decimal? perc)
        {
            if (perc == null)
            {
                return "-";
            }
            var roundedPercentage = GetRoundedValue(perc.Value);
            var prefix = roundedPercentage >= 0 ? "+" : "";
            return $"{prefix}{roundedPercentage}%";
        }

        public string GetColorIfPositiveFav(decimal? n, int roundValueToNumberOfDigits = -1)
        {
            decimal number = n == null ? 0 : roundValueToNumberOfDigits == - 1 ? n.Value : GetRoundedValue(n, roundValueToNumberOfDigits).Value;
            if (number == 0)
            {
                return "";
            }
            return number > 0 ? "green" : "red";
        }

        public string GetColorIfNegativeFav(decimal? n, int roundValueToNumberOfDigits = -1)
        {
            decimal number = n == null ? 0 : roundValueToNumberOfDigits == -1 ? n.Value : GetRoundedValue(n, roundValueToNumberOfDigits).Value;
            if (number == 0)
            {
                return "";
            }
            return number < 0 ? "green" : "red";
        }

        public decimal? GetRoundedValue(decimal? dec, int decimalPlaces = 0)
        {
            if (dec == null)
            {
                return null;
            }
            return Math.Round(dec.Value, decimalPlaces, MidpointRounding.AwayFromZero);
        }

        public bool IsPositive(decimal? number) {
            return number == null || number >= 0;        
        }

        public decimal? GetPercentageChange(decimal? firstVal, decimal? secondVal)
        {
            // if first and second value are == 0 or == null
            if ((firstVal == 0 || firstVal == null) &&
                (secondVal == 0 || secondVal == null))
            {
                return 0;
            }
            // if only first value is > 0:
            if (secondVal == 0 || secondVal == null)
            {
                return -100;
            }
            // if only second value is > 0:
            if (firstVal == 0 || firstVal == null)
            {
                return null;
            }
            // if first and second value are > 0
            var tempRes = decimal.Divide(secondVal.Value - firstVal.Value, Math.Abs(firstVal.Value));
            return decimal.Multiply(tempRes, 100);
        }


        public string GetDegreeOfRotation(decimal? percentChangeExact, bool isFirstYearOfTimeline)
        {
            if (percentChangeExact == 0)
            {
                return "90";
            }
            else if (percentChangeExact > 0 ||
                    (percentChangeExact == null && isFirstYearOfTimeline == false))
            {
                return "45";
            }
            else if (percentChangeExact < 0)
            {
                return "135";
            }
            return null;
        }

    }
}
