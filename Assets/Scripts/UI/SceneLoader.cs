using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


//PANTALLA DE CARGA QUE NO HACE FALTA ETAR EN ESCENA, SE CREA SOLA LA PANTALLA DE CARGA

public class SceneLoader : MonoBehaviour
{
    private const float FadeTime = 0.25f;      //fade in / fade out de la pantalla
    private const float FinishTime = 0.8f;     //tiempo que tarda la barra en pasar de LoadCap a 100% con la escena nueva ya activa
    private const float LoadCap = 0.85f;       //hasta donde llega la barra mientras la escena se carga
    private const float BarSpeed = 1.2f;       //velocidad maxima de la barra (1 = llena en un segundo)
    private const float TitleSize = 65f;       //mismo tamano que el titulo del menu
    private const float PulseSpeed = 3f;       //velocidad del latido del icono

    //Resources.Load: la fuente del menu esta en TextMesh Pro/Examples & Extras/Resources/Fonts & Materials
    private const string TitleFontPath = "Fonts & Materials/Electronic Highway Sign SDF";
    //item_radio.png tiene que estar dentro de una carpeta Resources (Assets/Resources)
    private const string IconPath = "item_radio";

    private static readonly Color TitleColor = new Color(0.7830189f, 0.74589336f, 0.37304202f, 1f);//igual al del menu

    private static SceneLoader instance;
    public static bool IsLoading => instance != null && instance.loading;

    private Canvas canvas;
    private CanvasGroup group;
    private Image icon;
    private RectTransform barFill;
    private bool loading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()//por si el Domain Reload esta desactivado
    {
        instance = null;
    }

    //minShowTime = tiempo minimo que dura la carga en pantalla (para que la barra no salte si la escena carga rapido)
    public static void Load(string sceneName, float minShowTime = 1f)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("SceneLoader", typeof(RectTransform));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneLoader>();
        }

        if (instance.loading) return;
        instance.StartCoroutine(instance.LoadRoutine(sceneName, minShowTime));
    }

    private void Awake()
    {
        BuildUI();
        canvas.enabled = false;
    }

    private void Update()
    {
        if (!loading || icon == null) return;

        //el icono de la radio late suave para que se note que sigue cargando
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulseSpeed);
        Color c = icon.color;
        c.a = Mathf.Lerp(0.35f, 1f, pulse);
        icon.color = c;
    }

    private IEnumerator LoadRoutine(string sceneName, float minShowTime)
    {
        loading = true;
        SetBar(0f);
        canvas.enabled = true;
        group.blocksRaycasts = true;
        yield return Fade(1f);

        float startTime = Time.unscaledTime;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

        if (op == null)//nombre de escena invalido (el error ya sale en consola)
        {
            yield return Fade(0f);
            Hide();
            yield break;
        }

        op.allowSceneActivation = false;//la escena se carga pero no se activa hasta cumplir el tiempo minimo

        //fase 1: la barra sube hasta LoadCap. op.progress se frena en 0.9 mientras allowSceneActivation sea false
        float shown = 0f;
        float elapsed = 0f;
        while (op.progress < 0.9f || elapsed < minShowTime)
        {
            elapsed = Time.unscaledTime - startTime;

            float real = Mathf.Clamp01(op.progress / 0.9f);
            float timeT = Mathf.Clamp01(elapsed / Mathf.Max(minShowTime, 0.01f));
            float paced = 1f - (1f - timeT) * (1f - timeT);//ease-out: arranca rapido y se va frenando

            float target = Mathf.Min(real, paced) * LoadCap;//nunca mas rapido que la carga real ni que el tiempo
            shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * BarSpeed);
            SetBar(shown);
            yield return null;
        }

        //fase 2: activar la escena (Awake/Start). Puede trabarse un instante: la barra queda quieta en lo que llego
        op.allowSceneActivation = true;
        yield return op;

        yield return null;
        yield return null;

        //fase 3: con la escena ya activa la barra completa el resto hasta 100%
        for (float t = 0f; t < FinishTime; t += Time.unscaledDeltaTime)
        {
            SetBar(Mathf.Lerp(shown, 1f, Mathf.SmoothStep(0f, 1f, t / FinishTime)));
            yield return null;
        }
        SetBar(1f);

        yield return Fade(0f);
        Hide();
    }

    private void SetBar(float value)
    {
        barFill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
    }

    private void Hide()
    {
        group.blocksRaycasts = false;
        canvas.enabled = false;
        loading = false;
    }

    private IEnumerator Fade(float target)
    {
        float start = group.alpha;
        for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(start, target, t / FadeTime);
            yield return null;
        }
        group.alpha = target;
    }

    // ---------- UI armada por codigo ----------

    private void BuildUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;//arriba de todo

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0f;//igual que el canvas del menu

        gameObject.AddComponent<GraphicRaycaster>();
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        //negro completo (bloquea los clicks de abajo)
        RectTransform bg = CreateImage("Fondo", transform, Color.black);
        bg.anchorMin = Vector2.zero;
        bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero;
        bg.offsetMax = Vector2.zero;

        //titulo con la fuente y el color del menu
        GameObject titleGo = new GameObject("Titulo", typeof(RectTransform));
        titleGo.transform.SetParent(bg, false);
        TextMeshProUGUI title = titleGo.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(TitleFontPath);
        if (font != null) title.font = font;//si no esta, usa la fuente por defecto de TMP
        title.text = "Zombie Cabin";
        title.fontSize = TitleSize;
        title.color = TitleColor;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
        Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1200f, 120f), new Vector2(0f, 90f));

        //barra de carga (abajo de la radio)
        RectTransform barBack = CreateImage("BarraFondo", bg, new Color32(40, 38, 32, 255));
        barBack.GetComponent<Image>().raycastTarget = false;
        Place(barBack, new Vector2(0.5f, 0f), new Vector2(600f, 10f), new Vector2(0f, 140f));

        barFill = CreateImage("BarraRelleno", barBack, TitleColor);
        barFill.GetComponent<Image>().raycastTarget = false;
        barFill.anchorMin = Vector2.zero;
        barFill.anchorMax = new Vector2(0f, 1f);
        barFill.offsetMin = Vector2.zero;
        barFill.offsetMax = Vector2.zero;

        //icono de la radio arriba de la barra
        Sprite sprite = Resources.Load<Sprite>(IconPath);
        if (sprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>(IconPath);
            if (tex != null)
                sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        if (sprite == null)
        {
            Debug.LogWarning("SceneLoader: no se encontro 'item_radio' en Resources. Mover item_radio.png a Assets/Resources.");
            return;
        }

        RectTransform iconRect = CreateImage("IconoRadio", bg, Color.white);
        icon = iconRect.GetComponent<Image>();
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Place(iconRect, new Vector2(0.5f, 0f), new Vector2(150f, 150f), new Vector2(0f, 270f));
    }

    private static RectTransform CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 position)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
    }
}