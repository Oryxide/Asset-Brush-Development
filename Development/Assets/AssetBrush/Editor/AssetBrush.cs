using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
//using Unity.EditorCoroutines.Editor;

public class AssetBrush : EditorWindow
{
    // States
    States State;
    enum States { Idle, Painting, Erasing };
    bool ShowSettings = true;
    bool ShowWarning = false;
    bool BrushEnabled = false;

    // VARIABLES
    Ray Raycast;
    int LayerMask = ~(1 << 2);
    Vector2 ToolScroll;
    Vector2 AssetGroupScroll;
    Vector2 MousePosition2D;
    Vector3 MousePosition3D;
    Vector3 PreviousPaintPosition;
    float PreviousBrushSize = 3;
    static GameObject SearchedPrefab;
    List<GameObject> ObjectList = new List<GameObject>();
    List<GameObject> SpawnedObjects = new List<GameObject>();
    Dictionary <int, int> ObjectLayers = new Dictionary<int, int>();
    private Camera EditorCamera = UnityEditor.SceneView.lastActiveSceneView.camera;

    // ASSETS
    GameObject Brush;
    GameObject BrushAsset;
    Material PaintMaterial;
    Material EraseMaterial;

    // DEFAULT USER SETTINGS
    int DefaultBrushSize = 3; 
    int DefaultMinimumPadding = 1;
    int DefaultMaxObjects = 10;
    int DefaultMinimumRotation = 0;
    int DefaultMaximumRotation = 360;
    float DefaultMinimumSizeScale = 1f;
    float DefaultMaximumSizeScale = 1f;

    // USER SETTINGS
    int BrushSize; 
    int MinimumPadding;
    int MaxObjects;
    int MinimumRotation;
    int MaximumRotation;
    float MinimumSizeScale;
    float MaximumSizeScale;
    Transform SelectedParent;

    [MenuItem("Window/Asset Brush")]
    public static void ShowWindow()
    {
        AssetBrush window = GetWindow<AssetBrush>("Asset Brush");
    }

    void OnEnable()
    {   
        SetUserSettings();
        State = States.Idle;

        BrushAsset = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/AssetBrush/Assets/Brush.prefab", typeof(GameObject));
        PaintMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/AssetBrush/Assets/PaintMaterial.mat", typeof(Material));
        EraseMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/AssetBrush/Assets/EraseMaterial.mat", typeof(Material));

        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDestroy()
    { 
        // Saves the settings when the user closes the tool
        EditorPrefs.SetInt("BrushSize", BrushSize);
        EditorPrefs.SetInt("MinimumPadding", MinimumPadding);
        EditorPrefs.SetInt("MinimumRotation", MinimumRotation);
        EditorPrefs.SetInt("MaximumRotation", MaximumRotation);
        EditorPrefs.SetFloat("MinimumSizeScale", MinimumSizeScale);
        EditorPrefs.SetFloat("MaximumSizeScale", MaximumSizeScale);
        EditorPrefs.SetInt("MaxObjects", MaxObjects);
    }

    void OnGUI()
    {
        GUI.skin.button.wordWrap = true; // Allows for words to wrap instead of going into the void

        // SETTINGS
        ToolScroll = EditorGUILayout.BeginScrollView(ToolScroll); // If the UI can't fit into the size of the window then you'll be able to scroll
        EditorGUILayout.Space();
        ShowSettings = EditorGUILayout.Foldout(ShowSettings, "Settings"); // Gives the option to minimise the settings
        if (ShowSettings)
        {
            BrushSize = EditorGUILayout.IntSlider(new GUIContent("Brush Size", "Defines the size of the brush"), BrushSize, 1, 10);

            if (State != States.Idle && BrushSize != PreviousBrushSize) // Updates the brush size when the value is changed
            {
                PreviousBrushSize = BrushSize;
                Brush.transform.localScale = new Vector3(BrushSize, Brush.transform.localScale.y, BrushSize);
            }

            MinimumPadding = EditorGUILayout.IntSlider(new GUIContent("Minimum Padding", "Defines the minimum distance between game objects"), MinimumPadding, 1, 10);
            
            MaxObjects = EditorGUILayout.IntSlider(new GUIContent("Max Objects", "Defines the maximum amount of game objects allowed in a paint cycle"), MaxObjects, 1, 40);
            EditorGUILayout.Space();

            MinimumRotation = EditorGUILayout.IntSlider(new GUIContent("Minimum Rotation", "Defines the minimum Y rotation for a game object"), MinimumRotation > MaximumRotation ? MaximumRotation : MinimumRotation, 0, 360);
            MaximumRotation = EditorGUILayout.IntSlider(new GUIContent("Maximum Rotation", "Defines the maximum Y rotation for a game object"), MaximumRotation < MinimumRotation ? MinimumRotation : MaximumRotation, 0, 360);
            EditorGUILayout.Space();

            MinimumSizeScale = EditorGUILayout.FloatField(new GUIContent("Minimum Size Scale", "Defines the minimum scale a game object can be multiplied by"), MinimumSizeScale);
            if (MinimumSizeScale < .1f) // Prevents the float field from going below .1
            {
                MinimumSizeScale = .1f;
            }
            MaximumSizeScale = EditorGUILayout.FloatField(new GUIContent("Maximum Size Scale", "Defines the maximum scale a game object can be multiplied by"), MaximumSizeScale);
            if (MaximumSizeScale < .1f) // Prevents the float field from going below .1
            {
                MaximumSizeScale = .1f;
            }
            EditorGUILayout.Space();

            GUILayout.Label(SelectedParent ? "Parent selected ( " + SelectedParent.name + " )" : "No parent selected", EditorStyles.boldLabel);
            if (GUILayout.Button("Set selected as parent", GUILayout.Height(25)))
            {
                GameObject SelectedObject = Selection.activeGameObject;
                if (SelectedObject)
                {
                    SelectedParent = SelectedObject.transform;
                }
            }
            if (GUILayout.Button("Set scene as parent", GUILayout.Height(25)))
            {
                SelectedParent = null;
            }
        }
        HorizontalLine();

        // ASSET GROUP
        GUILayout.Label("Objects to paint with:", EditorStyles.boldLabel);

        if (ShowWarning)
        {
            // Alerts the user if they try to enable painting before adding any prefabs
            EditorGUILayout.HelpBox("Add an asset to the asset group before painting", MessageType.Warning);
        }
        
        float ScrollHeight = 0;
        float MaxIconsPerRow = Mathf.Floor(position.size.x/85);
        if (ObjectList.Count > 0)
        {
            // Determines the height of the scrollview
            ScrollHeight = ObjectList.Count <= MaxIconsPerRow ? 105f : 210f;
        }

        AssetGroupScroll = EditorGUILayout.BeginScrollView(AssetGroupScroll, GUILayout.Height(ScrollHeight));
        if (ObjectList.Count >= 1)
        {
            EditorGUILayout.BeginHorizontal();
        }

        for (int i = 1; i <= ObjectList.Count; i++)
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label(AssetPreview.GetAssetPreview(ObjectList[i - 1]), GUILayout.Height(80), GUILayout.Width(80));
            if (GUILayout.Button("-", GUILayout.Width(80)))
            {
                ObjectList.RemoveAt(i - 1);
                Repaint();
            }
            EditorGUILayout.EndVertical();

            if (i % MaxIconsPerRow == 0) // End of row
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal(); // Ends the previous horizontal container
                if (i != ObjectList.Count)
                {
                    EditorGUILayout.BeginHorizontal(); // Starts a new horizontal container if i is not the final entry in ObjectList
                }
            }
        }

        if (ObjectList.Count % MaxIconsPerRow != 0)
        {
            // If ObjectList.Count is not the end of a row then end the previous horizontal container
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        SearchedPrefab = EditorGUILayout.ObjectField("", SearchedPrefab, typeof(GameObject), false) as GameObject; // Allows the user to search for a non-scene prefab
        if (GUILayout.Button("Add"))
        {
            if (ObjectList.Count == 0)
            {
                ShowWarning = true;
                // Sets ShowWarning to true which will display a warning
            }

            if (SearchedPrefab)
            {
                ObjectList.Add(SearchedPrefab); // Adds the searched prefab to ObjectList and resets the  ObjectField
                ShowWarning = false;
                SearchedPrefab = null;
            }
        }
        EditorGUILayout.EndHorizontal();
        HorizontalLine();

        // BUTTONS
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(State == States.Painting ? "Disable Painting" : "Enable Painting", GUILayout.Height(35)))
        {
            if (ObjectList.Count > 0)
            {
                State = State == States.Painting ? States.Idle : States.Painting;
                // ^ Assigns State to an Idle if the State is Painting or Painting if the state is Idle
                if (State == States.Painting) 
                {
                    CreateBrush();
                    SetLayers();
                }
                else if (State == States.Idle)
                {
                    DestroyImmediate(Brush);
                    ResetLayers();
                }
            }
        }
        if (GUILayout.Button(State == States.Erasing ? "Disable Erasing" : "Enable Erasing", GUILayout.Height(35)))
        {
            State = State == States.Erasing ? States.Idle : States.Erasing; 
            // ^ Assigns State to an Idle if the State is Erasing or Erasing if the state is Idle
            if (State == States.Erasing) 
                {
                    CreateBrush();
                    SetLayers();
                }
                else if (State == States.Idle)
                {
                    DestroyImmediate(Brush);
                    ResetLayers();
                }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Reset Settings", GUILayout.Height(35)))
        {

            if (EditorUtility.DisplayDialog("Reset settings?", "Are you sure you want to reset your settings?", "Yes", "No"))
            {
                EditorPrefs.DeleteAll(); // Wipes the saved settings
                SetUserSettings(); // Saves the default settings
            }
        }
        GUILayout.Label("IMPORTANT: You can only erase GameObjects that you have painted during this session.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();
    }

    void OnSceneGUI(SceneView sceneView)
    {
        Event CurrentEvent = Event.current;
        MousePosition2D = CurrentEvent.mousePosition;

        if (State != States.Idle)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive)); // Disables selection

            if (CurrentEvent.button == 0) // Mouse input
            {
                if (CurrentEvent.type == EventType.MouseDown)
                {
                    Paint();
                    BrushEnabled = true;
                    
                }
                else if (CurrentEvent.type == EventType.MouseUp)
                {
                    BrushEnabled = false;
                }
            }
        }
    }

    void Update()
    {
        if (Brush)
        {
            RaycastHit Hit;
            Raycast = EditorCamera.ScreenPointToRay(new Vector2(MousePosition2D.x, (EditorCamera.pixelHeight - 0) - MousePosition2D.y)); 
            // ^ Returns a ray going from the camera through a point on the screen
            if (Physics.Raycast(Raycast, out Hit, Mathf.Infinity, LayerMask))
            {
                MousePosition3D = Hit.point; // Hit.point is the position of the mouse in the world sapce of teh scene
                Brush.transform.position = MousePosition3D + new Vector3(0, .1f, 0); 
                // Assigns the position of the brush to the mouse position and raises it by .1 metres to prevent clipping
            }
        }

        if (BrushEnabled)
        {
            if (State == States.Painting)
            {
                Paint();
            }
            else if (State == States.Erasing) 
            {
                foreach (Collider HitCollider in Physics.OverlapSphere(MousePosition3D, BrushSize))
                {
                    // ^ Get all colliders at the 3d mouse position within the area of the BrushSize
                    if (HitCollider != null && HitCollider.gameObject.layer == 2) // Only destroy objects that have a layer of 2 ( the layer of spawned objects )
                    {
                        Undo.DestroyObjectImmediate(HitCollider.transform.root.gameObject); // Allows for deleted objects to be brought back

                    }
                }
            }
        }
    }

    void OnSelectionChange()
    {
        if (SelectedParent == null) // If the selected parent is null then the OnGUI function is updated to update the UI
        {
            Repaint();
        }
    }

    void Paint()
    {
        if (BrushEnabled)
        {
            if (State == States.Painting)
            {
                if (PreviousPaintPosition != Vector3.zero) // If you don't assign a value to a Vecto3 variable, by default it will be Vector3.zero
                {
                    if ((PreviousPaintPosition - Brush.transform.position).magnitude >= 2) // Objects are only painted when the brush is moved 2 metres
                    {
                        PreviousPaintPosition = Brush.transform.position;
                        for (int i = 0; i < MaxObjects; i++)
                        {
                            SpawnObject(ObjectList[Random.Range(0, ObjectList.Count)]); // Passes a random object from the ObjectList to spawn
                        }
                        Undo.IncrementCurrentGroup(); // Allows for all the objects spawned from this function call to be undone at once
                    }
                }
                else
                {
                    PreviousPaintPosition = Brush.transform.position;
                }
            }
        }
    }

    bool CanPlaceObject(Vector3 Position)
    {
        // Determines whether SpawnObject() can place an object in the specified position
        // Physics.OverlapShere returns an array of colliders ( mine also includes colliders that act as a trigger )
        // ~LayerMask means that only colliders in that LayerMask will be included
        foreach (Collider HitCollider in Physics.OverlapSphere(Position, MinimumPadding, ~LayerMask, QueryTriggerInteraction.UseGlobal))
        {
            for (int i = 0; i < SpawnedObjects.Count; i++)
            {
                if (HitCollider.gameObject == SpawnedObjects[i])
                {
                    return false;
                }
            }
        }
        return true;
    }

    void SpawnObject(GameObject Object)
    {
        float Rotation = Random.Range(MinimumRotation, MaximumRotation);
        float Scale = Random.Range(MinimumSizeScale, MaximumSizeScale); 

        for (int Attempt = 1; Attempt <= 2; Attempt++)
        {
            Vector2 RandomPosition = Random.insideUnitCircle;
            Vector3 ObjectPosition = MousePosition3D + new Vector3(RandomPosition.x, 0, RandomPosition.y) * BrushSize;

            if (CanPlaceObject(ObjectPosition))
            {
                if (Physics.Raycast(ObjectPosition + new Vector3(0, 1, 0), Vector3.down, 1, LayerMask)) // If objects are on a surface
                {
                    GameObject SpawnedObject = Instantiate(Object, ObjectPosition, Quaternion.Euler(Object.transform.eulerAngles.x, Rotation, Object.transform.eulerAngles.z), SelectedParent);
                    // ^ Creates the obejct with a randomised rotation and parents it to selected ( or scene if null )
                    Transform ObjectTransform = SpawnedObject.transform;
                    Renderer ObjectRenderer = SpawnedObject.GetComponent<Renderer>();

                    SetLayer(SpawnedObject, 2);
                    ObjectTransform.localScale *= Scale; // Increases the scale by the randomised scale

                    if (ObjectRenderer)
                    {
                        ObjectTransform.position += new Vector3(0, ObjectRenderer.bounds.extents.y, 0);
                    }
                    else
                    {
                        // Creates a new bound that covers every object under the SpawnedObject
                        Renderer[] Renderers = SpawnedObject.GetComponentsInChildren<Renderer>();
                        Bounds bounds = Renderers[0].bounds;
                        foreach (Renderer ChildRenderer in Renderers)
                        {
                            bounds.Encapsulate(ChildRenderer.bounds);
                        }

                        BoxCollider collider = SpawnedObject.AddComponent(typeof(BoxCollider)) as BoxCollider;
                        // ^ Creates a collider so that CanPlaceObject() and the Erase feature can detect the object
                        collider.isTrigger = true;
                        collider.center = new Vector3(0, bounds.center.y / 2, 0); 
                        // ^ Increases the position by half the y size so that the object isn't in the gronud
                        collider.size = bounds.size;
                    }

                    Undo.RegisterCreatedObjectUndo(SpawnedObject, "Paint Object"); // Allows for painted objects to be undone
                    SpawnedObjects.Add(SpawnedObject);
                    break;
                }
            }
        }
    }

    void HorizontalLine()
    {
        // Creates a line with two spaces below
        // Helps with readability
        EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }

    void SetLayer(GameObject Object, int Layer)
    {
        // Saves the objects original layer in a dictionary and assigns a new layer
        foreach (Transform ObjectTransform in Object.GetComponentsInChildren<Transform>())
        {
            ObjectLayers[ObjectTransform.gameObject.GetInstanceID()] = ObjectTransform.gameObject.layer;

            ObjectTransform.gameObject.layer = Layer;
        }
    }

    void SetLayers()
    {
        // Calls SetLayer() for each spawned object to assign the layer to 2 
        foreach (GameObject Object in SpawnedObjects)
        {   
            if (Object) 
            {
                SetLayer(Object, 2);
            }
        }
    }

    void ResetLayers()
    {
        // Assigns the spawned objects original layer back
        foreach (GameObject Object in SpawnedObjects)
        {
            if (Object && ObjectLayers.ContainsKey(Object.GetInstanceID())) 
            {
                
                SetLayer(Object, ObjectLayers[Object.GetInstanceID()]);
            }
        }
        ObjectLayers = new Dictionary<int, int>();
    }

    void SetUserSettings()
    {
        // Saves the settings so that the user can use them in another session
        BrushSize = EditorPrefs.GetInt("BrushSize", DefaultBrushSize);
        MinimumPadding = EditorPrefs.GetInt("MinimumPadding", DefaultMinimumPadding);
        MinimumRotation = EditorPrefs.GetInt("MinimumRotation", DefaultMinimumRotation);
        MaximumRotation = EditorPrefs.GetInt("MaximumRotation", DefaultMaximumRotation);
        MinimumSizeScale = EditorPrefs.GetFloat("MinimumSizeScale", DefaultMinimumSizeScale);
        MaximumSizeScale = EditorPrefs.GetFloat("MaximumSizeScale", DefaultMaximumSizeScale);
        MaxObjects = EditorPrefs.GetInt("MaxObjects", DefaultMaxObjects);
    }

    void CreateBrush()
    {
        // Creates the brush that indiciates where objects will spawn and assigns a material
        // depending on whether the user is painting or erasing
        if (!Brush)
        {
            Brush = Instantiate(BrushAsset);
            Brush.transform.localScale = new Vector3(BrushSize, Brush.transform.localScale.y, BrushSize);
        }

        Brush.GetComponent<MeshRenderer>().material = State == States.Painting ? PaintMaterial : EraseMaterial;
    }
}