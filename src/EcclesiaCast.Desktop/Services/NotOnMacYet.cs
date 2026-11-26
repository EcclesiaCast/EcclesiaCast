using System.Reflection;
using EcclesiaCast.Presentation.Services;
using Serilog;

namespace EcclesiaCast.Desktop.Services;

/// <summary>
/// The one stand-in for the dialogs phase 3 has not made yet (the song
/// editor, the theme manager, the backup, the importers… some forty of them).
/// Any of their interfaces is served by this same class: when the operator
/// opens one it says «Esto todavía no está en la versión de Mac» and answers
/// as a dialog closed without choosing anything (no song, no file, false,
/// cancel), so the program carries on as if the operator had cancelled.
///
/// <see cref="Silent{T}"/> serves the services of functions that do not exist
/// in the Avalonia program and are never offered there (the updates,
/// «Seguir la canción», capturing a window): the same answers, without the
/// notice, only a line in the log.
/// </summary>
public class NotOnMacYet : DispatchProxy
{
    public const string Message = "Esto todavía no está en la versión de Mac.";

    private string _name = string.Empty;
    private bool _silent;

    /// <summary>The stand-in for the dialog <typeparamref name="T"/>.</summary>
    public static T For<T>() where T : class => Create<T>(silent: false);

    /// <summary>The stand-in for a service that is never offered on a Mac.</summary>
    public static T Silent<T>() where T : class => Create<T>(silent: true);

    private static T Create<T>(bool silent) where T : class
    {
        var proxy = Create<T, NotOnMacYet>();
        var self = (NotOnMacYet)(object)proxy;
        self._name = typeof(T).Name;
        self._silent = silent;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod!;

        // Reading a property, or hearing an event, is not opening anything.
        if (!method.IsSpecialName)
        {
            Log.Information("Todavía no está en Mac: {Service}.{Method}", _name, method.Name);
            if (!_silent)
                Notice.Show(Message, "Todavía no", NoticeKind.Info);
        }

        return Nothing(method.ReturnType);
    }

    /// <summary>What a dialog closed without choosing answers.</summary>
    internal static object? Nothing(Type type)
    {
        if (type == typeof(void))
            return null;
        if (type == typeof(string))
            return null;
        // A choice among options: none (-1), never the first one.
        if (type == typeof(int))
            return -1;
        if (type == typeof(Task))
            return Task.CompletedTask;
        if (type.IsArray)
            return Array.CreateInstance(type.GetElementType()!, 0);
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();
            if (definition == typeof(Task<>))
            {
                return typeof(Task).GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(arguments[0]).Invoke(null, [Nothing(arguments[0])]);
            }

            if (definition == typeof(IReadOnlyList<>) || definition == typeof(IEnumerable<>)
                || definition == typeof(IReadOnlyCollection<>) || definition == typeof(IList<>)
                || definition == typeof(ICollection<>))
                return Array.CreateInstance(arguments[0], 0);

            // (int Index, bool Repeat) and the like: each part as nothing.
            if (type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true)
                return Activator.CreateInstance(type, arguments.Select(Nothing).ToArray());
        }

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
