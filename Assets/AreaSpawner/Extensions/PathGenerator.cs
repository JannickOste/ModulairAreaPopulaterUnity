using System.Threading.Tasks;
using System.Linq;
using System.Numerics;

class PathGenerator : ExtensionBase
{
    private readonly System.Collections.Generic.List<System.Numerics.Vector3> positions = new System.Collections.Generic.List<System.Numerics.Vector3>();
    private readonly System.Collections.Generic.List<AreaTile> activeTiles = new System.Collections.Generic.List<AreaTile>();
    private readonly System.Collections.Generic.List<AreaTile> visitedTiles = new System.Collections.Generic.List<AreaTile>();

    private readonly System.Collections.Generic.List<UnityEngine.Transform> childObjects = new System.Collections.Generic.List<UnityEngine.Transform>();


    /// <summary> Instantiate generator </summary>
    public PathGenerator()
    {
        this.requiredPreloadTypes = new System.Collections.Generic.List<System.Type>()
        {
            typeof(ObjectGenerator)
        };

        foreach (AreaObject obj in UnityEngine.GameObject.FindObjectsOfType<AreaObject>().OrderBy(i => i.name))
        {
            if (!obj.name.StartsWith("PATH_TARGET_")) continue; // todo: Change to lambda variables in editor not properly set.
            positions.Add(new System.Numerics.Vector3(
                obj.gameObject.transform.position.x,
                obj.gameObject.transform.position.y,
                obj.gameObject.transform.position.z
            ));
        }

        System.Collections.IEnumerator childEnum = childObjects.GetEnumerator();
        while(childEnum.MoveNext())
        {
            childObjects.Add((UnityEngine.Transform)childEnum.Current);
        }

    }

    /// <summary> Main generator function </summary>
    protected async override Task Create()
    {
        UnityEngine.Debug.Log("Starting path generation");
        activeTiles.Clear();
        visitedTiles.Clear();
        parent.ActionOnAreaThread(() => CreatePaths());

        await Task.Delay(0);
    }

    /// <summary> A* path search -> HAS TO BE RUN ON MAIN THREAD</summary>
    /// <param name="tiles"></param>
    private void CreatePaths()
    {
        if (positions.Count() >= 2)
        {
            for (int i = 0; i < positions.Count - 1; i++)
            {
                UnityEngine.Debug.Log(positions[i]);

                activeTiles.Clear();
                visitedTiles.Clear();

                AreaTile start = new AreaTile()
                {
                    position = positions[i]
                };

                AreaTile end = new AreaTile()
                {
                    position = positions[i + 1]
                };

                start.SetDistance(end.position);
                activeTiles.Add(start);

                GeneratePathPoint(start, end);
            }
        }
        else UnityEngine.Debug.Log("2 or more path points required to generate path");

    }

    private void TracePath(AreaTile areaTile) 
    {
        while (areaTile != null)
        {
            parent.CreateAreaObject(null, areaTile.position, UnityEngine.Quaternion.identity);
            areaTile = areaTile.Parent;
        }
    }


    private void GeneratePathPoint(AreaTile current, AreaTile end)
    {

        while (activeTiles.Any())
        {
            current = activeTiles.OrderBy(tile => tile.CostDistance).First();

            // Destination reached.
            if ((System.Math.Round(current.position.X, 0) == System.Math.Round(end.position.X, 0))
            && (System.Math.Round(current.position.Z, 0) == System.Math.Round(end.position.Z, 0)))
            {
                TracePath(current);
                return;
            }

            visitedTiles.Add(current);
            activeTiles.Remove(current);

            foreach (AreaTile tile in GetTileGrid(current, end))
            {
                double currentTileX = System.Math.Round(tile.position.X, 0);
                double currentTileZ = System.Math.Round(tile.position.Z, 0);
                if (visitedTiles.Any(cTile => (System.Math.Round(cTile.position.X, 0) == currentTileX)
                                           && (System.Math.Round(cTile.position.Z, 0) == currentTileZ)))
                    continue;

                if (activeTiles.Any(cTile => (System.Math.Round(cTile.position.X, 0) == currentTileX)
                                           && (System.Math.Round(cTile.position.Z, 0) == currentTileZ)))
                {
                    AreaTile scannedTile = activeTiles.First(cTile => (System.Math.Round(cTile.position.X, 0) == currentTileX)
                                                                 && (System.Math.Round(cTile.position.Z, 0) == currentTileZ));

                    if (scannedTile.CostDistance > tile.CostDistance)
                    {
                        activeTiles.Remove(scannedTile);
                        activeTiles.Add(tile);
                    }
                }
                else activeTiles.Add(tile);
            }
        }
    }



    private System.Collections.Generic.IEnumerable<AreaTile> GetTileGrid(AreaTile currentTile, AreaTile targetTile)
    {
        for (int z = -1; z < 2; z++)
        {
            for (int x = -1; x < 2; x++)
            {
                Vector3 position = currentTile.position;
                position.X += x;
                position.Z += z;

                
                if (parent.PositionWithinObject(parent.gameObject, position) & childObjects.All(obj => !parent.PositionWithinObject(obj.gameObject, position)))
                {
                    AreaTile gridTile = new AreaTile
                    {
                        position = position,
                        Parent = currentTile,
                        Cost = currentTile.Cost + 1
                    };

                    gridTile.SetDistance(targetTile.position);

                    yield return gridTile;
                }
            }
        }
    }
}

