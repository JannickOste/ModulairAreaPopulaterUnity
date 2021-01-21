public class AreaObject : UnityEngine.MonoBehaviour
{
    public Area parent;
    public AreaTile tileParent; 

    public System.Type masterType;

    public System.Collections.Generic.List<UnityEngine.Collision> collidingObjects = new System.Collections.Generic.List<UnityEngine.Collision>();

    public void OnCollisionEnter(UnityEngine.Collision collision)
    {
        if(collision.collider.GetComponent<UnityEngine.TerrainCollider>() == null) collidingObjects.Add(collision);
    }
    public void OnCollisionExit(UnityEngine.Collision collision) => collidingObjects.Remove(collision);

    [UnityEngine.ExecuteInEditMode]
    public void OnDestroy()
    {
        if(masterType != null)
        {
            if (masterType.IsAssignableFrom(typeof(AreaTile)))
                parent.areaObjects.Remove(this);
        }
    }

}

