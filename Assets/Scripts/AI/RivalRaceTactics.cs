using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Small race rules around PPO: inventory use, optional route bonuses and
// acceleration on clear straights. Steering still comes from the neural policy.
public sealed class RivalRaceTactics : MonoBehaviour
{
    public Transform NavigationPath { get; private set; }
    public int PlannedBonuses { get; private set; }
    public int RampPasses { get; private set; }
    public int BoostPasses { get; private set; }
    public int CruiseRequests { get; private set; }
    public int TricksPerformed { get; private set; }
    Player kart;
    ItemManager items;
    TrackProgress progress;
    Transform source;
    GameObject privateUI;
    float nextRamp, nextBoost;
    static readonly float[] FrontAngles = { -12, 0, 12 };

    void Start()
    {
        if (MooMooRival.LearningSession || KartRunSettings.Argument("duel-tactics", "1") == "0")
        { enabled = false; return; }
        kart = GetComponent<Player>();
        items = GetComponent<ItemManager>();
        progress = GetComponent<TrackProgress>();
        IsolateInventory();
        items.AutoUseItems = true;
        RebuildRoute();
    }
    void Update()
    {
        if (items.path != source) RebuildRoute();
        if (RACE_MANAGER.RACE_STARTED && !RACE_MANAGER.RACE_COMPLETED && kart.enabled && kart.TryPerformTrick()) TricksPerformed++;
    }
    void IsolateInventory()
    {
        var original = items.ItemUI;
        privateUI = Instantiate(original, original.transform.parent);
        privateUI.name = "Luigi inventory (private)";
        var visibility = privateUI.GetComponent<CanvasGroup>();
        if (!visibility) visibility = privateUI.AddComponent<CanvasGroup>();
        visibility.alpha = 0; visibility.interactable = visibility.blocksRaycasts = false;
        var oldImages = original.GetComponentsInChildren<Image>(true);
        var newImages = privateUI.GetComponentsInChildren<Image>(true);
        int imageIndex = System.Array.IndexOf(oldImages, items.your_item);
        if (imageIndex < 0) throw new System.InvalidOperationException("The rival inventory image must belong to its UI.");
        items.your_item = newImages[imageIndex];
        var oldAudio = original.GetComponentsInChildren<AudioSource>(true);
        var newAudio = privateUI.GetComponentsInChildren<AudioSource>(true);
        int select = System.Array.IndexOf(oldAudio, items.PlaySelectsound);
        int selected = System.Array.IndexOf(oldAudio, items.Selected);
        if (select >= 0) items.PlaySelectsound = newAudio[select];
        if (selected >= 0) items.Selected = newAudio[selected];
        items.ItemUI = privateUI;
        // Include visible Luigi in star/bullet visuals; never re-enable hidden Mario.
        items.playerRenderers = items.playerRenderers.Where(r => r && r.enabled && r.gameObject.activeInHierarchy)
            .Concat(GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.enabled)).Distinct().ToArray();
        items.normalMaterials = items.playerRenderers.Select(r => r.sharedMaterial).ToArray();
    }
    struct Bonus { public float arc; public Vector3 offset; }
    void RebuildRoute()
    {
        source = items.path;
        if (!source || source.childCount < 3) return;
        if (NavigationPath) Destroy(NavigationPath.gameObject);
        int n = source.childCount;
        var points = Enumerable.Range(0, n).Select(i => source.GetChild(i).position).ToArray();
        var authored = (Vector3[])points.Clone();
        // The early right bend is taken a little wider, only where road exists.
        if (KartRunSettings.Argument("duel-start-line", "1") != "0")
            for (int i = 3; i <= Mathf.Min(19, n - 2); i++)
            {
                if (i > 7 && i < 16) continue;
                Vector3 before = Vector3.ProjectOnPlane(authored[i]-authored[i-1],Vector3.up).normalized;
                Vector3 after = Vector3.ProjectOnPlane(authored[i+1]-authored[i],Vector3.up).normalized;
                Vector3 outward = (before-after).normalized;
                // The first grass incursion measured near checkpoint 8 is
                // the next right bend (nodes 16-19), not the initial grid.
                for (float margin = i >= 16 ? 3 : 2; margin >= 1; margin--)
                {
                    Vector3 candidate = authored[i] + outward * margin;
                    if (!RoadAt(candidate)) continue;
                    points[i] = candidate; break;
                }
            }
        var arcs = new float[n + 1];
        for (int i = 0; i < n; i++) arcs[i+1] = arcs[i] + Vector3.Distance(points[i], points[(i+1)%n]);
        float length = arcs[n];
        var bonuses = new List<Bonus>();
        if (KartRunSettings.Argument("duel-route-bonuses", "1") != "0")
            foreach (var feature in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (!feature.isTrigger || (!feature.CompareTag("TrickCollider") && !feature.CompareTag("GliderPanel"))) continue;
                if (Mathf.Max(feature.bounds.size.x, feature.bounds.size.z) > 60) continue;
                Vector3 center = feature.bounds.center;
                float closest = float.PositiveInfinity, arc = 0;
                Vector3 projection = center;
                for (int i = 0; i < n; i++)
                {
                    Vector3 a = points[i], ab = points[(i+1)%n] - a;
                    Vector3 flat = Vector3.ProjectOnPlane(ab, Vector3.up);
                    float t = Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(center-a,Vector3.up),flat) / Mathf.Max(flat.sqrMagnitude,.001f));
                    Vector3 p = a + t * ab;
                    float distance = Vector3.ProjectOnPlane(center-p,Vector3.up).sqrMagnitude;
                    if (distance < closest) { closest=distance; projection=p; arc=arcs[i]+t*ab.magnitude; }
                }
                if (closest > 40*40 || bonuses.Any(b => Mathf.Abs(b.arc-arc) < 12)) continue;
                Vector3 offset = Vector3.ProjectOnPlane(center-projection, Vector3.up);
                bool safe = true;
                // Do not lure the kart across grass to reach a tempting bonus.
                for (float d=-45; d<=45; d+=4)
                {
                    float weight = Mathf.SmoothStep(0,1,1-Mathf.Abs(d)/45);
                    if (!RoadAt(At(arc+d,points,arcs) + offset*weight)) { safe=false; break; }
                }
                if (safe) bonuses.Add(new Bonus { arc=arc, offset=offset });
            }
        PlannedBonuses = bonuses.Count;
        NavigationPath = new GameObject("Luigi race route").transform;
        NavigationPath.gameObject.hideFlags = HideFlags.HideInHierarchy;
        for (float s=0; s<length; s+=5)
        {
            Vector3 point = At(s,points,arcs);
            foreach (var bonus in bonuses)
            {
                float delta = Mathf.Abs(s-bonus.arc);
                delta = Mathf.Min(delta,length-delta);
                if (delta < 45) point += bonus.offset * Mathf.SmoothStep(0,1,1-delta/45);
            }
            var node = new GameObject("Route point").transform;
            node.SetParent(NavigationPath,false); node.position=point;
        }
        Debug.Log($"RIVAL RACE ROUTE: {PlannedBonuses} reachable bonuses; early bend adjusted.");
    }
    static Vector3 At(float arc, Vector3[] points, float[] arcs)
    {
        arc = Mathf.Repeat(arc,arcs[arcs.Length-1]);
        for (int i=0;i<points.Length;i++)
            if (arc <= arcs[i+1]) return Vector3.Lerp(points[i],points[(i+1)%points.Length],
                (arc-arcs[i])/Mathf.Max(.001f,arcs[i+1]-arcs[i]));
        return points[0];
    }
    bool RoadAt(Vector3 point)
    {
        float nearest=float.PositiveInfinity; bool road=false;
        foreach (var hit in Physics.RaycastAll(point+Vector3.up*15,Vector3.down,35,kart.mask,QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform) || hit.normal.y<.4f || hit.distance>=nearest) continue;
            if (!hit.collider.CompareTag("Ground") && !hit.collider.CompareTag("Dirt") &&
                !hit.collider.CompareTag("TrickCollider") && !hit.collider.CompareTag("GliderPanel")) continue;
            nearest=hit.distance; road=!hit.collider.CompareTag("Dirt");
        }
        return road;
    }
    public float AdjustThrottle(float requested)
    {
        if (!enabled || !kart || requested < .3f || !kart.grounded || kart.GLIDER_FLY || kart.JUMP_PANEL ||
            kart.HitByBanana_ || kart.HitByShell_ || Mathf.Abs(kart.DriftSide) > .1f || KartEpisode.IsOnGrass(transform)) return requested;
        float floor = float.TryParse(KartRunSettings.Argument("duel-cruise", "0.85"),System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,out float value) ? Mathf.Clamp01(value) : .85f;
        Vector3 guide=KartAgent.PathDirection(transform.position,GetComponent<Rigidbody>().linearVelocity.magnitude,NavigationPath,progress.TrackDirection);
        if (Vector3.Dot(transform.forward,guide)<.98f || KartAgent.RoadAhead(kart,18,0)!=1 ||
            KartAgent.RoadAhead(kart,18,-3)!=1 || KartAgent.RoadAhead(kart,18,3)!=1) return requested;
        foreach (float angle in FrontAngles)
            foreach (var hit in Physics.RaycastAll(transform.position+transform.up,Quaternion.AngleAxis(angle,transform.up)*transform.forward,35,~0,QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && Mathf.Abs(Vector3.Dot(hit.normal,transform.up))<.7f) return requested;
        if (requested < floor) CruiseRequests++;
        return Mathf.Max(requested,floor);
    }
    void OnTriggerEnter(Collider other) => RecordBonus(other);
    void OnCollisionEnter(Collision collision) => RecordBonus(collision.collider);
    void RecordBonus(Collider other)
    {
        if (!RACE_MANAGER.RACE_STARTED) return;
        if (other.CompareTag("TrickCollider") && Time.time>nextRamp) { RampPasses++; nextRamp=Time.time+3; }
        if (other.CompareTag("GliderPanel") && Mathf.Max(other.bounds.size.x,other.bounds.size.z)<60 && Time.time>nextBoost)
        { BoostPasses++; nextBoost=Time.time+3; }
    }
    void OnDestroy()
    {
        if (NavigationPath) Destroy(NavigationPath.gameObject);
        if (privateUI) Destroy(privateUI);
    }
}
