using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using InterfaceAttributeAnalyzer;

namespace InterfaceAttributeAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void EmptyText_NoDiagnostic()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }
        [TestMethod]
        public void InterfaceAttribute_HasImpl_NoDiagnostic()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        internal class DynamicInterfaceCastableImplementationAttribute : Attribute
        {
        }
        public interface TypeName
        {   
            void Foo();
        }
        [DynamicInterfaceCastableImplementation]
        public interface TypeImpl : TypeName
        {
            void Foo() {}
        }
    }";
            VerifyCSharpDiagnostic(test);
        }
        [TestMethod]
        public void InterfaceAttribute_HasImplExpression_NoDiagnostic()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        internal class DynamicInterfaceCastableImplementationAttribute : Attribute
        {
        }
        public interface TypeName
        {   
            int Foo();
        }
        [DynamicInterfaceCastableImplementation]
        public interface TypeImpl : TypeName
        {
            int Foo() => 1;
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void InterfaceAttribute_OtherAttribute_NoDiagnostic()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        internal class TestAttribute : Attribute
        {
        }
        public interface TypeName
        {   
            void Foo();
        }
        [Test]
        public interface TypeImpl : TypeName
        {
            void Foo();
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void InterfaceAttribute_SingleDiagnostic()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        internal class DynamicInterfaceCastableImplementationAttribute : Attribute
        {
        }
        public interface TypeName
        {   
            void Foo();
        }
        [DynamicInterfaceCastableImplementation]
        public interface TypeImpl : TypeName
        {
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "InterfaceAttributeAnalyzer",
                Message = String.Format("Interface '{0}' should provide implementations for all methods.", "TypeImpl"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 13, 9)
                        }
            };
            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new InterfaceAttributeAnalyzerAnalyzer();
        }
    }

    
}
