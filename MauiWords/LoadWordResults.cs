using MauiWords.Models;

namespace MauiWords
{
    public class LoadWordResults
    {
        public string Result { get; set; } = null;
        public string Message { get; set; } = null;
        public bool ShowWord { get; set; } = true;
        public int LettersToShow { get; set; } = 1;

        public List<WordResult> WordResults { get; set; } = new List<WordResult>();
    }
}