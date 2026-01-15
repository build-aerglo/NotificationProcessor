#!/bin/bash

# Bash script to send test messages to Azure Queue Storage using Azure CLI
# Requires: az cli installed and authenticated

QUEUE_NAME="notifications"
CONNECTION_STRING="${AZURE_STORAGE_CONNECTION_STRING}"

if [ -z "$CONNECTION_STRING" ]; then
    echo "Error: AZURE_STORAGE_CONNECTION_STRING environment variable is not set"
    exit 1
fi

echo "Sending test messages to queue: $QUEUE_NAME"
echo "============================================================"

# Send email notification
echo ""
echo "1. Sending email notification..."
EMAIL_MESSAGE=$(cat TestMessages/email-test.json)
az storage message put \
    --queue-name "$QUEUE_NAME" \
    --content "$EMAIL_MESSAGE" \
    --connection-string "$CONNECTION_STRING" \
    --output none

if [ $? -eq 0 ]; then
    echo "✓ Email notification sent successfully"
else
    echo "✗ Failed to send email notification"
fi

# Send SMS notification
echo ""
echo "2. Sending SMS notification..."
SMS_MESSAGE=$(cat TestMessages/sms-test.json)
az storage message put \
    --queue-name "$QUEUE_NAME" \
    --content "$SMS_MESSAGE" \
    --connection-string "$CONNECTION_STRING" \
    --output none

if [ $? -eq 0 ]; then
    echo "✓ SMS notification sent successfully"
else
    echo "✗ Failed to send SMS notification"
fi

echo ""
echo "============================================================"
echo "✓ All test messages sent!"
echo ""
echo "Check your Azure Function logs to see the processing results."
