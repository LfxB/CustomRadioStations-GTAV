using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;

namespace CustomRadioStations
{
    internal static class Decorators
    {
        internal static Entity DEntity;

        const string prefix = "CRSSH_";

        const string loadedOnce = "HasLoadedOnce";

        internal static void Init(Entity entity)
        {
            DEntity = entity;

            DecoratorHelper.UnlockDecorators();

            DecoratorHelper.SAFE_DECOR_REGISTER(prefix + loadedOnce, DecoratorHelper.DecoratorType.Bool);

            DecoratorHelper.LockDecorators();

            ScriptHasLoadedOnce = true;
        }

        internal static bool ScriptHasLoadedOnce
        {
            get
            {
                return DecoratorHelper.DECOR_EXIST_ON(DEntity, prefix + loadedOnce) ? DecoratorHelper.DECOR_GET_BOOL(DEntity, prefix + loadedOnce) : false;
            }
            set
            {
                DecoratorHelper.DECOR_SET_BOOL(DEntity, prefix + loadedOnce, value);
            }
        }
    }
}
