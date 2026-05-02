
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace project
{
    public class SessionCheckAttribute : ActionFilterAttribute
    {
        private readonly string _requiredRole;

        public SessionCheckAttribute(string requiredRole)
        {
            _requiredRole = requiredRole;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            var userRole = context.HttpContext.Session.GetString("UserRole");

            if (userId == null || userRole == null)
            {
                context.Result = new RedirectToActionResult("Log", "Login", null);
                return;
            }

            if (userRole != _requiredRole)
            {
                if (userRole == "Admin")
                {
                    context.Result = new RedirectToActionResult("index2", "User", null);
                }
                else
                {
                    context.Result = new RedirectToActionResult("index1", "User", null);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}