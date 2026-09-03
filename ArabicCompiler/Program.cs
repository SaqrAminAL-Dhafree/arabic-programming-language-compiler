// Program.cs (الكود النهائي الكامل والمصحح - بعد الخطوة 7)
using System;
using System.IO;
using Antlr4.Runtime;

namespace ArabicCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        string sourceFileName = "program.arb";

        // استخدام ملف "program.arb" الذي قدمته لاختبار الإجراءات
        string arabicSourceCode = File.ReadAllText(sourceFileName, System.Text.Encoding.UTF8);

        var inputStream = new AntlrInputStream(arabicSourceCode);
        var lexer = new ExprLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new ExprParser(tokenStream);
        var tree = parser.program();

        if (parser.NumberOfSyntaxErrors == 0)
        {
            Console.WriteLine(">>> التحليل القواعدي اكتمل بنجاح! <<<\n");

            var codeGenerator = new CodeGenerator("InMemoryAssembly");

            // 1. *** إنشاء جدول الرموز (SymbolTable) ***
            var symbolTable = new SymbolTable();

            // 2. *** المرحلة 1: المحلل الدلالي (Semantic Analyzer) ***
            // --- (تعديل الخطوة 7): تمرير codeGenerator إلى المحلل الدلالي
            var semanticVisitor = new SemanticAnalyzerVisitor(symbolTable, codeGenerator);
            semanticVisitor.Visit(tree);
            // هذا يحقق مرحلة التحقق وتسجيل الـ MethodBuilder

            // 3. *** المرحلة 2: مولد الشيفرة (Code Generation) ***
            // يمرر CodeGenerator وجدول الرموز (الذي أصبح معبأ بالبيانات من المرحلة الأولى)
            var visitor = new ArabicVisitor(codeGenerator, symbolTable);

            visitor.Visit(tree); // توليد شيفرة IL في الذاكرة

            Console.WriteLine("--- بدء تنفيذ شيفرة IL المولدة ---\n");

            try
            {
                codeGenerator.Execute(); // تنفيذ الكود المولد
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                // عرض الرسالة الداخلية لخطأ InvalidProgramException
                Console.WriteLine($"\n!!! خطأ في وقت التشغيل (IL Error) !!!");
                Console.WriteLine(ex.InnerException?.Message ?? ex.Message);
            }

            Console.WriteLine("\n--- اكتمل التنفيذ ---\n");
        }
        else
        {
            Console.WriteLine(">>> فشل التحليل القواعدي. تم العثور على أخطاء. <<<\n");
        }
        }
    }
}