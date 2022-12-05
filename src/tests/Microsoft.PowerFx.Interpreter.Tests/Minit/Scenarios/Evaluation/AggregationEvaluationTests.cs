// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Reflection;
using Microsoft.PowerFx.Interpreter.Minit;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests.Minit.Scenarios.Evaluation
{
    public class AggregationEvaluationTests : IClassFixture<MinitDataFixture>
    {
        private readonly MinitDataFixture _fixture;

        public AggregationEvaluationTests(MinitDataFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [InlineData("Sum(ViewEvents, Duration)", 270)]
        public void EvaluateTotalDurationOfEventsInView(string fxExpression, object expectedEvaluationResult)
        {
            // Arrange
            var engine = PowerFxEngineFactory.Create(_fixture.Events);

            // Act
            var evaluationResult = engine.Eval(fxExpression);

            // Assert
            Assert.Equal(expectedEvaluationResult, evaluationResult.ToObject());
        }

        [Theory]
        [InlineData("Sum(ProcessEvents, Duration)", 480.0)]
        public void EvaluateTotalDurationOfEventsInProcess(string fxExpression, object expectedEvaluationResult)
        {
            // Arrange
            var engine = PowerFxEngineFactory.Create(_fixture.Events);

            // Act
            var evaluationResult = engine.Eval(fxExpression);

            // Assert
            Assert.Equal(expectedEvaluationResult, evaluationResult.ToObject());
        }
    }
}
