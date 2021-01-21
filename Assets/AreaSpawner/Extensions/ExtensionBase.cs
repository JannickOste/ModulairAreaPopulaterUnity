using System.Linq;

public class ExtensionBase
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    public bool isCompleted { get; protected set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    public bool isStarted { get;  set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    public System.Collections.Generic.List<System.Type> requiredPreloadTypes { get; protected set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    protected System.Threading.CancellationToken token { get; private set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    protected Area parent { get; private set; }

    #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    /// <summary> Main extension function </summary>
    protected async virtual System.Threading.Tasks.Task Create() => throw new System.NotImplementedException("Area extensions require a Create task override");
    #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    /// <summary> Fetch create worker process of extension </summary>
    /// <param name="token">Cancelation token</param>
    /// <param name="parent">Target area</param>
    public System.Threading.Tasks.Task GetWorker(System.Threading.CancellationToken token, Area parent)
    {
        this.token = token;
        this.parent = parent;


        return WorkerGuard();
    }

    /// <summary> Checks worker requirements and delays thread if necessary </summary>
    /// <returns></returns>
    public async System.Threading.Tasks.Task WorkerGuard()
    {
        if(requiredPreloadTypes == null) _ = Create();
        else 
        {
            ExtensionBase[] requiredBases = this.parent.ExtensionSingletons
                                                .Where(inst => requiredPreloadTypes.Count(t => inst.GetType() == t) != 0)
                                                .ToArray();

            _ = System.Threading.Tasks.Task.Run(async() =>
            {
                while (requiredBases.Count(t => !t.isCompleted) > 0)
                    await System.Threading.Tasks.Task.Delay(3);

                await Create();
            });
        }

        await System.Threading.Tasks.Task.Delay(0);
    }


}

