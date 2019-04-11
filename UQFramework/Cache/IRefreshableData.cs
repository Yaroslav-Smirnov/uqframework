using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UQFramework.Cache
{
    interface IRefreshableData
    {
        void NotifyUpdated(IEnumerable<string> identifiers);
        void NotifyFullRefreshRequired();
    }
}
