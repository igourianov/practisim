using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;

class Program
{
	static void Main(string[] args)
	{
		if (args.Length < 1)
		{
			Console.WriteLine("Usage: stg_normalize <inputfile> | --stdio");
			return;
		}

		if (args[0] == "--stdio")
		{
			Console.OutputEncoding = Encoding.UTF8;
			using (var output = Console.OpenStandardOutput())
			using (var input = Console.OpenStandardInput())
			using (var reader = new StreamReader(input, true))
				WriteSortedJson(reader.ReadToEnd(), output);
			return;
		}

		string inputFile = args[0];
		if (!Path.IsPathRooted(inputFile))
		{
			inputFile = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, inputFile));
		}
		if (!File.Exists(inputFile))
		{
			Console.WriteLine($"File not found: {inputFile}");
			return;
		}

		string outputFile = Path.Combine(Path.GetDirectoryName(inputFile) ?? "", Path.GetFileNameWithoutExtension(inputFile) + "_utf8.json");

		using (var outputStream = File.Open(outputFile, FileMode.Create, FileAccess.Write))
			WriteSortedJson(File.ReadAllText(inputFile), outputStream);

		File.Move(outputFile, inputFile, true);
	}

	static void WriteSortedJson(string json, Stream output)
	{
		using (var doc = JsonDocument.Parse(json))
		using (var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true, IndentCharacter = '\t', IndentSize = 1, NewLine = "\n" }))
		{
			writer.WriteStartObject();
			foreach (var prop in doc.RootElement.EnumerateObject())
			{
				if (prop.NameEquals("propList"))
				{
					writer.WritePropertyName("propList");
					writer.WriteStartArray();
					var sorted = prop.Value.EnumerateArray().OrderBy(e => e.GetProperty("uniqueID").GetInt32());
					foreach (var item in sorted)
						item.WriteTo(writer);
					writer.WriteEndArray();
				}
				else
				{
					prop.WriteTo(writer);
				}
			}
			writer.WriteEndObject();
		}

	}
}
