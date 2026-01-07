
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;



string? instanceUrl = null;
string? username = null;
string? apiToken = null;
DateTime cutoffDate = DateTime.MinValue;
bool debug = false;
bool noRateLimit = false;

// Parse args: --instance, --username, --token, --date, --debug, --verbose
for (int i = 0; i < args.Length; i++)
{
	switch (args[i])
	{
		case "--instance":
			if (i + 1 < args.Length) instanceUrl = args[++i];
			break;
		case "--username":
			if (i + 1 < args.Length) username = args[++i];
			break;
		case "--token":
			if (i + 1 < args.Length) apiToken = args[++i];
			break;
		case "--date":
			if (i + 1 < args.Length && DateTime.TryParseExact(args[++i], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
				cutoffDate = dt;
			break;
		case "--debug":
		case "--verbose":
			debug = true;
			break;
        case "--no-rate-limit":
            noRateLimit = true;
            break;
	}
}

if (string.IsNullOrWhiteSpace(instanceUrl))
{
	Console.Write("Enter Mastodon instance URL (e.g. https://mastodon.social): ");
	instanceUrl = Console.ReadLine();
}
if (string.IsNullOrWhiteSpace(username))
{
	Console.Write("Enter your Mastodon username (without @): ");
	username = Console.ReadLine();
}
if (string.IsNullOrWhiteSpace(apiToken))
{
	Console.Write("Enter your Mastodon API access token: ");
	apiToken = Console.ReadLine();
}
if (cutoffDate == DateTime.MinValue)
{
	Console.Write("Enter cutoff date (YYYY-MM-DD): ");
	string? dateInput = Console.ReadLine();
	if (!DateTime.TryParseExact(dateInput, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out cutoffDate))
	{
		Console.WriteLine("Invalid date format. Exiting.");
		return;
	}
}

if (string.IsNullOrWhiteSpace(instanceUrl) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(apiToken))
{
	Console.WriteLine("All fields are required. Exiting.");
	return;
}

using var client = new HttpClient();
client.BaseAddress = new Uri(instanceUrl);
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);



// Get authenticated user's account info for more reliable ID
var verifyResp = await client.GetAsync("/api/v1/accounts/verify_credentials");
if (!verifyResp.IsSuccessStatusCode)
{
	Console.WriteLine($"Failed to verify credentials: {verifyResp.StatusCode}");
	return;
}
var verifyJson = await verifyResp.Content.ReadAsStringAsync();
var verifyDoc = JsonDocument.Parse(verifyJson).RootElement;
string myAcct = verifyDoc.GetProperty("acct").GetString() ?? "";
string myId = verifyDoc.GetProperty("id").GetString() ?? "";

if (debug)
	Console.WriteLine($"Authenticated as: {myAcct} (ID: {myId})");

string accountId = myId;
if (!string.Equals(myAcct, username, StringComparison.OrdinalIgnoreCase))
{
	// If username doesn't match authenticated user, search for the username
	var acctResp = await client.GetAsync($"/api/v1/accounts/search?q={username}&limit=1");
	if (!acctResp.IsSuccessStatusCode)
	{
		Console.WriteLine($"Failed to fetch account info: {acctResp.StatusCode}");
		return;
	}
	var acctJson = await acctResp.Content.ReadAsStringAsync();
	var acctArr = JsonDocument.Parse(acctJson).RootElement;
	if (acctArr.GetArrayLength() == 0)
	{
		Console.WriteLine("Account not found.");
		return;
	}
	accountId = acctArr[0].GetProperty("id").GetString()!;
	if (debug)
		Console.WriteLine($"Found account ID for {username}: {accountId}");
}
else if (debug)
{
	Console.WriteLine($"Operating on authenticated user's posts.");
}

int deleted = 0;
int checkedCount = 0;
string? maxId = null;
bool more = true;
int page = 0;


while (more)
{
	page++;
	var url = $"/api/v1/accounts/{accountId}/statuses?limit=40&exclude_reblogs=false&exclude_replies=false" + (maxId != null ? $"&max_id={maxId}" : "");
	var resp = await client.GetAsync(url);
	if (!resp.IsSuccessStatusCode)
	{
		Console.WriteLine($"Failed to fetch statuses: {resp.StatusCode}");
		break;
	}
	var json = await resp.Content.ReadAsStringAsync();
	var statuses = JsonDocument.Parse(json).RootElement;
	if (statuses.GetArrayLength() == 0)
	{
		if (page == 1)
			Console.WriteLine("No posts found for this account.");
		break;
	}

	foreach (var status in statuses.EnumerateArray())
	{
		checkedCount++;
		var createdAtStr = status.GetProperty("created_at").GetString();
		DateTime createdAt = DateTime.MinValue;
		if (!string.IsNullOrEmpty(createdAtStr))
		{
			DateTime.TryParse(createdAtStr, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out createdAt);
		}
		var id = status.GetProperty("id").GetString();
		var uri = status.TryGetProperty("uri", out var uriProp) ? uriProp.GetString() : "";
		var content = status.TryGetProperty("content", out var contentProp) ? contentProp.GetString() : "";
		if (createdAt < cutoffDate)
		{
			var delResp = await client.DeleteAsync($"/api/v1/statuses/{id}");
			if (delResp.IsSuccessStatusCode)
			{
				deleted++;
				if (debug)
					Console.WriteLine($"Deleted post {id} from {createdAt:u} ({uri})");
			}
			else
			{
				Console.WriteLine($"Failed to delete post {id}: {delResp.StatusCode}");
				if (delResp.StatusCode.ToString() == "TooManyRequests")
				{
					Console.WriteLine("Received TooManyRequests error. Stopping execution to avoid further rate limiting.");
					return;
				}
			}
			if (!noRateLimit)
			{
				// Wait 10-20 seconds to avoid rate limit
				var rand = new Random();
				int delay = rand.Next(10000, 20001); // ms
				if (debug)
					Console.WriteLine($"Waiting {delay / 1000.0:F1} seconds to avoid rate limit...");
				await Task.Delay(delay);
			}
		}
		else if (debug)
		{
			// Show post info if not deleted
			Console.WriteLine($"Kept post {id} from {createdAt:u} ({uri})");
		}
		maxId = id;
	}
	// If less than 40, we're done
	if (statuses.GetArrayLength() < 40)
		more = false;
}

Console.WriteLine($"Checked {checkedCount} posts. Deleted {deleted} posts before {cutoffDate:yyyy-MM-dd}.");
