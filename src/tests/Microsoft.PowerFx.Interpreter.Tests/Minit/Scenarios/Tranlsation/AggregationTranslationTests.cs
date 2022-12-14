// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Reflection;
using Microsoft.PowerFx.Interpreter.Minit;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests.Minit.Scenarios.Tranlsation
{
    public class AggregationTranslationTests
    {
        private readonly MinitConverter _converter;

        public AggregationTranslationTests()
        {
            _converter = new MinitConverter();
        }

        [Theory]
        [InlineData("Sum(ViewEvents, Duration)", "Sum(ViewEvents, Duration())")]
        public void TranslateTotalDurationOfEventsInView(string fxExpression, string expectedMinitExpression)
        {
            Assert.Equal(expectedMinitExpression, _converter.Convert(fxExpression));
        }

        [Theory]
        [InlineData("Sum(ProcessEvents, Duration)", "Sum(ProcessEvents, Duration())")]
        public void TranslateTotalDurationOfEventsInProcess(string fxExpression, string expectedMinitExpression)
        {
            Assert.Equal(expectedMinitExpression, _converter.Convert(fxExpression));
        }

        [Theory]
        [InlineData("Sum(CaseEvents, Duration)", "Sum(CaseEvents, Duration())")]
        public void TranslateTotalDurationOfEventsPerCase(string fxExpression, string expectedMinitExpression)
        {
            Assert.Equal(expectedMinitExpression, _converter.Convert(fxExpression));
        }

        [Theory]
        [InlineData("Sum(EventsPerAttribute, Duration)", "Sum(EventsPerAttribute, Duration())")]
        public void TranslateTotalDurationPerActivity(string fxExpression, string expectedMinitExpression)
        {
            Assert.Equal(expectedMinitExpression, _converter.Convert(fxExpression));
        }

        [Theory]
        [InlineData("Sum(EventsPerAttribute, Duration)", "Sum(EventsPerAttribute, Duration())")]
        public void TranslateTotalDurationPerUser(string fxExpression, string expectedMinitExpression)
        {
            Assert.Equal(expectedMinitExpression, _converter.Convert(fxExpression));
        }

        [Theory]
        [InlineData("Sum(EventsPerAttribute, Duration)", "Sum(EventsPerAttribute, Duration())")]
        public void TranslateTotalDurationOfCasesProcessedPerUser(string fxExpression, string expectedMinitExpression)
        {
            Assert.Equal(expectedMinitExpression, _converter.Convert(fxExpression));
        }
    }
}
