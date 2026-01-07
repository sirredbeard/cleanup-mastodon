# MastodonCleaner

A .NET terminal application to delete all Mastodon posts (toots) before a specified date for a given account.

## Usage

You can provide settings via command-line flags or interactively:

### Command-line flags

```
dotnet run -- \
  --instance <mastodon_instance_url> \
  --username <username> \
  --token <api_access_token> \
  --date <YYYY-MM-DD>
```

Example:

```
dotnet run -- --instance https://mastodon.social --username alice --token YOUR_TOKEN --date 2025-01-01
```

If any flag is omitted, you will be prompted for it interactively.


- `--instance` : Mastodon instance URL (e.g. https://mastodon.social)
- `--username` : Mastodon username (without @)
- `--token`    : Mastodon API access token
- `--date`     : Cutoff date (YYYY-MM-DD); all posts before this date will be deleted
- `--debug` or `--verbose` : Print detailed debug output (kept/deleted posts, account info, rate limit waits)
- `--no-rate-limit` : Same as `--rate-limit-level 0`. Do not wait between deletions (may cause rate limiting or errors)
- `--rate-limit-level <0|1|2|3>` : Set rate limit wait time between deletions (0 = no wait, 1 = 5–10s, 2 = 20–30s [default], 3 = 30–60s)
- `--long-wait` : If rate limited, waits until the next :00 or :30 (half hour) and retries deletion.

## How it works
- Authenticates with the Mastodon API using your access token
- Fetches all statuses for the account (with robust pagination)
- Deletes all statuses (posts) created before the specified date
- Waits a random 20–30 seconds between deletions to avoid API rate limits by default (unless `--rate-limit-level 0` or `--no-rate-limit` is used)
- Use `--rate-limit-level` to adjust: 0 = no wait, 1 = 5–10s, 2 = 20–30s (default), 3 = 30–60s
- If `--long-wait` is set and a rate limit is hit, waits until the next :00 or :30 (half hour) and retries deletion.
- Prints debug output for each post (kept or deleted), if `--debug` or `--verbose` is specified

## How to run
1. Build the project:
   ```sh
   dotnet build
   ```
2. Run the application (see above for flags):
   ```sh
   dotnet run -- --instance <url> --username <name> --token <token> --date <YYYY-MM-DD>
   ```

## Warning
- This action is irreversible. Deleted posts cannot be recovered.
- Use with caution and ensure you have backups if needed.
- The app waits 20–30 seconds between deletions to avoid Mastodon API rate limits by default. Use `--rate-limit-level` to adjust (see above). Use `--rate-limit-level 0` or `--no-rate-limit` to disable this delay (not recommended for most users). 
- If `--long-wait` is set, the app will wait until the next half hour and retry once if rate limited.

## License
Apache 2.0. See LICENSE file.
