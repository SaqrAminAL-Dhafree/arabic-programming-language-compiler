// SymbolTable.cs (كود كامل - جديد)
using System.Collections.Generic;
using System;

namespace ArabicCompiler
{
    // يمثل نطاقاً واحداً (مثل دالة، بلوك)
    public class Scope
    {
    public Scope? ParentScope { get; }
    private readonly Dictionary<string, Symbol> _symbols = new Dictionary<string, Symbol>();

    public Scope(Scope? parent)
    {
        ParentScope = parent;
    }

    // Expose symbols for debugging/dump purposes
    public IEnumerable<Symbol> GetSymbols()
    {
        return _symbols.Values;
    }

    public void AddSymbol(Symbol symbol)
    {
        if (_symbols.ContainsKey(symbol.Name))
        {
            throw new Exception($"خطأ دلالي: المعرف '{symbol.Name}' مُعرّف مسبقاً في هذا النطاق.");
        }
        _symbols.Add(symbol.Name, symbol);
    }

    // البحث في النطاق الحالي والنطاقات الأبوية (Resolve)
    public Symbol ResolveSymbol(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
        {
            return symbol;
        }

        if (ParentScope != null)
        {
            return ParentScope.ResolveSymbol(name);
        }

        throw new Exception($"خطأ دلالي: المتغير '{name}' غير مُعرَّف.");
    }
}

// الفئة الرئيسية لإدارة جداول النطاق (Stack of Scopes)
public class SymbolTable
{
    private Scope? _currentScope;
    
    public SymbolTable()
    {
        // النطاق العام للبرنامج يبدأ بنطاق أبوي فارغ
        _currentScope = new Scope(null); 
    }

    // الدخول إلى نطاق جديد (مثلاً: عند دخول إلى كتلة { ... })
    public void EnterScope()
    {
        // النطاق الجديد يأخذ النطاق الحالي كأبوي
        _currentScope = new Scope(_currentScope); 
    }

    // الخروج من النطاق الحالي
    public void ExitScope()
    {
        if (_currentScope?.ParentScope == null)
        {
            // هذا الخطأ لا ينبغي أن يحدث في برنامج سليم
            throw new Exception("خطأ داخلي: محاولة الخروج من النطاق العام.");
        }
        _currentScope = _currentScope.ParentScope;
    }

    // إضافة رمز إلى النطاق الحالي
    public void Add(Symbol symbol)
    {
        _currentScope!.AddSymbol(symbol);
    }

    // البحث عن رمز باستخدام قواعد النطاق
    public Symbol Resolve(string name)
    {
        return _currentScope!.ResolveSymbol(name);
    }

    // إرجاع قائمة بكل الرموز في جميع النطاقات مع تسمية بسيطة للنطاق
    public List<SymbolDumpEntry> GetAllSymbols()
    {
        var root = _currentScope;
        // الوصول إلى الجذر
        while (root?.ParentScope != null) root = root.ParentScope;
        var result = new List<SymbolDumpEntry>();
        CollectScopeSymbols(root, "Global", result);
        return result;
    }

    private void CollectScopeSymbols(Scope? scope, string scopeName, List<SymbolDumpEntry> result)
    {
        if (scope == null) return;
        foreach (var sym in scope.GetSymbols())
        {
            result.Add(new SymbolDumpEntry(sym.Name, sym.Type, scopeName));
        }
        // لا يوجد وسيلة مباشرة للوصول إلى النطاقات الفرعية من هذا التصميم البسيط.
        // لكن لأننا نتخيل أن النطاقات تُنشأ وتتراجع فقط، سنعرض الرموز من الجذر فقط كدليل.
    }
}

    public record SymbolDumpEntry(string Name, DataType Type, string Scope);
}