namespace BlazorApp.Client.Pages
{
    public class AILearningSession
    {
        public GameMode Mode { get; set; }
        public DifficultyLevel Difficulty { get; set; }
        public List<string> WordsLearned { get; set; } = new();
        public int Score { get; set; }
        public DateTime StartTime { get; set; }
    }
}
