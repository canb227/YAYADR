
using System;

public interface IGameObject: IEquatable<IGameObject>
{

    public ulong id { get; set; }

    public float netPriority { get; set; }

    bool IEquatable<IGameObject>.Equals(IGameObject other)
    {
        return (this.id == other.id);
    }

}

