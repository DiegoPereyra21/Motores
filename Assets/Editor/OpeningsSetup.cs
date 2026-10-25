using System.Collections.Generic;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// Herramienta para armar las aberturas (CabinOpening) de la cabaña a partir de los paneles de puertas y ventanas que ya existen.
// Seleccionas los paneles en la Hierarchy y usas el menu Zombie Cabin ▸ Barricadas ▸ Crear aberturas desde la seleccion.
// Por cada panel crea un objeto "Opening_..." con: CabinOpening, zona de interaccion (capa Interact), NavMeshLink y puntos de ataque.
// Es de un solo uso: cuando las aberturas esten armadas este archivo se puede borrar.
public static class OpeningsSetup
{
    private const string BarricadeItemPath = "Assets/ScriptableObjects/Items/Crafted/reinforcement_plank.asset";
    private const string ParentName = "Openings";
    private const float LinkOffset = 1f; //a que distancia de la abertura (a cada lado) van los extremos del NavMeshLink
    private const float AttackOffset = 1.2f; //a que distancia hacia afuera van los puntos de ataque de los zombies
    private const float ZoneDepth = 1.6f; //profundidad de la zona de interaccion (la mitad a cada lado)

    private class PanelInfo
    {
        public GameObject source;
        public GameObject opening; //la abertura creada a partir de este panel
        public OpeningType type;
        public Vector3 center;
        public Vector3 normal; //horizontal, apunta hacia afuera de la cabaña
        public float width;
        public float height;
        public Vector3 outside; //punto del NavMesh afuera de la abertura
        public Vector3 inside; //punto del NavMesh adentro
        public bool outsideOnNavMesh;
        public bool insideOnNavMesh;
        public bool passableHere; //el NavMesh es continuo de un lado al otro de ESTA abertura (sin link ya se puede pasar)
    }

    [MenuItem("Zombie Cabin/Barricadas/Crear aberturas desde la seleccion")]
    private static void CreateFromSelection()
    {
        if (GameObject.Find(ParentName) != null)
        {
            EditorUtility.DisplayDialog("Aberturas", $"Ya existe un objeto \"{ParentName}\". Borralo primero con Zombie Cabin ▸ Barricadas ▸ Borrar todas las aberturas.", "OK");
            return;
        }

        int interactLayer = LayerMask.NameToLayer("Interact");
        if (interactLayer < 0)
        {
            EditorUtility.DisplayDialog("Aberturas", "No existe la capa \"Interact\".", "OK");
            return;
        }

        ItemData barricadeItem = AssetDatabase.LoadAssetAtPath<ItemData>(BarricadeItemPath);
        if (barricadeItem == null)
            Debug.LogWarning($"[OpeningsSetup] No se encontro el item en {BarricadeItemPath}. Hay que asignarlo a mano en cada abertura.");

        List<PanelInfo> panels = ReadPanels(Selection.gameObjects);
        if (panels.Count == 0)
        {
            EditorUtility.DisplayDialog("Aberturas", "Selecciona en la Hierarchy los paneles de las puertas y ventanas (objetos con MeshFilter).", "OK");
            return;
        }

        OrientOutward(panels);
        foreach (PanelInfo panel in panels)
            AnalyzeNavMesh(panel);

        GameObject parent = new GameObject(ParentName);
        Undo.RegisterCreatedObjectUndo(parent, "Crear aberturas");

        List<Object> created = new List<Object>();
        int doorCount = 0;
        int windowCount = 0;

        foreach (PanelInfo panel in panels)
        {
            int index = panel.type == OpeningType.Door ? ++doorCount : ++windowCount;
            panel.opening = CreateOpening(panel, parent.transform, $"Opening_{panel.type}_{index}", interactLayer, barricadeItem);
            created.Add(panel.opening);
        }

        EditorSceneManager.MarkSceneDirty(parent.scene);
        Selection.objects = created.ToArray();
        PrintReport(panels);
    }

    [MenuItem("Zombie Cabin/Barricadas/Borrar todas las aberturas")]
    private static void DeleteAll()
    {
        GameObject parent = GameObject.Find(ParentName);
        if (parent == null) return;

        if (!EditorUtility.DisplayDialog("Aberturas", $"Se va a borrar \"{ParentName}\" con todas las aberturas. Seguro?", "Borrar", "Cancelar"))
            return;

        Scene scene = parent.scene;
        Undo.DestroyObjectImmediate(parent);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    // lee centro, ancho, alto y normal de cada panel (la normal es el eje mas finito del mesh)
    private static List<PanelInfo> ReadPanels(GameObject[] selected)
    {
        List<PanelInfo> result = new List<PanelInfo>();

        foreach (GameObject go in selected)
        {
            MeshFilter filter = go.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;

            Transform t = go.transform;
            Bounds local = filter.sharedMesh.bounds;
            Vector3 scale = t.lossyScale;
            Vector3[] axes = { t.right, t.up, t.forward };
            float[] dims =
            {
                local.size.x * Mathf.Abs(scale.x),
                local.size.y * Mathf.Abs(scale.y),
                local.size.z * Mathf.Abs(scale.z)
            };

            int thin = 0;
            for (int i = 1; i < 3; i++)
            {
                if (dims[i] < dims[thin]) thin = i;
            }

            //de los otros dos ejes, el mas vertical es el alto
            int a = (thin + 1) % 3;
            int b = (thin + 2) % 3;
            int heightAxis = Mathf.Abs(axes[a].y) > Mathf.Abs(axes[b].y) ? a : b;
            int widthAxis = heightAxis == a ? b : a;

            Vector3 normal = axes[thin];
            normal.y = 0f;

            PanelInfo panel = new PanelInfo
            {
                source = go,
                type = t.parent != null && t.parent.name == "Doors" ? OpeningType.Door : OpeningType.Window,
                center = t.TransformPoint(local.center),
                normal = normal.normalized,
                width = dims[widthAxis],
                height = dims[heightAxis]
            };

            //paneles duplicados en el mismo lugar: se queda solo el primero
            bool duplicated = result.Exists(p => Vector3.Distance(p.center, panel.center) < 0.3f);
            if (duplicated)
            {
                Debug.LogWarning($"[OpeningsSetup] {go.name} esta en el mismo lugar que otro panel seleccionado, se ignora.", go);
                continue;
            }

            result.Add(panel);
        }

        return result;
    }

    // la normal de cada panel se da vuelta si apunta hacia el centro de la cabaña (promedio de los paneles seleccionados)
    private static void OrientOutward(List<PanelInfo> panels)
    {
        Vector3 centroid = Vector3.zero;
        foreach (PanelInfo panel in panels)
            centroid += panel.center;
        centroid /= panels.Count;

        foreach (PanelInfo panel in panels)
        {
            if (Vector3.Dot(panel.normal, panel.center - centroid) < 0f)
                panel.normal = -panel.normal;
        }
    }

    // busca los puntos del NavMesh a cada lado de la abertura y revisa si el NavMesh es continuo por ESTA abertura
    private static void AnalyzeNavMesh(PanelInfo panel)
    {
        Vector3 groundBase = panel.center + Vector3.down * (panel.height * 0.5f);

        panel.outside = SampleNavMesh(groundBase + panel.normal * LinkOffset, out panel.outsideOnNavMesh);
        panel.inside = SampleNavMesh(groundBase - panel.normal * LinkOffset, out panel.insideOnNavMesh);

        if (panel.outsideOnNavMesh && panel.insideOnNavMesh)
            panel.passableHere = IsPassable(panel.outside, panel.inside);
    }

    // NavMesh.Raycast devuelve true si hay un borde del NavMesh entre los dos puntos (o sea, hay una pared). Si no hay borde, se puede caminar de un punto al otro
    private static bool IsPassable(Vector3 outside, Vector3 inside)
    {
        return !NavMesh.Raycast(outside, inside, out NavMeshHit _, NavMesh.AllAreas);
    }

    // Diagnostico de las aberturas que ya estan en la escena (usa los extremos del NavMeshLink de cada una), sin crear nada
    [MenuItem("Zombie Cabin/Barricadas/Diagnosticar NavMesh de las aberturas")]
    private static void DiagnoseExisting()
    {
        CabinOpening[] openings = Object.FindObjectsByType<CabinOpening>(FindObjectsSortMode.InstanceID);
        if (openings.Length == 0)
        {
            EditorUtility.DisplayDialog("Aberturas", "No hay aberturas en la escena.", "OK");
            return;
        }

        int passable = 0;

        foreach (CabinOpening opening in openings)
        {
            NavMeshLink link = opening.GetComponent<NavMeshLink>();
            if (link == null) continue;

            Vector3 outside = opening.transform.TransformPoint(link.startPoint);
            Vector3 inside = opening.transform.TransformPoint(link.endPoint);
            string info;

            if (!NavMesh.SamplePosition(outside, out _, 0.5f, NavMesh.AllAreas) || !NavMesh.SamplePosition(inside, out _, 0.5f, NavMesh.AllAreas))
            {
                info = "algun extremo del link NO esta sobre el NavMesh";
            }
            else if (IsPassable(outside, inside))
            {
                info = "ABIERTA en el NavMesh: los zombies ya pasan por aca sin link";
                passable++;
            }
            else
            {
                info = "sellada en el NavMesh: solo pasan por el link";
            }

            Debug.Log($"[OpeningsSetup] {opening.name} ({opening.Type}, {opening.Size.x:0.0} x {opening.Size.y:0.0} m) -> {info}", opening);
        }

        Debug.Log($"[OpeningsSetup] {passable} de {openings.Length} aberturas dejan pasar al NavMesh sin link.");
    }

    private static Vector3 SampleNavMesh(Vector3 point, out bool found)
    {
        found = NavMesh.SamplePosition(point, out NavMeshHit hit, 3f, NavMesh.AllAreas);
        return found ? hit.position : point;
    }

    private static GameObject CreateOpening(PanelInfo panel, Transform parent, string name, int layer, ItemData barricadeItem)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Crear abertura");

        go.transform.SetParent(parent, false);
        go.transform.position = panel.center;
        go.transform.rotation = Quaternion.LookRotation(panel.normal, Vector3.up); //forward = hacia afuera
        go.layer = layer;

        //zona de interaccion: trigger para que el raycast del player la detecte sin bloquearlo
        BoxCollider zone = go.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(panel.width, panel.height, ZoneDepth);

        //el link deja pasar a los zombies cuando la abertura esta abierta (CabinOpening lo apaga con la barricada)
        NavMeshLink link = go.AddComponent<NavMeshLink>();
        link.width = Mathf.Min(panel.width, 2f);
        link.bidirectional = true;
        link.startPoint = go.transform.InverseTransformPoint(panel.outside);
        link.endPoint = go.transform.InverseTransformPoint(panel.inside);

        Transform[] attackPoints = CreateAttackPoints(go.transform, panel);

        CabinOpening opening = go.AddComponent<CabinOpening>();
        SerializedObject so = new SerializedObject(opening);
        so.FindProperty("type").enumValueIndex = (int)panel.type;
        so.FindProperty("size").vector2Value = new Vector2(panel.width, panel.height);
        so.FindProperty("barricadeItem").objectReferenceValue = barricadeItem;

        SerializedProperty pointsProperty = so.FindProperty("attackPoints");
        pointsProperty.arraySize = attackPoints.Length;
        for (int i = 0; i < attackPoints.Length; i++)
            pointsProperty.GetArrayElementAtIndex(i).objectReferenceValue = attackPoints[i];

        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    // 1 a 3 puntos repartidos a lo ancho, afuera de la abertura y apoyados en el NavMesh
    private static Transform[] CreateAttackPoints(Transform opening, PanelInfo panel)
    {
        int count = Mathf.Clamp(Mathf.FloorToInt(panel.width / 1.2f), 1, 3);
        Transform[] points = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            GameObject point = new GameObject($"AttackPoint_{i + 1}");
            Undo.RegisterCreatedObjectUndo(point, "Crear punto de ataque");
            point.transform.SetParent(opening, false);

            float x = (i - (count - 1) * 0.5f) * (panel.width / count);
            Vector3 world = opening.TransformPoint(new Vector3(x, -panel.height * 0.5f, AttackOffset));
            point.transform.position = SampleNavMesh(world, out _);

            points[i] = point.transform;
        }

        return points;
    }

    private static void PrintReport(List<PanelInfo> panels)
    {
        //una linea de log por abertura: al hacer clic en la linea en la Consola se selecciona esa abertura en la Hierarchy
        int leaks = 0;
        int offNavMesh = 0;

        foreach (PanelInfo panel in panels)
        {
            string navInfo;

            if (!panel.outsideOnNavMesh || !panel.insideOnNavMesh)
            {
                navInfo = $"SIN NavMesh ({(panel.outsideOnNavMesh ? "" : "afuera ")}{(panel.insideOnNavMesh ? "" : "adentro")})";
                offNavMesh++;
            }
            else if (panel.passableHere)
            {
                navInfo = "ABIERTA en el NavMesh: los zombies ya pasan por aca sin link";
                leaks++;
            }
            else
            {
                navInfo = "sellada en el NavMesh: solo pasan por el link";
            }

            Debug.Log($"[OpeningsSetup] {panel.opening.name} (panel \"{panel.source.name}\", Y {panel.center.y:0.0}) -> {panel.type}, {panel.width:0.0} x {panel.height:0.0} m | {navInfo}", panel.opening);
        }

        Debug.Log($"[OpeningsSetup] Se crearon {panels.Count} aberturas. NavMesh: {offNavMesh} sin NavMesh a algun lado, {leaks} abiertas en el NavMesh. Revisa que la flecha verde de cada abertura apunte hacia AFUERA de la cabaña.");
    }
}
