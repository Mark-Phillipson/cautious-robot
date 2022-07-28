using System;

namespace BlazorApp.Client.Models
{
	public class WordResult
	{
		public Guid RandomOrder { get; set; } = Guid.NewGuid();
		
		public string? word { get; set; }
		public Result[]? results { get; set; }
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
		public string? definition { get; set; }
		public string? partOfSpeech { get; set; }
		public string[]? synonyms { get; set; }
		public string[]? typeOf { get; set; }
		public string[]? hasTypes { get; set; }
		public string[]? derivation { get; set; }
		public string[]? examples { get; set; }
	}

}