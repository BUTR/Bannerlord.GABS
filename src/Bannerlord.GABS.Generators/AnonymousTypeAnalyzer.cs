using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lib.GAB.Generators
{
    /// <summary>
    /// Extracts schema information from anonymous type expressions in tool methods.
    /// </summary>
    internal static class AnonymousTypeAnalyzer
    {
        internal const string ToolAttributeFullName = "Lib.GAB.Tools.ToolAttribute";
        internal const string ToolResponseAttributeFullName = "Lib.GAB.Tools.ToolResponseAttribute";

        /// <summary>
        /// Guards against infinite recursion when following method delegation chains.
        /// Tracks method symbols currently being analyzed to break cycles like A → B → A.
        /// </summary>
        [ThreadStatic]
        private static HashSet<string>? _visitedMethods;

        /// <summary>
        /// Represents a single field in an extracted anonymous type schema.
        /// </summary>
        internal sealed class FieldSchema : IEquatable<FieldSchema>
        {
            public string Name { get; }
            public string JsonType { get; }
            public bool Nullable { get; }
            public string? Description { get; }
            public List<FieldSchema>? Items { get; }

            public FieldSchema(string name, string jsonType, bool nullable = false, string? description = null, List<FieldSchema>? items = null)
            {
                Name = name;
                JsonType = jsonType;
                Nullable = nullable;
                Description = description;
                Items = items;
            }

            public bool Equals(FieldSchema? other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return Name == other.Name
                    && JsonType == other.JsonType
                    && Nullable == other.Nullable
                    && Description == other.Description
                    && FieldListEquals(Items, other.Items);
            }

            public override bool Equals(object? obj) => Equals(obj as FieldSchema);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = Name.GetHashCode();
                    hash = hash * 31 + JsonType.GetHashCode();
                    hash = hash * 31 + Nullable.GetHashCode();
                    hash = hash * 31 + (Description?.GetHashCode() ?? 0);
                    if (Items != null)
                    {
                        foreach (var item in Items)
                            hash = hash * 31 + item.GetHashCode();
                    }
                    return hash;
                }
            }
        }

        /// <summary>
        /// Lightweight diagnostic data that does not retain references to SyntaxTree / Location,
        /// allowing the incremental generator pipeline to release old compilations.
        /// </summary>
        internal sealed class DiagnosticInfo
        {
            public DiagnosticDescriptor Descriptor { get; }
            public string FilePath { get; }
            public TextSpan Span { get; }
            public LinePositionSpan LineSpan { get; }
            public object[] MessageArgs { get; }

            public DiagnosticInfo(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
            {
                Descriptor = descriptor;
                MessageArgs = messageArgs;

                var mappedSpan = location.GetMappedLineSpan();
                FilePath = mappedSpan.Path ?? "";
                Span = location.SourceSpan;
                LineSpan = mappedSpan.Span;
            }

            public Diagnostic ToDiagnostic()
            {
                var location = Location.Create(FilePath, Span, LineSpan);
                return Diagnostic.Create(Descriptor, location, MessageArgs);
            }
        }

        /// <summary>
        /// Represents the extracted schema for a tool method, or diagnostics if invalid.
        /// Implements IEquatable so the incremental generator pipeline can detect unchanged results.
        /// Diagnostics are excluded from equality — they are transient and always re-reported.
        /// </summary>
        internal sealed class ToolSchema : IEquatable<ToolSchema>
        {
            public string ToolName { get; internal set; } = "";
            public string Namespace { get; internal set; } = "";
            public string ClassName { get; internal set; } = "";
            public string MethodName { get; internal set; } = "";
            public string ReturnType { get; internal set; } = "";
            public string Parameters { get; internal set; } = "";
            public string Modifiers { get; internal set; } = "";
            public List<FieldSchema>? Fields { get; internal set; }
            public List<string> UsingNamespaces { get; internal set; } = new List<string>();
            /// <summary>
            /// Mutable accumulator for diagnostics. Excluded from equality — diagnostics are
            /// transient and always re-reported by the incremental pipeline.
            /// </summary>
            public List<DiagnosticInfo> Diagnostics { get; } = new List<DiagnosticInfo>();

            public bool Equals(ToolSchema? other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return ToolName == other.ToolName
                    && Namespace == other.Namespace
                    && ClassName == other.ClassName
                    && MethodName == other.MethodName
                    && ReturnType == other.ReturnType
                    && Parameters == other.Parameters
                    && Modifiers == other.Modifiers
                    && FieldListEquals(Fields, other.Fields)
                    && StringListEquals(UsingNamespaces, other.UsingNamespaces);
            }

            public override bool Equals(object? obj) => Equals(obj as ToolSchema);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = ToolName.GetHashCode();
                    hash = hash * 31 + Namespace.GetHashCode();
                    hash = hash * 31 + ClassName.GetHashCode();
                    hash = hash * 31 + MethodName.GetHashCode();
                    hash = hash * 31 + ReturnType.GetHashCode();
                    hash = hash * 31 + Parameters.GetHashCode();
                    hash = hash * 31 + Modifiers.GetHashCode();
                    if (Fields != null)
                    {
                        foreach (var field in Fields)
                            hash = hash * 31 + field.GetHashCode();
                    }
                    foreach (var ns in UsingNamespaces)
                        hash = hash * 31 + ns.GetHashCode();
                    return hash;
                }
            }
        }

        private static bool FieldListEquals(List<FieldSchema>? a, List<FieldSchema>? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i])) return false;
            }
            return true;
        }

        private static bool StringListEquals(List<string>? a, List<string>? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Analyzes a [Tool]-attributed method and extracts its anonymous type schema.
        /// </summary>
        public static ToolSchema Analyze(MethodDeclarationSyntax method, SemanticModel semanticModel)
        {
            var toolName = GetToolName(method, semanticModel);
            var result = new ToolSchema { ToolName = toolName };

            if (string.IsNullOrEmpty(toolName))
                return result;

            // Extract method and class info for partial declaration generation
            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            if (methodSymbol != null)
            {
                result.MethodName = methodSymbol.Name;
                result.ReturnType = method.ReturnType.ToString();
                result.Parameters = BuildStrippedParameterList(method.ParameterList);
                result.ClassName = methodSymbol.ContainingType?.Name ?? "";
                result.Namespace = methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString() ?? "";
                // Preserve modifiers but exclude 'partial' (we'll add it ourselves)
                result.Modifiers = string.Join(" ", method.Modifiers.Select(m => m.Text).Where(m => m != "partial"));
                // Collect namespaces from parameter types and return type for using directives
                result.UsingNamespaces = CollectTypeNamespaces(methodSymbol);
            }

            // Find all anonymous object creations in the method body (including lambdas)
            var body = (SyntaxNode?) method.Body ?? method.ExpressionBody;
            if (body == null)
                return result;

            var allAnonymousList = body.DescendantNodes()
                .OfType<AnonymousObjectCreationExpressionSyntax>()
                .ToList();

            // Scan anonymous type members for dictionary-typed expressions
            ReportDictionaryUsages(allAnonymousList, toolName, semanticModel, result.Diagnostics);

            if (allAnonymousList.Count == 0)
            {
                // No anonymous types — check if non-error returns call a method with [ToolResponse] attributes
                result.Fields = TryExtractFromDelegatedMethod(body, semanticModel);
                return result;
            }

            // Classify each anonymous type
            var allAnonymousSet = new HashSet<AnonymousObjectCreationExpressionSyntax>(allAnonymousList);
            var topLevel = new List<AnonymousObjectCreationExpressionSyntax>();

            foreach (var anon in allAnonymousList)
            {
                if (IsErrorDto(anon))
                    continue;

                if (IsNestedAnonymousType(anon, allAnonymousSet))
                    continue;

                topLevel.Add(anon);
            }

            if (topLevel.Count == 0)
            {
                // Only error DTOs found — check if non-error returns call a method with [ToolResponse]
                result.Fields = TryExtractFromDelegatedMethod(body, semanticModel);
                return result;
            }

            // Deduplicate by property name+type set
            var distinct = DeduplicateByShape(topLevel, semanticModel);

            if (distinct.Count > 1)
            {
                result.Diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.MultipleAnonymousTypeShapes,
                    method.Identifier.GetLocation(),
                    method.Identifier.Text,
                    distinct.Count));
                return result;
            }

            // Extract schema from the single canonical anonymous type
            var canonical = distinct[0];
            result.Fields = ExtractFields(canonical, toolName, semanticModel, result.Diagnostics);

            return result;
        }

        private static string GetToolName(MethodDeclarationSyntax method, SemanticModel semanticModel)
        {
            foreach (var attrList in method.AttributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(attr);
                    var attrSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
                    if (attrSymbol == null) continue;

                    var containingType = attrSymbol.ContainingType?.ToDisplayString();
                    if (containingType != ToolAttributeFullName) continue;

                    // Get the first constructor argument (tool name)
                    if (attr.ArgumentList?.Arguments.Count > 0)
                    {
                        var arg = attr.ArgumentList.Arguments[0];
                        var constValue = semanticModel.GetConstantValue(arg.Expression);
                        if (constValue.HasValue && constValue.Value is string name)
                            return name;
                    }
                }
            }
            return "";
        }

        /// <summary>
        /// Checks if an anonymous type is the error DTO pattern: new { error = "..." }
        /// A single property named "error" of type string.
        /// </summary>
        private static bool IsErrorDto(AnonymousObjectCreationExpressionSyntax anon)
        {
            var members = anon.Initializers;
            if (members.Count != 1) return false;

            var member = members[0];
            var nameToken = GetMemberName(member);
            return nameToken == "error";
        }

        /// <summary>
        /// Checks if an anonymous type is nested (i.e., it's an array element schema,
        /// not a top-level return). Covers:
        ///  - Inside a LINQ .Select() lambda
        ///  - Inside another anonymous object creation
        ///  - Passed as an argument to a method call (e.g. list.Add(new { ... }))
        ///  - Nested inside a ternary conditional used as a member initializer
        /// </summary>
        private static bool IsNestedAnonymousType(
            AnonymousObjectCreationExpressionSyntax anon,
            HashSet<AnonymousObjectCreationExpressionSyntax> allAnonymous)
        {
            // Track whether we've crossed a lambda boundary
            bool insideLambda = false;

            // Walk up the syntax tree
            var current = anon.Parent;
            while (current != null)
            {
                // Detect lambda boundaries — once we cross one, the anonymous type
                // is a return value from the lambda, not a direct argument/initializer
                if (current is AnonymousFunctionExpressionSyntax)
                {
                    insideLambda = true;
                }

                // If we hit another anonymous object creation that's a parent, this is nested
                if (current is AnonymousObjectCreationExpressionSyntax parentAnon && allAnonymous.Contains(parentAnon))
                    return true;

                // Argument passed to a method call — check whether it's a collection
                // builder call (Select/Add → always nested) or any other call
                // (nested only when not across a lambda boundary).
                if (current is ArgumentSyntax &&
                    current.Parent is ArgumentListSyntax parentArgList &&
                    parentArgList.Parent is InvocationExpressionSyntax invocation)
                {
                    string? methodName = null;
                    if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                        methodName = memberAccess.Name.Identifier.Text;
                    else if (invocation.Expression is MemberBindingExpressionSyntax memberBinding)
                        methodName = memberBinding.Name.Identifier.Text;

                    // Select/Add — always nested (even across lambdas)
                    if (methodName == "Select" || methodName == "Add")
                        return true;

                    // Any other method call — nested only outside lambdas
                    if (!insideLambda)
                        return true;
                }

                // Collection initializer: new List<T> { new { ... } }
                if (current is InitializerExpressionSyntax &&
                    current.Parent is ObjectCreationExpressionSyntax)
                {
                    return true;
                }

                // Local variable initializer (not across a lambda boundary)
                if (!insideLambda &&
                    current is EqualsValueClauseSyntax && current.Parent is VariableDeclaratorSyntax)
                {
                    return true;
                }

                current = current.Parent;
            }
            return false;
        }

        /// <summary>
        /// Deduplicates anonymous types by their property name sets.
        /// Types with the same set of property names are considered the same shape —
        /// this is intentionally name-only because the same method commonly returns
        /// an "inactive" variant (with null/default literals) and an "active" variant
        /// (with real values), which share the same field names but differ in resolved types.
        /// Among same-name-set candidates, prefers the variant with the most specific
        /// (non-"object") JSON types — "object" typically comes from null-literal
        /// expressions like (string?)null and is less informative.
        /// </summary>
        private static List<AnonymousObjectCreationExpressionSyntax> DeduplicateByShape(
            List<AnonymousObjectCreationExpressionSyntax> candidates,
            SemanticModel semanticModel)
        {
            var groups = new Dictionary<string, (AnonymousObjectCreationExpressionSyntax best, int objectCount)>();

            foreach (var anon in candidates)
            {
                var names = anon.Initializers
                    .Select(m => GetMemberName(m))
                    .OrderBy(n => n);
                var key = string.Join(",", names);

                // Count how many fields resolve to "object" (null-literal / unresolved)
                var objectCount = anon.Initializers.Count(m =>
                {
                    var typeInfo = semanticModel.GetTypeInfo(m.Expression);
                    var (jsonType, _) = TypeMapper.MapToJsonSchemaType(typeInfo.Type);
                    return jsonType == "object";
                });

                if (!groups.TryGetValue(key, out var existing) || objectCount < existing.objectCount)
                {
                    groups[key] = (anon, objectCount);
                }
            }

            return groups.Values.Select(g => g.best).ToList();
        }

        /// <summary>
        /// Extracts field schemas from a nested anonymous type (no diagnostics for missing doc comments).
        /// </summary>
        private static List<FieldSchema> ExtractNestedFields(
            AnonymousObjectCreationExpressionSyntax anon,
            SemanticModel semanticModel)
        {
            return ExtractFields(anon, null, semanticModel, null);
        }

        /// <summary>
        /// Extracts field schemas from an anonymous type, including doc comments.
        /// When toolName is null, missing-doc-comment diagnostics are suppressed (nested context).
        /// </summary>
        private static List<FieldSchema> ExtractFields(
            AnonymousObjectCreationExpressionSyntax anon,
            string? toolName,
            SemanticModel semanticModel,
            List<DiagnosticInfo>? diagnostics)
        {
            var fields = new List<FieldSchema>();

            foreach (var member in anon.Initializers)
            {
                var name = GetMemberName(member);
                var typeInfo = semanticModel.GetTypeInfo(member.Expression);
                var (jsonType, nullable) = TypeMapper.MapToJsonSchemaType(typeInfo.Type);

                // If the type couldn't be resolved but the expression is a ?. chain, mark as nullable string
                if (typeInfo.Type == null && HasNullConditionalAccess(member.Expression))
                {
                    jsonType = "string";
                    nullable = true;
                }
                // Also check for explicit (string?)null casts
                else if (typeInfo.ConvertedType?.SpecialType == SpecialType.System_String &&
                         typeInfo.Type?.SpecialType != SpecialType.System_String)
                {
                    jsonType = "string";
                    nullable = true;
                }
                // If type resolved to 'object' but expression ends with ?.ToString(), it's a nullable string
                else if (jsonType == "object" && ExpressionEndsWithToString(member.Expression))
                {
                    jsonType = "string";
                    nullable = true;
                }
                // If type resolved to 'object' but expression has ?. chain, likely nullable string
                else if (jsonType == "object" && HasNullConditionalAccess(member.Expression))
                {
                    jsonType = "string";
                    nullable = true;
                }

                var description = ExtractDocComment(member);

                if (description == null && diagnostics != null && toolName != null)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.MissingDocComment,
                        member.GetLocation(),
                        name,
                        toolName));
                }

                // Check for nested anonymous types — both array element schemas and direct object nesting
                List<FieldSchema>? items;

                // Direct anonymous object: field = new { a = 1, b = 2 }
                if (jsonType == "object" && TryResolveAnonymousObject(member.Expression, semanticModel) is { } nestedAnon)
                {
                    items = ExtractNestedFields(nestedAnon, semanticModel);
                }
                else
                {
                    // Array element schemas (Select projections, DTOs, etc.)
                    items = TryExtractNestedAnonymousType(member.Expression, semanticModel);
                    if (items != null && items.Count > 0)
                        jsonType = "array";
                }

                fields.Add(new FieldSchema(name, jsonType, nullable, description, items));
            }

            return fields;
        }

        /// <summary>
        /// Checks anonymous type members for dictionary-typed expressions, reporting GABSG003
        /// for each. Only flags dictionaries that flow into the schema (anonymous type fields),
        /// not dictionaries used purely for internal computation.
        /// </summary>
        private static void ReportDictionaryUsages(
            List<AnonymousObjectCreationExpressionSyntax> anonymousTypes,
            string toolName,
            SemanticModel semanticModel,
            List<DiagnosticInfo> diagnostics)
        {
            foreach (var anon in anonymousTypes)
            {
                foreach (var member in anon.Initializers)
                {
                    var typeInfo = semanticModel.GetTypeInfo(member.Expression);
                    var type = typeInfo.Type ?? typeInfo.ConvertedType;
                    if (type != null && TypeMapper.IsDictionaryType(type))
                    {
                        var name = GetMemberName(member);
                        diagnostics.Add(new DiagnosticInfo(
                            Diagnostics.DictionaryInToolResponse,
                            member.GetLocation(),
                            name,
                            toolName));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the property name from an anonymous type member.
        /// Handles both explicit (name = expr) and implicit (name) forms.
        /// </summary>
        private static string GetMemberName(AnonymousObjectMemberDeclaratorSyntax member)
        {
            if (member.NameEquals != null)
                return member.NameEquals.Name.Identifier.Text;

            // Implicit name — inferred from expression
            if (member.Expression is IdentifierNameSyntax identifier)
                return identifier.Identifier.Text;

            if (member.Expression is MemberAccessExpressionSyntax memberAccess)
                return memberAccess.Name.Identifier.Text;

            return member.Expression.ToString();
        }

        /// <summary>
        /// Extracts a /// doc comment from the leading trivia of an anonymous type member.
        /// </summary>
        private static string? ExtractDocComment(AnonymousObjectMemberDeclaratorSyntax member)
        {
            // Check leading trivia on the member itself
            var trivia = member.GetLeadingTrivia();

            foreach (var t in trivia)
            {
                // Roslyn may parse /// on anonymous members as SingleLineCommentTrivia
                if (t.IsKind(SyntaxKind.SingleLineCommentTrivia))
                {
                    var text = t.ToString().TrimStart();
                    if (text.StartsWith("///"))
                    {
                        return text.Substring(3).Trim();
                    }
                }

                // Or as proper doc comment trivia
                if (t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    var structure = t.GetStructure();
                    if (structure is DocumentationCommentTriviaSyntax docComment)
                    {
                        var text = docComment.Content
                            .OfType<XmlTextSyntax>()
                            .SelectMany(x => x.TextTokens)
                            .Select(token => token.Text.Trim())
                            .Where(s => !string.IsNullOrEmpty(s));
                        var joined = string.Join(" ", text);
                        if (!string.IsNullOrEmpty(joined))
                            return joined;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Builds a parameter list string without attributes to avoid duplication
        /// in partial method declarations.
        /// </summary>
        private static string BuildStrippedParameterList(ParameterListSyntax parameterList)
        {
            if (parameterList.Parameters.Count == 0)
                return "()";

            var parts = new List<string>();
            foreach (var param in parameterList.Parameters)
            {
                // Rebuild parameter without attribute lists
                var stripped = param.WithAttributeLists(new SyntaxList<AttributeListSyntax>());
                parts.Add(stripped.ToFullString().Trim());
            }
            return "(" + string.Join(", ", parts) + ")";
        }

        /// <summary>
        /// Collects distinct non-System namespaces from a method's return type and parameter types.
        /// These are emitted as using directives in the generated partial class file.
        /// </summary>
        private static List<string> CollectTypeNamespaces(IMethodSymbol method)
        {
            var namespaces = new HashSet<string>();

            CollectNamespacesFromType(method.ReturnType, namespaces);
            foreach (var param in method.Parameters)
            {
                CollectNamespacesFromType(param.Type, namespaces);
            }

            // Remove namespaces that are already emitted by the generator boilerplate
            namespaces.Remove("System.Threading.Tasks");
            namespaces.Remove("Lib.GAB.Tools");

            var sorted = namespaces.ToList();
            sorted.Sort(StringComparer.Ordinal);
            return sorted;
        }

        private static void CollectNamespacesFromType(ITypeSymbol type, HashSet<string> namespaces)
        {
            // Skip primitives and special types (they don't need using directives)
            if (type.SpecialType != SpecialType.None) return;

            // Unwrap generic type arguments (e.g., Task<object> → check Task's ns + object's ns)
            if (type is INamedTypeSymbol named && named.IsGenericType)
            {
                foreach (var arg in named.TypeArguments)
                    CollectNamespacesFromType(arg, namespaces);
            }

            // Unwrap arrays
            if (type is IArrayTypeSymbol array)
            {
                CollectNamespacesFromType(array.ElementType, namespaces);
                return;
            }

            var ns = type.ContainingNamespace;
            if (ns != null && !ns.IsGlobalNamespace)
            {
                var display = ns.ToDisplayString();
                // Skip the root "System" namespace (primitives), but keep sub-namespaces
                // like System.Collections.Generic that are needed for List<T>, etc.
                if (display != "System")
                    namespaces.Add(display);
            }
        }

        /// <summary>
        /// Tries to resolve an expression to an AnonymousObjectCreationExpressionSyntax.
        /// Handles direct expressions, variable references, ternaries, and casts.
        /// </summary>
        private static AnonymousObjectCreationExpressionSyntax? TryResolveAnonymousObject(
            ExpressionSyntax expression, SemanticModel semanticModel)
        {
            // Direct: new { ... }
            if (expression is AnonymousObjectCreationExpressionSyntax anon)
                return anon;

            // Unwrap interleaved casts and parens: (Type)((expr)) or ((Type)expr)
            var unwrapped = expression;
            bool changed = true;
            while (changed)
            {
                changed = false;
                if (unwrapped is CastExpressionSyntax cast) { unwrapped = cast.Expression; changed = true; }
                if (unwrapped is ParenthesizedExpressionSyntax paren) { unwrapped = paren.Expression; changed = true; }
            }
            if (unwrapped is AnonymousObjectCreationExpressionSyntax unwrappedAnon)
                return unwrappedAnon;

            // Ternary: condition ? new { ... } : new { ... }
            if (unwrapped is ConditionalExpressionSyntax conditional)
            {
                return TryResolveAnonymousObject(conditional.WhenTrue, semanticModel)
                    ?? TryResolveAnonymousObject(conditional.WhenFalse, semanticModel);
            }

            // Variable reference: resolve to initializer
            if (unwrapped is IdentifierNameSyntax identifier &&
                semanticModel.GetSymbolInfo(identifier).Symbol is ILocalSymbol local)
            {
                var declarator = local.DeclaringSyntaxReferences
                    .Select(r => r.GetSyntax())
                    .OfType<VariableDeclaratorSyntax>()
                    .FirstOrDefault();

                if (declarator?.Initializer?.Value != null)
                    return TryResolveAnonymousObject(declarator.Initializer.Value, semanticModel);
            }

            return null;
        }

        /// <summary>
        /// Tries to extract nested item schema from an expression that represents an array field.
        /// Dispatches to focused extraction strategies in priority order.
        /// </summary>
        private static List<FieldSchema>? TryExtractNestedAnonymousType(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            return TryExtractFromArrayInitializer(expression, semanticModel)
                ?? TryExtractFromSelectLambda(expression, semanticModel)
                ?? TryExtractFromConditional(expression, semanticModel)
                ?? TryExtractFromVariableReference(expression, semanticModel)
                ?? TryExtractFromUnwrappedType(expression, semanticModel);
        }

        /// <summary>
        /// Extracts schema from inline array initializers: new[] { new { ... }, new { ... } }
        /// Also handles explicit array creation: new object[] { new { ... } }
        /// </summary>
        private static List<FieldSchema>? TryExtractFromArrayInitializer(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            InitializerExpressionSyntax? initializer = null;

            // new[] { ... }
            if (expression is ImplicitArrayCreationExpressionSyntax implicitArray)
                initializer = implicitArray.Initializer;
            // new T[] { ... }
            else if (expression is ArrayCreationExpressionSyntax explicitArray)
                initializer = explicitArray.Initializer;

            if (initializer == null || initializer.Expressions.Count == 0)
                return null;

            // Find the first anonymous object in the initializer elements
            foreach (var elem in initializer.Expressions)
            {
                if (elem is AnonymousObjectCreationExpressionSyntax anon)
                    return ExtractNestedFields(anon, semanticModel);
            }

            return null;
        }

        /// <summary>
        /// Extracts schema from .Select() lambda projections.
        /// e.g., items.Select((b, i) => new { index = i, name = b.Name }).ToList()
        /// Only matches the outermost .Select() calls — nested .Select() inside another
        /// Select's lambda argument are skipped to avoid extracting inner schemas.
        /// </summary>
        private static List<FieldSchema>? TryExtractFromSelectLambda(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            // Find all .Select() invocations, then filter to only the outermost ones.
            var allSelectInvocations = expression.DescendantNodesAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .Where(inv => GetInvocationMethodName(inv) == "Select")
                .ToList();

            // Build a set for fast lookup, then keep only those not nested inside another Select's lambda
            var selectSet = new HashSet<InvocationExpressionSyntax>(allSelectInvocations);
            var selectInvocations = allSelectInvocations.Where(inv =>
            {
                // Walk ancestors up to the root expression; if we hit another Select's argument list, skip
                var current = inv.Parent;
                while (current != null && current != expression)
                {
                    if (current is ArgumentSyntax &&
                        current.Parent is ArgumentListSyntax argList &&
                        argList.Parent is InvocationExpressionSyntax parentInv &&
                        parentInv != inv &&
                        selectSet.Contains(parentInv))
                    {
                        return false;
                    }
                    current = current.Parent;
                }
                return true;
            });

            foreach (var selectInv in selectInvocations)
            {
                if (selectInv.ArgumentList.Arguments.Count == 0) continue;

                var lambdaArg = selectInv.ArgumentList.Arguments[0].Expression;

                // Find anonymous object creation inside the lambda
                var nestedAnon = lambdaArg.DescendantNodes()
                    .OfType<AnonymousObjectCreationExpressionSyntax>()
                    .FirstOrDefault();

                if (nestedAnon != null)
                {
                    return ExtractNestedFields(nestedAnon, semanticModel);
                }

                // Check if the argument is a method group reference: .Select(SerializeLogEntry)
                var methodGroupSymbol = semanticModel.GetSymbolInfo(lambdaArg).Symbol as IMethodSymbol
                    ?? semanticModel.GetSymbolInfo(lambdaArg).CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
                if (methodGroupSymbol != null)
                {
                    var fields = ExtractFieldsFromMethodAttributes(methodGroupSymbol);
                    if (fields != null) return fields;

                    fields = ExtractFieldsFromMethodBody(methodGroupSymbol, semanticModel);
                    if (fields != null) return fields;
                }

                // Check if the lambda calls a method with [ToolResponse] attributes
                // e.g., .Select(h => SerializeHero(h))
                var nestedInvocations = lambdaArg.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .ToList();

                foreach (var nestedInv in nestedInvocations)
                {
                    var calledMethod = ResolveMethodSymbol(nestedInv, expression, semanticModel);

                    if (calledMethod != null)
                    {
                        var fields = ExtractFieldsFromMethodAttributes(calledMethod);
                        if (fields != null) return fields;

                        fields = ExtractFieldsFromMethodBody(calledMethod, semanticModel);
                        if (fields != null) return fields;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Recurses into both branches of a ternary conditional expression.
        /// </summary>
        private static List<FieldSchema>? TryExtractFromConditional(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            if (expression is ConditionalExpressionSyntax conditional)
            {
                return TryExtractNestedAnonymousType(conditional.WhenTrue, semanticModel)
                    ?? TryExtractNestedAnonymousType(conditional.WhenFalse, semanticModel);
            }
            return null;
        }

        /// <summary>
        /// Resolves a variable identifier to its initializer and .Add() calls to extract schema.
        /// </summary>
        private static List<FieldSchema>? TryExtractFromVariableReference(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            if (!(expression is IdentifierNameSyntax identifier))
                return null;

            VariableDeclaratorSyntax? declarator = null;

            // Try semantic resolution first
            if (semanticModel.GetSymbolInfo(identifier).Symbol is ILocalSymbol local)
            {
                declarator = local.DeclaringSyntaxReferences
                    .Select(r => r.GetSyntax())
                    .OfType<VariableDeclaratorSyntax>()
                    .FirstOrDefault();
            }

            // Fallback: syntax-only search by name in ancestor blocks.
            // Walk from innermost to outermost scope and only match declarations
            // that appear before the identifier to avoid cross-scope name collisions.
            if (declarator == null)
            {
                var searchName = identifier.Identifier.Text;
                var identifierSpanStart = identifier.SpanStart;
                foreach (var block in identifier.Ancestors().OfType<BlockSyntax>())
                {
                    // Search direct child declarations in this block (not nested blocks)
                    declarator = block.Statements
                        .OfType<LocalDeclarationStatementSyntax>()
                        .SelectMany(s => s.Declaration.Variables)
                        .FirstOrDefault(d => d.Identifier.Text == searchName && d.SpanStart < identifierSpanStart);
                    if (declarator != null) break;
                }
            }

            if (declarator?.Initializer?.Value != null)
            {
                var fromInit = TryExtractNestedAnonymousType(declarator.Initializer.Value, semanticModel);
                if (fromInit != null) return fromInit;

                // Last resort: search the entire initializer subtree for anonymous objects
                // (handles cases where the chain is wrapped in ConditionalAccessExpression etc.)
                var anonInInit = declarator.Initializer.Value.DescendantNodes()
                    .OfType<AnonymousObjectCreationExpressionSyntax>()
                    .FirstOrDefault();
                if (anonInInit != null)
                    return ExtractNestedFields(anonInInit, semanticModel);
            }

            // Check for .Add(new { ... }) pattern on this variable.
            // Use the block where the variable was declared (or innermost scope) to avoid
            // matching a different variable with the same name in an unrelated scope.
            var varName = identifier.Identifier.Text;
            var containingBlock = declarator?.Ancestors().OfType<BlockSyntax>().FirstOrDefault()
                ?? identifier.Ancestors().OfType<BlockSyntax>().FirstOrDefault();

            if (containingBlock != null)
            {
                var fromAdd = TryExtractFromAddCalls(containingBlock, varName, semanticModel);
                if (fromAdd != null) return fromAdd;
            }

            return null;
        }

        /// <summary>
        /// Unwraps casts/parens and checks whether the expression's type or the
        /// variable's declared type is a collection of a known DTO.
        /// </summary>
        private static List<FieldSchema>? TryExtractFromUnwrappedType(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            // Unwrap interleaved casts and parens to get the underlying expression
            var unwrapped = expression;
            bool changed = true;
            while (changed)
            {
                changed = false;
                if (unwrapped is CastExpressionSyntax cast) { unwrapped = cast.Expression; changed = true; }
                if (unwrapped is ParenthesizedExpressionSyntax paren) { unwrapped = paren.Expression; changed = true; }
            }

            // Check if the (unwrapped) expression's type is List<T> where T is a DTO
            var typeToCheck = semanticModel.GetTypeInfo(unwrapped).Type ?? semanticModel.GetTypeInfo(expression).Type;
            var dtoFields = TryExtractFromElementType(typeToCheck);
            if (dtoFields != null)
                return dtoFields;

            // If it's a variable, resolve it and check its declared type AND initializer
            if (unwrapped is IdentifierNameSyntax varRef)
            {
                var sym = semanticModel.GetSymbolInfo(varRef).Symbol;
                if (sym is ILocalSymbol localVar)
                {
                    dtoFields = TryExtractFromElementType(localVar.Type);
                    if (dtoFields != null)
                        return dtoFields;

                    dtoFields = TryExtractFromDtoCreationsInInitializer(localVar, semanticModel);
                    if (dtoFields != null)
                        return dtoFields;
                }
            }

            return null;
        }

        /// <summary>
        /// Searches a variable's initializer for object creation expressions of concrete DTO types.
        /// </summary>
        private static List<FieldSchema>? TryExtractFromDtoCreationsInInitializer(ILocalSymbol localVar, SemanticModel semanticModel)
        {
            var declarator = localVar.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax())
                .OfType<VariableDeclaratorSyntax>()
                .FirstOrDefault();

            if (declarator?.Initializer?.Value == null)
                return null;

            var dtoCreations = declarator.Initializer.Value.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
                .ToList();

            foreach (var creation in dtoCreations)
            {
                var createdType = semanticModel.GetTypeInfo(creation).Type;
                if (createdType != null &&
                    createdType.TypeKind == TypeKind.Class &&
                    !createdType.IsAnonymousType &&
                    createdType.SpecialType == SpecialType.None &&
                    createdType.Name != "Object" &&
                    createdType.Name != "Dictionary" &&
                    createdType.Name != "List")
                {
                    var dtoFields = TryExtractFromElementType(createdType)
                        ?? ExtractFieldsFromProperties(createdType);
                    if (dtoFields != null)
                        return dtoFields;
                }
            }

            return null;
        }

        /// <summary>
        /// Searches a block for variableName.Add(new { ... }) calls and extracts the anonymous type.
        /// </summary>
        private static List<FieldSchema>? TryExtractFromAddCalls(
            BlockSyntax block, string varName, SemanticModel semanticModel)
        {
            var addCalls = block.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(inv => inv.Expression is MemberAccessExpressionSyntax ma &&
                              ma.Name.Identifier.Text == "Add" &&
                              ma.Expression is IdentifierNameSyntax id &&
                              id.Identifier.Text == varName);

            foreach (var addCall in addCalls)
            {
                if (addCall.ArgumentList.Arguments.Count == 0) continue;

                var arg = addCall.ArgumentList.Arguments[0].Expression;

                // Direct anonymous type: list.Add(new { ... })
                if (arg is AnonymousObjectCreationExpressionSyntax anon)
                {
                    return ExtractNestedFields(anon, semanticModel);
                }

                // Unwrap cast: list.Add((object)new { ... })
                var unwrapped = arg;
                while (unwrapped is CastExpressionSyntax c) unwrapped = c.Expression;
                if (unwrapped is AnonymousObjectCreationExpressionSyntax anonUnwrapped)
                {
                    return ExtractNestedFields(anonUnwrapped, semanticModel);
                }

                // Check descendant anonymous types
                var descendantAnon = arg.DescendantNodes()
                    .OfType<AnonymousObjectCreationExpressionSyntax>()
                    .FirstOrDefault();
                if (descendantAnon != null)
                {
                    return ExtractNestedFields(descendantAnon, semanticModel);
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts [ToolResponse] fields from a method's attributes.
        /// </summary>
        private static List<FieldSchema>? ExtractFieldsFromMethodAttributes(IMethodSymbol method)
        {
            var responseAttrs = method.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == ToolResponseAttributeFullName)
                .ToList();

            if (responseAttrs.Count == 0) return null;

            var fields = new List<FieldSchema>();
            foreach (var attr in responseAttrs)
            {
                string fieldName = "";
                string fieldType = "string";
                string? fieldDescription = null;
                bool fieldNullable = false;

                if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string name)
                    fieldName = name;

                foreach (var named in attr.NamedArguments)
                {
                    switch (named.Key)
                    {
                        case "Type": fieldType = named.Value.Value as string ?? "string"; break;
                        case "Description": fieldDescription = named.Value.Value as string; break;
                        case "Nullable": fieldNullable = named.Value.Value is true; break;
                    }
                }

                if (!string.IsNullOrEmpty(fieldName))
                    fields.Add(new FieldSchema(fieldName, fieldType, fieldNullable, fieldDescription));
            }

            return fields.Count > 0 ? fields : null;
        }

        /// <summary>
        /// Analyzes a called method's body for anonymous type returns.
        /// </summary>
        private static List<FieldSchema>? ExtractFieldsFromMethodBody(
            IMethodSymbol method, SemanticModel semanticModel)
        {
            // Guard against infinite recursion in delegation chains (A → B → A)
            var methodKey = method.ToDisplayString();
            var visited = _visitedMethods ??= new HashSet<string>();
            if (!visited.Add(methodKey))
                return null;

            try
            {
                return ExtractFieldsFromMethodBodyCore(method, semanticModel);
            }
            finally
            {
                visited.Remove(methodKey);
            }
        }

        private static List<FieldSchema>? ExtractFieldsFromMethodBodyCore(
            IMethodSymbol method, SemanticModel semanticModel)
        {
            var syntax = method.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax())
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault();

            if (syntax == null) return null;

            var body = (SyntaxNode?) syntax.Body ?? syntax.ExpressionBody;
            if (body == null) return null;

            var allAnonSet = new HashSet<AnonymousObjectCreationExpressionSyntax>(
                body.DescendantNodes().OfType<AnonymousObjectCreationExpressionSyntax>());

            var topLevel = new List<AnonymousObjectCreationExpressionSyntax>();
            foreach (var a in allAnonSet)
            {
                if (!IsErrorDto(a) && !IsNestedAnonymousType(a, allAnonSet))
                    topLevel.Add(a);
            }

            var distinct = DeduplicateByShape(topLevel, semanticModel);
            if (distinct.Count == 1)
            {
                return ExtractNestedFields(distinct[0], semanticModel);
            }

            return null;
        }

        /// <summary>
        /// Tries to extract schema from a DTO class used as the element type of a collection.
        /// e.g., List&lt;BarterableItemInfo&gt; → reads BarterableItemInfo's public properties.
        /// </summary>
        private static List<FieldSchema>? TryExtractFromElementType(ITypeSymbol? typeSymbol)
        {
            if (typeSymbol == null) return null;

            // Unwrap List<T>, IEnumerable<T>, T[], etc. to get T
            var elementType = GetCollectionElementType(typeSymbol);
            if (elementType == null) return null;

            // Skip primitives, string, object, anonymous types, dictionaries
            if (elementType.SpecialType != SpecialType.None) return null;
            if (elementType.IsAnonymousType) return null;
            if (elementType.Name == "Object" || elementType.Name == "Dictionary") return null;
            if (elementType.TypeKind != TypeKind.Class && elementType.TypeKind != TypeKind.Struct) return null;

            return ExtractFieldsFromProperties(elementType);
        }

        /// <summary>
        /// Unwraps collection types to get the element type.
        /// List&lt;T&gt; → T, T[] → T, IEnumerable&lt;T&gt; → T, etc.
        /// </summary>
        private static ITypeSymbol? GetCollectionElementType(ITypeSymbol type)
        {
            // Array
            if (type is IArrayTypeSymbol arrayType)
                return arrayType.ElementType;

            // Dictionaries are not collections for schema purposes — they serialize as JSON objects
            if (TypeMapper.IsDictionaryType(type))
                return null;

            // Generic collections: List<T>, IList<T>, IEnumerable<T>, etc.
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.TypeArguments.Length == 1)
            {
                if (TypeMapper.IsKnownCollectionMetadataName(namedType.OriginalDefinition.MetadataName))
                    return namedType.TypeArguments[0];
            }

            // Check interfaces for IEnumerable<T>
            foreach (var iface in type.AllInterfaces)
            {
                if (iface.IsGenericType && iface.TypeArguments.Length == 1 &&
                    iface.OriginalDefinition.MetadataName == "IEnumerable`1")
                {
                    return iface.TypeArguments[0];
                }
            }

            return null;
        }

        /// <summary>
        /// When a [Tool] method has no anonymous type returns (only error DTOs or method calls),
        /// check if any non-error return expression is a method invocation whose target method
        /// has [ToolResponse] attributes. If so, inherit those as the schema.
        /// </summary>
        private static List<FieldSchema>? TryExtractFromDelegatedMethod(SyntaxNode body, SemanticModel semanticModel)
        {
            // Collect candidate return expressions from return statements in the method body
            // and lambdas (e.g. MainThreadDispatcher.EnqueueAsync(() => { return SerializeParty(); })),
            // but NOT from local functions — those are independent scopes whose returns
            // should not be treated as the tool method's own return schema.
            var returnExpressions = new List<ExpressionSyntax>();

            // Expression-bodied methods/lambdas (skip those inside local functions)
            foreach (var arrowClause in body.DescendantNodesAndSelf().OfType<ArrowExpressionClauseSyntax>())
            {
                if (!IsInsideLocalFunction(arrowClause, body))
                    returnExpressions.Add(arrowClause.Expression);
            }

            // Explicit return statements including those inside lambdas (skip local functions)
            foreach (var ret in body.DescendantNodes().OfType<ReturnStatementSyntax>())
            {
                if (ret.Expression != null && !IsInsideLocalFunction(ret, body))
                    returnExpressions.Add(ret.Expression);
            }

            foreach (var expr in returnExpressions)
            {
                // Skip error DTOs
                if (expr is AnonymousObjectCreationExpressionSyntax anon && IsErrorDto(anon))
                    continue;

                // Check if this return is a method invocation
                if (!(expr is InvocationExpressionSyntax invocation))
                    continue;

                var calledSymbol = ResolveMethodSymbol(invocation, body, semanticModel);
                if (calledSymbol == null)
                    continue;

                var fields = ExtractFieldsFromMethodAttributes(calledSymbol);
                if (fields != null) return fields;

                fields = ExtractFieldsFromMethodBody(calledSymbol, semanticModel);
                if (fields != null) return fields;
            }

            return null;
        }

        /// <summary>
        /// Resolves the method symbol for an invocation expression, with a fallback
        /// that looks up the method by name in the enclosing type declaration.
        /// </summary>
        private static IMethodSymbol? ResolveMethodSymbol(
            InvocationExpressionSyntax invocation,
            SyntaxNode contextNode,
            SemanticModel semanticModel)
        {
            var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol != null)
                return symbol;

            var methodName = invocation.Expression switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
                _ => (string?) null
            };

            if (methodName == null)
                return null;

            var enclosingType = contextNode.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (enclosingType == null)
                return null;

            var typeSymbol = semanticModel.GetDeclaredSymbol(enclosingType) as INamedTypeSymbol;
            if (typeSymbol == null) return null;

            var argCount = invocation.ArgumentList.Arguments.Count;
            var candidates = typeSymbol.GetMembers(methodName).OfType<IMethodSymbol>().ToList();

            // Prefer the overload whose parameter count matches the argument count
            return candidates.FirstOrDefault(m => m.Parameters.Length == argCount)
                ?? candidates.FirstOrDefault();
        }

        /// <summary>
        /// Extracts FieldSchema list from the public instance properties of a type symbol.
        /// Shared helper used by both TryExtractFromElementType and TryExtractNestedAnonymousType.
        /// </summary>
        private static List<FieldSchema>? ExtractFieldsFromProperties(ITypeSymbol typeSymbol)
        {
            var props = typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
                .ToList();

            if (props.Count == 0) return null;

            var fields = new List<FieldSchema>();
            foreach (var prop in props)
            {
                var (jsonType, nullable) = TypeMapper.MapToJsonSchemaType(prop.Type);
                var description = ExtractXmlSummary(prop.GetDocumentationCommentXml());

                fields.Add(new FieldSchema(prop.Name, jsonType, nullable, description));
            }

            return fields.Count > 0 ? fields : null;
        }

        /// <summary>
        /// Extracts the text content of a &lt;summary&gt; element from an XML doc comment string.
        /// Uses simple string extraction to avoid a System.Xml.Linq dependency.
        /// </summary>
        private static string? ExtractXmlSummary(string? xml)
        {
            if (string.IsNullOrEmpty(xml))
                return null;

            var summaryStart = xml!.IndexOf("<summary>");
            var summaryEnd = xml!.IndexOf("</summary>");
            if (summaryStart < 0 || summaryEnd <= summaryStart)
                return null;

            var text = xml.Substring(summaryStart + 9, summaryEnd - summaryStart - 9).Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }

        /// <summary>
        /// Extracts the method name from an invocation expression, handling both
        /// regular member access (a.B()) and null-conditional member binding (a?.B()).
        /// </summary>
        private static string? GetInvocationMethodName(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax ma)
                return ma.Name.Identifier.Text;
            if (invocation.Expression is MemberBindingExpressionSyntax mb)
                return mb.Name.Identifier.Text;
            return null;
        }

        /// <summary>
        /// Checks whether the outermost call in the expression is a ToString() invocation,
        /// using syntax structure rather than string matching.
        /// Handles both direct (x.ToString()) and null-conditional (x?.ToString()) forms.
        /// </summary>
        private static bool ExpressionEndsWithToString(ExpressionSyntax expression)
        {
            // Direct: x.ToString()
            if (expression is InvocationExpressionSyntax invocation &&
                invocation.ArgumentList.Arguments.Count == 0 &&
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.Text == "ToString")
            {
                return true;
            }

            // Null-conditional: x?.ToString()
            if (expression is ConditionalAccessExpressionSyntax conditionalAccess &&
                conditionalAccess.WhenNotNull is InvocationExpressionSyntax condInvocation &&
                condInvocation.ArgumentList.Arguments.Count == 0 &&
                condInvocation.Expression is MemberBindingExpressionSyntax memberBinding &&
                memberBinding.Name.Identifier.Text == "ToString")
            {
                return true;
            }

            return false;
        }

        private static bool HasNullConditionalAccess(ExpressionSyntax expression)
        {
            if (expression.IsKind(SyntaxKind.ConditionalAccessExpression))
                return true;

            return expression.DescendantNodes()
                .Any(n => n.IsKind(SyntaxKind.ConditionalAccessExpression));
        }

        /// <summary>
        /// Checks whether a syntax node is inside a local function declaration
        /// relative to the given method body root. Lambdas are NOT considered
        /// local functions — only <see cref="LocalFunctionStatementSyntax"/>.
        /// </summary>
        private static bool IsInsideLocalFunction(SyntaxNode node, SyntaxNode bodyRoot)
        {
            var current = node.Parent;
            while (current != null && current != bodyRoot)
            {
                if (current is LocalFunctionStatementSyntax)
                    return true;
                current = current.Parent;
            }
            return false;
        }
    }
}