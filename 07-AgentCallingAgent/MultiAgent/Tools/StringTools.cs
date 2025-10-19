using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiAgent.Tools;

public static class StringTools
{
    public static string Reverse(string input)
    {
        return new string(input.Reverse().ToArray());
    }

    public static string UpperCase(string input)
    {
        return input.ToUpper();
    }

    public static string LowerCase(string input) {
        return input.ToLower(); 
    }
}
