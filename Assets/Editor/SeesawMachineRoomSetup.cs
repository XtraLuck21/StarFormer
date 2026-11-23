using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Authors only the assigned Level02 seesaw machinery; safe to run repeatedly.</summary>
public static class SeesawMachineRoomSetup
{
    private const string ScenePath = "Assets/Scenes/Level02.unity";

    [MenuItem("Tools/Seesaw Room/Configure Connected Machine")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode first.");
        var stage = UnityEngine.Object.FindFirstObjectByType<SeesawRoomStage>();
        if (stage == null || stage.gameObject.scene.path != ScenePath) throw new InvalidOperationException("Open Level02 first.");
        Transform room = stage.transform;
        var data = new SerializedObject(stage);
        var player = Ref<PlayerPossessionController>(stage, "player");
        var supply = Ref<SeesawRoomSupplies>(stage, "supplies");
        var overload = room.GetComponent<SeesawOverloadSequence>();
        var finale = room.GetComponent<SeesawBouncyFinale>();
        var exit = room.GetComponent<SeesawRoomExit>();
        Transform root = Child(room, "Connected Elevator Machine");
        var machine = root.GetComponent<SeesawMachineRoom>();
        if (machine == null) machine = root.gameObject.AddComponent<SeesawMachineRoom>();
        machine.enabled = false;
        SpriteRenderer template = Ref<SpriteRenderer>(exit, "sourceVisual");
        Transform frame = Child(root, "Fixed Elevator Frame");
        frame.position = new Vector3(28.03f, -13.8f, 0f);
        var gears = new SerializedObject(exit).FindProperty("gears");
        var drive = new SeesawGear2D[4];
        for (int i = 0; i < 4; i++)
        {
            drive[i] = (SeesawGear2D)gears.GetArrayElementAtIndex(i).objectReferenceValue;
            drive[i].transform.position = new Vector3(28.03f + (i % 2 == 0 ? -3.5f : 3.5f), -11.4f - i / 2 * 4.7f, 0f);
            drive[i].transform.localScale = Vector3.one * 1.85f;
        }
        // Only this room's ceiling/walls rise. Entry and Smile Face landing anchors stay put.
        Ref<SeesawGearBarrier>(stage, "gearBarrier").gameObject.SetActive(false);
        Transform ceiling = room.Find("Seesaw Room Ceiling");
        ceiling.position = new Vector3(ceiling.position.x, -4.2f, 0f);
        foreach (string name in new[] { "Seesaw Room Left Wall", "Seesaw Room Right Wall" })
        {
            Transform wall = room.Find(name);
            wall.position = new Vector3(wall.position.x, -14f, 0f);
            wall.localScale = new Vector3(wall.localScale.x, 20.2f, 1f);
        }
        Transform yellow = Ref<Transform>(stage, "yellowButton");
        yellow.position = new Vector3(40.9f, ceiling.position.y - .55f, 0f);
        var ceilingButton = yellow.GetComponent<SeesawSpiderCeilingButton>();
        var buttonData = new SerializedObject(ceilingButton);
        SerializedProperty calls = buttonData.FindProperty("onPressed.m_PersistentCalls.m_Calls");
        for (int i = calls.arraySize - 1; i >= 0; i--)
            if (calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Target").objectReferenceValue is SeesawGear2D)
                calls.DeleteArrayElementAtIndex(i);
        buttonData.ApplyModifiedPropertiesWithoutUndo();
        data.FindProperty("fullView").rectValue = new Rect(10f, -27.5f, 35f, 24f);
        data.ApplyModifiedPropertiesWithoutUndo();
        var volume = Ref<BoxCollider2D>(supply, "roomVolume");
        volume.transform.position = new Vector3(28.03f, -14f, 0f);
        volume.size = new Vector2(38f, 28f);
        RemoveLoadingApparatus(root);
        var piston = Block(root, "Four Stroke Ratchet", Vector2.zero, new Vector2(3.4f,.32f), template, new Color32(154,120,220,255));
        piston.transform.position = new Vector3(23f,-20.95f,0f);
        var ratchet = piston.GetComponent<RatchetingCeiling>();
        if (ratchet == null) ratchet = piston.gameObject.AddComponent<RatchetingCeiling>();
        SetFloat(ratchet, "maximumRise", .64f);
        SetFloat(ratchet, "ballLiftStep", .16f);
        SetFloat(ratchet, "liftSpeed", 1.6f);
        Transform hinge = Child(root, "Approach Bridge Hinge");
        Collider2D edge = Ref<Collider2D>(finale, "leftCliffEdge");
        Collider2D landing = Ref<Collider2D>(finale, "leftCliffLanding");
        Physics2D.SyncTransforms();
        float start = edge.bounds.max.x - .2f;
        float end = landing.bounds.min.x + .35f;
        float width = Mathf.Max(1f, end - start);
        hinge.position = new Vector3(start, landing.bounds.max.y - .16f, 0f);
        hinge.localRotation = Quaternion.identity;
        var bridge = Block(hinge, "Hinged Approach Bridge", new Vector2(width * .5f,0f), new Vector2(width,.32f), template, new Color32(213,166,103,255));
        bridge.enabled = false;
        hinge.localRotation = Quaternion.Euler(0f,0f,80f);
        // Explicit scene references; no runtime name search or edits to shared prefabs.
        Set(machine, "player", player); Set(machine, "stage", stage); Set(machine, "loading", overload);
        Set(machine, "finale", finale); Set(machine, "lift", exit); Set(machine, "supplies", supply);
        Set(machine, "frame", frame);
        Set(machine, "impactSurface", piston); Set(machine, "bridgeHinge", hinge); Set(machine, "bridgeSurface", bridge);
        SetArray(machine, "gears", drive);
        SetArray(machine, "contacts", new Transform[] { Ref<SeesawColorContact>(stage,"blueContact").transform,
            Ref<SeesawColorContact>(stage,"greenContact").transform, yellow, piston.transform });
        SetArray(machine, "maintenanceGates", new Transform[] { Ref<BoxCollider2D>(overload,"entrance").transform,
            Ref<BoxCollider2D>(overload,"exit").transform });
        SetArray(machine, "climbGates", new Transform[] { Ref<BoxCollider2D>(stage,"gate").transform,
            Ref<BoxCollider2D>(stage,"leftGate").transform });
        Set(stage, "machine", machine); Set(overload, "machine", machine); Set(finale, "machine", machine);
        ConfigureBallSockets(stage,supply,finale);
        Transform sockets = Ref<SeesawTilt2D>(stage,"seesaw").transform.Find("Recessed Ball Sockets");
        SetArray(machine,"contacts",new Transform[] {sockets.Find("Right Blue Ball Hole"),sockets.Find("Left Green Ball Hole"),yellow,piston.transform});
        var exitData = new SerializedObject(exit);
        exitData.FindProperty("platformLocalPosition").vector2Value = new Vector2(.21f,8.22f);
        exitData.ApplyModifiedPropertiesWithoutUndo();
        // The complete carriage is suspended clear of the initial wind and ball route.
        SetFloat(exit, "shakeStrength", .13f);
        machine.enabled = true;
        EditorUtility.SetDirty(machine);
        EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
        EditorSceneManager.SaveScene(stage.gameObject.scene);
        Validate();
    }

    private static void ConfigureBallSockets(SeesawRoomStage stage,SeesawRoomSupplies supply,SeesawBouncyFinale finale)
    {
        var board=Ref<BoxCollider2D>(finale,"board");
        var skin=Ref<SpriteRenderer>(finale,"beamSkin");
        var tilt=Ref<SeesawTilt2D>(stage,"seesaw");
        Physics2D.SyncTransforms();
        Bounds bounds=board.bounds;
        Transform root=Child(tilt.transform,"Recessed Ball Sockets");
        root.localPosition=Vector3.zero;root.localScale=Vector3.one;
        var redBall=Ref<WindReleasedBall>(supply,"ballPrefab");
        float diameter=redBall.GetComponent<CircleCollider2D>().radius*2f*Mathf.Abs(redBall.transform.lossyScale.x);
        float left=bounds.min.x+1.2f,right=bounds.max.x-1.2f,width=diameter+.2f;
        float[] edges={bounds.min.x,left-width*.5f,left+width*.5f,right-width*.5f,right+width*.5f,bounds.max.x};
        for(int i=0;i<3;i++)
        {
            float start=edges[i*2],end=edges[i*2+1];
            Transform panel=Child(root,"Solid beam panel "+i);
            panel.position=new Vector3((start+end)*.5f,bounds.center.y,0f);
            panel.localScale=Vector3.one;
            var collider=panel.GetComponent<BoxCollider2D>();
            if(collider==null)collider=panel.gameObject.AddComponent<BoxCollider2D>();
            collider.size=new Vector2(end-start,bounds.size.y);
            Transform face=Child(panel,"Original beam artwork");
            var visual=face.GetComponent<SpriteRenderer>();
            if(visual==null)visual=face.gameObject.AddComponent<SpriteRenderer>();
            visual.sprite=skin.sprite;visual.sharedMaterial=skin.sharedMaterial;visual.color=skin.color;
            visual.sortingLayerID=skin.sortingLayerID;visual.sortingOrder=skin.sortingOrder;
            Vector2 painted=(skin.sprite.rect.size-new Vector2(18f,24f))/skin.sprite.pixelsPerUnit;
            face.localScale=new Vector3((end-start)/painted.x,bounds.size.y/painted.y,1f);
        }
        // Retain the original full-size reference for the door, camera and supply bounds.
        // Only the three separated panels provide collision support across this board.
        board.isTrigger=true;
        board.GetComponent<SpriteRenderer>().enabled=false;
        skin.enabled=false;
        foreach(var old in tilt.GetComponentsInChildren<SeesawEndSensor>(true))
        {
            old.enabled=false;
            old.GetComponent<BoxCollider2D>().enabled=false;
        }
        // The new socket rims replace the old floating key/lock pictures. Keep their
        // physical contacts and UnityEvents as the seesaw transmission's internal switches.
        foreach(string field in new[]{"blueContact","greenContact"})
        {
            var contact=Ref<SeesawColorContact>(stage,field);
            foreach(Transform visualRoot in new[]{contact.transform,Ref<BoxCollider2D>(contact,"matchingButton").transform})
            {
                var symbol=visualRoot.GetComponent<SeesawButtonSymbol>();
                if(symbol!=null)symbol.enabled=false;
                foreach(var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            }
        }
        for(int i=0;i<2;i++)
        {
            Transform hole=Child(root,i==0?"Left Green Ball Hole":"Right Blue Ball Hole");
            hole.position=new Vector3(i==0?left:right,bounds.max.y,0f);
            hole.localScale=Vector3.one;
            var receiver=hole.GetComponent<BoxCollider2D>();
            if(receiver==null)receiver=hole.gameObject.AddComponent<BoxCollider2D>();
            receiver.isTrigger=true;receiver.size=new Vector2(width-.1f,.12f);receiver.offset=new Vector2(0f,-width+.1f);
            var socket=hole.GetComponent<SeesawBallSocket>();
            if(socket==null)socket=hole.gameObject.AddComponent<SeesawBallSocket>();
            socket.enabled=false;
            Set(socket,"seesaw",tilt);Set(socket,"supplies",supply);Set(socket,"stage",stage);
            Set(socket,"gear",Ref<SeesawGear2D>(stage,i==0?"greenGear":"blueGear"));
            var data=new SerializedObject(socket);
            data.FindProperty("mouthWidth").floatValue=width;
            data.FindProperty("side").enumValueIndex=(int)(i==0?SeesawTilt2D.Side.Left:SeesawTilt2D.Side.Right);
            data.ApplyModifiedPropertiesWithoutUndo();
            socket.enabled=true;
            var floor=Block(hole,"Socket floor",new Vector2(0f,-width),new Vector2(width,.1f),
                board.GetComponent<SpriteRenderer>(),new Color32(57,65,65,255));
            floor.enabled=true;
            // An outer lip catches fast wind-blown balls before they skim over the small opening.
            Block(hole,"Outer catching lip",new Vector2((i==0?-1f:1f)*(width*.5f+.03f),.2f),
                new Vector2(.12f,.45f),board.GetComponent<SpriteRenderer>(),
                Ref<SeesawGear2D>(stage,i==0?"greenGear":"blueGear").GetComponent<SpriteRenderer>().color);
        }
    }

    [MenuItem("Tools/Seesaw Room/Apply Presentation Adjustments")]
    public static void ApplyPresentationAdjustments()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var stage=UnityEngine.Object.FindFirstObjectByType<SeesawRoomStage>();
        if(stage==null || stage.gameObject.scene.path!=ScenePath)return;
        var exit=stage.GetComponent<SeesawRoomExit>();
        var data=new SerializedObject(exit);
        data.FindProperty("platformLocalPosition").vector2Value=new Vector2(.21f,8.22f);
        data.ApplyModifiedPropertiesWithoutUndo();
        var machine=UnityEngine.Object.FindFirstObjectByType<SeesawMachineRoom>();
        machine.enabled=false;
        ConfigureBallSockets(stage,Ref<SeesawRoomSupplies>(stage,"supplies"),stage.GetComponent<SeesawBouncyFinale>());
        machine.enabled=true;
        EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
        EditorSceneManager.SaveScene(stage.gameObject.scene);
    }

    private static void RemoveLoadingApparatus(Transform root)
    {
        foreach(string name in new[]{"Three Ball Counterweight","Wind Gate Hopper"})
        {
            Transform child=root.Find(name);
            if(child!=null)Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    [MenuItem("Tools/Seesaw Room/Apply Direct Yellow Switch")]
    public static void ApplyDirectYellowSwitch()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var stage=UnityEngine.Object.FindFirstObjectByType<SeesawRoomStage>();
        if(stage==null || stage.gameObject.scene.path!=ScenePath)return;
        var machine=UnityEngine.Object.FindFirstObjectByType<SeesawMachineRoom>();
        machine.enabled=false;
        RemoveLoadingApparatus(machine.transform);
        var button=Ref<Transform>(stage,"yellowButton").GetComponent<SeesawSpiderCeilingButton>();
        var data=new SerializedObject(button);
        var calls=data.FindProperty("onPressed.m_PersistentCalls.m_Calls");
        for(int i=0;i<calls.arraySize;i++)
        {
            var call=calls.GetArrayElementAtIndex(i);
            if(call.FindPropertyRelative("m_Target").objectReferenceValue is SeesawOverloadSequence)
                call.FindPropertyRelative("m_MethodName").stringValue="ActivateYellowSwitch";
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        machine.enabled=true;
        EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
        EditorSceneManager.SaveScene(stage.gameObject.scene);
    }

    [MenuItem("Tools/Seesaw Room/Frame Connected Machine")]
    public static void FrameMachine()
    {
        var machine = UnityEngine.Object.FindFirstObjectByType<SeesawMachineRoom>();
        if (machine == null) return;
        var view = SceneView.GetWindow<SceneView>();
        view.in2DMode = true;
        view.Frame(new Bounds(new Vector3(27.5f,-15.5f,0f), new Vector3(36f,25f,1f)), true);
        Selection.activeGameObject = machine.gameObject;
        view.Focus();
    }

    [MenuItem("Tools/Seesaw Room/Validate Connected Machine")]
    public static void Validate()
    {
        var machine = UnityEngine.Object.FindFirstObjectByType<SeesawMachineRoom>();
        if (machine == null || !machine.ReferencesReady) throw new InvalidOperationException("Incomplete machine references.");
        var stage = UnityEngine.Object.FindFirstObjectByType<SeesawRoomStage>();
        Rect bounds = new SerializedObject(stage).FindProperty("fullView").rectValue;
        if (!bounds.Contains(machine.ImpactSurface.bounds.center) || !machine.ImpactSurface.TryGetComponent(out RatchetingCeiling ratchet) || !ratchet.BallRaisesCeiling)
            throw new InvalidOperationException("Keep the active ratchet inside the machine room.");
        if (Ref<SeesawMachineRoom>(stage, "machine") != machine) throw new InvalidOperationException("Stage is not linked.");
        var button = Ref<Transform>(stage,"yellowButton").GetComponent<SeesawSpiderCeilingButton>();
        var calls = new SerializedObject(button).FindProperty("onPressed.m_PersistentCalls.m_Calls");
        for (int i = 0; i < calls.arraySize; i++)
            if (calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Target").objectReferenceValue is SeesawGear2D)
                throw new InvalidOperationException("Yellow must require three physical loads.");
        Debug.Log("SEESAW MACHINE: scene references, load gate and scoped bindings verified.");
    }

    private static Transform Child(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child;
        child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }
    private static BoxCollider2D Block(Transform parent, string name, Vector2 position, Vector2 size, SpriteRenderer template, Color color)
    {
        Transform item = Child(parent,name);
        item.localPosition = position; item.localRotation = Quaternion.identity; item.localScale = Vector3.one;
        var oldFace = item.GetComponent<SpriteRenderer>();
        if (oldFace != null) UnityEngine.Object.DestroyImmediate(oldFace);
        Transform face = Child(item, "Face");
        var visual = face.GetComponent<SpriteRenderer>();
        if (visual == null) visual = face.gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = template.sprite; visual.sharedMaterial = template.sharedMaterial;
        visual.color = color; visual.sortingLayerID = template.sortingLayerID; visual.sortingOrder = 3;
        face.localPosition = Vector3.zero;
        face.localScale = new Vector3(size.x / template.sprite.bounds.size.x, size.y / template.sprite.bounds.size.y, 1f);
        var collider = item.GetComponent<BoxCollider2D>();
        if (collider == null) collider = item.gameObject.AddComponent<BoxCollider2D>();
        collider.size = size; collider.isTrigger = false;
        return collider;
    }
    private static T Ref<T>(UnityEngine.Object target, string field) where T : UnityEngine.Object
        => (T)new SerializedObject(target).FindProperty(field).objectReferenceValue;
    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue=value; data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetFloat(UnityEngine.Object target,string field,float value)
    {
        var data = new SerializedObject(target); data.FindProperty(field).floatValue=value; data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetArray<T>(UnityEngine.Object target, string field, T[] values) where T : UnityEngine.Object
    {
        var data = new SerializedObject(target); var list = data.FindProperty(field); list.arraySize = values.Length;
        for (int i=0;i<values.Length;i++) list.GetArrayElementAtIndex(i).objectReferenceValue=values[i];
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
