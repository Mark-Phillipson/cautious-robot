using System;

namespace BlazorApp.Client.Models
{
	public class WordResult
	{
		public Guid RandomOrder { get; set; } = Guid.NewGuid();
		
		public string? word { get; }
		public Result[]? results { get;  }
		public Syllables? syllables { get; set; }
		//The pronunciation contains characters that mess up the Serialising of values in JSON to an object
		// public Pronunciation? pronunciation { get; set; }
		public float frequency { get; set; }
	}

	public class Syllables
	{
		public int count { get; set; }
		public string[]? list { get; set; }
	}

	public class Pronunciation
	{
		public string? all { get; set; }
	}

	public class Result
	{
		public string? definition { get; }
		public string? partOfSpeech { get;  }
		public string[]? synonyms { get;  }
		public string[]? typeOf { get; set; }
		public string[]? hasTypes { get; set; }
		public string[]? derivation { get; set; }
		public string[]? examples { get; set; }
	}

}