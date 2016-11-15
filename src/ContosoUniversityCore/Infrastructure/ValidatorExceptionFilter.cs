namespace ContosoUniversityCore.Infrastructure
{
    using FluentValidation;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public class ValidatorExceptionFilter : ExceptionFilterAttribute
    {
        private readonly ILogger<ValidatorExceptionFilter> _logger;

        public ValidatorExceptionFilter(ILoggerFactory loggingFactory)
        {
            _logger = loggingFactory.CreateLogger<ValidatorExceptionFilter>();
        }

        public override void OnException(ExceptionContext context)
        {
            var exception = context.Exception as ValidationException;
            if (exception == null) return;

            SetModelState(context, exception);
            GenerateResponse(context);
            LogError(context, exception);
        }

        private static void SetModelState(ExceptionContext context, ValidationException exception)
        {
            var modelState = context.ModelState;
            foreach (var error in exception.Errors)
            {
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        private static void GenerateResponse(ExceptionContext context)
        {
            var result = new ContentResult();
            var content = JsonConvert.SerializeObject(context.ModelState,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
            result.Content = content;
            result.ContentType = "application/json";

            context.HttpContext.Response.StatusCode = 400;
            context.Result = result;
        }

        private void LogError(ExceptionContext actionExecutedContext, ValidationException exception)
        {
            _logger.LogWarning(exception.Message);
        }
    }
}