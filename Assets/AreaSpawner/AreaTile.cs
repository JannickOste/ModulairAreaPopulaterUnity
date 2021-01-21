public class AreaTile
{
    public UnityEngine.GameObject objParent;
    public System.Numerics.Vector3 position;    

    //A*
    public float Distance { get; set; }

    public System.UInt32 Cost { get; set; }
    public System.UInt32 CostDistance { get; set; }

    public AreaTile Parent { get; set; }

    public void SetDistance(System.Numerics.Vector3 endTarget)
       => this.Distance = System.Math.Abs(endTarget.X - position.X) + System.Math.Abs(endTarget.Z - position.Z);
}

