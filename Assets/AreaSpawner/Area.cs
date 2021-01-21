using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public struct AreaData
{
    public float width;
    public float height;

    public System.Numerics.Vector3 position;
}

public class Area : MonoBehaviour
{
    // Area extensions
    public List<ExtensionBase> ExtensionSingletons { get; private set; }

    // Threading
    private readonly List<System.Action> mainThreadActions = new List<System.Action>();
    private readonly Dictionary<System.Type, System.Threading.Tasks.Task> workers = new Dictionary<System.Type, System.Threading.Tasks.Task>();

    public System.Threading.CancellationTokenSource WorkerTokenSource { get; private set; }

    // Editor fields.
    public ExtensionField[] fields = System.Reflection.Assembly.GetAssembly(typeof(ExtensionBase))
                                            .GetTypes()
                                            .Where(t => t.IsSubclassOf(typeof(ExtensionBase)))
                                            .Select(t => new ExtensionField(t))
                                            .ToArray();
    public AreaData data { get; private set; }

    // Spawn data
    public int spawnWeight = 9;
    public bool resetSpawns = false;
    public bool deletaPathCollider = false;
    public List<AreaObject> areaObjects;

    public void Start()
    {
        data = new AreaData
        {
            position = new System.Numerics.Vector3(
                 x: this.transform.position.x + (this.transform.localScale / 2).x,
                 y: this.transform.position.y,
                 z: this.transform.position.z - (this.transform.localScale / 2).z
            ),
            width = System.Math.Abs((this.transform.localScale).x),
            height = System.Math.Abs((this.transform.localScale).z)
        };

        areaObjects = UnityEngine.GameObject.FindObjectsOfType<AreaObject>().ToList();

        WorkerTokenSource = new System.Threading.CancellationTokenSource();
        ExtensionSingletons = new List<ExtensionBase>(fields.Length);

        foreach (ExtensionField newExt in fields.Where(f => f.enabled & ExtensionSingletons.Count(i => f.targetType.IsInstanceOfType(i)) == 0))
        {
            CreateInstance(newExt);
        }

    }

    public void FixedUpdate()
    {
        if (resetSpawns) ResetObjects();
        else if (WorkerTokenSource.IsCancellationRequested) WorkerTokenSource = new System.Threading.CancellationTokenSource();
        else
        {
            if (workers.Count(i => ExtensionSingletons.Where(j => j.GetType().IsAssignableFrom(i.Key)).First().isStarted) > 0)
                StartCoroutine("StartWorkers");


            if (mainThreadActions.Count() > 0)
            {
                lock (mainThreadActions)
                {
                    System.Action act = mainThreadActions.FirstOrDefault();
                    if (act != null)
                    {
                        act.Invoke();
                        mainThreadActions.RemoveAt(0);
                    }
                }
            }
        } 
    }

    public void OnApplicationQuit() => ResetObjects();

    private void CreateInstance(ExtensionField field)
    {
        if (field.targetType == null)
        {
            UnityEngine.Debug.Log($"[Area::{this.name}]: Unable to load extesion type of \"{field.targetType}\".");
            return;
        }

        ExtensionBase extensionInstance = ExtensionSingletons
                                                .Where(t => t.GetType().IsAssignableFrom(field.targetType))
                                                .FirstOrDefault();

        if (extensionInstance == null)
        {
            extensionInstance = (ExtensionBase)System.Activator.CreateInstance(field.targetType);
            ExtensionSingletons.Add(extensionInstance);
        }

        System.Threading.CancellationToken token = WorkerTokenSource.Token;
        workers[field.targetType] = extensionInstance.GetWorker(token, this);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
    private IEnumerator StartWorkers()
    {
        lock (workers)
        {
            System.Threading.Tasks.Task.WaitAll(workers.Values.ToArray());
            workers.Clear();
        }

        yield return null;
    }

    public void ActionOnAreaThread(System.Action target)
    {
        mainThreadActions.Add(target);
    }




    public void CreateAreaObject(UnityEngine.Object model, UnityEngine.Vector3 position, Quaternion rotation, System.Action<GameObject> callback, params System.Type[] componentTypes)
    {
        mainThreadActions.Add(new System.Action(() =>
        {
            //GameObject newObject = UnityEngine.GameObject.Instantiate(model, position, rotation) as GameObject;
            GameObject newObject = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Cube);

            newObject.transform.rotation = rotation;

            Terrain[] hits = Physics.SphereCastAll(position, 3f, Vector3.forward, 5f)
                                        .Select(i => i.collider.GetComponent<Terrain>())
                                        .Where(t => t != null)
                                        .ToArray();

            newObject.transform.position = hits.Length == 0
                                            ? position
                                            : new Vector3(position.x, hits.First().SampleHeight(position), position.z);

            newObject.transform.SetParent(this.transform);

            foreach (System.Type addType in componentTypes)
                newObject.AddComponent(addType);


            callback?.Invoke(newObject);
        }));
    }
    public void CreateAreaObject(UnityEngine.Object model, System.Numerics.Vector3 position, Quaternion rotation, System.Action<GameObject> callback, params System.Type[] componentTypes)
    => CreateAreaObject(model, new UnityEngine.Vector3(position.X, position.Y, position.Z), rotation, callback, componentTypes);

    public void CreateAreaObject(UnityEngine.Object model, System.Numerics.Vector3 position, Quaternion rotation, params System.Type[] componentTypes)
        => CreateAreaObject(model, new UnityEngine.Vector3(position.X, position.Y, position.Z), rotation, null, componentTypes);
    public void CreateAreaObject(UnityEngine.Object model, UnityEngine.Vector3 position, Quaternion rotation, params System.Type[] componentTypes)
        => CreateAreaObject(model, position, rotation, null, componentTypes);
   

    /**
     *  Extension assist functions.
     */

    public int SpawnChance()
    {
        int spawnChance = (int)(3 * (10 - spawnWeight));

        return spawnChance <= 0 ? 1 : spawnChance;
    }

    public void ResetObjects()
    {
        foreach(Transform child in this.transform)
        {
            Destroy(child.gameObject);
        }

        resetSpawns = false;
        WorkerTokenSource.Cancel();
        mainThreadActions.Clear();
        ExtensionSingletons.ForEach(s => s.isStarted = false);
        foreach(ExtensionField field in fields)
        {
            if (field.enabled) CreateInstance(field);
        }
    }

    public bool PositionWithinObject(GameObject target, System.Numerics.Vector3 position) =>
        PositionWithinObject(target, new UnityEngine.Vector3(position.X, position.Y, position.Z));

    public bool PositionWithinObject(GameObject target, Vector3 position)
    {
        Vector3 cornerDiffrence = target.transform.localScale / 2;
        Vector3[] corners = Enumerable.Range(0, 4)
                .Select(i => new System.Func<int, Vector3>((j) =>
                {
                    float x_offset = cornerDiffrence.x + (j / 2 >= 1 ? 0 - cornerDiffrence.x * 2 : 0);
                    Vector3 offset = new Vector3(
                                    x: j % 2 == 0 ? x_offset : -x_offset,
                                    y: 0,
                                    z: cornerDiffrence.z);

                    return target.transform.position + (j % 2 == 0 ? offset : -offset);
                })(i)).ToArray();


        UnityEngine.Vector3 btmLeft = corners.ElementAt(0);
        UnityEngine.Vector3 topLeft = corners.ElementAt(1);
        UnityEngine.Vector3 btmRight = corners.ElementAt(2);
        UnityEngine.Vector3 topRight = corners.ElementAt(3);
        return (position.x >=
                (topLeft.x < topRight.x ? topLeft.x : topRight.x)
                    && position.x <= (topLeft.x > topRight.x ? topLeft.x : topRight.x)
                ) & (
                    (position.z >= (topLeft.z < btmLeft.z ? topLeft.z : btmLeft.z)
                    && position.z <= (topLeft.z > btmLeft.z ? topLeft.z : btmLeft.z)));
    }
}
