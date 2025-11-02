using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIAgentRAG;

public enum TypeOfQuestion
{
    MovieGenreRanking,
    MovieGenreSearch,
    GenericMovieQuestion
}
public class IntentResponse
{
    public TypeOfQuestion TypeOfQuestion { get; set; }
    public string Genre { get; set; } = string.Empty;
    public int NumberOfResults { get; set; }
}