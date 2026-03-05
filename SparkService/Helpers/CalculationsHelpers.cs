using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.Helpers
{
    public static class CalculationsHelpers
    {
        public static int CalculateAge(DateTime dateOfBirth)
        {
            int age = 0;
            age = DateTime.Now.Year - dateOfBirth.Year;
            if (DateTime.Now.DayOfYear < dateOfBirth.DayOfYear)
                age = age - 1;

            return age;
        }

        public static int ToInches(string height)
        {
            if (height.Contains("'"))
            {
                string sfeet = height.Split("'")[0];
                string sinches = height.Split("'")[1];
                int feet, inches;
                if (int.TryParse(sfeet, out feet) && int.TryParse(sinches, out inches))
                {
                    return feet * 12 + inches;
                }
                return 0;
            }
            else
            {
                int output;
                if (int.TryParse(height, out output))
                {
                    return output * 12;
                }
                return 0;
            }
        }
    }
}
