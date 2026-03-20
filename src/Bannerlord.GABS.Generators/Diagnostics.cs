using Microsoft.CodeAnalysis;

namespace Lib.GAB.Generators
{
    internal static class Diagnostics
    {
        public static readonly DiagnosticDescriptor MultipleAnonymousTypeShapes = new DiagnosticDescriptor(
            id: "GABSG001",
            title: "Multiple anonymous type shapes in tool method",
            messageFormat: "Tool method '{0}' has {1} distinct anonymous return type shapes. Use a single anonymous type for success responses (error DTOs are excluded), or refactor to a named DTO.",
            category: "Lib.GAB.Tools",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DictionaryInToolResponse = new DiagnosticDescriptor(
            id: "GABSG003",
            title: "Dictionary type used in tool response",
            messageFormat: "Anonymous type member '{0}' in tool '{1}' uses a dictionary type. Dictionary keys are runtime values and cannot be represented in a compile-time schema. Use an array of objects instead (e.g. new {{ name = key, value = val }}).",
            category: "Lib.GAB.Tools",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingDocComment = new DiagnosticDescriptor(
            id: "GABSG002",
            title: "Anonymous type member missing doc comment",
            messageFormat: "Anonymous type member '{0}' in tool '{1}' has no /// doc comment. Add one for schema description.",
            category: "Lib.GAB.Tools",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);
    }
}