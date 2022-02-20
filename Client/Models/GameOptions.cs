using System.ComponentModel.DataAnnotations;

namespace BlazorApp.Client.Models
{

public class GameOptions
{

		public bool ShowOptions{ get; set; } = false;
		public string? BeginsWith { get; set;}= null;

		[Range(1, 40, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
		public int? MaximumWordLength { get; set;} = 45;
		public string? APIKey { get; set; } = null;
		public bool IncludeSynonymsInstead { get; set; }= false;
	}
}