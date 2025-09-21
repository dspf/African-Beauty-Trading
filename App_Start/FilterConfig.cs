using System.Web;
using System.Web.Mvc;

namespace African_Beauty_Trading
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
