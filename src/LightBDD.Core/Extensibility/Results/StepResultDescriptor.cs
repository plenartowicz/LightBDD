namespace LightBDD.Core.Extensibility.Results
{
    /// <summary>
    /// Simple step result descriptor. It stores result of a step method.
    /// </summary>
    public class StepResultDescriptor : DefaultStepResultDescriptor
    {
        /// <summary>
        /// Result of a step method (can be null).
        /// </summary>
        public object StepResult { get; }

        /// <summary>
        /// Constructor allowing store result of a step method.
        /// </summary>
        /// <param name="stepResult">result of a step method (can be null).</param>
        public StepResultDescriptor(object stepResult)
        {
            StepResult = stepResult;
        }
    }
}