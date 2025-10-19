using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiAgent.Tools;

public static class NumericTools
{
    public static double Add(double a, double b)
    {
        return a + b;
    }
    public static double Subtract(double a, double b)
    {
        return a - b;
    }
    public static double Multiply(double a, double b)
    {
        return a * b;
    }
    public static double Divide(double a, double b)
    {
        if (b == 0)
        {
            throw new ArgumentException("Division by zero is not allowed.");
        }
        return a / b;
    }
    public static int RandomNumber(int min, int max)
    {
        Random rand = new Random();
        return rand.Next(min, max);
    }
    public static int AnswerToEveythingNumber()
    {
        return 42; // The answer to life, the universe and everything
    }
}
