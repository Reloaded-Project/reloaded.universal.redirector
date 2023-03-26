using System.Diagnostics.CodeAnalysis;
using Reloaded.Hooks.Definitions;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Reloaded.Universal.Redirector.Lib.Utility;

/// <summary>
/// Wrapper for <see cref="IHook{TFunction}"/> to introduce devirtualisation.
/// </summary>
/// <typeparam name="T"></typeparam>
public class AHook<[DynamicallyAccessedMembers(PublicParameterlessConstructor | PublicMethods | NonPublicMethods | PublicFields | PublicNestedTypes)]T> : IHook<T>
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

    /// <inheritdoc />
    public IHook<T> Activate() => _child.Activate();

    /// <summary/>
    public void Disable() => _child.Disable();

    /// <summary/>
    public void Enable() => _child.Enable();

    /// <inheritdoc />
    public T OriginalFunction => _child.OriginalFunction;

    /// <inheritdoc />
    public IReverseWrapper<T> ReverseWrapper => _child.ReverseWrapper;

    /// <inheritdoc />
    bool IHook<T>.IsHookEnabled => _child.IsHookEnabled;

    /// <inheritdoc />
    bool IHook<T>.IsHookActivated => _child.IsHookActivated;

    /// <inheritdoc />
    nint IHook<T>.OriginalFunctionAddress => _child.OriginalFunctionAddress;

    /// <inheritdoc />
    nint IHook<T>.OriginalFunctionWrapperAddress => _child.OriginalFunctionWrapperAddress;
    
    /// <inheritdoc />
    bool IHook.IsHookEnabled => _child.IsHookEnabled;

    /// <inheritdoc />
    bool IHook.IsHookActivated => _child.IsHookActivated;

    /// <inheritdoc />
    nint IHook.OriginalFunctionAddress => _child.OriginalFunctionAddress;

    /// <inheritdoc />
    nint IHook.OriginalFunctionWrapperAddress => _child.OriginalFunctionWrapperAddress;
}

/// <summary>
/// Utility functions tied to AHook.
/// </summary>
public static class AHookExtensions
{
    /// <summary>
    /// Converts from <see cref="IHook{TFunction}"/> to <see cref="AHook{T}"/>
    /// </summary>
    public static AHook<T> ToAHook<[DynamicallyAccessedMembers(PublicParameterlessConstructor | PublicMethods | NonPublicMethods | PublicFields | PublicNestedTypes)]T>(this IHook<T> hook) => new(hook);

    /// <summary>
    /// Activates an <see cref="IHook{TFunction}"/> as an <see cref="AHook{T}"/>.
    /// </summary>
    public static AHook<T> ActivateAHook<[DynamicallyAccessedMembers(PublicParameterlessConstructor | PublicMethods | NonPublicMethods | PublicFields | PublicNestedTypes)]T>(this IHook<T> hook) => hook.Activate().ToAHook();
}