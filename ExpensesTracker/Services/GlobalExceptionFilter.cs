using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace ExpensesTracker.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "An unhandled exception occurred");

            if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                context.Result = new JsonResult(new
                {
                    success = false,
                    message = "An error occurred. Please try again later."
                })
                {
                    StatusCode = 500
                };
            }
            else
            {
                context.Result = new RedirectToActionResult("Error", "Home", new { area = "" });
            }

            context.ExceptionHandled = true;
        }
    }
}