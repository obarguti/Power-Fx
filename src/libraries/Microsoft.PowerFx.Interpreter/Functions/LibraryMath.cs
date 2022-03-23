﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Functions
{
    // Direct ports from JScript. 
    internal static partial class Library
    {
        private static readonly object _randomizerLock = new object();

        [ThreadSafeProtectedByLock(nameof(_randomizerLock))]
        private static Random _random;

        // Support for aggregators. Helpers to ensure that Scalar and Tabular behave the same.
        private interface IAggregator
        {
            void Apply(FormulaValue value);

            FormulaValue GetResult(IRContext irContext);
        }

        private class SumAgg : IAggregator
        {
            protected int _count;
            protected double _accumulator;

            public void Apply(FormulaValue value)
            {
                if (value is BlankValue)
                {
                    return;
                }

                var n1 = (NumberValue)value;

                _accumulator += n1.Value;
                _count++;
            }

            public virtual FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return new BlankValue(irContext);
                }

                return new NumberValue(irContext, _accumulator);
            }
        }

        private class MinAgg : IAggregator
        {
            protected double _minValue = double.MaxValue;
            protected int _count;

            public void Apply(FormulaValue value)
            {
                _count++;
                if (value is BlankValue)
                {
                    return;
                }

                var n1 = (NumberValue)value;
                if (n1.Value < _minValue)
                {
                    _minValue = n1.Value;
                }
            }

            public virtual FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return new BlankValue(irContext);
                }

                return new NumberValue(irContext, _minValue);
            }
        }

        private class MaxAgg : IAggregator
        {
            protected double _maxValue = double.MinValue;
            protected int _count;

            public void Apply(FormulaValue value)
            {
                _count++;
                if (value is BlankValue)
                {
                    return;
                }

                var n1 = (NumberValue)value;
                if (n1.Value > _maxValue)
                {
                    _maxValue = n1.Value;
                }
            }

            public virtual FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return new BlankValue(irContext);
                }

                return new NumberValue(irContext, _maxValue);
            }
        }

        private class AverageAgg : SumAgg
        {
            public override FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return CommonErrors.DivByZeroError(irContext);
                }

                return new NumberValue(irContext, _accumulator / _count);
            }
        }

        private static FormulaValue RunAggregator(IAggregator agg, IRContext irContext, FormulaValue[] values)
        {
            foreach (var value in values)
            {
                agg.Apply(value);
            }

            return agg.GetResult(irContext);
        }

        private static FormulaValue RunAggregator(IAggregator agg, EvalVisitor runner, SymbolContext context, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];

            foreach (var row in arg0.Rows)
            {
                if (row.IsValue)
                {
                    var childContext = context.WithScopeValues(row.Value);
                    var value = arg1.Eval(runner, childContext);

                    if (value is NumberValue number)
                    {
                        value = FiniteChecker(irContext, 0, number);
                    }

                    if (value is ErrorValue error)
                    {
                        return error;
                    }

                    agg.Apply(value);
                }
            }

            return agg.GetResult(irContext);
        }

        private static FormulaValue Sqrt(IRContext irContext, NumberValue[] args)
        {
            var n1 = args[0];

            var result = Math.Sqrt(n1.Value);

            return new NumberValue(irContext, result);
        }

        // Sum(1,2,3)     
        internal static FormulaValue Sum(IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(new SumAgg(), irContext, args);
        }

        // Sum([1,2,3], Value * Value)     
        public static FormulaValue SumTable(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(new SumAgg(), runner, symbolContext, irContext, args);
        }

        // Max(1,2,3)     
        internal static FormulaValue Max(IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(new MaxAgg(), irContext, args);
        }

        // Max([1,2,3], Value * Value)     
        public static FormulaValue MaxTable(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(new MaxAgg(), runner, symbolContext, irContext, args);
        }

        // Min(1,2,3)     
        internal static FormulaValue Min(IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(new MinAgg(), irContext, args);
        }

        // Min([1,2,3], Value * Value)     
        public static FormulaValue MinTable(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(new MinAgg(), runner, symbolContext, irContext, args);
        }

        // Average ignores blanks.
        // Average(1,2,3)
        public static FormulaValue Average(IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(new AverageAgg(), irContext, args);
        }

        // Average([1,2,3], Value * Value)     
        public static FormulaValue AverageTable(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TableValue)args[0];

            if (arg0.Rows.Count() == 0)
            {
                return CommonErrors.DivByZeroError(irContext);
            }

            return RunAggregator(new AverageAgg(), runner, symbolContext, irContext, args);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-mod
        public static FormulaValue Mod(IRContext irContext, NumberValue[] args)
        {
            var arg0 = args[0];
            var arg1 = args[1];

            return new NumberValue(irContext, arg0.Value % arg1.Value);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-sequence
        public static FormulaValue Sequence(IRContext irContext, NumberValue[] args)
        {
            var records = args[0].Value;
            var start = args[1].Value;
            var step = args[2].Value;

            var rows = LazySequence(records, start, step).Select(n => new NumberValue(IRContext.NotInSource(FormulaType.Number), n));

            return new InMemoryTableValue(irContext, StandardTableNodeRecords(irContext, rows.ToArray()));
        }

        private static IEnumerable<double> LazySequence(double records, double start, double step)
        {
            var x = start;
            for (var i = 1; i <= records; i++)
            {
                yield return x;
                x += step;
            }
        }

        public static FormulaValue Abs(IRContext irContext, NumberValue[] args)
        {
            var arg0 = args[0];
            var x = arg0.Value;
            var val = Math.Abs(x);
            return new NumberValue(irContext, val);
        }

        public static FormulaValue Round(IRContext irContext, NumberValue[] args)
        {
            var numberArg = args[0].Value;
            var digitsArg = args[1].Value;

            var x = Round(numberArg, digitsArg);
            return new NumberValue(irContext, x);
        }

        public static double Round(double number, double digits)
        {
            if (digits == 0)
            {
                return Math.Round(number);
            }

            var multiplier = Math.Pow(10, digits < 0 ? Math.Ceiling(digits) : Math.Floor(digits));

            // Deal with catastrophic loss of precision
            if (IsInvalidDouble(multiplier))
            {
                return Math.Round(number);
            }

            return Math.Round(number * multiplier) / multiplier;
        }

        // Char is used for PA string escaping 
        public static FormulaValue RoundUp(IRContext irContext, NumberValue[] args)
        {
            var numberArg = args[0].Value;
            var digitsArg = args[1].Value;

            var x = RoundUp(numberArg, digitsArg);
            return new NumberValue(irContext, x);
        }

        public static double RoundUp(double number, double digits)
        {
            if (digits == 0)
            {
                return number < 0 ? Math.Floor(number) : Math.Ceiling(number);
            }

            var multiplier = Math.Pow(10, digits < 0 ? Math.Ceiling(digits) : Math.Floor(digits));

            // Contracts.Assert(multiplier != 0);

            // Deal with catastrophic loss of precision
            if (IsInvalidDouble(multiplier))
            {
                return number < 0 ? Math.Floor(number) : Math.Ceiling(number);
            }

            // TASK: 74286: Spec corner case behavior: NaN, +Infinity, -Infinity.
            return number < 0 ?
                Math.Floor(number * multiplier) / multiplier :
                Math.Ceiling(number * multiplier) / multiplier;
        }

        public static FormulaValue RoundDown(IRContext irContext, NumberValue[] args)
        {
            var numberArg = args[0].Value;
            var digitsArg = args[1].Value;

            var x = RoundDown(numberArg, digitsArg);
            return new NumberValue(irContext, x);
        }

        public static double RoundDown(double number, double digits)
        {
            if (digits == 0)
            {
                return number < 0 ? Math.Ceiling(number) : Math.Floor(number);
            }

            var multiplier = Math.Pow(10, digits < 0 ? Math.Ceiling(digits) : Math.Floor(digits));

            // DebugContracts.assert(multiplier !== 0);

            // Deal with catastrophic loss of precision
            if (IsInvalidDouble(multiplier))
            {
                return number < 0 ? Math.Ceiling(number) : Math.Floor(number);
            }

            // TASK: 74286: Spec corner case behavior: NaN, +Infinity, -Infinity.
            return number < 0 ?
                Math.Ceiling(number * multiplier) / multiplier :
                Math.Floor(number * multiplier) / multiplier;
        }

        public static FormulaValue Int(IRContext irContext, NumberValue[] args)
        {
            var arg0 = args[0];
            var x = arg0.Value;
            var val = Math.Floor(x);
            return new NumberValue(irContext, val);
        }

        public static FormulaValue Ln(IRContext irContext, NumberValue[] args)
        {
            var number = args[0].Value;
            return new NumberValue(irContext, Math.Log(number));
        }

        public static FormulaValue Log(IRContext irContext, NumberValue[] args)
        {
            var number = args[0].Value;
            var numberBase = args[1].Value;
            if (numberBase == 1)
            {
                return GetDiv0Error(irContext);
            }

            return new NumberValue(irContext, Math.Log(number, numberBase));
        }

        public static FormulaValue Exp(IRContext irContext, NumberValue[] args)
        {
            var exponent = args[0].Value;
            return new NumberValue(irContext, Math.Pow(Math.E, exponent));
        }

        public static FormulaValue Power(IRContext irContext, NumberValue[] args)
        {
            var number = args[0].Value;
            var exponent = args[1].Value;

            if (number == 0)
            {
                if (exponent < 0)
                {
                    return GetDiv0Error(irContext);
                }
                else if (exponent == 0)
                {
                    return new ErrorValue(irContext, new ExpressionError
                    {
                        Kind = ErrorKind.Numeric,
                        Span = irContext.SourceContext,
                        Message = "Invalid exponent"
                    });
                }
            }

            var result = new NumberValue(irContext, Math.Pow(number, exponent));
            return FiniteChecker(irContext, 1, result);
        }

        private static FormulaValue Rand(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            lock (_randomizerLock)
            {
                if (_random == null)
                {
                    _random = new Random();
                }

                return new NumberValue(irContext, _random.NextDouble());
            }
        }

        public static FormulaValue RandBetween(IRContext irContext, NumberValue[] args)
        {
            var lower = args[0].Value;
            var upper = args[1].Value;

            if (lower > upper)
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"Lower value cannot be greater than Upper value",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.Numeric
                });
            }

            lower = Math.Ceiling(lower);
            upper = Math.Floor(upper);

            lock (_randomizerLock)
            {
                if (_random == null)
                {
                    _random = new Random();
                }

                return new NumberValue(irContext, Math.Floor((_random.NextDouble() * (upper - lower + 1)) + lower));
            }
        }

        private static FormulaValue Pi(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            return new NumberValue(irContext, Math.PI);
        }

        // Given the absence of Math.Cot function, we compute Cot(x) as 1/Tan(x)
        // Reference: https://en.wikipedia.org/wiki/Trigonometric_functions
        private static FormulaValue Cot(IRContext irContext, NumberValue[] args)
        {
            var arg = args[0].Value;
            var tan = Math.Tan(arg);
            if (tan == 0)
            {
                return GetDiv0Error(irContext);
            }

            return new NumberValue(irContext, 1 / tan);
        }

        // Given the absence of Math.Acot function, we compute acot(x) as pi/2 - atan(x)
        // Reference: https://en.wikipedia.org/wiki/Inverse_trigonometric_functions
        public static FormulaValue Acot(IRContext irContext, NumberValue[] args)
        {
            var arg = args[0].Value;
            var atan = Math.Atan(arg);
            return new NumberValue(irContext, (Math.PI / 2) - atan);
        }

        public static FormulaValue Atan2(IRContext irContext, NumberValue[] args)
        {
            var x = args[0].Value;
            var y = args[1].Value;

            if (x == 0 && y == 0)
            {
                return GetDiv0Error(irContext);
            }

            // Unlike Excel, C#'s Math.Atan2 expects 'y' as first argument and 'x' as second.
            return new NumberValue(irContext, Math.Atan2(y, x));
        }

        public static Func<IRContext, NumberValue[], FormulaValue> SingleArgTrig(string functionName, Func<double, double> function)
        {
            return (IRContext irContext, NumberValue[] args) =>
            {
                var arg = args[0].Value;
                var result = function(arg);
                if (double.IsNaN(result) || double.IsInfinity(result))
                {
                    return new ErrorValue(irContext, new ExpressionError
                    {
                        Message = $"Invalid argument to the {functionName} function.",
                        Span = irContext.SourceContext,
                        Kind = ErrorKind.Numeric
                    });
                }

                return new NumberValue(irContext, result);
            };
        }

        private static ErrorValue GetDiv0Error(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError
            {
                Kind = ErrorKind.Div0,
                Span = irContext.SourceContext,
                Message = "Division by zero"
            });
        }
    }
}
