// ArabicVisitor.cs (الكود الكامل والنهائي - بعد الخطوة 8)

using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace ArabicCompiler
{
    public class ArabicVisitor : ExprBaseVisitor<Type?>
    {
    private readonly CodeGenerator _codeGenerator;
    private readonly SymbolTable _symbolTable;
    private ILGenerator _currentILGenerator = null!;

    public ArabicVisitor(CodeGenerator codeGenerator, SymbolTable symbolTable)
    {
        _codeGenerator = codeGenerator;
        _symbolTable = symbolTable;
    }

    // --- (الدوال المساعدة GetDotNetType و GetArabicType ... تبقى كما هي) ---
    #region Helper Methods
    private Type GetDotNetType(DataType dataType)
    {
        return dataType switch
        {
            DataType.الصحيح => typeof(int),
            DataType.الحقيقي => typeof(double),
            DataType.المنطقي => typeof(bool),
            DataType.الخيط_الرمزي => typeof(string),
            _ => throw new Exception($"خطأ دلالي: نوع البيانات '{dataType}' غير مدعوم في IL حالياً.")
        };
    }
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
    #endregion

    // ------------------------------------------------------------------ # إدارة النطاق (Scope Management)

    // --- (إصلاح الخطوة 8) ---
    public override Type? VisitProgram(ExprParser.ProgramContext context)
    {
        _currentILGenerator = _codeGenerator.MainILGenerator;

        // 1. زيارة التعريفات (لتوليد LocalBuilder والإجراءات)
        Visit(context.block().definitions_part());
        // 2. زيارة التعليمات (لتوليد IL للـ Main)
        Visit(context.block().instructions_list());
        return null;
    }

    // --- (إصلاح الخطوة 8) ---
    public override Type? VisitBlock(ExprParser.BlockContext context)
    {
        // إذا كان الأب هو procedure_block، فإن VisitProcedure_block سيتولى أمره
        if (context.Parent is ExprParser.Procedure_blockContext)
        {
            Visit(context.definitions_part());
            Visit(context.instructions_list());
            return null;
        }

        // أي كتلة أخرى هي نطاق جديد (مثل داخل IF)
        _symbolTable.EnterScope();
        Visit(context.definitions_part());
        Visit(context.instructions_list());
        _symbolTable.ExitScope();
        return null;
    }

    // ------------------------------------------------------------------ # معالجة التعريفات

    public override Type? VisitDefinitions_part(ExprParser.Definitions_partContext context)
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
        return null;
    }
    public override Type? VisitVariables_definition(ExprParser.Variables_definitionContext context)
    {
        foreach (var varDef in context.variable_def())
        {
            Visit(varDef);
        }
        return null;
    }
    public override Type? VisitVariable_def(ExprParser.Variable_defContext context)
    {
        var variablesGroup = context.variables_group();
        var dataTypeText = variablesGroup.data_type().GetText();
        var dataType = GetArabicType(dataTypeText);
        if (dataType == DataType.غير_معروف)
        {
            throw new Exception($"خطأ دلالي: نوع البيانات '{dataTypeText}' غير معروف أو غير مدعوم.");
        }
        var dotNetType = GetDotNetType(dataType);

        foreach (var idToken in variablesGroup.ID())
        {
            var name = idToken.GetText();
            var symbol = _symbolTable.Resolve(name);
            if (symbol is VariableInfo variableInfo)
            {
                // (ملاحظة: هذا يفترض أن المتغيرات العامة هي Locals في Main)
                variableInfo.LocalBuilder = _currentILGenerator.DeclareLocal(dotNetType);
                // تعيين قيمة ابتدائية للمتغير مباشرة بعد تعريفه
                if (dotNetType == typeof(int))
                {
                    GeneratePushInt(0);
                    GenerateStoreLocal(variableInfo.LocalBuilder);
                }
                else if (dotNetType == typeof(double))
                {
                    _currentILGenerator.Emit(OpCodes.Ldc_R8, 0.0);
                    GenerateStoreLocal(variableInfo.LocalBuilder);
                }
                else if (dotNetType == typeof(bool))
                {
                    GeneratePushInt(0); // false
                    GenerateStoreLocal(variableInfo.LocalBuilder);
                }
                else if (dotNetType == typeof(string))
                {
                    GeneratePushString("");
                    GenerateStoreLocal(variableInfo.LocalBuilder);
                }
            }
            else
            {
                throw new Exception($"خطأ داخلي: لم يتم العثور على الرمز المسجل للمتغير '{name}'.");
            }
        }
        return null;
    }

    // ------------------------------------------------------------------ # معالجة الإجراءات

    public override Type? VisitProcedure_header(ExprParser.Procedure_headerContext context)
    {
        // (المرحلة 1 قامت بكل العمل، لا نفعل شيئاً هنا)
        return null;
    }

    public override Type? VisitProcedure_block(ExprParser.Procedure_blockContext context)
    {
        var procHeaderContext = context.Parent.GetChild(0) as ExprParser.Procedure_headerContext;
        var procName = procHeaderContext!.ID().GetText();
        var procedureInfo = _symbolTable.Resolve(procName) as ProcedureInfo;

        if (procedureInfo?.MethodBuilder == null) return null;

        var newILGenerator = _codeGenerator.GetProcedureILGenerator(procedureInfo.MethodBuilder);
        var oldILGenerator = _currentILGenerator;
        _currentILGenerator = newILGenerator; // التبديل إلى ILGenerator الخاص بالإجراء

        _symbolTable.EnterScope();
        if (procedureInfo.Parameters != null)
        {
            for (int i = 0; i < procedureInfo.Parameters.Count; i++)
            {
                var param = procedureInfo.Parameters[i];
                _symbolTable.Add(new ArgumentInfo(param.Name, param.Type, i));
            }
        }

        Visit(context.block());

        _codeGenerator.GenerateReturn(newILGenerator);

        _currentILGenerator = oldILGenerator; // إعادة التبديل إلى ILGenerator السابق
        _symbolTable.ExitScope();

        return null;
    }

    // ------------------------------------------------------------------ # دوال التوليد الوسيطة
    // (جميع الدوال المساعدة Generate... و Define... تبقى كما هي)
    #region IL Generation Helpers
    private Label DefineNewLabel()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (DefineNewLabel).");
        return _currentILGenerator.DefineLabel();
    }
    private void MarkLabel(Label label)
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (MarkLabel).");
        _currentILGenerator.MarkLabel(label);
        _codeGenerator.Log($"MarkLabel {label.GetHashCode()}");
    }
    private void GenerateJump(Label label)
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateJump).");
        _currentILGenerator.Emit(OpCodes.Br, label);
        _codeGenerator.Log($"Br {label.GetHashCode()}");
    }
    private void GenerateJumpIfFalse(Label label)
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateJumpIfFalse).");
        _currentILGenerator.Emit(OpCodes.Brfalse, label);
        _codeGenerator.Log("Brfalse");
    }
    private void GeneratePushInt(int value)
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GeneratePushInt).");
        _currentILGenerator.Emit(OpCodes.Ldc_I4, value);
        _codeGenerator.Log($"PushInt {value}");
    }
    private void GeneratePushString(string value)
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GeneratePushString).");
        _currentILGenerator.Emit(OpCodes.Ldstr, value);
        _codeGenerator.Log($"PushString \"{value}\"");
    }
    private void GeneratePrintString()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GeneratePrintString).");
        _currentILGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new[] { typeof(string) })!);
        _codeGenerator.Log("PrintString");
    }

    // اطبع قيمة int مباشرةً باستخدام overload الصحيح
    private void GeneratePrintInt()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GeneratePrintInt).");
        _currentILGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new[] { typeof(int) })!);
    }

    // اطبع بدون سطر جديد (Write) للسلاسل
    private void GenerateWriteString()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateWriteString).");
        _currentILGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", new[] { typeof(string) })!);
    }

    // اطبع بدون سطر جديد (Write) للأعداد الصحيحة
    private void GenerateWriteInt()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateWriteInt).");
        _currentILGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", new[] { typeof(int) })!);
    }

    // اطبع سطر جديد بدون محتوى
    private void GenerateWriteLine()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateWriteLine).");
        _currentILGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", Type.EmptyTypes)!);
        _codeGenerator.Log("WriteLine");
    }
    private void GeneratePop()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GeneratePop).");
        _currentILGenerator.Emit(OpCodes.Pop);
        _codeGenerator.Log("Pop");
    }
    private void GenerateAdd()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateAdd).");
        _currentILGenerator.Emit(OpCodes.Add);
        _codeGenerator.Log("Add");
    }
    private void GenerateSubtract()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateSubtract).");
        _currentILGenerator.Emit(OpCodes.Sub);
        _codeGenerator.Log("Sub");
    }
    private void GenerateMultiply()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateMultiply).");
        _currentILGenerator.Emit(OpCodes.Mul);
        _codeGenerator.Log("Mul");
    }
    private void GenerateDivide()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateDivide).");
        _currentILGenerator.Emit(OpCodes.Div);
        _codeGenerator.Log("Div");
    }
    private void GenerateModulo()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateModulo).");
        _currentILGenerator.Emit(OpCodes.Rem);
        _codeGenerator.Log("Mod");
    }
    private void GenerateAnd()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateAnd).");
        _currentILGenerator.Emit(OpCodes.And);
        _codeGenerator.Log("And");
    }
    private void GenerateOr()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateOr).");
        _currentILGenerator.Emit(OpCodes.Or);
        _codeGenerator.Log("Or");
    }
    private void GenerateNot()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateNot).");
        _currentILGenerator.Emit(OpCodes.Ldc_I4_0);
        _currentILGenerator.Emit(OpCodes.Ceq);
        _codeGenerator.Log("Not");
    }
    private void GenerateLoadSymbol(Symbol symbol)
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateLoadSymbol).");
        if (symbol is VariableInfo varInfo)
        {
            if (varInfo.LocalBuilder == null)
                throw new Exception($"خطأ داخلي: لم يتم إنشاء LocalBuilder للمتغير '{varInfo.Name}'.");
            _currentILGenerator.Emit(OpCodes.Ldloc, varInfo.LocalBuilder);
            _codeGenerator.Log($"Load {varInfo.Name}");
        }
        else if (symbol is ArgumentInfo argInfo)
        {
            _currentILGenerator.Emit(OpCodes.Ldarg, argInfo.ArgumentIndex);
            _codeGenerator.Log($"LoadArg {argInfo.ArgumentIndex}");
        }
        else
        {
            throw new Exception($"خطأ داخلي: لا يمكن تحميل الرمز '{symbol.Name}'.");
        }
    }
    private void GenerateStoreLocal(LocalBuilder local)
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateStoreLocal).");
        _currentILGenerator.Emit(OpCodes.Stloc, local);
        _codeGenerator.Log($"Store local #{local.LocalIndex}");
    }
    private void GenerateCall(MethodInfo methodInfo)
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateCall).");
        if (methodInfo == null)
            throw new Exception("MethodBuilder لم يتم تهيئته قبل الاستدعاء (GenerateCall).");
        _currentILGenerator.Emit(OpCodes.Call, methodInfo);
        _codeGenerator.Log($"Call {methodInfo.Name}");
    }
    private void GenerateLessThan()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateLessThan).");
        _currentILGenerator.Emit(OpCodes.Clt);
        _codeGenerator.Log("Clt");
    }
    private void GenerateEqual()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateEqual).");
        _currentILGenerator.Emit(OpCodes.Ceq);
        _codeGenerator.Log("Ceq");
    }
    private void GenerateIntToString()
    {
        if (_currentILGenerator == null)
            throw new Exception("ILGenerator لم يتم تهيئته قبل الاستخدام (GenerateIntToString).");
        _currentILGenerator.Emit(OpCodes.Call, typeof(int).GetMethod("ToString", Type.EmptyTypes)!);
        _codeGenerator.Log("IntToString");
    }
    #endregion

    // ------------------------------------------------------------------ # معالجة التعليمات (Statements)
    // (جميع دوال Visit للتعليمات والتعابير تبقى كما هي)
    #region Statement and Expression Visitors
    public override Type? VisitInstructions_list(ExprParser.Instructions_listContext context)
    {
        foreach (var instruction in context.instruction())
        {
            Visit(instruction);
        }
        return null;
    }
    public override Type? VisitInstruction(ExprParser.InstructionContext context)
    {
        if (context.GetChild(0) != null)
        {
            var instructionType = context.GetChild(0);
            var resultType = Visit(instructionType);
            if (!(instructionType is ExprParser.Assignment_statementContext) &&
                !(instructionType is ExprParser.Output_statementContext) &&
                !(instructionType is ExprParser.Call_statementContext))
            {
                if (resultType != null && resultType != typeof(void))
                {
                    GeneratePop();
                }
            }
        }
        return null;
    }
    public override Type? VisitAssignment_statement(ExprParser.Assignment_statementContext context)
    {
        var variableName = context.variable_access().ID().GetText();
        var symbol = _symbolTable.Resolve(variableName);
        if (symbol is not VariableInfo variableSymbol || variableSymbol.LocalBuilder == null)
        {
            throw new Exception($"خطأ دلالي: المعاملات أو الرمز '{variableName}' غير صالح للتعيين.");
        }
        Visit(context.expression());
        GenerateStoreLocal(variableSymbol.LocalBuilder);
        return null;
    }

    public override Type? VisitOutput_statement(ExprParser.Output_statementContext context)
    {
        // اطبع كل عناصر قائمة الطباعة
        Visit(context.print_list());
        return null;
    }
    public override Type? VisitPrint_list(ExprParser.Print_listContext context)
    {
        var items = context.print_item();
        for (int i = 0; i < items.Length; i++)
        {
            var expr = items[i].expression();
            var exprType = Visit(expr);
            if (exprType == typeof(int))
            {
                GenerateWriteInt();
            }
            else
            {
                GenerateWriteString();
            }

            // أضف فاصل مسافة بين العناصر ما لم تكن آخر عنصر
            if (i < items.Length - 1)
            {
                GeneratePushString(" ");
                GenerateWriteString();
            }
        }
        // نهاية السطر بعد طباعة كل العناصر
        GenerateWriteLine();
        return null;
    }
    public override Type? VisitPrint_item(ExprParser.Print_itemContext context) => Visit(context.expression());

    public override Type? VisitCall_statement(ExprParser.Call_statementContext context)
    {
        var procName = context.ID().GetText();

        if (procName == "اطبع")
        {
            if (context.actual_params_list() != null)
            {
                var args = context.actual_params_list().actual_param();
                for (int i = 0; i < args.Length; i++)
                {
                    var actual = args[i];
                    if (actual.expression() != null)
                    {
                        var t = Visit(actual.expression());
                        if (t == typeof(int)) GenerateWriteInt();
                        else GenerateWriteString();
                    }

                    // فاصل مسافة بين الوسائط (إن لم تكن آخر واحدة)
                    if (i < args.Length - 1)
                    {
                        GeneratePushString(" ");
                        GenerateWriteString();
                    }
                }
            }
            // بعد طباعة المعاملات، انهِ السطر
            GenerateWriteLine();
            return null;
        }

        var symbol = _symbolTable.Resolve(procName);
        if (symbol?.Type != DataType.الاجراء || symbol is not ProcedureInfo procedureInfo || procedureInfo.MethodBuilder == null)
        {
            throw new Exception($"خطأ دلالي: المعرّف '{procName}' ليس إجراءً مُعرَّفاً.");
        }

        if (context.actual_params_list() != null)
        {
            Visit(context.actual_params_list());
        }
        GenerateCall(procedureInfo.MethodBuilder);
        return null;
    }
    public override Type? VisitActual_params_list(ExprParser.Actual_params_listContext context)
    {
        foreach (var actualParam in context.actual_param())
        {
            Visit(actualParam);
        }
        return null;
    }
    public override Type? VisitActual_param(ExprParser.Actual_paramContext context)
    {
        if (context.expression() != null)
        {
            return Visit(context.expression());
        }
        return null;
    }

    public override Type? VisitInput_statement(ExprParser.Input_statementContext context)
    {
        // READ(variable) -> اقرأ من Console.ReadLine ثم خزّن بالقيمة المناسبة
        var varAccess = context.variable_access();
        var varName = varAccess.ID().GetText();
        var symbol = _symbolTable.Resolve(varName);
        if (symbol is not VariableInfo variableSymbol || variableSymbol.LocalBuilder == null)
        {
            throw new Exception($"خطأ دلالي: المتغير '{varName}' غير صالح للاستقبال أو لم يتم إنشاؤه.");
        }

        // استدعاء Console.ReadLine()
        _currentILGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadLine", Type.EmptyTypes)!);

        // تحويل السلسلة إلى النوع المناسب ثم تخزينها
        var targetType = GetDotNetType(symbol.Type);
        if (targetType == typeof(int))
        {
            _currentILGenerator.Emit(OpCodes.Call, typeof(int).GetMethod("Parse", new[] { typeof(string) })!);
            GenerateStoreLocal(variableSymbol.LocalBuilder);
        }
        else if (targetType == typeof(double))
        {
            _currentILGenerator.Emit(OpCodes.Call, typeof(double).GetMethod("Parse", new[] { typeof(string) })!);
            GenerateStoreLocal(variableSymbol.LocalBuilder);
        }
        else if (targetType == typeof(bool))
        {
            // نعتمد على bool.Parse (يتوقع "True"/"False")
            _currentILGenerator.Emit(OpCodes.Call, typeof(bool).GetMethod("Parse", new[] { typeof(string) })!);
            GenerateStoreLocal(variableSymbol.LocalBuilder);
        }
        else if (targetType == typeof(string))
        {
            // ليست هنالك حاجة لتحويل
            GenerateStoreLocal(variableSymbol.LocalBuilder);
        }
        else
        {
            throw new NotSupportedException($"لا يدعم READ النوع '{symbol.Type}'.");
        }

        return null;
    }

    public override Type? VisitConditional_statement(ExprParser.Conditional_statementContext context)
    {
        var conditionContext = context.condition(0);
        var thenInstruction = context.instruction(0);
        var elseLabel = DefineNewLabel();
        var endIfLabel = DefineNewLabel();
        Visit(conditionContext);
        GenerateJumpIfFalse(elseLabel);
        Visit(thenInstruction);
        GenerateJump(endIfLabel);
        MarkLabel(elseLabel);
        if (context.ELSE() != null)
        {
            var elseInstruction = context.instruction().Length > 1 ? context.instruction(context.instruction().Length - 1) : null;
            if (elseInstruction != null)
            {
                Visit(elseInstruction);
            }
        }
        MarkLabel(endIfLabel);
        return null;
    }
    public override Type? VisitLoop_statement(ExprParser.Loop_statementContext context)
    {
        if (context.while_loop_statement() != null)
            return Visit(context.while_loop_statement());
        if (context.for_loop_statement() != null)
            return Visit(context.for_loop_statement());
        if (context.repeat_until_statement() != null)
            return Visit(context.repeat_until_statement());
        return null;
    }

    public override Type? VisitRepeat_until_statement(ExprParser.Repeat_until_statementContext context)
    {
        var startLabel = DefineNewLabel();
        MarkLabel(startLabel);
        // تنفيذ التعليمات داخل الـ repeat
        Visit(context.instruction());
        // تقييم الشرط
        Visit(context.condition());
        // إذا كان الشرط كاذب، اعد القفز إلى البداية
        GenerateJumpIfFalse(startLabel);
        return null;
    }
    public override Type? VisitWhile_loop_statement(ExprParser.While_loop_statementContext context)
    {
        var conditionContext = context.condition();
        var loopInstruction = context.instruction();
        var loopStartLabel = DefineNewLabel();
        var loopEndLabel = DefineNewLabel();
        MarkLabel(loopStartLabel);
        Visit(conditionContext);
        GenerateJumpIfFalse(loopEndLabel);
        Visit(loopInstruction);
        GenerateJump(loopStartLabel);
        MarkLabel(loopEndLabel);
        return null;
    }
    public override Type? VisitFor_loop_statement(ExprParser.For_loop_statementContext context)
    {
        var iterationRange = context.iteration_range();
        var loopInstruction = context.instruction();
        var counterId = iterationRange.ID().GetText();
        var loopStartLabel = DefineNewLabel();
        var loopEndLabel = DefineNewLabel();
        var counterSymbol = _symbolTable.Resolve(counterId) as VariableInfo;
        if (counterSymbol == null || counterSymbol.LocalBuilder == null)
        {
            throw new Exception($"خطأ دلالي: متغير حلقة التكرار '{counterId}' غير مُعرَّف أو لم يتم إنشاء LocalBuilder له.");
        }
        Visit(iterationRange.expression(0));
        GenerateStoreLocal(counterSymbol.LocalBuilder);
        MarkLabel(loopStartLabel);
        GenerateLoadSymbol(counterSymbol);
        var finalValueContext = iterationRange.expression(1);
        Visit(finalValueContext);
        GeneratePushInt(1);
        GenerateAdd();
        GenerateLessThan();
        GenerateJumpIfFalse(loopEndLabel);
        Visit(loopInstruction);
        GenerateLoadSymbol(counterSymbol);
        if (iterationRange.STEP() != null)
        {
            var stepValueContext = iterationRange.expression(2);
            Visit(stepValueContext);
        }
        else
        {
            GeneratePushInt(1);
        }
        GenerateAdd();
        GenerateStoreLocal(counterSymbol.LocalBuilder);
        GenerateJump(loopStartLabel);
        MarkLabel(loopEndLabel);
        return null;
    }

    public override Type? VisitExpression(ExprParser.ExpressionContext context)
    {
        var leftType = Visit(context.simple_expression(0));
        if (context.relational_op() != null)
        {
            Visit(context.simple_expression(1));
            var op = context.relational_op().GetText();
            if (op == "<" || op == "اقل")
            {
                GenerateLessThan();
            }
            else if (op == "==" || op == "يساوي")
            {
                GenerateEqual();
            }
            return typeof(bool);
        }
        return leftType;
    }
    public override Type? VisitSimple_expression(ExprParser.Simple_expressionContext context)
    {
        var resultType = Visit(context.term(0));
        for (int i = 0; i < context.add_op().Length; i++)
        {
            Visit(context.term(i + 1));
            var op = context.add_op(i).GetText();
            if (op == "+" || op == "مجموع")
            {
                GenerateAdd();
                resultType = typeof(int);
            }
            else if (op == "-")
            {
                GenerateSubtract();
                resultType = typeof(int);
            }
            else if (op == "||" || op == "او")
            {
                GenerateOr();
                resultType = typeof(bool);
            }
        }
        return resultType;
    }
    public override Type? VisitTerm(ExprParser.TermContext context)
    {
        var resultType = Visit(context.factor(0));
        for (int i = 0; i < context.mul_op().Length; i++)
        {
            Visit(context.factor(i + 1));
            var op = context.mul_op(i).GetText();
            if (op == "*" || op == "ضرب")
            {
                GenerateMultiply();
                resultType = typeof(int);
            }
            else if (op == "/")
            {
                GenerateDivide();
                resultType = typeof(int);
            }
            else if (op == "%")
            {
                GenerateModulo();
                resultType = typeof(int);
            }
            else if (op == "&&" || op == "و")
            {
                GenerateAnd();
                resultType = typeof(bool);
            }
        }
        return resultType;
    }
    public override Type? VisitFactor(ExprParser.FactorContext context)
    {
        if (context.constant_value() != null)
        {
            return Visit(context.constant_value());
        }
        if (context.variable_access() != null)
        {
            return Visit(context.variable_access());
        }
        return null;
    }
    public override Type? VisitVariable_access(ExprParser.Variable_accessContext context)
    {
        var variableName = context.ID().GetText();
        var symbol = _symbolTable.Resolve(variableName);
        GenerateLoadSymbol(symbol);
        return GetDotNetType(symbol.Type);
    }

    public override Type? VisitConstant_value(ExprParser.Constant_valueContext context)
    {
        if (context.numeric_value() != null)
        {
            return Visit(context.numeric_value());
        }
        if (context.ID() != null)
        {
            var variableName = context.ID().GetText();
            try
            {
                var symbol = _symbolTable.Resolve(variableName);
                GenerateLoadSymbol(symbol);
                return GetDotNetType(symbol.Type);
            }
            catch (Exception ex) when (ex.Message.Contains("غير مُعرَّف"))
            {
                throw new NotSupportedException($"خطأ دلالي: المعرّف '{variableName}' غير مُعرَّف كمتغير، ولا ندعم استخدامه كثابت بعد.", ex);
            }
        }
        if (context.literal_value() != null)
        {
            var literal = context.literal_value();
            if (literal.STRING_LITERAL() != null)
            {
                var text = literal.STRING_LITERAL().GetText();
                var cleanText = text.Substring(1, text.Length - 2);
                GeneratePushString(cleanText);
                return typeof(string);
            }
        }
        if (context.logical_value() != null)
        {
            throw new NotSupportedException("خطأ دلالي: دعم القيم المنطقية (True/False) غير مُطبق بعد.");
        }
        return null;
    }
    public override Type? VisitNumeric_value(ExprParser.Numeric_valueContext context)
    {
        if (context.INTEGER() != null)
        {
            if (int.TryParse(context.INTEGER().GetText(), out int value))
            {
                GeneratePushInt(value);
                return typeof(int);
            }
            else
            {
                throw new Exception($"خطأ في القيمة: '{context.GetText()}' ليست عددًا صحيحًا صالحًا.");
            }
        }
        return null;
    }
    #endregion
    }
}