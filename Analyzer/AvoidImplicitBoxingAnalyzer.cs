using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AvoidImplicitBoxing
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class AvoidImplicitBoxingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AvoidImplicitBoxing";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Conversions";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion);
        }

        private void AnalyzeConversion(OperationAnalysisContext context)
        {
            Debug.Assert(context.Operation is IConversionOperation);

            // Only look at conversions that were implicitly added
            if (!IsImplicitBoxingConversion(context.Operation))
            {
                return;
            }

            var conversionOperation = (IConversionOperation)context.Operation;

            // Implicit boxing conversion detected: flag
            context.ReportDiagnostic(Diagnostic.Create(Rule, conversionOperation.Syntax.GetLocation(),
                formatSymbol(conversionOperation.Operand.Type),
                formatSymbol(conversionOperation.Type)));

            string formatSymbol(ISymbol symbol)
            {
                return symbol.ToMinimalDisplayString(conversionOperation.SemanticModel, conversionOperation.Syntax.SpanStart);
            }
        }

        private static bool IsImplicitBoxingConversion(IOperation? baseOperation)
        {
            if (!(baseOperation is IConversionOperation { IsImplicit: true } conversionOperation))
            {
                return false;
            }

            return IsBoxingConversion(conversionOperation);
        }

        internal static bool IsBoxingConversion(IConversionOperation conversionOperation)
        {
            return conversionOperation.Language == LanguageNames.CSharp
                ? isCSBoxingConversion(conversionOperation)
                : isVBBoxingConversion(conversionOperation);

            static bool isCSBoxingConversion(IConversionOperation operation)
            {
                Debug.Assert(operation.Language == LanguageNames.CSharp);
                return Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetConversion(operation).IsBoxing;
            }

            static bool isVBBoxingConversion(IConversionOperation operation)
            {
                Debug.Assert(operation.Language == LanguageNames.VisualBasic);
                return operation.Type.IsReferenceType && (operation.Operand.Type?.IsValueType ?? false);
            }
        }
    }
}
