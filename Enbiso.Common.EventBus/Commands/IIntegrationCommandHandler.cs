using System;
using System.Collections.Generic;
using System.Text;

namespace Enbiso.Common.EventBus.Commands
{
    public interface IIntegrationCommandHandler
    {
        void Handle(IntegrationCommand command);
    }

    public interface IIntegrationCommandHandler<T> : IIntegrationCommandHandler
    {
        void Handle(IntegrationCommand<T> command);
    }
}
