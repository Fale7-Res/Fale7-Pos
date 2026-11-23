namespace Falah.Server;

using System.Collections.Concurrent;
using System.Reflection;
using Falah.Domain.Common;

/// <summary>
/// Independent audit 2026-11-23, F-04: one check, for every write, that its request id is a real key.
///
/// Every command replays by its request id before it validates anything, so the id is what makes a retry
/// return the original result. An empty one makes two DIFFERENT operations the "same" one: the second
/// "replays" the first and is never recorded. QA-03 fixed that inside four handlers; the other writes kept
/// it. Checked here instead, once, so a new endpoint cannot forget it.
///
/// Rule, read from the request record itself: a <c>string ...RequestId</c> property is required - missing,
/// blank or longer than <see cref="InputRules.MaxRequestIdLength"/> is refused. A <c>string? ...RequestId</c>
/// is optional - it may be left out, but a blank or over-long one is refused all the same.
/// </summary>
public sealed class RequestIdFilter : IEndpointFilter
{
    private static readonly ConcurrentDictionary<Type, (PropertyInfo Property, bool Required)[]> Shapes = new();
    private static readonly NullabilityInfoContext Nullability = new();

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is null) continue;
            foreach (var (property, required) in ShapeOf(argument.GetType()))
            {
                var value = property.GetValue(argument) as string;
                var missing = value is null ? required : string.IsNullOrWhiteSpace(value);
                if (missing || value?.Length > InputRules.MaxRequestIdLength)
                    return Results.UnprocessableEntity(new { error = "رقم العملية مطلوب وصالح. حدّث البرنامج وأعد المحاولة." });
            }
        }
        return await next(context);
    }

    private static (PropertyInfo, bool)[] ShapeOf(Type type) => Shapes.GetOrAdd(type, t =>
        t.Namespace?.StartsWith("Falah.Application", StringComparison.Ordinal) != true
            ? Array.Empty<(PropertyInfo, bool)>()
            : t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.Name.EndsWith("RequestId", StringComparison.Ordinal))
                .Select(p =>
                {
                    lock (Nullability) return (p, Nullability.Create(p).ReadState != NullabilityState.Nullable);
                })
                .ToArray());
}
