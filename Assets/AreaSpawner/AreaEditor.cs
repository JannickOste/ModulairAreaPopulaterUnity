using System.Linq;

[UnityEditor.CustomEditor(typeof(Area))]
public class AreaEditor : UnityEditor.Editor
{
    private bool createPathTarget = false;
    private Area TargetArea => target as Area;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        TargetArea.resetSpawns = UnityEditor.EditorGUILayout.Toggle("Reset area:", UnityEngine.Application.isPlaying ? TargetArea.resetSpawns : false);
        this.ParseFields(serializedObject.FindProperty("fields"));

        if (TargetArea.fields.Count(i => i.enabled) != 0)
        {
            UnityEditor.EditorGUILayout.LabelField("");
            this.ParseConfiguration();
        }
        serializedObject.ApplyModifiedProperties();
    }

    public void ParseConfiguration()
    {
        UnityEditor.EditorGUILayout.LabelField("[Configuration]:");
        //-----------//
        if(TargetArea.fields.Where(i => i.targetType == typeof(ObjectGenerator)).FirstOrDefault().enabled)
        {

            UnityEditor.EditorGUI.indentLevel++;

            UnityEditor.EditorGUILayout.LabelField("[] Object generator:");
            UnityEditor.EditorGUI.indentLevel++;
            TargetArea.spawnWeight = UnityEditor.EditorGUILayout.IntSlider("Spawn Weight:", TargetArea.spawnWeight, 10, 1);

            UnityEditor.EditorGUI.indentLevel--;
        }
        //------------//

        if (TargetArea.fields.Where(i => i.targetType == typeof(PathGenerator)).FirstOrDefault().enabled)
        {

            UnityEditor.EditorGUILayout.LabelField("[] Path generator:");

            UnityEditor.EditorGUI.indentLevel++;

            TargetArea.deletaPathCollider = UnityEditor.EditorGUILayout.Toggle("Delete objects colliding /w path:", TargetArea.deletaPathCollider);

            if (UnityEditor.EditorGUILayout.Toggle("Create path target: ", createPathTarget))
                CreatePathTarget();

            if (UnityEditor.EditorGUILayout.Toggle("Delete all path targets: ", createPathTarget))
            {
                UnityEngine.GameObject targetObject;
                while (true)
                {
                    bool set = false;
                    // todo: look into remove while -> Doesnt reset all for some reason.
                    foreach (UnityEngine.Transform areaObject in TargetArea.transform)
                    {
                        if (areaObject.name.StartsWith("PATH_TARGET_"))
                        {
                            set = true;
                            DestroyImmediate(areaObject.gameObject);
                        }
                    }
                    if (!set) break;
                }
            }

            UnityEditor.EditorGUI.indentLevel--;
        }
        if (!UnityEngine.Application.isPlaying) UnityEditor.EditorGUI.indentLevel--;

    }

    public void ParseFields (UnityEditor.SerializedProperty fieldsSet)
    {

        UnityEditor.EditorGUILayout.LabelField("[Extensions]:");
        UnityEditor.EditorGUI.indentLevel++;
        for (int i = 0; i < fieldsSet.arraySize; i++)
        {
            UnityEditor.SerializedProperty property = fieldsSet.GetArrayElementAtIndex(i);
            UnityEditor.EditorGUILayout.PropertyField(property, new UnityEngine.GUIContent($"[] {TargetArea.fields[i].Name}"));
        }
        UnityEditor.EditorGUI.indentLevel--;
    }

    public void CreatePathTarget()
    {
        UnityEngine.GameObject editorObject = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
        int index = UnityEngine.GameObject.FindObjectsOfType<AreaObject>().Count(i => i.name.StartsWith("PATH_TARGET_"));
        editorObject.name = $"PATH_TARGET_{index}";
        editorObject.AddComponent<AreaObject>();
        editorObject.GetComponent<AreaObject>().parent = TargetArea;
        editorObject.GetComponent<AreaObject>().masterType = typeof(AreaTile);

        editorObject.transform.SetParent(TargetArea.transform);
        editorObject.transform.position = TargetArea.transform.position;

        editorObject.GetComponent<AreaObject>().tileParent = new AreaTile();
        createPathTarget = false;
    }
}

[System.Serializable]
public class ExtensionField
{
    public string Name => string.Join(" ", nameSanitzerReg.Split(targetType.FullName));
    public bool enabled = false;
    public System.Type targetType = default;

    private readonly System.Text.RegularExpressions.Regex nameSanitzerReg = new System.Text.RegularExpressions.Regex(@"(?=[A-Z])");
    public ExtensionField(System.Type targetType)
    {
        this.targetType = targetType;
    }

    public void SetConfiguration()
    {

    }
}