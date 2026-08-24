using Microsoft.AspNetCore.Mvc;

namespace web.Infrastructure
{
    /// <summary>
    /// Typed shape for a JSON toast response (see ToastExtensions.ToastJson), used instead of an anonymous
    /// object so ToastTranslationFilter (see ToastTranslation.cs) can reliably recognize and translate it -
    /// an anonymous type can't be pattern-matched or rebuilt with a changed property.
    /// </summary>
    public sealed record ToastPayload(bool Success, string Message, string Type);

    public static class ToastExtensions
    {
        public const string SuccessKey = "SuccessMessage";
        public const string InfoKey = "InfoMessage";
        public const string WarningKey = "WarningMessage";
        public const string ErrorKey = "ErrorMessage";

        public static void ToastSuccess(this Controller controller, string message)
        {
            controller.TempData[SuccessKey] = message;
        }

        public static void ToastInfo(this Controller controller, string message)
        {
            controller.TempData[InfoKey] = message;
        }

        public static void ToastWarning(this Controller controller, string message)
        {
            controller.TempData[WarningKey] = message;
        }

        public static void ToastError(this Controller controller, string message)
        {
            controller.TempData[ErrorKey] = message;
        }

        /// <summary>
        /// JSON result for AJAX form submissions, consumed by FvToast.show on the client.
        /// </summary>
        public static JsonResult ToastJson(this Controller controller, bool success, string message, string type)
        {
            return controller.Json(new ToastPayload(success, message, type));
        }

        public static JsonResult ToastSuccessJson(this Controller controller, string message)
            => controller.ToastJson(true, message, "success");

        public static JsonResult ToastErrorJson(this Controller controller, string message)
            => controller.ToastJson(false, message, "error");

        public static JsonResult ToastWarningJson(this Controller controller, string message)
            => controller.ToastJson(false, message, "warning");
    }
}
