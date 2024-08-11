// Create a new dictionary of strings, with string keys.
Dictionary<string, string> dict = new Dictionary<string, string>();

// Adding elements to the Dictionary
dict.Add("A", "Apple");
dict.Add("B", "Banana");
dict.Add("C", "Cherry");

// Accessing elements in the Dictionary
string fruitA = dict["A"]; // fruitA will be "Apple"

// Checking if a key exists
bool hasB = dict.ContainsKey("B"); // hasB will be true

// Removing an element
dict.Remove("B");

// Iterating over a Dictionary
foreach (KeyValuePair<string, string> item in dict)
{
    Console.WriteLine("Key: {0}, Value: {1}", item.Key, item.Value);
}