using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentApp.Models;

public class Movie
{
    public string Title { get; set; }
    public string Director { get; set; }
    public int ReleaseYear { get; set; }
    public double Rating { get; set; }
    public bool IsAvailableOnStreaming { get; set; }
    public List<string> Tags { get; set; }
    public string MusicComposer { get; set; }
    public Genre Genre { get; set; }
}

public enum Genre
{
    Action,
    Comedy,
    Drama,
    Horror,
    SciFi,
    Romance,
    Documentary,
    Thriller,
    Animation,
    Fantasy
}

public class MovieResult
{
    public List<Movie> Movies { get; set; } = new();
}
