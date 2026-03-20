using Microsoft.CodeAnalysis;

namespace Lib.GAB.Generators
{
    internal static class TypeMapper
    {
        /// <summary>
        /// Maps a Roslyn type symbol to a JSON Schema type string.
        /// Returns the JSON type and whether the value is nullable.
        /// </summary>
        public static (string jsonType, bool nullable) MapToJsonSchemaType(ITypeSymbol? typeSymbol)
        {
            if (typeSymbol == null)
                return ("object", false);

            // Unwrap Nullable<T>
            if (typeSymbol is INamedTypeSymbol namedType &&
                namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
                namedType.TypeArguments.Length == 1)
            {
                var (innerType, _) = MapToJsonSchemaType(namedType.TypeArguments[0]);
                return (innerType, true);
            }

            // Check nullable annotation (string? etc.)
            var isNullableRef = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;

            switch (typeSymbol.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return ("boolean", isNullableRef);

                case SpecialType.System_String:
                    return ("string", isNullableRef);

                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    return ("integer", isNullableRef);

                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                    return ("number", isNullableRef);

                case SpecialType.System_DateTime:
                    return ("string", isNullableRef);
            }

            // Check for DateTimeOffset
            if (typeSymbol.Name == "DateTimeOffset")
                return ("string", isNullableRef);

            // Enums serialize as their string representation in JSON
            if (typeSymbol.TypeKind == TypeKind.Enum)
                return ("string", isNullableRef);

            // Check for array types
            if (typeSymbol is IArrayTypeSymbol)
                return ("array", isNullableRef);

            // Dictionary types serialize as JSON objects, not arrays.
            // Check before collections since Dictionary<K,V> implements IEnumerable<KeyValuePair<K,V>>.
            if (IsDictionaryType(typeSymbol))
                return ("object", isNullableRef);

            // Check for generic collection types by metadata name (List<T>, IEnumerable<T>, etc.)
            if (typeSymbol is INamedTypeSymbol genericType && genericType.IsGenericType &&
                genericType.TypeArguments.Length == 1)
            {
                var originalDef = genericType.OriginalDefinition;
                if (IsCollectionType(originalDef))
                    return ("array", isNullableRef);
            }

            // Check for IEnumerable<T> interface implementation (catch-all for collections)
            foreach (var iface in typeSymbol.AllInterfaces)
            {
                if (iface.IsGenericType && iface.TypeArguments.Length == 1 &&
                    IsGenericIEnumerable(iface.OriginalDefinition))
                {
                    return ("array", isNullableRef);
                }
            }

            // Anonymous types and everything else → object
            return ("object", isNullableRef);
        }

        /// <summary>
        /// Checks whether a metadata name corresponds to a known generic collection type.
        /// Used by both TypeMapper and AnonymousTypeAnalyzer for collection element extraction.
        /// </summary>
        public static bool IsKnownCollectionMetadataName(string metadataName)
        {
            switch (metadataName)
            {
                case "List`1":
                case "IList`1":
                case "IEnumerable`1":
                case "ICollection`1":
                case "IReadOnlyList`1":
                case "IReadOnlyCollection`1":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks whether a metadata name corresponds to a known generic dictionary type.
        /// </summary>
        public static bool IsKnownDictionaryMetadataName(string metadataName)
        {
            switch (metadataName)
            {
                case "Dictionary`2":
                case "IDictionary`2":
                case "IReadOnlyDictionary`2":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks whether a type symbol is a known dictionary type (direct or via interfaces).
        /// </summary>
        public static bool IsDictionaryType(ITypeSymbol? typeSymbol)
        {
            if (typeSymbol is INamedTypeSymbol named && named.IsGenericType &&
                named.TypeArguments.Length == 2 && IsDictionaryTypeDef(named.OriginalDefinition))
            {
                return true;
            }

            if (typeSymbol != null)
            {
                foreach (var iface in typeSymbol.AllInterfaces)
                {
                    if (iface.IsGenericType && iface.TypeArguments.Length == 2 &&
                        IsDictionaryTypeDef(iface.OriginalDefinition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsDictionaryTypeDef(INamedTypeSymbol originalDefinition)
        {
            if (!IsInSystemCollectionsGeneric(originalDefinition))
                return false;

            return IsKnownDictionaryMetadataName(originalDefinition.MetadataName);
        }

        private static bool IsCollectionType(INamedTypeSymbol originalDefinition)
        {
            if (!IsInSystemCollectionsGeneric(originalDefinition))
                return false;

            return IsKnownCollectionMetadataName(originalDefinition.MetadataName);
        }

        private static bool IsGenericIEnumerable(INamedTypeSymbol originalDefinition)
        {
            return originalDefinition.MetadataName == "IEnumerable`1"
                && IsInSystemCollectionsGeneric(originalDefinition);
        }

        private static bool IsInSystemCollectionsGeneric(INamedTypeSymbol type)
        {
            var ns = type.ContainingNamespace;
            return ns != null
                && ns.Name == "Generic"
                && ns.ContainingNamespace?.Name == "Collections"
                && ns.ContainingNamespace?.ContainingNamespace?.Name == "System"
                && ns.ContainingNamespace?.ContainingNamespace?.ContainingNamespace?.IsGlobalNamespace == true;
        }
    }
}