using UnityEngine;

public static class KartRoadSensorChecks
{
    public static void Run()
    {
        var root = new GameObject("Road sensor checks");
        try
        {
            root.transform.position = new Vector3(12000, 10000, 12000);
            var kartObject = new GameObject("Kart");
            kartObject.transform.SetParent(root.transform, false);
            var kart = kartObject.AddComponent<Player>();
            kart.mask = ~0;
            var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.transform.SetParent(root.transform, false);
            surface.transform.localPosition = new Vector3(0, -1, 10);
            surface.tag = "Ground";
            Physics.SyncTransforms();
            if (KartAgent.RoadAhead(kart, 10, 0) != 1) throw new System.Exception("Road not detected ahead.");
            surface.tag = "Dirt";
            if (KartAgent.RoadAhead(kart, 10, 0) != 0) throw new System.Exception("Grass not distinguished from road.");
            if (KartAgent.RoadAhead(kart, 10, 6) != -1) throw new System.Exception("Missing lateral ground not detected.");
            surface.GetComponent<Collider>().isTrigger = true;
            Physics.SyncTransforms();
            if (KartAgent.RoadAhead(kart, 10, 0) != -1) throw new System.Exception("Trigger counted as road.");
            var path = new GameObject("Test route").transform;
            path.SetParent(root.transform, false);
            foreach (var p in new[] { new Vector3(-10,0,0), new Vector3(-10,0,100), new Vector3(10,0,100), new Vector3(10,0,0) })
            {
                var point = new GameObject("Waypoint").transform;
                point.SetParent(path, false); point.position = p;
            }
            var straight = KartAgent.PathDirection(new Vector3(-8,0,20), 40, path, Vector3.forward);
            var corner = KartAgent.PathDirection(new Vector3(-10,0,90), 40, path, Vector3.forward);
            if (straight.x >= 0 || straight.z <= 0 || corner.x <= 0 || corner.z <= 0)
                throw new System.Exception("Route observation does not return toward the road or anticipate its corner.");
            Debug.Log("KartRoadSensorChecks PASS: road, grass, missing ground and ignored triggers.");
        }
        finally { Object.DestroyImmediate(root); }
    }
}
