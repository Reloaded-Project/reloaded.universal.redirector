using System.Diagnostics.CodeAnalysis;
using Reloaded.Hooks.Definitions;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Reloaded.Universal.Redirector.Lib.Utility;

/// <summary>
/// Wrapper for <see cref="IHook{TFunction}"/> to introduce devirtualisation.
/// </summary>
/// <typeparam name="T"></typeparam>
[DynamicallyAccessedMembers(All)]
public struct AHook<[DynamicallyAccessedMembers(All)]T>
{
    private readonly IHook<T> _child;

    /// <summary>
    /// See <see cref="OriginalFunction"/>. Alias for performance.
    /// </summary>
    public T Original;

    /// <summary/>
    public AHook(IHook<T> child)
    {
        _child = child;
        Original = OriginalFunction;
    }

    /// <inheritdoc cref="IHook{T}.Activate"/>
    public IHook<T> Activate() => _child.Activate();

    /// <summary/>
    public void Disable() => _child.Disable();

    /// <summary/>
    public void Enable() => _child.Enable();

    /// <inheritdoc cref="IHook{T}.ReverseWrapper"/>
    public T OriginalFunction => _child.OriginalFunction;

    /// <inheritdoc cref="IHook{T}.ReverseWrapper"/>
    public IReverseWrapper<T> ReverseWrapper => _child.ReverseWrapper;

    /// <inheritdoc cref="IHook.IsHookEnabled"/>
    bool IsHookEnabled => _child.IsHookEnabled;

    /// <inheritdoc cref="IHook.IsHookActivated"/>
    bool IsHookActivated => _child.IsHookActivated;

    /// <inheritdoc cref="IHook.OriginalFunctionAddress"/>
    nint OriginalFunctionAddress => _child.OriginalFunctionAddress;

    /// <inheritdoc cref="IHook.OriginalFunctionWrapperAddress"/>
    nint OriginalFunctionWrapperAddress => _child.OriginalFunctionWrapperAddress;
}

/// <summary>
/// Utility functions tied to AHook.
/// </summary>
public static class AHookExtensions
{
    /// <summary>
    /// Converts from <see cref="IHook{TFunction}"/> to <see cref="AHook{T}"/>
    /// </summary>
    public static AHook<T> ToAHook<[DynamicallyAccessedMembers(All)]T>(this IHook<T> hook) => new(hook);

    /// <summary>
    /// Activates an <see cref="IHook{TFunction}"/> as an <see cref="AHook{T}"/>.
    /// </summary>
    public static AHook<T> ActivateAHook<[DynamicallyAccessedMembers(All)]T>(this IHook<T> hook) => hook.Activate().ToAHook();
}