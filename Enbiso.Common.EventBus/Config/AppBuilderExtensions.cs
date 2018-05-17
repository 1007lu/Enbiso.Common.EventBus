using System;
using Enbiso.Common.EventBus.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Enbiso.Common.EventBus.Config
{
    public static class AppBuilderExtensions
    {
        public static void UseEventBus(this IApplicationBuilder app, Action<IEventBus> init)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
            eventBus.Initialize(() => init(eventBus));
        }
    }
}