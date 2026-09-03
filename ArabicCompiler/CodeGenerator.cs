// CodeGenerator.cs (الكود النهائي الكامل)
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace ArabicCompiler
{
    public class CodeGenerator
    {
        private readonly System.Text.StringBuilder _logBuilder = new System.Text.StringBuilder();
    private readonly AssemblyBuilder _assemblyBuilder;
    private readonly ModuleBuilder _moduleBuilder;
    private readonly TypeBuilder _typeBuilder;
    private readonly MethodBuilder _mainMethodBuilder;
    private readonly ILGenerator _ilGenerator;

    // خاصية للوصول إلى ILGenerator الرئيسي
    public ILGenerator MainILGenerator => _ilGenerator;

    private readonly string _assemblyName;

    public CodeGenerator(string assemblyName)
    {
        _assemblyName = assemblyName;
        var assemblyNameObj = new AssemblyName(_assemblyName);

        _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyNameObj, AssemblyBuilderAccess.Run);
        _moduleBuilder = _assemblyBuilder.DefineDynamicModule($"{_assemblyName}.exe");
        _typeBuilder = _moduleBuilder.DefineType("Program", TypeAttributes.Public);
        _mainMethodBuilder = _typeBuilder.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(void), null);
        _ilGenerator = _mainMethodBuilder.GetILGenerator();
    }

    // تسجيل تعليمات التوليد (للـ UI)
    public void Log(string text)
    {
        _logBuilder.AppendLine(text);
    }

    public string GetLogText() => _logBuilder.ToString();

    // ------------------- دوال توليد شيفرة العمليات الحسابية والطباعة -------------------

    // دالة طباعة النصوص (STRING) - للطباعة الموحدة النهائية
    public void GeneratePrintString()
    {
        var writeLineMethod = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
        _ilGenerator.Emit(OpCodes.Call, writeLineMethod!);
    }

    // *** دالة التحويل إلى String (مهمة للطباعة الموحدة) ***
    public void GenerateIntToString()
    {
        var toStringMethod = typeof(int).GetMethod("ToString", Type.EmptyTypes);
        _ilGenerator.Emit(OpCodes.Call, toStringMethod!);
    }

    public void GeneratePushInt(int value)
    {
        _ilGenerator.Emit(OpCodes.Ldc_I4, value);
        Log($"PushInt {value}");
    }

    public void GeneratePushString(string value)
    {
        _ilGenerator.Emit(OpCodes.Ldstr, value);
        Log($"PushString \"{value}\"");
    }

    public void GenerateAdd() { _ilGenerator.Emit(OpCodes.Add); Log("Add"); }
    public void GenerateSubtract() { _ilGenerator.Emit(OpCodes.Sub); Log("Sub"); }
    public void GenerateMultiply() { _ilGenerator.Emit(OpCodes.Mul); Log("Mul"); }

    // القسمة الصحيحة في IL
    public void GenerateDivide() { _ilGenerator.Emit(OpCodes.Div); Log("Div"); }

    public void GenerateModulo() { _ilGenerator.Emit(OpCodes.Rem); Log("Mod"); }

    // العمليات المنطقية (على الأعداد الصحيحة)
    public void GenerateAnd() { _ilGenerator.Emit(OpCodes.And); Log("And"); }
    public void GenerateOr() { _ilGenerator.Emit(OpCodes.Or); Log("Or"); }

    // --- (تطبيق إصلاح الخطوة 3) ---
    public void GenerateNot()
    {
        // _ilGenerator.Emit(OpCodes.Ldc_I4_M1); // -1 (Bitwise NOT - خاطئ)
        // _ilGenerator.Emit(OpCodes.Xor);      

        _ilGenerator.Emit(OpCodes.Ldc_I4_0); // (Logical NOT - صحيح)
        _ilGenerator.Emit(OpCodes.Ceq);
        Log("Not");
    }

    public void GeneratePop() => _ilGenerator.Emit(OpCodes.Pop);

    // ------------------- دوال دعم الإجراءات (Procedures) -------------------

    public MethodBuilder DefineProcedure(string name, Type returnType, Type[]? parameterTypes)
    {
        Log($"DefineProcedure {name}");
    return _typeBuilder.DefineMethod(
            name,
            MethodAttributes.Public | MethodAttributes.Static,
            returnType,
            parameterTypes
        );
    }

    public ILGenerator GetProcedureILGenerator(MethodBuilder methodBuilder)
    {
        return methodBuilder.GetILGenerator();
    }

    public void GenerateCall(MethodInfo methodInfo)
    {
        _ilGenerator.Emit(OpCodes.Call, methodInfo);
        Log($"Call {methodInfo.Name}");
    }

    public void GenerateReturn(ILGenerator ilGenerator)
    {
        ilGenerator.Emit(OpCodes.Ret);
        Log("Return");
    }

    // ------------------- دوال القفز والمقارنة -------------------

    public Label DefineNewLabel() => _ilGenerator.DefineLabel();
    public void MarkLabel(Label label) => _ilGenerator.MarkLabel(label);
    public void GenerateJump(Label label) => _ilGenerator.Emit(OpCodes.Br, label);
    public void GenerateJumpIfFalse(Label label) { _ilGenerator.Emit(OpCodes.Brfalse, label); Log("Brfalse"); }
    public void GenerateLessThan() => _ilGenerator.Emit(OpCodes.Clt);
    public void GenerateEqual() => _ilGenerator.Emit(OpCodes.Ceq);

    // ------------------- دالة التشغيل -------------------

    public void Execute()
    {
        _ilGenerator.Emit(OpCodes.Ret);
        var createdType = _typeBuilder.CreateType();
        if (createdType == null)
            throw new Exception("لم يتم إنشاء النوع الديناميكي (Program) بنجاح. تحقق من تعريفات IL.");

        var mainMethod = createdType.GetMethod("Main");
        if (mainMethod == null)
            throw new Exception("لم يتم العثور على دالة Main في النوع الديناميكي. تحقق من تعريف الدالة.");

        try
        {
            mainMethod.Invoke(null, null);
        }
        catch (Exception ex)
        {
            throw new Exception($"حدث خطأ أثناء تنفيذ Main: {ex.Message}", ex);
        }
    }
    }
}