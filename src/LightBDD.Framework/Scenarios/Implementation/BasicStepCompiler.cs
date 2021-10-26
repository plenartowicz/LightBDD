using LightBDD.Core.Execution;
using LightBDD.Core.Extensibility;
using LightBDD.Core.Extensibility.Results;
using LightBDD.Framework.Implementation;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace LightBDD.Framework.Scenarios.Implementation
{
    internal static class BasicStepCompiler
    {
        public static StepDescriptor ToAsynchronousStep(Func<Task> step)
        {
            try
            {
                var methodInfo = step.GetMethodInfo();
                EnsureNotGenerated(methodInfo);
                return new StepDescriptor(methodInfo, new AsyncStepExecutor(step).ExecuteAsync);
            }
            catch (Exception ex)
            {
                return StepDescriptor.CreateInvalid(ex);
            }
        }

        public static StepDescriptor ToSynchronousStep(Action step)
        {
            try
            {
                var methodInfo = step.GetMethodInfo();
                EnsureNotGenerated(methodInfo);
                return new StepDescriptor(methodInfo, new StepExecutor(step).Execute);
            }
            catch (Exception ex)
            {
                return StepDescriptor.CreateInvalid(ex);
            }
        }

        private static void EnsureNotGenerated(MethodInfo methodInfo)
        {
            if (Reflector.IsGenerated(methodInfo))
                throw new ArgumentException($"The basic step syntax does not support compiler generated methods, such as {methodInfo}, as rendered step name will be unreadable. Please either pass the step method name directly or use other methods for declaring steps.");
        }

        private class AsyncStepExecutor
        {
            private static readonly MethodInfo AsResultDescriptorMethod = ((Func<Task, Task<IStepResultDescriptor>>)AsResultDescriptor<IStepResultDescriptor>).GetMethodInfo().GetGenericMethodDefinition();
            private static readonly MethodInfo WrapIntoResultDescriptorMethod = ((Func<Task, Task<IStepResultDescriptor>>)WrapIntoResultDescriptor<IStepResultDescriptor>).GetMethodInfo().GetGenericMethodDefinition();
            private readonly Func<Task> _invocation;

            public AsyncStepExecutor(Func<Task> invocation)
            {
                _invocation = invocation;
            }

            public async Task<IStepResultDescriptor> ExecuteAsync(object context, object[] args)
            {
                var task = _invocation.Invoke();
                await ScenarioExecutionFlow.WrapScenarioExceptions(task);

                return await ConvertToResultDescriptor(task);
            }

            private static bool HasResultDescriptor(Task task)
            {
                var taskType = task.GetType().GetTypeInfo();
                if (!taskType.IsGenericType)
                    return false;
                return taskType.GenericTypeArguments.Length == 1
                    && typeof(IStepResultDescriptor).GetTypeInfo().IsAssignableFrom(taskType.GenericTypeArguments[0].GetTypeInfo());
            }

            private static bool IsGeneric(Task task)
            {
                var taskType = task.GetType().GetTypeInfo();
                return taskType.IsGenericType && taskType.GenericTypeArguments.Length == 1;
            }

            private static Task<IStepResultDescriptor> AsResultDescriptor(Task task)
            {
                return (Task<IStepResultDescriptor>)AsResultDescriptorMethod
                                                    .MakeGenericMethod(task.GetType().GetTypeInfo().GenericTypeArguments)
                                                    .Invoke(null, new object[] { task });
            }

            private static async Task<IStepResultDescriptor> AsResultDescriptor<T>(Task stepTask) where T : IStepResultDescriptor
            {
                return await (Task<T>)stepTask;
            }

            private static Task<IStepResultDescriptor> WrapIntoResultDescriptor(Task task)
            {
                return (Task<IStepResultDescriptor>)WrapIntoResultDescriptorMethod
                                                    .MakeGenericMethod(task.GetType().GetTypeInfo().GenericTypeArguments)
                                                    .Invoke(null, new object[] { task });
            }

            private static async Task<IStepResultDescriptor> WrapIntoResultDescriptor<T>(Task stepTask)
            {
                var result = await (Task<T>)stepTask;
                return new StepResultDescriptor(result);
            }

            private static async Task<IStepResultDescriptor> ConvertToResultDescriptor(Task task)
            {
                if (!IsGeneric(task))
                {
                    return DefaultStepResultDescriptor.Instance;
                }

                if (!HasResultDescriptor(task))
                {
                    return await WrapIntoResultDescriptor(task);
                }

                return await AsResultDescriptor(task);
            }
        }

        private class StepExecutor
        {
            private readonly Action _invocation;

            public StepExecutor(Action invocation)
            {
                _invocation = invocation;
            }
            public Task<IStepResultDescriptor> Execute(object context, object[] args)
            {
                try
                {
                    _invocation.Invoke();
                    return Task.FromResult(DefaultStepResultDescriptor.Instance);
                }
                catch (Exception e)
                {
                    throw new ScenarioExecutionException(e);
                }
            }
        }
    }
}