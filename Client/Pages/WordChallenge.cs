namespace BlazorApp.Client.Pages
{
    public class WordChallenge
    {
        public ChallengeType Type { get; set; }
        public string TargetWord { get; set; } = "";
        public string Question { get; set; } = "";
        public List<string>? Options { get; set; }
        public string? CorrectAnswer { get; set; }
        public bool IsOpenEnded { get; set; }
        public string? Context { get; set; }
    }
}
