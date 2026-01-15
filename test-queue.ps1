# PowerShell script to send test messages to Azure Queue Storage
# Requires Azure.Storage PowerShell module

param(
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = $env:AZURE_STORAGE_CONNECTION_STRING,

    [Parameter(Mandatory=$false)]
    [string]$QueueName = "notifications"
)

# Check if connection string is provided
if ([string]::IsNullOrEmpty($ConnectionString)) {
    Write-Error "AZURE_STORAGE_CONNECTION_STRING environment variable is not set or -ConnectionString parameter not provided"
    exit 1
}

# Import the module
Import-Module Azure.Storage -ErrorAction SilentlyContinue

Write-Host "Sending test messages to queue: $QueueName" -ForegroundColor Cyan
Write-Host "=" * 60

# Email test message
$emailMessage = Get-Content "TestMessages/email-test.json" -Raw
Write-Host "`n1. Sending email notification..." -ForegroundColor Yellow

# SMS test message
$smsMessage = Get-Content "TestMessages/sms-test.json" -Raw
Write-Host "2. Sending SMS notification..." -ForegroundColor Yellow

Write-Host "`nTo send messages, use Azure CLI or Azure Storage Explorer:" -ForegroundColor Green
Write-Host "az storage message put --queue-name $QueueName --content '$($emailMessage -replace "'", "''")' --connection-string `"YOUR_CONNECTION_STRING`"" -ForegroundColor White
Write-Host "`nor use Azure Storage Explorer to manually add messages to the queue." -ForegroundColor White
