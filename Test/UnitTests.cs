using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.MSTest.AnalyzerVerifier<AvoidImplicitBoxing.AvoidImplicitBoxingAnalyzer>;

namespace AvoidImplicitBoxing
{
    [TestClass]
    public class UnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task EmptyFile()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);

        }

        [TestMethod]
        public async Task SimpleBoxingConversion()
        {
            var test = @"
class C
{
    void M()
    {
        object o = 1;
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test,
                    // /0/Test0.cs(6,20): warning AvoidImplicitBoxing: Implicit boxing conversion from 'int' to 'object'
                    VerifyCS.Diagnostic().WithSpan(6, 20, 6, 21).WithArguments("int", "object")
                );
        }

        [TestMethod]
        public async Task SimpleExplicitBoxingNoWarning()
        {
            var test = @"
class C
{
    void M()
    {
        object o = (object)1;
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
