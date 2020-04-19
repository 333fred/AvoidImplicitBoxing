using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace AvoidImplicitBoxing
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class AvoidImplicitBoxingDiagnosticsSuppression : DiagnosticSuppressor
    {
        private static readonly LocalizableString SuppressionJustification = new LocalizableResourceString(nameof(Resources.SuppressionJustification), Resources.ResourceManager, typeof(Resources));
        private static readonly SuppressionDescriptor SuppressionDescriptor = new SuppressionDescriptor(id: "SPR0004", suppressedDiagnosticId: "IDE0004", SuppressionJustification);

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(SuppressionDescriptor);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                var sourceTree = diagnostic.Location.SourceTree;
                if (sourceTree is null)
                {
                    continue;
                }

                var conversionSyntax = sourceTree.GetRoot().FindNode(diagnostic.Location.SourceSpan);
                switch (conversionSyntax.RawKind)
                {
                    case (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.CastExpression when conversionSyntax.Language == LanguageNames.CSharp:
                    case (int)Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CTypeExpression when conversionSyntax.Language == LanguageNames.VisualBasic:
                    case (int)Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DirectCastExpression when conversionSyntax.Language == LanguageNames.VisualBasic:
                    case (int)Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.TryCastExpression when conversionSyntax.Language == LanguageNames.VisualBasic:
                        break;

                    default:
                        continue;
                }

                var model = context.GetSemanticModel(sourceTree);
                var conversionOperation = model.GetOperation(conversionSyntax);
                if (!AvoidImplicitBoxingAnalyzer.IsImplicitBoxingConversion(conversionOperation))
                {
                    return;
                }

                context.ReportSuppression(Suppression.Create(SuppressionDescriptor, diagnostic));
            }
        }
    }
}
