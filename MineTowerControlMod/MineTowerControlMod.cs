using Mafi.Collections;
using Mafi.Core.Game;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineTowerControlMod
{
    public sealed class MineTowerControlMod : IMod
    {
        public string Name => "MineTowerControlMod";

        public int Version => 1;
        public static Version ModVersion = new Version(0, 0, 3);
        public bool IsUiOnly => false;

        public Option<IConfig> ModConfig { get; }

        public void ChangeConfigs(Lyst<IConfig> configs)
        {
        }

        public void Initialize(DependencyResolver resolver, bool gameWasLoaded)
        {
            LogWrite.Info("Initializing ");
        }

        public void RegisterDependencies(DependencyResolverBuilder depBuilder, ProtosDb protosDb, bool gameWasLoaded)
        {
            LogWrite.Info("Register Dependencies ");
        }

        public void RegisterPrototypes(ProtoRegistrator registrator)
        {
            LogWrite.Info("Registrating Prototypes");
        }

        public void EarlyInit(DependencyResolver resolver)
        {
            LogWrite.Info("EarlyInit");
        }
    }

}
