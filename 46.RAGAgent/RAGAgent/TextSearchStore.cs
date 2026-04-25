using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace RAGAgent;

public class TextSearchStore
{
    private readonly VectorStoreCollection<string, TextSearchDocument> _collection;
    public TextSearchStore(InMemoryVectorStore vectorStore, string collectionName, int dimensions)
    {
        _collection = vectorStore.GetCollection<string, TextSearchDocument>(collectionName);
    }

    public async Task UpsertDocumentAsync(IEnumerable<TextSearchDocument> documents)
    {
        await _collection.EnsureCollectionExistsAsync();

        foreach(var doc in documents)
        {
            var record = new TextSearchDocument
            {
                SourceId = doc.SourceId,
                SourceName = doc.SourceName,
                SourceLink = doc.SourceLink,
                Text = doc.Text,
                Embedding = doc.Text
            };
            await _collection.UpsertAsync(record);
        }
    }
    public async Task<IEnumerable<TextSearchDocument>> SearchAsync(string query, 
                    int topK, 
                    CancellationToken cancellationToken = default)
    {
        var results = _collection.SearchAsync(query, topK,cancellationToken:cancellationToken);

        var documents = new List<TextSearchDocument>();
        await foreach(var result in results)
        {
            documents.Add(new TextSearchDocument
            {
                SourceId = result.Record.SourceId,
                SourceName = result.Record.SourceName,
                SourceLink = result.Record.SourceLink,
                Text = result.Record.Text
            });
        }

        return documents;
    }

    public static IEnumerable<TextSearchDocument> GetSampleDocuments()
    {
        return new List<TextSearchDocument>
        {
            new()
            {
                SourceId = "tip-001",
                SourceName = "Speed Reading Techniques",
                SourceLink = "https://www.booktips.com/speed-reading",
                Text = """
                    Speed reading involves training your eyes to move faster across the page
                    while maintaining comprehension. Key techniques include:
                    1. Minimize subvocalization (the habit of silently pronouncing each word).
                    2. Use a pointer (finger or pen) to guide your eyes and reduce regression.
                    3. Expand your peripheral vision to take in multiple words per fixation.
                    4. Practice with a timer to gradually increase your words-per-minute rate.
                    Beginners typically read 200–250 WPM; with practice, 400–600 WPM is achievable.
                """
            },
            new()
            {
                SourceId= "tip-002",
                SourceName = "Active Reading & Annotation Guide",
                SourceLink = "https://www.booktips.com/active-reading",
                Text ="""
                Active reading means engaging with the text rather than passively scanning it.
                   Effective methods:
                   • Highlight sparingly — only the single most important idea per paragraph.
                   • Write margin notes in your own words to strengthen retention.
                   • Ask questions as you read: 'Why does the author claim this?' 'Do I agree?'
                   • Summarize each chapter in 2–3 sentences before moving on.
                   • Review your annotations within 24 hours to move ideas into long-term memory.
                """
            },
           new()
            {
                SourceId   = "tip-003",
                SourceName = "Building a Reading Habit",
                SourceLink = "https://www.booktips.com/reading-habit",
                Text       = """
                    Consistent reading habits are built through small, daily commitments.
                    Tips to stay consistent:
                    • Set a fixed reading time — morning coffee, lunch break, or before bed.
                    • Start with just 10–15 minutes per day; consistency beats duration.
                    • Keep a book on your nightstand and your phone in another room.
                    • Track progress with apps like Goodreads to maintain motivation.
                    • Join a book club for social accountability.
                    • Allow yourself to abandon books you don't enjoy — there are no rules.
                    """
            },

            // ── Genre Guides ────────────────────────────────────────────
            new()
            {
                SourceId   = "genre-001",
                SourceName = "Beginner's Guide to Science Fiction",
                SourceLink = "https://www.booktips.com/genres/sci-fi",
                Text       = """
                    Science fiction explores the impact of imagined technology, space, or societal
                    change on humanity. Great entry points for new readers:
                    • 'The Martian' by Andy Weir — fast-paced, humorous, near-future survival story.
                    • 'Ender's Game' by Orson Scott Card — military strategy meets coming-of-age.
                    • 'Project Hail Mary' by Andy Weir — optimistic, puzzle-driven space adventure.
                    • 'Dune' by Frank Herbert — epic world-building; start here for classic SF.
                    Tip: If dense world-building feels overwhelming, begin with Weir before Herbert.
                    """
            },
            new()
            {
                SourceId   = "genre-002",
                SourceName = "Beginner's Guide to Literary Fiction",
                SourceLink = "https://www.booktips.com/genres/literary-fiction",
                Text       = """
                    Literary fiction prioritises character depth, prose style, and thematic richness
                    over plot-driven momentum. Accessible starting points:
                    • 'The Kite Runner' by Khaled Hosseini — emotional, character-driven redemption arc.
                    • 'A Man Called Ove' by Fredrik Backman — warm, gently humorous, deeply human.
                    • 'Where the Crawdads Sing' by Delia Owens — lyrical prose with a mystery thread.
                    • 'Educated' by Tara Westover — memoir that reads like literary fiction.
                    Literary fiction rewards slow, attentive reading — resist the urge to skim.
                    """
            },
            new()
            {
                SourceId   = "genre-003",
                SourceName = "Beginner's Guide to Mystery & Thriller",
                SourceLink = "https://www.booktips.com/genres/mystery-thriller",
                Text       = """
                    Mystery and thriller novels are built on suspense, clues, and revelation.
                    Sub-genres to explore:
                    • Cozy Mystery: low violence, amateur sleuth — try Agatha Christie's 'And Then There Were None'.
                    • Psychological Thriller: unreliable narrators — try 'Gone Girl' by Gillian Flynn.
                    • Police Procedural: realistic detective work — try 'The Girl with the Dragon Tattoo'.
                    • Legal Thriller: courtroom drama — try any John Grisham novel.
                    Reading tip: Keep a character list in the front cover to track suspects efficiently.
                    """
            },

            // ── Specific Book Summaries / Insights ──────────────────────
            new()
            {
                SourceId   = "book-001",
                SourceName = "Atomic Habits — Key Insights",
                SourceLink = "https://www.booktips.com/books/atomic-habits",
                Text       = """
                    'Atomic Habits' by James Clear (2018) argues that tiny, consistent changes
                    compound into remarkable results. Core concepts:
                    1. The 1% Rule: improving 1% daily yields a 37× improvement in a year.
                    2. Habit Loop: Cue → Craving → Response → Reward (building on Duhigg's model).
                    3. Identity-based habits: instead of 'I want to read more', say 'I am a reader'.
                    4. Environment design: make good habits obvious and bad habits invisible.
                    5. The Two-Minute Rule: scale any new habit down to a two-minute version to start.
                    Best for: readers interested in productivity, self-improvement, and behavioural psychology.
                    """
            },
            new()
            {
                SourceId   = "book-002",
                SourceName = "Sapiens — Key Insights",
                SourceLink = "https://www.booktips.com/books/sapiens",
                Text       = """
                    'Sapiens: A Brief History of Humankind' by Yuval Noah Harari (2011) traces
                    the story of Homo sapiens from Stone Age foragers to Silicon Age rulers.
                    Key themes:
                    1. The Cognitive Revolution (~70,000 BCE): language and shared myths enabled
                       large-scale human cooperation — the foundation of civilisation.
                    2. The Agricultural Revolution: Harari provocatively calls it 'history's biggest
                       fraud', arguing it made most humans work harder for a poorer diet.
                    3. Shared fictions: money, nations, and corporations only exist because we
                       collectively believe in them.
                    4. The future: Harari warns that Homo sapiens may soon engineer its own successor.
                    Best for: readers curious about history, anthropology, and big-picture thinking.
                    """
            },
            new()
            {
                SourceId   = "book-003",
                SourceName = "Deep Work — Key Insights",
                SourceLink = "https://www.booktips.com/books/deep-work",
                Text       = """
                    'Deep Work' by Cal Newport (2016) makes the case that the ability to focus
                    without distraction is becoming both increasingly rare and increasingly valuable.
                    Core ideas:
                    1. Deep Work vs Shallow Work: cognitively demanding tasks vs logistical tasks
                       (email, meetings). Most knowledge workers spend too much time on the latter.
                    2. Four philosophies of deep work: Monastic, Bimodal, Rhythmic, Journalistic.
                    3. Embrace boredom: resist the urge to check your phone the moment you're idle —
                       this trains your attention span.
                    4. Quit social media ruthlessly: apply a craftsman's mindset — only use a tool
                       if its benefits substantially outweigh its negatives.
                    5. Drain the shallows: schedule every minute of your workday to protect deep blocks.
                    Best for: knowledge workers, students, writers, and programmers.
                    """
            },
        };
    }
    
}
