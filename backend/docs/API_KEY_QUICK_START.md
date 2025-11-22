# API Key Quick Start Guide

Quick reference for setting up API keys in StockSensePro.

## Development (Local Machine)

### Step 1: Get Your API Key

1. Visit https://www.alphavantage.co/support/#api-key
2. Enter your email and get your free API key
3. Copy the API key (it will look like: `ABC123XYZ456`)

### Step 2: Set Up User Secrets

```bash
# Navigate to the API project
cd backend/src/StockSensePro.Api

# Initialize User Secrets (if not already done)
dotnet user-secrets init

# Set your Alpha Vantage API key
dotnet user-secrets set "AlphaVantage:ApiKey" "YOUR_ACTUAL_API_KEY_HERE"

# Verify it was set
dotnet user-secrets list
```

### Step 3: Enable Alpha Vantage

Update `appsettings.Development.json`:

```json
{
  "AlphaVantage": {
    "Enabled": true
  },
  "DataProvider": {
    "PrimaryProvider": "AlphaVantage",
    "FallbackProvider": "YahooFinance",
    "Strategy": "Fallback"
  }
}
```

### Step 4: Run the Application

```bash
dotnet run
```

Check the logs for:
```
[INF] Alpha Vantage API key configured and validated: Key=ABC1...Z456, Length=16
```

---

## Production (Docker/Cloud)

### Using Environment Variables

```bash
# Set environment variable
export AlphaVantage__ApiKey="YOUR_ACTUAL_API_KEY_HERE"
export AlphaVantage__Enabled=true

# Run the application
dotnet run --environment Production
```

### Using Docker Compose

Create `.env.production` (add to `.gitignore`):

```env
ALPHA_VANTAGE_API_KEY=your_actual_api_key_here
```

Update `docker-compose.yml`:

```yaml
services:
  api:
    environment:
      - AlphaVantage__ApiKey=${ALPHA_VANTAGE_API_KEY}
      - AlphaVantage__Enabled=true
```

Run:

```bash
docker-compose --env-file .env.production up
```

---

## Verification

### Check Configuration

```bash
# Development: List User Secrets
dotnet user-secrets list

# Production: Check environment variable (without exposing value)
env | grep AlphaVantage__ApiKey | sed 's/=.*/=***/'
```

### Test API Key

```bash
# Replace YOUR_KEY with your actual API key
curl "https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol=AAPL&apikey=YOUR_KEY"
```

Expected response:
```json
{
  "Global Quote": {
    "01. symbol": "AAPL",
    "05. price": "176.50",
    ...
  }
}
```

### Check Application Logs

Look for these log messages on startup:

✅ **Success:**
```
[INF] Validating API key configuration...
[INF] Alpha Vantage API key configured and validated: Key=ABC1...Z456, Length=16
[INF] API key validation complete
```

⚠️ **Warning (Missing Key):**
```
[WRN] Alpha Vantage is enabled but API key is not configured. Provider will not be available.
```

⚠️ **Warning (Invalid Key):**
```
[WRN] Alpha Vantage API key appears to be invalid (too short). Expected at least 8 characters, got 4.
```

---

## Troubleshooting

### "API key is not configured"

**Solution**: Set the API key using User Secrets or environment variables (see above).

### "API key appears to be invalid"

**Solution**: 
1. Verify your API key at https://www.alphavantage.co/support/#api-key
2. Ensure you copied the entire key (no spaces or line breaks)
3. Test the key manually using curl (see above)

### User Secrets not working

**Solution**:
1. Ensure `ASPNETCORE_ENVIRONMENT=Development`
2. Run `dotnet user-secrets init` in the API project directory
3. Verify `.csproj` has `<UserSecretsId>` element

### Environment variables not loading

**Solution**:
1. Use double underscores: `AlphaVantage__ApiKey` (not `AlphaVantage:ApiKey`)
2. Restart your terminal/IDE after setting environment variables
3. Check with: `echo $AlphaVantage__ApiKey` (Linux/Mac) or `echo %AlphaVantage__ApiKey%` (Windows)

---

## Security Reminders

❌ **NEVER:**
- Commit API keys to Git
- Share API keys in chat/email
- Log API keys in plain text
- Use production keys in development

✅ **ALWAYS:**
- Use User Secrets for development
- Use Environment Variables or Key Vault for production
- Use different keys per environment
- Rotate keys regularly (every 90 days)

---

## Need More Help?

See the comprehensive guide: [API_KEY_SECURITY.md](./API_KEY_SECURITY.md)

- User Secrets: [Section](./API_KEY_SECURITY.md#using-user-secrets)
- Environment Variables: [Section](./API_KEY_SECURITY.md#using-environment-variables)
- Azure Key Vault: [Section](./API_KEY_SECURITY.md#azure-key-vault-integration)
- Troubleshooting: [Section](./API_KEY_SECURITY.md#troubleshooting)
