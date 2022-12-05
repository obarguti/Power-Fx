// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Reflection;
using Microsoft.PowerFx.Interpreter.Minit;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests.Minit.Scenarios.Tranlsation
{
    public class FiltrationTranslationTests
    {
        [Theory]
        [InlineData("Unknown", "SumIf(CaseEvents, activity=\"A\" || activity=\"B\", Duration()) > ToTime(0, 25, 0)")]
        public void TranslateFilterCasesWithTotalDurationForActivitiesAAndBLongerThan25Minutes(string fxExpression, string expectedMinitExpression)
        {
            // Arrange
            var converter = new MinitConverter();

            // Act
            var translationResult = converter.Convert(fxExpression);

            // Assert
            Assert.Equal(expectedMinitExpression, translationResult);
        }
    }
}
