# API Key Security Guide

This document provides comprehensive guidance on securely storing and managing API keys for StockSensePro, including Alpha Vantage and other third-party service credentials.

## Table of Contents

1. [Overview](#overview)
2. [Development Environment](#development-environment)
3. [Production Environment](#production-environment)
4. [Azure Key Vault Integration](#azure-key-vault-integration)
5. [API Key Validation](#api-key-validation)
6. [Best Practices](#best-practices)
7. [Troubleshooting](#troubleshooting)

---

## Overview

API keys are sensitive credentials that must never be committed to source control or exposed in logs, error messages, or client responses. StockSensePro supports multiple secure storage mechanisms depending on the environment:

- **Development**: User Secrets (local machine only)
- **Production**: Environment Variables or Azure Key Vault
- **CI/CD**: Environment Variables

### Security Requirements

✅ **DO:**
- Store API keys in secure configuration stores
- Use different keys for development, staging, and production
- Rotate API keys regularly
- Validate API key format on startup
- Log API key validation results (without exposing the key)

❌ **DON'T:**
- Commit API keys to source control (Git)
- Log API keys in plain text
- Include API keys in error messages
- Expose API keys in client-side responses
- Share API keys across environments

---

## Development Environment

### Using User Secrets

User Secrets is the recommended approach for local development. It stores secrets outside the project directory in a user-specific location on your machine.

#### Setup User Secrets

1. **Navigate to the API project directory:**

```bash
cd backend/src/StockSensePro.Api
```

2. **Initialize User Secrets (if not already done):**

```bash
dotnet user-secrets init
```

This adds a `UserSecretsId` to your `.csproj` file.

3. **Set the Alpha Vantage API key:**

```bash
dotnet user-secrets set "AlphaVantage:ApiKey" "YOUR_ALPHA_VANTAGE_API_KEY"
```

4. **Verify the secret was set:**

```bash
dotnet user-secrets list
```

Expected output:
```
AlphaVantage:ApiKey = YOUR_ALPHA_VANTAGE_API_KEY
```

#### Where Are User Secrets Stored?

User Secrets are stored in a JSON file on your local machine:

- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

#### Setting Multiple Secrets

You can set multiple API keys or configuration values:

```bash
# Alpha Vantage API Key
dotnet user-secrets set "AlphaVantage:ApiKey" "YOUR_ALPHA_VANTAGE_KEY"

# Database connection string (if needed)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=stocksensepro;Username=user;Password=pass"

# Redis connection string (if needed)
dotnet user-secrets set "ConnectionStrings:RedisConnection" "localhost:6379,password=yourpassword"
```

#### Removing Secrets

```bash
# Remove a specific secret
dotnet user-secrets remove "AlphaVantage:ApiKey"

# Clear all secrets
dotnet user-secrets clear
```

#### How User Secrets Work

User Secrets are automatically loaded by the ASP.NET Core configuration system in Development environment. The configuration hierarchy is:

1. `appsettings.json`
2. `appsettings.Development.json`
3. **User Secrets** (overrides previous values)
4. Environment Variables (overrides all previous values)
5. Command-line arguments (highest priority)

---

## Production Environment

### Using Environment Variables

Environment variables are the recommended approach for production deployments, including Docker containers, Kubernetes, and cloud platforms.

#### Setting Environment Variables

**Windows (PowerShell):**

```powershell
$env:AlphaVantage__ApiKey = "YOUR_ALPHA_VANTAGE_API_KEY"
```

**Windows (Command Prompt):**

```cmd
set AlphaVantage__ApiKey=YOUR_ALPHA_VANTAGE_API_KEY
```

**Linux/macOS:**

```bash
export AlphaVantage__ApiKey="YOUR_ALPHA_VANTAGE_API_KEY"
```

**Note:** Use double underscores (`__`) to represent nested configuration sections in environment variables.

#### Docker Compose

Add environment variables to your `docker-compose.yml`:

```yaml
services:
  api:
    image: stocksensepro-api:latest
    environment:
      - AlphaVantage__ApiKey=${ALPHA_VANTAGE_API_KEY}
      - AlphaVantage__Enabled=true
      - DataProvider__PrimaryProvider=AlphaVantage
    env_file:
      - .env.production
```

Create a `.env.production` file (add to `.gitignore`):

```env
ALPHA_VANTAGE_API_KEY=your_actual_api_key_here
```

#### Kubernetes Secrets

Create a Kubernetes secret:

```bash
kubectl create secret generic stocksensepro-secrets \
  --from-literal=alpha-vantage-api-key=YOUR_ALPHA_VANTAGE_API_KEY
```

Reference in your deployment:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: stocksensepro-api
spec:
  template:
    spec:
      containers:
      - name: api
        image: stocksensepro-api:latest
        env:
        - name: AlphaVantage__ApiKey
          valueFrom:
            secretKeyRef:
              name: stocksensepro-secrets
              key: alpha-vantage-api-key
```

#### Azure App Service

Set application settings in Azure Portal or using Azure CLI:

```bash
az webapp config appsettings set \
  --resource-group myResourceGroup \
  --name myAppName \
  --settings AlphaVantage__ApiKey="YOUR_ALPHA_VANTAGE_API_KEY"
```

---

## Azure Key Vault Integration

Azure Key Vault provides enterprise-grade secret management with access control, auditing, and automatic rotation.

### Prerequisites

1. Azure subscription
2. Azure Key Vault instance
3. Managed Identity or Service Principal for authentication

### Setup Azure Key Vault

#### 1. Create Key Vault

```bash
az keyvault create \
  --name stocksensepro-keyvault \
  --resource-group myResourceGroup \
  --location eastus
```

#### 2. Add Secrets to Key Vault

```bash
az keyvault secret set \
  --vault-name stocksensepro-keyvault \
  --name AlphaVantage--ApiKey \
  --value "YOUR_ALPHA_VANTAGE_API_KEY"
```

**Note:** Use double dashes (`--`) in secret names to represent nested configuration sections.

#### 3. Grant Access to Your Application

**Using Managed Identity (recommended):**

```bash
# Enable managed identity for your App Service
az webapp identity assign \
  --resource-group myResourceGroup \
  --name myAppName

# Grant access to Key Vault
az keyvault set-policy \
  --name stocksensepro-keyvault \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

**Using Service Principal:**

```bash
az keyvault set-policy \
  --name stocksensepro-keyvault \
  --spn <service-principal-app-id> \
  --secret-permissions get list
```

### Configure Application to Use Key Vault

#### 1. Install NuGet Packages

```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

#### 2. Update Program.cs

Add Key Vault configuration before building the application:

```csharp
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
if (!builder.Environment.IsDevelopment())
{
    var keyVaultName = builder.Configuration["KeyVault:Name"];
    if (!string.IsNullOrEmpty(keyVaultName))
    {
        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        
        // Use DefaultAzureCredential for automatic authentication
        // Works with Managed Identity, Azure CLI, Visual Studio, etc.
        builder.Configuration.AddAzureKeyVault(
            keyVaultUri,
            new DefaultAzureCredential());
            
        Log.Information("Azure Key Vault configured: {KeyVaultUri}", keyVaultUri);
    }
}
```

#### 3. Update appsettings.Production.json

```json
{
  "KeyVault": {
    "Name": "stocksensepro-keyvault"
  }
}
```

### Key Vault Secret Naming Conventions

Azure Key Vault has naming restrictions. Use these conventions:

| Configuration Path | Key Vault Secret Name |
|-------------------|----------------------|
| `AlphaVantage:ApiKey` | `AlphaVantage--ApiKey` |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings--DefaultConnection` |
| `DataProvider:PrimaryProvider` | `DataProvider--PrimaryProvider` |

The `--` (double dash) is automatically converted to `:` (colon) by the configuration system.

### Testing Key Vault Locally

You can test Key Vault integration locally using Azure CLI authentication:

```bash
# Login to Azure
az login

# Set Key Vault name in environment
export KeyVault__Name=stocksensepro-keyvault

# Run the application
dotnet run
```

---

## API Key Validation

StockSensePro validates API keys on startup to catch configuration issues early.

### Validation Rules

The application performs the following validations:

1. **Format Validation**: Checks if API key matches expected format
2. **Presence Validation**: Ensures API key is not empty when provider is enabled
3. **Length Validation**: Verifies API key meets minimum length requirements

### Validation Behavior

**On Startup:**
- If Alpha Vantage is enabled but API key is missing or invalid, a **warning** is logged
- The application continues to start but Alpha Vantage provider will not be available
- If Alpha Vantage is the primary provider, the application will fall back to the fallback provider (if configured)

**Validation Logs:**

```
[INF] Validating Alpha Vantage configuration...
[WRN] Alpha Vantage is enabled but API key is not configured
[INF] Alpha Vantage API key validation: Valid=True, Length=16
```

### Manual Validation

You can manually validate your API key configuration:

```bash
# Check if API key is set (without exposing the value)
dotnet user-secrets list | grep AlphaVantage

# Test API connectivity
curl "https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol=AAPL&apikey=YOUR_API_KEY"
```

### Validation Code Example

The validation logic in `Program.cs`:

```csharp
// Validate Alpha Vantage configuration
if (alphaVantageSettings.Enabled)
{
    if (string.IsNullOrWhiteSpace(alphaVantageSettings.ApiKey))
    {
        Log.Warning("Alpha Vantage is enabled but API key is not configured. Provider will not be available.");
    }
    else if (alphaVantageSettings.ApiKey.Length < 8)
    {
        Log.Warning("Alpha Vantage API key appears to be invalid (too short). Expected at least 8 characters.");
    }
    else
    {
        // Mask the API key for logging (show only first 4 and last 4 characters)
        var maskedKey = alphaVantageSettings.ApiKey.Length > 8
            ? $"{alphaVantageSettings.ApiKey[..4]}...{alphaVantageSettings.ApiKey[^4..]}"
            : "****";
        
        Log.Information("Alpha Vantage API key configured: {MaskedKey}", maskedKey);
    }
}
```

---

## Best Practices

### 1. Use Different Keys Per Environment

Obtain separate API keys for each environment:

- **Development**: Free tier key for testing
- **Staging**: Separate key for pre-production testing
- **Production**: Premium tier key with higher rate limits

### 2. Rotate API Keys Regularly

- Rotate API keys every 90 days
- Use Azure Key Vault automatic rotation when possible
- Update all environments when rotating keys

### 3. Monitor API Key Usage

- Track API call counts per key
- Set up alerts for unusual usage patterns
- Monitor rate limit consumption

### 4. Implement Least Privilege

- Grant only necessary permissions to access secrets
- Use separate service principals for different environments
- Regularly audit access logs

### 5. Never Log API Keys

The application automatically masks API keys in logs:

```csharp
// ✅ GOOD: Masked logging
Log.Information("API key configured: {MaskedKey}", MaskApiKey(apiKey));

// ❌ BAD: Plain text logging
Log.Information("API key: {ApiKey}", apiKey);
```

### 6. Validate Configuration in CI/CD

Add validation steps to your deployment pipeline:

```yaml
# Example GitHub Actions workflow
- name: Validate Configuration
  run: |
    if [ -z "$ALPHA_VANTAGE_API_KEY" ]; then
      echo "Error: ALPHA_VANTAGE_API_KEY not set"
      exit 1
    fi
  env:
    ALPHA_VANTAGE_API_KEY: ${{ secrets.ALPHA_VANTAGE_API_KEY }}
```

---

## Troubleshooting

### Issue: "Alpha Vantage API key is not configured"

**Cause**: API key is not set in configuration.

**Solution**:
1. Verify User Secrets are set: `dotnet user-secrets list`
2. Check environment variables: `echo $AlphaVantage__ApiKey`
3. Ensure `AlphaVantage:Enabled` is set to `true` in configuration

### Issue: "Invalid API key" error from Alpha Vantage

**Cause**: API key is incorrect or expired.

**Solution**:
1. Verify API key at https://www.alphavantage.co/support/#api-key
2. Test API key manually:
   ```bash
   curl "https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol=AAPL&apikey=YOUR_KEY"
   ```
3. Update the API key in your configuration store

### Issue: User Secrets not loading

**Cause**: User Secrets are only loaded in Development environment.

**Solution**:
1. Check `ASPNETCORE_ENVIRONMENT` is set to `Development`
2. Verify `UserSecretsId` exists in `.csproj` file
3. Run `dotnet user-secrets init` if needed

### Issue: Azure Key Vault authentication fails

**Cause**: Managed Identity or Service Principal lacks permissions.

**Solution**:
1. Verify Managed Identity is enabled
2. Check Key Vault access policies
3. Test authentication locally with `az login`
4. Review Azure Key Vault audit logs

### Issue: API key visible in logs

**Cause**: Logging configuration is too verbose or API key is logged directly.

**Solution**:
1. Review logging configuration in `appsettings.json`
2. Ensure API keys are masked before logging
3. Use structured logging with sensitive data filtering

---

## Additional Resources

- [ASP.NET Core User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Key Vault Configuration Provider](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)
- [Alpha Vantage API Documentation](https://www.alphavantage.co/documentation/)
- [Environment Variables in .NET](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)

---

## Quick Reference

### Development Setup

```bash
cd backend/src/StockSensePro.Api
dotnet user-secrets init
dotnet user-secrets set "AlphaVantage:ApiKey" "YOUR_KEY"
dotnet run
```

### Production Setup (Environment Variables)

```bash
export AlphaVantage__ApiKey="YOUR_KEY"
export AlphaVantage__Enabled=true
dotnet run --environment Production
```

### Azure Key Vault Setup

```bash
# Create Key Vault
az keyvault create --name stocksensepro-kv --resource-group rg --location eastus

# Add secret
az keyvault secret set --vault-name stocksensepro-kv --name AlphaVantage--ApiKey --value "YOUR_KEY"

# Grant access
az keyvault set-policy --name stocksensepro-kv --object-id <id> --secret-permissions get list
```

---

**Last Updated**: November 2025  
**Version**: 1.0
