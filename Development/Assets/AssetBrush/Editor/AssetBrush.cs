﻿using UnityEngine;
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

        SceneView.duringSceneGui += SceneGUI;
    }

    void OnDestroy()
    { 
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
        GUI.skin.button.wordWrap = true;

        // SETTINGS
        ToolScroll = EditorGUILayout.BeginScrollView(ToolScroll);
        EditorGUILayout.Space();
        ShowSettings = EditorGUILayout.Foldout(ShowSettings, "Settings");
        if (ShowSettings)
        {
            BrushSize = EditorGUILayout.IntSlider(new GUIContent("Brush Size", "Defines the size of the brush"), BrushSize, 1, 10);

            if (State != States.Idle && BrushSize != PreviousBrushSize) 
            {
                PreviousBrushSize = BrushSize;
                Brush.transform.localScale = new Vector3(BrushSize, Brush.transform.localScale.y, BrushSize);
            }

            MinimumPadding = EditorGUILayout.IntSlider(new GUIContent("Minimum Padding", "Defines the minimum distance between game objects"), MinimumPadding, 0, 10);
            
            MaxObjects = EditorGUILayout.IntSlider(new GUIContent("Max Objects", "Defines the maximum amount of game objects allowed in a paint cycle"), MaxObjects, 1, 40);
            EditorGUILayout.Space();

            MinimumRotation = EditorGUILayout.IntSlider(new GUIContent("Minimum Rotation", "Defines the minimum Y rotation for a game object"), MinimumRotation > MaximumRotation ? MaximumRotation : MinimumRotation, 0, 360);
            MaximumRotation = EditorGUILayout.IntSlider(new GUIContent("Maximum Rotation", "Defines the maximum Y rotation for a game object"), MaximumRotation < MinimumRotation ? MinimumRotation : MaximumRotation, 0, 360);
            EditorGUILayout.Space();

            MinimumSizeScale = EditorGUILayout.FloatField(new GUIContent("Minimum Size Scale", "Defines the minimum scale a game object can be multiplied by"), MinimumSizeScale);
            if (MinimumSizeScale < .1f)
            {
                MinimumSizeScale = .1f;
            }
            MaximumSizeScale = EditorGUILayout.FloatField(new GUIContent("Maximum Size Scale", "Defines the maximum scale a game object can be multiplied by"), MaximumSizeScale);
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
            EditorGUILayout.HelpBox("Add an asset to the asset group before painting", MessageType.Warning);
        }

        float MaxRowEntries = Mathf.Floor(position.size.x/85);
        
        float ScrollHeight = 0;

        if (ObjectList.Count > 0)
        {
            ScrollHeight = ObjectList.Count <= MaxRowEntries ? 105f : 210f;
        }
        else
        {
            ScrollHeight = 0f;
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

            if (i % Mathf.Floor(position.size.x/85) == 0)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                if (i != ObjectList.Count)
                {
                    EditorGUILayout.BeginHorizontal();
                }
            }
        }

        if (ObjectList.Count % Mathf.Floor(position.size.x / 85) != 0)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        SearchedPrefab = EditorGUILayout.ObjectField("", SearchedPrefab, typeof(GameObject), false) as GameObject;
        if (GUILayout.Button("Add"))
        {
            if (ObjectList.Count == 0)
            {
                ShowWarning = true;
            }

            if (SearchedPrefab)
            {
                ObjectList.Add(SearchedPrefab);
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
                EditorPrefs.DeleteAll();
                SetUserSettings();
            }
        }
        GUILayout.Label("IMPORTANT: You can only erase GameObjects that you have painted during this session.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();
    }

    void SceneGUI(SceneView sceneView)
    {
        Event CurrentEvent = Event.current;
        MousePosition2D = CurrentEvent.mousePosition;
        
        if (State != States.Idle)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive)); // Disables selection

            if (CurrentEvent.button == 0)
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
            if (Physics.Raycast(Raycast, out Hit, Mathf.Infinity, LayerMask))
            {
                MousePosition3D = Hit.point;
                Brush.transform.position = MousePosition3D + new Vector3(0, .1f, 0);
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
                foreach (Collider HitCollider in Physics.OverlapSphere(MousePosition3D, BrushSize)) // * (Padding + 1)
                {
                    if (HitCollider.gameObject.layer == 2) 
                    {
                        Undo.DestroyObjectImmediate(HitCollider.gameObject);
                    }
                }
            }
        }
    }

    void OnSelectionChange()
    {
        if (SelectedParent == null)
        {
            Repaint();
        }
    }

    void PaintIteration()
    {
        for (int i = 0; i < MaxObjects; i++)
        {
            SpawnObject(ObjectList[Random.Range(0, ObjectList.Count)]);
        }
        Undo.IncrementCurrentGroup();
    }

    void Paint()
    {
        if (BrushEnabled)
        {
            if (State == States.Painting)
            {
                if (PreviousPaintPosition != null)
                {
                    if ((PreviousPaintPosition - Brush.transform.position).magnitude >= 2) //Brush.GetComponent<Renderer>().bounds.size.x/2
                    {
                        PreviousPaintPosition = Brush.transform.position;
                        PaintIteration();
                    }
                }
                else
                {
                    PreviousPaintPosition = Brush.transform.position;
                }
            }
        }
    }

    void SpawnObject(GameObject Object)
    {
        float Rotation = Random.Range(MinimumRotation, MaximumRotation);
        float Scale = Random.Range(MinimumSizeScale, MaximumSizeScale);

        for (int Attempt = 1; Attempt <= 2; Attempt++)
        {
            bool Overlapping = false;
            Vector3 ObjectSize = Object.GetComponent<Renderer>().bounds.size;
            float LongestSide = Mathf.Max(ObjectSize.x  , ObjectSize.z);
            Vector2 RandomPosition = Random.insideUnitCircle;
            Vector3 ObjectPosition = MousePosition3D + new Vector3(RandomPosition.x, 0, RandomPosition.y) * BrushSize;

            foreach (Collider HitCollider in Physics.OverlapSphere(ObjectPosition, MinimumPadding)) // * (Padding + 1)
            {
                for (int i = 0; i < SpawnedObjects.Count; i++)
                {
                    if (HitCollider.gameObject == SpawnedObjects[i])
                    {
                        Overlapping = true;
                    }
                }
            }

            if (!Overlapping)
            {
                GameObject SpawnedObject = Instantiate(Object, ObjectPosition, Quaternion.Euler(new Vector3(0, Rotation, 0)), SelectedParent);
                Transform ObjectTransform = SpawnedObject.transform;
                Vector3 Position = ObjectTransform.position;
                SpawnedObject.transform.localScale = SpawnedObject.transform.localScale * Scale;
                ObjectTransform.position = new Vector3(Position.x, Position.y + SpawnedObject.GetComponent<Renderer>().bounds.size.y/2, Position.z);
                ObjectLayers.Add(SpawnedObject.GetInstanceID(), SpawnedObject.layer);
                SpawnedObject.layer = 2;
                Undo.RegisterCreatedObjectUndo(SpawnedObject, "Paint Object");
                SpawnedObjects.Add(SpawnedObject);
                break;
            }
        }
    }

    void HorizontalLine()
    {
        EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }

    void SetLayers()
    {
        foreach (GameObject Object in SpawnedObjects)
        {   
            if (Object) 
            {
                if (!ObjectLayers.ContainsKey(Object.GetInstanceID()))
                {
                    ObjectLayers.Add(Object.GetInstanceID(), Object.layer);
                }
                Object.layer = 2;
            }
        }
    }

    void ResetLayers()
    {
        foreach (GameObject Object in SpawnedObjects)
        {
            if (Object && ObjectLayers.ContainsKey(Object.GetInstanceID())) 
            {
                Object.layer = ObjectLayers[Object.GetInstanceID()];
            }
        }
        ObjectLayers = new Dictionary<int, int>();
    }

    void SetUserSettings()
    {
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
        if (!Brush)
        {
            Brush = Instantiate(BrushAsset);
            Vector3 BrushScale = Brush.transform.localScale; 
            Brush.transform.localScale = new Vector3(BrushSize, Brush.transform.localScale.y, BrushSize);
        }
        MeshRenderer BrushRenderer = Brush.GetComponent<MeshRenderer>();
        if (State == States.Painting)
        {
            BrushRenderer.material = PaintMaterial;
        }
        else if (State == States.Erasing)
        {
            BrushRenderer.material = EraseMaterial;
        }
    }
}