using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgentApp.Models;

public class Movie
{
    public string Title { get; set; }
    public string Plot { get; set; }
    public decimal Rating { get; set; }
    public int Year { get; set; }

    internal string? GetTitleAndDetails()
    {
        throw new NotImplementedException();
    }
}
