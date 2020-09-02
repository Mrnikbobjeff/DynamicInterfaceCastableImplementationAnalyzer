using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace InterfaceAttributeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InterfaceAttributeAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "InterfaceAttributeAnalyzer";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInterface, SyntaxKind.InterfaceDeclaration);
        }

        static IEnumerable<ISymbol> GetAllMembersIncludingInherited(INamedTypeSymbol typeSymbol)
        {
            List<ISymbol> members = new List<ISymbol>();
            members.AddRange(typeSymbol.GetMembers());
            foreach (var interfaces in typeSymbol.AllInterfaces)
                members.AddRange(interfaces.GetMembers());

            return members;
        }

        private static void AnalyzeInterface(SyntaxNodeAnalysisContext context)
        {
            var interfaceSyntax = context.Node as InterfaceDeclarationSyntax;
            if (!interfaceSyntax.AttributeLists.Any())
                return;
            if (!interfaceSyntax.AttributeLists.SelectMany(x => x.Attributes).Any(attr => (attr.Name as IdentifierNameSyntax).Identifier.ValueText.Equals("DynamicInterfaceCastableImplementation")))
                return; //Attribute missing
            var typeSymbol = context.SemanticModel.GetDeclaredSymbol(interfaceSyntax);
            if (typeSymbol is null)
                return;
            var interfaceMembers = GetAllMembersIncludingInherited(typeSymbol);
            Dictionary<string, bool> memberDefinitions = new Dictionary<string, bool>();
            foreach(var m in interfaceMembers)
            {
                memberDefinitions[m.Name] = false;
            }
            foreach(var member in interfaceMembers.OfType<IMethodSymbol>())
            {
                var reference = (member.DeclaringSyntaxReferences.First());
                var syntax = member.DeclaringSyntaxReferences.FirstOrDefault();

                var newSyntax= syntax?.GetSyntax() as MethodDeclarationSyntax;
                if (newSyntax.Body != null || newSyntax.ExpressionBody != null)
                    memberDefinitions[member.Name] = true;
            }
            if (memberDefinitions.All(met => met.Value))
                return; // Interface has implementation either inherited or itself
            if (true)
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, interfaceSyntax.GetLocation(), interfaceSyntax.Identifier);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
