using System.Web;
using System.Web.Optimization;

namespace African_Beauty_Trading
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            var jquery = new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js");
            // Disable minification/transforms to avoid WebGrease usage temporarily
            jquery.Transforms.Clear();
            bundles.Add(jquery);

            var jqueryval = new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*",
                        "~/Scripts/jquery.validate.unobtrusive*");
            // Disable minification/transforms to avoid WebGrease usage temporarily
            jqueryval.Transforms.Clear();
            bundles.Add(jqueryval);

            // Add SignalR bundle
            var signalr = new ScriptBundle("~/bundles/signalr").Include(
                        "~/Scripts/jquery.signalR-{version}.js");
            signalr.Transforms.Clear();
            bundles.Add(signalr);

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            var modernizr = new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*");
            modernizr.Transforms.Clear();
            bundles.Add(modernizr);

            var bootstrap = new Bundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js");
            // Disable minification/transforms to avoid WebGrease usage temporarily
            bootstrap.Transforms.Clear();
            bundles.Add(bootstrap);

            var css = new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css");
            // Disable minification/transforms to avoid WebGrease usage temporarily
            css.Transforms.Clear();
            bundles.Add(css);

            // Optional: Create a separate bundle for chat pages
            var chat = new ScriptBundle("~/bundles/chat").Include(
                      "~/Scripts/jquery-{version}.js",
                      "~/Scripts/jquery.signalR-{version}.js",
                      "~/Scripts/bootstrap.js");
            chat.Transforms.Clear();
            bundles.Add(chat);
        }
    }
}
