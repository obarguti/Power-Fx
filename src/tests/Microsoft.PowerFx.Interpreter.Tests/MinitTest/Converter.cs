// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Minit
{
    public class Converter
    {
        // Builtin Identifiers
        // PowerFx can have identifiers that represent filters. 
        public static readonly string[] AggregationFunctions = { "GroupBy" };
        public static readonly string[] AggregationScopes = { "Process", "View" };
        public static readonly string[] AggregationDimensions = { "Cases", "Events", "Edges" };

        // By default, all events have 3 fields builtin. 
        // They can have additional custom fields too ("Attributes")
        internal readonly RecordType _eventType = RecordType.Empty()            
            .Add("Duration", FormulaType.Number) // Logically, (Start-End)
            .Add("Start", FormulaType.DateTime)
            .Add("End", FormulaType.DateTime)
            .Add("Activity", FormulaType.String);
        
        internal readonly ReadOnlySymbolTable _builtinSymbols;
        internal readonly IList<ISymbolSlot> _allAggregationFunctions;
        internal readonly IList<ISymbolSlot> _allAggregationScopes;
        internal readonly IList<ISymbolSlot> _allAggregationDimenstions;

        public Converter()
        {
            var symTable = new SymbolTable();
            _allAggregationFunctions = new List<ISymbolSlot>();
            _allAggregationScopes = new List<ISymbolSlot>();
            _allAggregationDimenstions = new List<ISymbolSlot>();

            foreach (var function in AggregationFunctions)
            {
                _allAggregationFunctions.Add(symTable.AddVariable(function, _eventType.ToTable()));
            }

            foreach (var scope in AggregationScopes)
            {
                _allAggregationScopes.Add(symTable.AddVariable(scope, _eventType.ToTable()));
            }

            foreach (var dim in AggregationDimensions)
            {
                _allAggregationDimenstions.Add(symTable.AddVariable(dim, _eventType.ToTable()));
            }

            _builtinSymbols = symTable;
        }

        public string Convert(string inputFx)
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);

            // Ensure we can parse and bind the expression. 
            var check = engine.Check(inputFx, symbolTable: _builtinSymbols);
            check.ThrowOnErrors();

            (var irnode, var ruleScopeSymbol) = IRTranslator.Translate(check._binding);

            var visitor = new MinitVisitor();
            var ctx = new VisitorContext
            {
                _parent = this
            };
            var result = irnode.Accept(visitor, ctx);

            var output = result._miniExpr;
            return output;
        }

        // Add SumGroup, SumIf functions 
    }

    // Context mpassed between each visitor node. 
    public class VisitorContext
    {
        public Converter _parent;        
    }

    // return result from visitor method. 
    public class VisitorResult
    {
        public string _miniExpr;
    }

    // Walk an Power Fx tree and write out in target language 
    internal class MinitVisitor : IRNodeVisitor<VisitorResult, VisitorContext>
    {
        private static bool Equals(ISymbolSlot slotA, ISymbolSlot slotB)
        {
            if (slotA == null && slotB == null)
            {
                return true;
            }

            return slotA != null && !slotA.IsDisposed() &&
                slotB != null && !slotB.IsDisposed() &&
                slotA.SlotIndex == slotB.SlotIndex &&
                slotA.Owner == slotB.Owner;
        }

        public override VisitorResult Visit(TextLiteralNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(NumberLiteralNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(BooleanLiteralNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ColorLiteralNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(RecordNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ErrorNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(LazyEvalNode node, VisitorContext context)
        {
            // Common for all predicates, like arg1 in Sum:
            //   Sum(ProcessEvents, Duration)
            var ret = node.Child.Accept(this, context);
            return ret;
        }

        public override VisitorResult Visit(CallNode node, VisitorContext context)
        {
            var func = node.Function;

            // GroupBy(<Process|View>, <Cases|Events|Edges>, <Max|Min|Count|...etc>(<Formula>))
            if (func.Name != "GroupBy")
            {
                throw new NotImplementedException($"Function {func.Name} is not implemented.");
            }

            var arg0 = GetAggregationScopeArgument(node, context);
            var arg1 = GetAggregationDimensionArgument(node, context);

            var arg2 = node.Args[2].Accept(this, context);

            var ret = new VisitorResult
            {
                _miniExpr = $"Sum({arg0}, {arg2._miniExpr})"
            };

            return ret;
        }

        private static string GetAggregationScopeArgument(CallNode node, VisitorContext context)
        {
            string arg0 = null;
            if (node.Args[0] is ResolvedObjectNode aggregationScopeNode && aggregationScopeNode.Value is ISymbolSlot slot)
            {
                var symbolSlot = context._parent._allAggregationScopes.SingleOrDefault(s => Equals(slot, s));

                if (symbolSlot is not null and NameSymbol)
                {
                    arg0 = ((NameSymbol)symbolSlot).Name;
                }
            }

            if (arg0 == null)
            {
                throw new NotImplementedException($"Unrecognized arg {node.Args[0].IRContext.SourceContext}");
            }

            return arg0;
        }

        private static string GetAggregationDimensionArgument(CallNode node, VisitorContext context)
        {
            string arg1 = null;
            if (node.Args[1] is ResolvedObjectNode aggregationDimensionNode && aggregationDimensionNode.Value is ISymbolSlot slot)
            {
                var symbolSlot = context._parent._allAggregationDimenstions.SingleOrDefault(s => Equals(slot, s));

                if (symbolSlot is not null and NameSymbol)
                {
                    arg1 = ((NameSymbol)symbolSlot).Name;
                }
            }

            if (arg1 == null)
            {
                throw new NotImplementedException($"Unrecognized arg {node.Args[1].IRContext.SourceContext}");
            }

            return arg1;
        }

        public override VisitorResult Visit(BinaryOpNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(UnaryOpNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ScopeAccessNode node, VisitorContext context)
        {
            if (node.Value is ScopeAccessSymbol sym)
            {
                // Refers to a field on Arg1. 
                var name = sym.Name.Value;
                if (name == "Duration")
                {
                    // In minit, duration is a function. 
                    var ret = new VisitorResult
                    {
                        _miniExpr = "Duration()"
                    };
                    return ret;
                }
            }

            throw new NotImplementedException();
        }

        public override VisitorResult Visit(RecordFieldAccessNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ResolvedObjectNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(SingleColumnTableAccessNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ChainingNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(AggregateCoercionNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }
    }
}
