# Configuration Guide

This guide explains how to configure the NotificationProcessor for local development and production deployment.

## Local Development

### local.settings.json

For local development, all configuration goes in `local.settings.json`. This file is **never committed** to source control.

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",

    "AzureQueueStorage__ConnectionString": "UseDevelopmentStorage=true",
    "AzureQueueStorage__QueueName": "notifications",

    "Smtp__Host": "smtp.example.com",
    "Smtp__Port": "587",
    "Smtp__Username": "your-smtp-username",
    "Smtp__Password": "your-smtp-password",
    "Smtp__FromEmail": "noreply@example.com",
    "Smtp__FromName": "Your Company",
    "Smtp__EnableSsl": "true",

    "Twilio__AccountSid": "your-twilio-account-sid",
    "Twilio__AuthToken": "your-twilio-auth-token",
    "Twilio__FromPhoneNumber": "+1234567890",

    "Database__ConnectionString": "Host=localhost;Database=notifications;Username=postgres;Password=postgres"
  }
}
```

### Using Azure Storage Emulator

For local queue testing, use Azurite (Azure Storage Emulator):

```bash
# Install Azurite
npm install -g azurite

# Start Azurite
azurite-queue
```

Then use `UseDevelopmentStorage=true` for `AzureWebJobsStorage` and `AzureQueueStorage__ConnectionString`.

## Production Configuration

### Azure Application Settings

In production, configure settings in the Azure Portal under **Function App → Configuration → Application Settings**.

#### Method 1: Direct Configuration (Not Recommended for Secrets)

Add each setting as an Application Setting:

| Name | Value |
|------|-------|
| `AzureQueueStorage__ConnectionString` | `DefaultEndpointsProtocol=https;...` |
| `AzureQueueStorage__QueueName` | `notifications` |
| `Smtp__Host` | `smtp.sendgrid.net` |
| `Smtp__Port` | `587` |
| `Smtp__Username` | `apikey` |
| `Smtp__Password` | `SG.xxxxxxxxxxxxx` |
| `Smtp__FromEmail` | `noreply@yourdomain.com` |
| `Smtp__FromName` | `Your Company` |
| `Smtp__EnableSsl` | `true` |
| `Twilio__AccountSid` | `ACxxxxxxxxxxxxxxxx` |
| `Twilio__AuthToken` | `your-auth-token` |
| `Twilio__FromPhoneNumber` | `+1234567890` |
| `Database__ConnectionString` | `Host=prod-db.postgres.database.azure.com;...` |

#### Method 2: Azure Key Vault (Recommended)

**Step 1: Create Key Vault Secrets**

Create secrets in Azure Key Vault:

```bash
# Create Key Vault (if not exists)
az keyvault create --name your-keyvault --resource-group your-rg --location eastus

# Create secrets
az keyvault secret set --vault-name your-keyvault --name SmtpPassword --value "your-smtp-password"
az keyvault secret set --vault-name your-keyvault --name TwilioAuthToken --value "your-twilio-auth-token"
az keyvault secret set --vault-name your-keyvault --name DatabaseConnectionString --value "Host=...;Password=..."
az keyvault secret set --vault-name your-keyvault --name QueueConnectionString --value "DefaultEndpointsProtocol=https;..."
```

**Step 2: Enable Managed Identity**

Enable System-assigned Managed Identity for your Function App:

```bash
az functionapp identity assign --name your-function-app --resource-group your-rg
```

**Step 3: Grant Access to Key Vault**

```bash
# Get the Function App's principal ID
PRINCIPAL_ID=$(az functionapp identity show --name your-function-app --resource-group your-rg --query principalId -o tsv)

# Grant Key Vault access
az keyvault set-policy --name your-keyvault --object-id $PRINCIPAL_ID --secret-permissions get list
```

**Step 4: Reference Secrets in Application Settings**

Use Key Vault reference syntax in Application Settings:

| Name | Value |
|------|-------|
| `Smtp__Password` | `@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/SmtpPassword/)` |
| `Twilio__AuthToken` | `@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/TwilioAuthToken/)` |
| `Database__ConnectionString` | `@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/DatabaseConnectionString/)` |
| `AzureQueueStorage__ConnectionString` | `@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/QueueConnectionString/)` |

Non-secret settings can be configured directly:

| Name | Value |
|------|-------|
| `Smtp__Host` | `smtp.sendgrid.net` |
| `Smtp__Port` | `587` |
| `Smtp__Username` | `apikey` |
| `Smtp__FromEmail` | `noreply@yourdomain.com` |
| `Smtp__FromName` | `Your Company` |
| `Smtp__EnableSsl` | `true` |
| `Twilio__AccountSid` | `ACxxxxxxxxxxxxxxxx` |
| `Twilio__FromPhoneNumber` | `+1234567890` |
| `AzureQueueStorage__QueueName` | `notifications` |

### Using Azure CLI to Set Application Settings

```bash
# Set non-secret settings
az functionapp config appsettings set --name your-function-app --resource-group your-rg --settings \
  "Smtp__Host=smtp.sendgrid.net" \
  "Smtp__Port=587" \
  "Smtp__Username=apikey" \
  "Smtp__FromEmail=noreply@yourdomain.com" \
  "Smtp__FromName=Your Company" \
  "Smtp__EnableSsl=true" \
  "Twilio__AccountSid=ACxxxxxxxxxxxxxxxx" \
  "Twilio__FromPhoneNumber=+1234567890" \
  "AzureQueueStorage__QueueName=notifications"

# Set Key Vault references
az functionapp config appsettings set --name your-function-app --resource-group your-rg --settings \
  "Smtp__Password=@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/SmtpPassword/)" \
  "Twilio__AuthToken=@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/TwilioAuthToken/)" \
  "Database__ConnectionString=@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/DatabaseConnectionString/)" \
  "AzureQueueStorage__ConnectionString=@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/QueueConnectionString/)"
```

## Configuration Settings Reference

### SMTP Configuration

| Setting | Description | Example |
|---------|-------------|---------|
| `Smtp__Host` | SMTP server hostname | `smtp.sendgrid.net`, `smtp.gmail.com` |
| `Smtp__Port` | SMTP server port | `587` (TLS), `465` (SSL), `25` (plain) |
| `Smtp__Username` | SMTP username | `apikey` (SendGrid), email (Gmail) |
| `Smtp__Password` | SMTP password/API key | **Store in Key Vault** |
| `Smtp__FromEmail` | Default sender email | `noreply@yourdomain.com` |
| `Smtp__FromName` | Default sender name | `Your Company` |
| `Smtp__EnableSsl` | Enable SSL/TLS | `true` (recommended) |

### Twilio Configuration

| Setting | Description | Example |
|---------|-------------|---------|
| `Twilio__AccountSid` | Twilio Account SID | `ACxxxxxxxxxxxxxxxx` (public, but treat as sensitive) |
| `Twilio__AuthToken` | Twilio Auth Token | **Store in Key Vault** |
| `Twilio__FromPhoneNumber` | Twilio phone number | `+1234567890` |

### Azure Queue Storage Configuration

| Setting | Description | Example |
|---------|-------------|---------|
| `AzureQueueStorage__ConnectionString` | Storage account connection string | **Store in Key Vault** |
| `AzureQueueStorage__QueueName` | Queue name | `notifications` |

### Database Configuration

| Setting | Description | Example |
|---------|-------------|---------|
| `Database__ConnectionString` | PostgreSQL connection string | **Store in Key Vault** |

## Email Provider Examples

### SendGrid

```json
{
  "Smtp__Host": "smtp.sendgrid.net",
  "Smtp__Port": "587",
  "Smtp__Username": "apikey",
  "Smtp__Password": "SG.xxxxxxxxxxxxx",
  "Smtp__EnableSsl": "true"
}
```

### Gmail (App Password)

```json
{
  "Smtp__Host": "smtp.gmail.com",
  "Smtp__Port": "587",
  "Smtp__Username": "your-email@gmail.com",
  "Smtp__Password": "your-app-password",
  "Smtp__EnableSsl": "true"
}
```

### Microsoft 365

```json
{
  "Smtp__Host": "smtp.office365.com",
  "Smtp__Port": "587",
  "Smtp__Username": "your-email@yourdomain.com",
  "Smtp__Password": "your-password",
  "Smtp__EnableSsl": "true"
}
```

### AWS SES

```json
{
  "Smtp__Host": "email-smtp.us-east-1.amazonaws.com",
  "Smtp__Port": "587",
  "Smtp__Username": "your-smtp-username",
  "Smtp__Password": "your-smtp-password",
  "Smtp__EnableSsl": "true"
}
```

## Security Best Practices

1. **Never commit `local.settings.json`** - It's in `.gitignore` by default
2. **Use Key Vault for all secrets** in production
3. **Enable Managed Identity** - Avoid storing Key Vault credentials
4. **Rotate secrets regularly** - Update Key Vault secrets, no code changes needed
5. **Use separate Key Vaults** for dev/staging/prod environments
6. **Monitor Key Vault access** - Enable logging and alerts
7. **Use least-privilege access** - Only grant "Get" and "List" permissions for secrets

## Verifying Configuration

### Local

After configuring `local.settings.json`, run the Function App locally:

```bash
cd src/NotificationProcessor.Functions
func start
```

Check logs for configuration errors.

### Production

After deploying and configuring Application Settings:

1. Go to Azure Portal → Function App → Configuration
2. Verify all settings show green checkmarks (Key Vault references resolved)
3. Check Application Insights logs for startup errors
4. Send a test notification to the queue

## Troubleshooting

### Key Vault Reference Not Resolving

**Symptom:** Setting shows red X in Azure Portal

**Solutions:**
- Verify Managed Identity is enabled
- Check Key Vault access policy includes the Function App's principal ID
- Verify Secret URI is correct (include trailing slash)
- Check secret exists and is enabled

### Connection String Issues

**Symptom:** "Connection string not configured" error

**Solutions:**
- Verify setting name uses double underscores (`Database__ConnectionString`)
- Check Key Vault secret is accessible
- Test connection string format separately

### SMTP Authentication Failed

**Symptom:** SMTP errors in logs

**Solutions:**
- Verify username/password are correct
- Check if email provider requires app-specific passwords
- Ensure port and SSL settings match provider requirements
- Check if IP needs to be whitelisted

### Templates Not Found

**Symptom:** "Template not found" errors

**Solutions:**
- Verify templates are deployed with the Function App
- Check template file names match exactly (case-sensitive)
- Ensure `Templates\**\*` is in `.csproj` with `CopyToOutputDirectory`

## Environment-Specific Configuration

### Development

```bash
# local.settings.json
"AzureQueueStorage__ConnectionString": "UseDevelopmentStorage=true"
"Database__ConnectionString": "Host=localhost;Database=notifications_dev;..."
```

### Staging

```bash
# Azure App Settings
az functionapp config appsettings set --name your-function-app-staging \
  --settings \
  "Database__ConnectionString=@Microsoft.KeyVault(SecretUri=https://kv-staging.vault.azure.net/secrets/DbConnectionString/)"
```

### Production

```bash
# Azure App Settings
az functionapp config appsettings set --name your-function-app-prod \
  --settings \
  "Database__ConnectionString=@Microsoft.KeyVault(SecretUri=https://kv-prod.vault.azure.net/secrets/DbConnectionString/)"
```

## Additional Resources

- [Azure Functions Configuration](https://learn.microsoft.com/en-us/azure/azure-functions/functions-app-settings)
- [Key Vault References](https://learn.microsoft.com/en-us/azure/app-service/app-service-key-vault-references)
- [Managed Identity](https://learn.microsoft.com/en-us/azure/app-service/overview-managed-identity)
- [local.settings.json](https://learn.microsoft.com/en-us/azure/azure-functions/functions-develop-local)
