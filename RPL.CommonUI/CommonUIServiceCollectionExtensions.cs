using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Text;

namespace RPL.CommonUI
{
    public static class CommonUIServiceCollectionExtensions
    {
        public static void AddCommonUI(this IServiceCollection services)
        {
            services.ConfigureOptions(typeof(CommonUIConfigureOptions));
        }
    }
}
