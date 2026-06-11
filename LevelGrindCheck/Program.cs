using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

if (args is not [var arg])
{
	Console.WriteLine("Usage: lgcheck <levelid|path|--reload>");
	Console.WriteLine("--reload refreshes the level id cache.");
	return 1;
}

var cachePath = Path.Combine(Path.GetTempPath(), "lgcheck_cache.json");

if (arg is "--reload")
{
	var newJson = await DownloadJson();
	await File.WriteAllTextAsync(cachePath, newJson);
	Console.WriteLine("Cache refreshed.");
	return 0;
}

IEnumerable<uint> levelIds;
if (File.Exists(arg))
{
	var lines = await File.ReadAllLinesAsync(arg);
	//tryparse and select (bool parsed, uint? value)
	var parsed = lines.Select(line => uint.TryParse(line, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? (id, str: null) : (id: (uint?)null, str: line));
	var invalid = parsed.FirstOrDefault(p => !p.id.HasValue).str;
	if (invalid is not null)
	{
		Console.WriteLine($"The file contains a line that's not a valid level id: {invalid}");
		return 1;
	}

	levelIds = parsed.Where(p => p.id.HasValue).Select(p => p.id!.Value);
}
else if (uint.TryParse(arg, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
{
	levelIds = [id];
}
else
{
	Console.WriteLine("That's not a level id or a path, you dumbass");
	return 1;
}

var json = await GetJson();
var root = JsonSerializer.Deserialize<JsonObject>(json)!;
var levelsWithCoins = root["levelsWithCoins"]!.AsArray().Cast<JsonValue>().Select(v => v.GetValue<uint>());
var levelsWithoutCoins = root["levelsWithoutCoins"]!.AsArray().Cast<JsonValue>().Select(v => v.GetValue<uint>());
var presentLevels = levelsWithCoins.Concat(levelsWithoutCoins);
var results = presentLevels.Intersect(levelIds).ToArray();
if (results is [])
{
	Console.WriteLine("No matching levels found.");
	return 0;
}

Console.Write($"{results.Length} matching level(s) found: ");
for (int i = 0; i < results.Length; i++)
{
	Console.Write(results[i]);
	if (i < results.Length - 1)
		Console.Write(", ");
}
Console.WriteLine();
return results.Length;

async Task<string> GetJson()
{
	if (File.Exists(cachePath))
	{
		var lastWriteTime = File.GetLastWriteTime(cachePath);
		if (DateTime.Now - lastWriteTime < TimeSpan.FromHours(24))
		{
			return await File.ReadAllTextAsync(cachePath);
		}
	}

	var newJson = await DownloadJson();
	await File.WriteAllTextAsync(cachePath, newJson);
	return newJson;
}

async Task<string> DownloadJson()
{
	var http = new HttpClient();
	http.DefaultRequestHeaders.UserAgent.Add(new("lgcheck", Assembly.GetExecutingAssembly().GetName().Version!.ToString()));
	var response = await http.GetAsync("https://api.delivel.tech/bootup_get");
	response.EnsureSuccessStatusCode();
	return await response.Content.ReadAsStringAsync();
}
