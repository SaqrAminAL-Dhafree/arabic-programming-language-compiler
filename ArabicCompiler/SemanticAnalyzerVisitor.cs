// SemanticAnalyzerVisitor.cs (الكود الكامل والنهائي - بعد الخطوة 8)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ArabicCompiler
{
    public class SemanticAnalyzerVisitor : ExprBaseVisitor<Type>
    {
    public SymbolTable SymbolTable { get; }
    private readonly CodeGenerator _codeGenerator;

    public SemanticAnalyzerVisitor(SymbolTable symbolTable, CodeGenerator codeGenerator)
    {
        SymbolTable = symbolTable;
        _codeGenerator = codeGenerator;
    }

    // --- (الدوال المساعدة GetArabicType, GetDotNetType, AnalyzeFormalParameters, CheckNumericOp, CheckLogicalOp ... تبقى كما هي) ---
    #region Helper Methods
    private DataType GetArabicType(string typeName)
    {
        return typeName switch
        {
            "صحيح" => DataType.الصحيح,
            "حقيقي" => DataType.الحقيقي,
            "منطقي" => DataType.المنطقي,
            "خيط_رمزي" => DataType.الخيط_الرمزي,
            "اجراء" => DataType.الاجراء,
            _ => DataType.غير_معروف
        };
    }
    private Type GetDotNetType(DataType dataType)
    {
        return dataType switch
        {
            DataType.الصحيح => typeof(int),
            DataType.الحقيقي => typeof(double),
            DataType.المنطقي => typeof(bool),
            DataType.الخيط_الرمزي => typeof(string),
            _ => throw new Exception($"خطأ دلالي: نوع البيانات غير مدعوم.")
        };
    }
    private List<Parameter> AnalyzeFormalParameters(ExprParser.Formal_params_listContext context)
    {
        var parameters = new List<Parameter>();
        foreach (var paramDef in context.param_def())
        {
            var variablesGroup = paramDef.variables_group();
            var dataTypeText = variablesGroup.data_type().GetText();
            var dataType = GetArabicType(dataTypeText);
            bool isByValue = paramDef.BY_VALUE() != null || paramDef.BY_REFERENCE() == null;
            foreach (var idToken in variablesGroup.ID())
            {
                parameters.Add(new Parameter(idToken.GetText(), dataType, isByValue));
            }
        }
        return parameters;
    }
    private Type CheckNumericOp(Type left, Type right, string op)
    {
        if (left == typeof(int) && right == typeof(int))
            return typeof(int);
        throw new Exception($"خطأ دلالي: لا يمكن إجراء العملية الحسابية '{op}' بين '{left.Name}' و '{right.Name}'.");
    }
    private Type CheckLogicalOp(Type left, Type right, string op)
    {
        if (left == typeof(bool) && right == typeof(bool))
            return typeof(bool);
        throw new Exception($"خطأ دلالي: العملية المنطقية '{op}' تتطلب معاملين من نوع منطقي.");
    }
    #endregion

    // ------------------------------------------------------------------ # إدارة النطاق

    // --- (إصلاح الخطوة 8: معالجة النطاق العام) ---
    public override Type VisitProgram(ExprParser.ProgramContext context)
    {
        // 1. زيارة التعريفات (لإضافة الرموز إلى النطاق العام)
        Visit(context.block().definitions_part());
        // 2. زيارة التعليمات (للتحقق منها)
        Visit(context.block().instructions_list());
        return typeof(void);
    }

    // --- (إصلاح الخطوة 8: هذه الدالة الآن للنطاقات المتداخلة فقط) ---
    public override Type VisitBlock(ExprParser.BlockContext context)
    {
        // إذا كان الأب هو procedure_block، فإن VisitProcedure_block سيتولى أمره
        if (context.Parent is ExprParser.Procedure_blockContext)
        {
            Visit(context.definitions_part());
            Visit(context.instructions_list());
            return typeof(void);
        }

        // أي كتلة أخرى هي نطاق جديد (مثل داخل IF)
        SymbolTable.EnterScope();
        Visit(context.definitions_part());
        Visit(context.instructions_list());
        SymbolTable.ExitScope();
        return typeof(void);
    }

    // ------------------------------------------------------------------ # معالجة التعريفات (تسجيل الرموز)

    public override Type VisitDefinitions_part(ExprParser.Definitions_partContext context)
    {
        if (context.variables_definition() != null)
        {
            Visit(context.variables_definition());
        }
        if (context.procedures_definition() != null)
        {
            foreach (var procDef in context.procedures_definition().procedure_def())
            {
                Visit(procDef.procedure_header());
            }
            foreach (var procDef in context.procedures_definition().procedure_def())
            {
                Visit(procDef.procedure_block());
            }
        }
        return typeof(void);
    }

    public override Type VisitVariables_definition(ExprParser.Variables_definitionContext context)
    {
        foreach (var varDef in context.variable_def())
        {
            Visit(varDef);
        }
        return typeof(void);
    }

    public override Type VisitVariable_def(ExprParser.Variable_defContext context)
    {
        var variablesGroup = context.variables_group();
        var dataTypeText = variablesGroup.data_type().GetText();
        var dataType = GetArabicType(dataTypeText);
        if (dataType == DataType.غير_معروف)
        {
            throw new Exception($"خطأ دلالي: نوع البيانات '{dataTypeText}' غير معروف.");
        }
        foreach (var idToken in variablesGroup.ID())
        {
            var name = idToken.GetText();
            SymbolTable.Add(new VariableInfo(name, dataType));
        }
        return typeof(void);
    }

    // (إصلاح الخطوة 7: إنشاء MethodBuilder هنا)
    public override Type VisitProcedure_header(ExprParser.Procedure_headerContext context)
    {
        var procName = context.ID().GetText();
        var parameters = new List<Parameter>();
        if (context.formal_params_list() != null)
        {
            parameters = AnalyzeFormalParameters(context.formal_params_list());
        }
        var procedureInfo = new ProcedureInfo(procName, DataType.الاجراء);
        procedureInfo.Parameters = parameters;

        var paramTypes = parameters.Select(p => GetDotNetType(p.Type)).ToArray();
        var methodBuilder = _codeGenerator.DefineProcedure(procName, typeof(void), paramTypes);
        procedureInfo.MethodBuilder = methodBuilder;

        SymbolTable.Add(procedureInfo);
        return typeof(void);
    }

    // (إصلاح الخطوة 4: إدارة نطاق المعاملات)
    public override Type VisitProcedure_block(ExprParser.Procedure_blockContext context)
    {
        var procHeaderContext = context.Parent.GetChild(0) as ExprParser.Procedure_headerContext;
        var procName = procHeaderContext!.ID().GetText();
        var symbol = SymbolTable.Resolve(procName);
        if (symbol is not ProcedureInfo procInfo)
        {
            return typeof(void);
        }
        SymbolTable.EnterScope();
        if (procInfo.Parameters != null)
        {
            for (int i = 0; i < procInfo.Parameters.Count; i++)
            {
                var param = procInfo.Parameters[i];
                SymbolTable.Add(new ArgumentInfo(param.Name, param.Type, i));
            }
        }
        Visit(context.block());
        SymbolTable.ExitScope();
        return typeof(void);
    }

    // ------------------------------------------------------------------ # دوال التحقق من التعليمات
    // (جميع دوال Visit الأخرى تبقى كما هي)
    #region Statement and Expression Visitors
    public override Type VisitInstructions_list(ExprParser.Instructions_listContext context)
    {
        foreach (var instruction in context.instruction())
        {
            Visit(instruction);
        }
        return typeof(void);
    }
    public override Type VisitInstruction(ExprParser.InstructionContext context)
    {
        if (context.GetChild(0) != null)
            Visit(context.GetChild(0));
        return typeof(void);
    }
    public override Type VisitAssignment_statement(ExprParser.Assignment_statementContext context)
    {
        var variableName = context.variable_access().ID().GetText();
        var symbol = SymbolTable.Resolve(variableName);
        var expectedType = GetDotNetType(symbol.Type);
        var actualType = Visit(context.expression());
        if (actualType != expectedType)
        {
            throw new Exception($"خطأ دلالي: لا يمكن تعيين قيمة من نوع '{actualType.Name}' إلى متغير من نوع '{expectedType.Name}'.");
        }
        return typeof(void);
    }
    public override Type VisitOutput_statement(ExprParser.Output_statementContext context)
    {
        Visit(context.print_list());
        return typeof(void);
    }
    public override Type VisitPrint_list(ExprParser.Print_listContext context)
    {
        Visit(context.print_item(0));
        return typeof(void);
    }
    public override Type VisitPrint_item(ExprParser.Print_itemContext context)
    {
        Visit(context.expression());
        return typeof(void);
    }
    public override Type VisitCall_statement(ExprParser.Call_statementContext context)
    {
        var procName = context.ID().GetText();
        if (procName == "اطبع")
        {
            if (context.actual_params_list() != null)
            {
                Visit(context.actual_params_list());
            }
            return typeof(void);
        }
        var symbol = SymbolTable.Resolve(procName);
        if (symbol is not ProcedureInfo procInfo)
        {
            throw new Exception($"خطأ دلالي: المعرّف '{procName}' ليس إجراءً.");
        }
        var expectedParams = procInfo.Parameters;
        var actualParamsContext = context.actual_params_list();
        var actualParamsArray = actualParamsContext?.actual_param();
        var actualCount = actualParamsArray?.Length ?? 0;
        var expectedCount = expectedParams?.Count ?? 0;
        if (expectedCount != actualCount)
        {
            throw new Exception($"خطأ دلالي: الإجراء '{procName}' يتوقع {expectedCount} معاملات، لكن تم تمرير {actualCount}.");
        }
        if (actualCount > 0)
        {
            for (int i = 0; i < expectedCount; i++)
            {
                var expectedType = GetDotNetType(expectedParams![i].Type);
                var actualType = Visit(actualParamsArray![i]);
                if (actualType != expectedType)
                {
                    throw new Exception($"خطأ دلالي: المعامل رقم {i + 1} في استدعاء '{procName}' يتوقع نوع '{expectedType.Name}' لكن تم تمرير '{actualType.Name}'.");
                }
            }
        }
        return typeof(void);
    }
    public override Type VisitActual_params_list(ExprParser.Actual_params_listContext context)
    {
        foreach (var actualParam in context.actual_param())
        {
            Visit(actualParam);
        }
        return typeof(void);
    }
    public override Type VisitActual_param(ExprParser.Actual_paramContext context)
    {
        if (context.expression() != null)
        {
            return Visit(context.expression());
        }
        return typeof(void);
    }
    public override Type VisitExpression(ExprParser.ExpressionContext context)
    {
        var leftType = Visit(context.simple_expression(0));
        if (context.relational_op() != null)
        {
            var rightType = Visit(context.simple_expression(1));
            if (leftType != rightType)
                throw new Exception($"خطأ دلالي: لا يمكن مقارنة '{leftType.Name}' مع '{rightType.Name}'.");
            return typeof(bool);
        }
        return leftType;
    }
    public override Type VisitSimple_expression(ExprParser.Simple_expressionContext context)
    {
        var resultType = Visit(context.term(0));
        for (int i = 0; i < context.add_op().Length; i++)
        {
            var rightType = Visit(context.term(i + 1));
            var op = context.add_op(i).GetText();
            if (op == "+" || op == "-")
            {
                resultType = CheckNumericOp(resultType, rightType, op);
            }
            else if (op == "||")
            {
                resultType = CheckLogicalOp(resultType, rightType, op);
            }
        }
        return resultType;
    }
    public override Type VisitTerm(ExprParser.TermContext context)
    {
        var resultType = Visit(context.factor(0));
        for (int i = 0; i < context.mul_op().Length; i++)
        {
            var rightType = Visit(context.factor(i + 1));
            var op = context.mul_op(i).GetText();
            if (op == "*" || op == "/" || op == "%")
            {
                resultType = CheckNumericOp(resultType, rightType, op);
            }
            else if (op == "&&")
            {
                resultType = CheckLogicalOp(resultType, rightType, op);
            }
        }
        return resultType;
    }
    public override Type VisitFactor(ExprParser.FactorContext context)
    {
        if (context.constant_value() != null)
        {
            return Visit(context.constant_value());
        }
        if (context.variable_access() != null)
        {
            var name = context.variable_access().ID().GetText();
            var symbol = SymbolTable.Resolve(name);
            if (symbol == null)
                throw new Exception($"خطأ دلالي: المتغير '{name}' غير مُعرَّف.");
            return GetDotNetType(symbol.Type);
        }
        if (context.expression() != null)
        {
            return Visit(context.expression());
        }
        return typeof(void);
    }
    public override Type VisitConstant_value(ExprParser.Constant_valueContext context)
    {
        if (context.numeric_value() != null)
            return typeof(int);
        if (context.literal_value()?.STRING_LITERAL() != null)
            return typeof(string);
        if (context.logical_value() != null)
            return typeof(bool);
        if (context.ID() != null)
        {
            var name = context.ID().GetText();
            var symbol = SymbolTable.Resolve(name);
            return GetDotNetType(symbol.Type);
        }
        throw new Exception($"خطأ دلالي: قيمة ثابتة غير مدعومة.");
    }
    public override Type VisitNumeric_value(ExprParser.Numeric_valueContext context)
    {
        return typeof(int);
    }
    #endregion
    }
}