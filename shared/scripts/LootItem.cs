using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAYADR.shared.scripts
{
    internal class LootItem: IGameObject, IPhysicsObject
    { 
            public ulong id { get => id; set => id = value; }

        float IGameObject.netPriority { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }


    }
}
