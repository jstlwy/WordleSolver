using System.CommandLine;
using System.Text.RegularExpressions;

class WordleSolver
{
	static List<string> GetPossibleWords(string currentWord,
		int targetWordLength, List<char> validLetters,
		char[] knownPositions, HashSet<string> dictionary)
	{
		List<string> possibleWords = new List<string>();

		int currentWordLength = currentWord.Length;
		if (currentWordLength < targetWordLength)
		{
			// Keep trying to append letters until a valid word is found.
			// First, check if the current position is known.
			char nextLetter = knownPositions[currentWordLength];
			if (nextLetter != '-')
			{
				List<string> words = GetPossibleWords(
					currentWord + nextLetter,
					targetWordLength,
					validLetters,
					knownPositions,
					dictionary
				);
				possibleWords.AddRange(words);
			}
			else
			{
				foreach (char c in validLetters)
				{
					List<string> words = GetPossibleWords(
						currentWord + c,
						targetWordLength,
						validLetters,
						knownPositions,
						dictionary
					);
					possibleWords.AddRange(words);
				}
			}
		}
		else if (dictionary.Contains(currentWord))
		{
			// A valid word has been found
			possibleWords.Add(currentWord);
		}

		return possibleWords;
	}

	static void SolveWordle(string wordFileName, int wordLength,
		List<char> validLetters, char[] knownPositions,
		List<char> requiredLetters, bool shouldSaveToTxt)
	{
		// Build the dictionary
		HashSet<string> dictionary = new HashSet<string>();
		string[] lines = System.IO.File.ReadAllLines(wordFileName);
		foreach (string line in lines)
		{
			dictionary.Add(line.ToLower());
		}
		if (dictionary.Count == 0)
		{
			Console.WriteLine("Failed to read in words from the text file.");
			return;
		}

		// Find all possible words
		List<string> possibleWords = GetPossibleWords(
			"",
			wordLength,
			validLetters,
			knownPositions,
			dictionary
		);

		// Exclude words without letters that must be included
		if (requiredLetters.Count > 0)
		{
			List<string> filteredWords = possibleWords.FindAll(word =>
			{
				return requiredLetters.All(letter => word.Contains(letter));
			});
			possibleWords = filteredWords;
		}

		if (shouldSaveToTxt)
		{
			File.WriteAllLines("solutions.txt", possibleWords);
		}
		else
		{
			foreach (string word in possibleWords)
			{
				Console.WriteLine(word);
			}
		}
	}

	static void Main(string[] args)
	{
		var debugOption = new Option<bool>(
			name: "--debug",
			description: "Print intermediate results to ensure the application is running correctly.",
			getDefaultValue: () => false
		);
		var wordFileOption = new Option<string>(
			name: "--dict",
			description: "User-specified text file from which to read in English-language words.",
			getDefaultValue: () => "freebsd_words.txt"
		);
		var wordLengthOption = new Option<int>(
			name: "--length",
			description: "The length of the word to be found.",
			getDefaultValue: () => 5
		);
		var excludeOption = new Option<string>(
			name: "--exclude",
			description: "List of letters that are known to not be in the word. " +
				"Separate multiple with a comma. " +
				"For example: -exclude m,s,e",
			getDefaultValue: () => ""
		);
		var includeOption = new Option<string>(
			name: "--include",
			description: "List of letters that are known to be in the word but whose " +
				"positions are unknown. Separate multiple with a comma. " +
				"For example: -include m,s,e",
			getDefaultValue: () => ""
		);
		var knownOption = new Option<string>(
			name: "--known",
			description: "List of known positions and letters. " +
				"Separate multiple with a comma. " +
				"For example: -known 1m,2o,3u",
			getDefaultValue: () => ""
		);
		var saveToTxtOption = new Option<bool>(
			name: "--save",
			description: "Save the potential solutions in a .txt file.",
			getDefaultValue: () => false
		);

		var rootCommand = new RootCommand("Command-line app for solving Wordle problems.");
		rootCommand.Add(debugOption);
		rootCommand.Add(wordFileOption);
		rootCommand.Add(wordLengthOption);
		rootCommand.Add(excludeOption);
		rootCommand.Add(includeOption);
		rootCommand.Add(knownOption);
		rootCommand.Add(saveToTxtOption);

		rootCommand.SetHandler(
			(debugOptionValue, wordFileOptionValue, wordLengthOptionValue, excludeOptionValue,
			includeOptionValue, knownOptionValue, saveToTxtOptionValue) =>
			{
				// Parse argument for valid letters
				// First, create set of letters to exclude
				HashSet<char> excludedLetterSet = new HashSet<char>();
				if (excludeOptionValue.Length > 0)
				{
					string[] excludeArgs = excludeOptionValue.ToLower().Split(",").Where(arg => arg.Length == 1).ToArray();
					foreach (string arg in excludeArgs)
					{
						char c = arg[0];
						if (Char.IsAsciiLetterLower(c))
							excludedLetterSet.Add(c);
					}
				}
				
				// Now, from the map of letters to exclude,
				// create a list of valid letters
				List<char> validLetters = new List<char>();
				for (char c = 'a'; c <= 'z'; c++)
				{
					if (!excludedLetterSet.Contains(c))
						validLetters.Add(c);
				}

				// Parse argument for required letters.
				// HashSet used to ensure no letters are repeated.
				HashSet<char> requiredLetterSet = new HashSet<char>();
				if (includeOptionValue.Length > 0)
				{
					string[] includeArgs = includeOptionValue.ToLower().Split(",").Where(arg => arg.Length == 1).ToArray();
					foreach (string arg in includeArgs)
					{
						char c = arg[0];
						if (Char.IsAsciiLetterLower(c))
							requiredLetterSet.Add(c);
					}
				}
				List<char> requiredLetters = requiredLetterSet.ToList();

				// Parse argument for known positions
				char[] knownPositions = new char[wordLengthOptionValue];
				for (int i = 0; i < knownPositions.Length; i++)
				{
					knownPositions[i] = '-';
				}
				if (knownOptionValue.Length > 0)
				{
					Regex numAlpha = new Regex(@"^\d[a-z]$");
					string[] knownArgs = knownOptionValue.ToLower().Split(",").Where(arg => numAlpha.IsMatch(arg)).ToArray();
					foreach (string arg in knownArgs)
					{
						int position = int.Parse(arg.Substring(0,1)) - 1;
						if (position < wordLengthOptionValue)
							knownPositions[position] = arg[1];
					}
				}

				if (debugOptionValue)
				{
					Console.WriteLine("Letters to exclude:");
					foreach (char c in excludedLetterSet)
					{
						Console.WriteLine(c);
					}

					Console.WriteLine("\nKnown letters:");
					for (int i = 0; i < knownPositions.Length; i++)
					{
						char c = knownPositions[i];
						if (c != '-')
							Console.WriteLine($"{i + 1} = {c}");
					}

					Console.WriteLine("\nValid letters:");
					foreach (char c in validLetters)
					{
						Console.WriteLine(c);
					}

					Console.WriteLine("\nRequired letters:");
					foreach (char c in requiredLetters)
					{
						Console.WriteLine(c);
					}

					Console.WriteLine();
				}

				SolveWordle(
					wordFileOptionValue,
					wordLengthOptionValue,
					validLetters,
					knownPositions,
					requiredLetters,
					saveToTxtOptionValue
				);
			},
			debugOption, wordFileOption, wordLengthOption, excludeOption, 
			includeOption, knownOption, saveToTxtOption);

		rootCommand.Invoke(args);
	}
}

