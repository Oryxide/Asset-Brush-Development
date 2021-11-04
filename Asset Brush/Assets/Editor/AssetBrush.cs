﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
//using Unity.EditorCoroutines.Editor;

public class AssetBrush : EditorWindow
{

    enum States { Idle, Painting, Erasing };

    //Debug.Log(Face.Left);


    // States
    States State;
    bool BrushEnabled = false;

    // VARIABLES
    GameObject Brush;
    GameObject BrushAsset;
    Vector2 MousePosition2D;
    Vector3 MousePosition3D;
    Vector2 ScrollPosition;
    bool ShowSettings = true;
    static GameObject SearchedPrefab;
    List<GameObject> ObjectList = new List<GameObject>();
    private Camera EditorCamera = UnityEditor.SceneView.lastActiveSceneView.camera;


    // USER SETTINGS
    float MinimumObjectRotation = 0;
    float MaximumObjectRotation = 360;
    float MinimumObjectSizeScale = .5f;
    float MaximumObjectSizeScale = 1.5f;
    Transform SelectedParent;
    float BrushSize = 3;
    float PreviousBrushSize = 3;
    float Padding = 0;

    [MenuItem("Window/Asset Brush")]
    public static void ShowWindow()
    {
        AssetBrush window = GetWindow<AssetBrush>("Asset Brush");
    }

    void HorizontalLine()
    {
        EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }

    void OnEnable()
    {
        State = States.Idle;
        BrushAsset = Resources.Load("Brush") as GameObject;
        SceneView.duringSceneGui += SceneGUI;
    }

    void SceneGUI(SceneView sceneView)
    {
        Event CurrentEvent = Event.current;

        MousePosition2D = CurrentEvent.mousePosition;
        RaycastHit Hit;
        Ray Raycast = HandleUtility.GUIPointToWorldRay(MousePosition2D); // ignore objects already spawned
        if (Physics.Raycast(Raycast, out Hit))
        {
            MousePosition3D = Hit.point;
        }

        if (State != States.Idle)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive)); // Disables selection
            if (CurrentEvent.button == 0)
            {
                if (CurrentEvent.type == EventType.MouseDown)
                {
                    BrushEnabled = true;
                }
                else if (CurrentEvent.type == EventType.MouseUp)
                {
                    BrushEnabled = false;
                }
            }
        }
    }

    void OnGUI()
    {
        GUI.skin.button.wordWrap = true;

        // SETTINGS
        EditorGUILayout.Space();
        ShowSettings = EditorGUILayout.Foldout(ShowSettings, "Settings");
        if (ShowSettings)
        {
            BrushSize = EditorGUILayout.Slider("Brush Size", BrushSize, 1, 10);

            if (State != States.Idle && BrushSize != PreviousBrushSize) 
            {
                PreviousBrushSize = BrushSize;
                Vector3 BrushScale = Brush.transform.localScale;
                Brush.transform.localScale = new Vector3(BrushSize, BrushScale.y, BrushSize);
            }

            EditorGUILayout.Space();

            Padding = EditorGUILayout.Slider("Object Padding", Padding, 0f, 1f);
            EditorGUILayout.Space();

            //GUILayout.Label("Object Rotation", EditorStyles.boldLabel);
            MinimumObjectRotation = EditorGUILayout.Slider("Minimum Object Rotation", MinimumObjectRotation > MaximumObjectRotation ? MaximumObjectRotation : MinimumObjectRotation, 0, 360);
            MaximumObjectRotation = EditorGUILayout.Slider("Maximum Object Rotation", MaximumObjectRotation < MinimumObjectRotation ? MinimumObjectRotation : MaximumObjectRotation, 0, 360);
            EditorGUILayout.Space();

            //GUILayout.Label("Object Scale", EditorStyles.boldLabel);
            MinimumObjectSizeScale = EditorGUILayout.FloatField("Minimum Object Scale", MinimumObjectSizeScale);
            MaximumObjectSizeScale = EditorGUILayout.FloatField("Maximum Object Scale", MaximumObjectSizeScale);
            // MinimumObjectSizeScale = EditorGUILayout.Slider("Minimum Object Scale", MinimumObjectSizeScale > MaximumObjectSizeScale ? MaximumObjectSizeScale : MinimumObjectSizeScale, -1, 1);
            // MaximumObjectSizeScale = EditorGUILayout.Slider("Maximum Object Scale", MaximumObjectSizeScale < MinimumObjectSizeScale ? MinimumObjectSizeScale : MaximumObjectSizeScale, -1, 1);
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

        if (ObjectList.Count == 0)
        {
            EditorGUILayout.HelpBox("Add an asset to the asset group before painting", MessageType.Warning);
        }

        ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition, GUILayout.Height(208));
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
            if (SearchedPrefab)
            {
                ObjectList.Add(SearchedPrefab);
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
                // ASSIGN THE 8TH LAYER TO THE SPAWNED OBJECTS
                CreateBrush();
            }
        }
        if (GUILayout.Button(State == States.Erasing ? "Disable Erasing" : "Enable Erasing", GUILayout.Height(35)))
        {
            State = State == States.Erasing ? States.Idle : States.Erasing;
            CreateBrush();
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Reset Settings", GUILayout.Height(35)))
        {
            MinimumObjectRotation = 0;
            MaximumObjectRotation = 360;
            MinimumObjectSizeScale = .5f;
            MaximumObjectSizeScale = 1.5f;
            SelectedParent = null;
            State = States.Idle;
        }
        GUILayout.Label("IMPORTANT: You can only erase GameObjects that you have painted during this session.", EditorStyles.wordWrappedLabel);
    }

    void OnSelectionChange()
    {
        if (SelectedParent == null)
        {
            Repaint();
        }
    }

    List<GameObject> SpawnedObjects = new List<GameObject>();

    void SpawnObject(GameObject Object)
    {
        float Rotation = Random.Range(MinimumObjectRotation, MaximumObjectRotation);
        float Scale = Random.Range(MinimumObjectSizeScale, MaximumObjectSizeScale);

        for (int Attempt = 1; Attempt <= 10; Attempt++)
        {
            bool Overlapping = false;
            Vector3 ObjectSize = Object.GetComponent<Renderer>().bounds.size;
            float LongestSide = Mathf.Max(ObjectSize.x, ObjectSize.y, ObjectSize.z);
            Vector2 RandomPosition = Random.insideUnitCircle;
            Vector3 ObjectPosition = MousePosition3D + new Vector3(RandomPosition.x, 0, RandomPosition.y) * BrushSize;

            foreach (Collider HitCollider in Physics.OverlapSphere(ObjectPosition, LongestSide * (Padding + 1)))
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
                SpawnedObject.transform.localScale = SpawnedObject.transform.localScale * Scale;
                Undo.RegisterCreatedObjectUndo(SpawnedObject, "Paint Object");
                SpawnedObjects.Add(SpawnedObject);
                break;
            }
        }
    }

    void CreateBrush()
    {
        if (Brush) 
        {
            DestroyImmediate(Brush);
        }
        else 
        {
            Brush = Instantiate(BrushAsset);
            Vector3 BrushScale = Brush.transform.localScale;
            Brush.transform.localScale = new Vector3(BrushScale.x * BrushSize, BrushScale.y, BrushScale.z * BrushSize);
        }
    }

    int Frames = 0;

    void Update()
    {
        Frames++;

        if (Brush)
        {
            Brush.transform.position = MousePosition3D + new Vector3(0, .1f, 0);
        }

        if (BrushEnabled)
        {
            if (State == States.Painting)
            {
                if (Frames % 10 == 0)
                {
                    for (int i = 0; i < ObjectList.Count; i++)
                    {
                        SpawnObject(ObjectList[Random.Range(0, ObjectList.Count)]);
                    }
                    //ObjectsSpawnedDuringPaintIteration = new List<GameObject>();
                }
            }
        }
    }
} 