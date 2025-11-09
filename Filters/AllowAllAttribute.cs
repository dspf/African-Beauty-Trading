using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace African_Beauty_Trading.Filters
{
    public class AllowAllAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Do nothing - allow all requests
            base.OnActionExecuting(filterContext);
        }
    }
}