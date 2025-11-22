# Dependency Injection Lifetime Fix

## Issue

The application failed to start with the following error:

```
System.InvalidOperationException: Cannot consume scoped service 'StockSensePro.Core.Interfaces.IStockDataProviderFactory' 
from singleton 'StockSensePro.Core.Interfaces.IProviderHealthMonitor'.
```

## Root Cause

There was a **service lifetime mismatch** in the dependency injection configuration:

- `IProviderHealthMonitor` was registered as **Singleton**
- `IStockDataProviderFactory` was registered as **Scoped**

In .NET dependency injection, a singleton service cannot depend on a scoped service because:
- Singletons live for the entire application lifetime
- Scoped services live only for the duration of a request
- This would cause the singleton to hold a reference to a disposed scoped service

## Solution

Changed `IStockDataProviderFactory` from **Scoped** to **Singleton** registration.

### Why This Is Safe

The `StockDataProviderFactory` can safely be a singleton because:

1. **Stateless Design**: The factory doesn't maintain any request-specific state
2. **Service Locator Pattern**: It uses `IServiceProvider` to resolve providers on-demand
3. **Creates Scoped Instances**: When `CreateProvider()` is called, it resolves scoped provider instances from the service provider
4. **Thread-Safe**: The factory operations are thread-safe

### Code Changes

**File**: `backend/src/StockSensePro.API/Program.cs`

**Before**:
```csharp
// Register provider factory for multi-provider support
builder.Services.AddScoped<IStockDataProviderFactory, StockDataProviderFactory>();
```

**After**:
```csharp
// Register provider factory for multi-provider support as singleton
// Factory creates scoped providers on demand, so it can be singleton
builder.Services.AddSingleton<IStockDataProviderFactory, StockDataProviderFactory>();
```

### Additional Fixes

Also updated `StockDataProviderFactory` to support Alpha Vantage provider:

**File**: `backend/src/StockSensePro.Infrastructure/Services/StockDataProviderFactory.cs`

1. Added Alpha Vantage to the provider switch statement
2. Updated `GetAvailableProviders()` to include all providers

## Verification

After the fix:
- ✅ Application starts successfully
- ✅ No DI validation errors
- ✅ Health endpoint responds correctly
- ✅ All three providers available (YahooFinance, AlphaVantage, Mock)

## Service Lifetime Guidelines

For future reference, here are the .NET DI lifetime rules:

| Service Lifetime | Can Depend On |
|------------------|---------------|
| **Transient** | Transient, Scoped, Singleton |
| **Scoped** | Scoped, Singleton |
| **Singleton** | Singleton only |

**Key Rule**: A service can only depend on services with equal or longer lifetimes.

## Related Documentation

- [Microsoft Docs: Dependency Injection Lifetimes](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-lifetimes)
- [Deployment Guide](./DEPLOYMENT_GUIDE.md)
- [Developer Guide](./ALPHA_VANTAGE_DEVELOPER_GUIDE.md)

---

**Fixed**: November 22, 2025  
**Version**: 1.0

