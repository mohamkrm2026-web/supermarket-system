using SuperMarket.Services;

namespace SuperMarket.Middleware
{
    public class LicenseMiddleware
    {
        private readonly RequestDelegate _next;

        public LicenseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // السماح بصفحة انتهاء الترخيص نفسها + الملفات الثابتة (CSS/JS)
            var path = context.Request.Path.Value ?? "";
            bool isAllowed = path.StartsWith("/License") ||
                             path.StartsWith("/api") ||
                             path.StartsWith("/css") ||
                             path.StartsWith("/js") ||
                             path.StartsWith("/lib") ||
                             path.StartsWith("/favicon");

            if (!LicenseService.IsValid() && !isAllowed)
            {
                context.Response.Redirect("/License/Expired");
                return;
            }

            await _next(context);
        }
    }
}
