using UnityEngine;

public class DuelDriverAppearance : ScriptableObject
{
    public GameObject model;
    public RuntimeAnimatorController animations;
    public Material suit, face;
    public Sprite mapIcon;

    public void Apply(Player rival)
    {
        var seat = rival.Driver.transform;
        var mount = new GameObject("Siège de Luigi").transform;
        mount.SetParent(seat.parent, false);
        mount.SetLocalPositionAndRotation(seat.localPosition, seat.localRotation);
        mount.localScale = seat.localScale * 0.96f;
        var visual = Instantiate(model, mount, false);
        visual.name = "Luigi";
        visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        var animator = visual.GetComponent<Animator>();
        animator.runtimeAnimatorController = animations;
        animator.applyRootMotion = false;
        foreach (var mesh in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var materials = mesh.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                string slot = materials[i] ? materials[i].name.ToLowerInvariant() : "";
                if (slot.Contains("eye") || slot.Contains("face")) materials[i] = face;
                else materials[i] = suit;
            }
            mesh.sharedMaterials = materials;
        }
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        foreach (var extraMesh in visual.GetComponentsInChildren<MeshRenderer>(true)) extraMesh.enabled = false;
        foreach (var mesh in seat.GetComponentsInChildren<SkinnedMeshRenderer>()) mesh.enabled = false;
        var map = rival.GetComponent<Minimap>();
        map.playerInMap = Instantiate(map.playerInMap, map.playerInMap.parent);
        map.playerInMap.name = "Luigi";
        var image = map.playerInMap.GetChild(0).GetComponent<UnityEngine.UI.Image>();
        if (image) image.sprite = mapIcon;
    }

}

