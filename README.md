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


#### Flags:
- `--instance` : Mastodon instance URL (e.g. https://mastodon.social)
- `--username` : Mastodon username (without @)
- `--token`    : Mastodon API access token
- `--date`     : Cutoff date (YYYY-MM-DD); all posts before this date will be deleted
- `--debug` or `--verbose` : Print detailed debug output (kept/deleted posts, account info, rate limit waits)
- `--no-rate-limit` : Do not wait between deletions (disables the 10–20 second delay; may cause rate limiting or errors)

## How it works
- Authenticates with the Mastodon API using your access token
- Fetches all statuses for the account (with robust pagination)
- Deletes all statuses (posts) created before the specified date
- Waits a random 10–20 seconds between deletions to avoid API rate limits (unless `--no-rate-limit` is used)
- Prints debug output for each post (kept or deleted)

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
- The app waits 10–20 seconds between deletions to avoid Mastodon API rate limits. Use `--no-rate-limit` to disable this delay (not recommended for most users).

## License
See LICENSE file.
