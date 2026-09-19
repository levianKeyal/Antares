using System;
using System.Threading.Tasks;

public static class StudentData
{
    private static StudentDataSessionService sessionService;

    public static bool IsReady =>
        sessionService != null
        && sessionService.IsDataReady
        && sessionService.RuntimeStore != null;

    public static void Initialize(StudentDataSessionService service)
    {
        sessionService = service;
    }

    public static bool TryGet<T>(StudentDataDefinition definition, out T value)
    {
        StudentDataRuntimeStore runtimeStore;
        if (!TryGetReadyStore(out runtimeStore))
        {
            value = default(T);
            return false;
        }

        return runtimeStore.TryGetValue(definition, out value);
    }

    public static T Get<T>(StudentDataDefinition definition)
    {
        T value;
        if (!IsReady)
        {
            throw new InvalidOperationException("Student data session is not ready.");
        }

        if (!TryGet(definition, out value))
        {
            throw new InvalidOperationException(
                $"Could not read student data definition '{GetDefinitionKey(definition)}'."
            );
        }

        return value;
    }

    public static bool Set(StudentDataDefinition definition, object value)
    {
        StudentDataRuntimeStore runtimeStore;
        return TryGetReadyStore(out runtimeStore)
            && runtimeStore.TrySet(definition, value, out _);
    }

    public static bool Increment(StudentDataDefinition definition, object amount)
    {
        StudentDataRuntimeStore runtimeStore;
        return TryGetReadyStore(out runtimeStore)
            && runtimeStore.TryIncrement(definition, amount, out _);
    }

    public static bool Maximum(StudentDataDefinition definition, object value)
    {
        StudentDataRuntimeStore runtimeStore;
        return TryGetReadyStore(out runtimeStore)
            && runtimeStore.TryMaximum(definition, value, out _);
    }

    public static bool Minimum(StudentDataDefinition definition, object value)
    {
        StudentDataRuntimeStore runtimeStore;
        return TryGetReadyStore(out runtimeStore)
            && runtimeStore.TryMinimum(definition, value, out _);
    }

    public static bool ResetToDefault(StudentDataDefinition definition)
    {
        StudentDataRuntimeStore runtimeStore;
        return TryGetReadyStore(out runtimeStore)
            && runtimeStore.TryResetToDefault(definition, out _);
    }

    public static Task<bool> FlushAsync()
    {
        if (sessionService == null || !IsReady)
        {
            return Task.FromResult(false);
        }

        return sessionService.FlushAsync();
    }

    private static bool TryGetReadyStore(out StudentDataRuntimeStore runtimeStore)
    {
        runtimeStore = IsReady ? sessionService.RuntimeStore : null;
        return runtimeStore != null;
    }

    private static string GetDefinitionKey(StudentDataDefinition definition)
    {
        return definition == null ? "<null>" : definition.Key;
    }
}
