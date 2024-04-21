using Mafi;
using Mafi.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineTowerControlMod
{
    [GlobalDependency(RegistrationMode.AsSelf, false)]
    public class MTCActions
    {
        private readonly EntitiesManager _entitiesManager;

        public MTCActions(EntitiesManager entitiesManager)
        {
            _entitiesManager = entitiesManager;
        }

        public int getMineTowerCount()
        {
            return 0;
        }
    }
}
