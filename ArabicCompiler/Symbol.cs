// Symbol.cs (كود كامل)
using System.Collections.Generic;
using System.Reflection.Emit; // نحتاجها لـ LocalBuilder

namespace ArabicCompiler
{
    // فئة لتمثيل معامل (parameter) في الإجراء
    public class Parameter
    {
    public string Name { get; }
    public DataType Type { get; }
    public bool IsByValue { get; } 

    public Parameter(string name, DataType type, bool isByValue = true)
    {
        Name = name;
        Type = type;
        IsByValue = isByValue;
    }
}

// الفئة الأساسية للرمز
public class Symbol
{
    public string Name { get; }
    public DataType Type { get; set; }
    public bool IsConstant { get; }

    // --- خاصة بالإجراءات فقط ---
    public List<Parameter>? Parameters { get; set; } 
    // يجب أن تكون ProcedureBody جزءاً من الـ Symbol لكي يمكن الرجوع إليها عند Call
    public ExprParser.Procedure_blockContext? ProcedureBody { get; set; } 

    public Symbol(string name, DataType type, bool isConstant = false)
    {
        Name = name;
        Type = type;
        IsConstant = isConstant;
        // القيمة الافتراضية ستكون جزءاً من المحلل الدلالي، لا حاجة لها هنا
    }
}
///////

public class ProcedureInfo : Symbol
{
    public MethodBuilder? MethodBuilder { get; set; }
    public ILGenerator? ILGenerator { get; set; } // ILGenerator الخاص بجسم الإجراء

    public ProcedureInfo(string name, DataType type, bool isConstant = false) 
        : base(name, type, isConstant) { }
}
// فئة خاصة لمتغيرات الذاكرة التي تحتاج إلى LocalBuilder
public class VariableInfo : Symbol
{
    // LocalBuilder يمثل المتغير المحلي في شيفرة IL
    public LocalBuilder? LocalBuilder { get; set; } 

    public VariableInfo(string name, DataType type, bool isConstant = false) 
        : base(name, type, isConstant) { }
}

///\
public class ArgumentInfo : Symbol
{
    public int ArgumentIndex { get; }
    public ArgumentInfo(string name, DataType type, int index) 
        : base(name, type) { ArgumentIndex = index; }
}

    public enum DataType { الصحيح, الحقيقي, المنطقي, الحرفي, الخيط_الرمزي, الاجراء, غير_معروف }
}