using UnityEngine;

class ObjectGenerator : ExtensionBase
{
    /// <summary> Main function of extension </summary>
    protected async override System.Threading.Tasks.Task Create()
    {
        UnityEngine.Debug.Log("Starting object generation.");

        _ = SetObjects();
        await System.Threading.Tasks.Task.Delay(0);
    }

    /// <summary> Populate terrain based on weight </summary>
    private async System.Threading.Tasks.Task SetObjects()
    {
        Vector3 startPosition = parent.transform.position;
        startPosition.x += (parent.transform.localScale / 2).x;
        startPosition.z -= (parent.transform.localScale / 2).z;

        int spawnChance = parent.SpawnChance();
        System.Random randomizer = new System.Random();
        for (int z = 0; z <= parent.transform.localScale.z; z+=randomizer.Next(spawnChance, spawnChance*2))
        {
            for (int x = 0; x <= parent.transform.localScale.x; x+=randomizer.Next(spawnChance, spawnChance*2))
            {
                token.ThrowIfCancellationRequested();

                Vector3 position = startPosition;
                position.x -= x;
                position.y = parent.transform.position.y;
                position.z += z;

                parent.CreateAreaObject(null,
                                       position,
                                        Quaternion.identity, typeof(AreaObject));
            }
        }

        this.parent.ActionOnAreaThread(new System.Action(() =>
        {
            this.isCompleted = true;
        }));
        await System.Threading.Tasks.Task.Delay(0);
    }
}