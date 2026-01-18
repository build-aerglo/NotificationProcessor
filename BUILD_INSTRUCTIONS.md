# Build Instructions

## Fixing Package Restore Errors

The build errors you're seeing are due to missing NuGet package references. All packages are already defined in the `.csproj` files, but need to be restored.

### Step 1: Clean the Solution

```bash
# Remove all bin and obj folders to clear cached builds
dotnet clean
```

### Step 2: Restore NuGet Packages

```bash
# Restore all NuGet packages
dotnet restore
```

### Step 3: Build the Solution

```bash
# Build the entire solution
dotnet build
```

## Required Packages (Already Configured)

### NotificationProcessor.Infrastructure
- ✅ Npgsql 8.0.5 (PostgreSQL driver)
- ✅ Twilio 7.5.3 (SMS service)
- ✅ Azure.Storage.Queues 12.21.0

### NotificationProcessor.Functions
- ✅ Microsoft.Azure.Functions.Worker 1.23.0
- ✅ Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues 5.5.3
- ✅ Microsoft.Azure.Functions.Worker.Sdk 1.17.4

## If Problems Persist

### Clear NuGet Cache

```bash
# Clear the NuGet cache completely
dotnet nuget locals all --clear

# Restore packages again
dotnet restore
```

### Check NuGet Sources

```bash
# List configured NuGet sources
dotnet nuget list source

# Add nuget.org if missing
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

### Verify SDK Version

```bash
# Check your .NET SDK version (should be 8.0 or higher)
dotnet --version
```

## Running Tests

After a successful build:

```bash
# Run all tests
dotnet test
```

## Running the Function App Locally

```bash
# Navigate to the Functions project
cd src/NotificationProcessor.Functions

# Start the Function App
func start
```

## Troubleshooting

If you continue to see "Cannot resolve symbol" errors in your IDE (Rider):

1. **Invalidate Caches**: In Rider, go to `File` → `Invalidate Caches` → Select all options → Click "Invalidate and Restart"

2. **Reload Projects**: Right-click on the solution → `Reload All Projects`

3. **Restore NuGet Packages in Rider**: Right-click on solution → `Restore NuGet Packages`
