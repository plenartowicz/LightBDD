﻿using LightBDD.Framework.Expectations.Implementation;

namespace LightBDD.Framework.Expectations
{
    /// <summary>
    /// Type allowing to define expectations.
    /// </summary>
    public static class Expect
    {
        /// <summary>
        /// Creates expectation composer.
        /// </summary>
        public static IExpectationComposer To => new ExpectationComposer();
    }
}