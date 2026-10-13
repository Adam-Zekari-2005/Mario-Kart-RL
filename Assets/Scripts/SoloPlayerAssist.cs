using UnityEngine;

// Local keyboard assistance; does not change lap/checkpoint progression.
[RequireComponent(typeof(Player),typeof(Rigidbody))]
public sealed class SoloPlayerAssist : MonoBehaviour
{
    Player kart;
    Rigidbody body;
    OutOfBounds recovery;
    Vector3 safePosition,previousPosition;
    Quaternion safeRotation,previousRotation;
    float nextSample,stuckTime,cooldown;
    bool showHelp=true;
    void Start()
    {
        kart=GetComponent<Player>();body=GetComponent<Rigidbody>();recovery=GetComponent<OutOfBounds>();
        safePosition=previousPosition=transform.position;
        safeRotation=previousRotation=transform.rotation;
    }
    void Update()
    {
        if(KartKeyboard.GetKeyDown(KeyCode.F1))showHelp=!showHelp;
        if(!RACE_MANAGER.RACE_STARTED||RACE_MANAGER.RACE_COMPLETED)return;
        cooldown-=Time.deltaTime;
        if(KartKeyboard.GetKeyDown(KeyCode.R)&&cooldown<=0)ReturnToTrack();
        if(recovery!=null&&(recovery.PlayerBeingMoved||recovery.FellInWater||recovery.outOfBounds))return;
        bool driving=KartKeyboard.GetKey(KeyCode.Space)&&!KartKeyboard.GetKey(KeyCode.S);
        stuckTime=driving&&body.linearVelocity.sqrMagnitude<1?stuckTime+Time.deltaTime:0;
        if(stuckTime>4&&cooldown<=0)ReturnToTrack();
        if(Time.time>=nextSample&&body.linearVelocity.sqrMagnitude>25&&!kart.GLIDER_FLY&&!kart.JUMP_PANEL&&!kart.antiGravity)
        {
            nextSample=Time.time+1;
            var items=GetComponent<ItemManager>();
            float nearPath=float.PositiveInfinity;
            Vector3 pathPoint=transform.position,pathDirection=transform.forward;
            if(items!=null&&items.path!=null)
                for(int i=0;i<items.path.childCount;i++)
                {
                    Vector3 a=items.path.GetChild(i).position,b=items.path.GetChild((i+1)%items.path.childCount).position;
                    Vector3 ab=b-a;float t=Mathf.Clamp01(Vector3.Dot(transform.position-a,ab)/Mathf.Max(ab.sqrMagnitude,.001f));
                    Vector3 point=a+t*ab;float distance=Vector3.Distance(transform.position,point);
                    if(distance<nearPath){nearPath=distance;pathPoint=point;pathDirection=ab;}
                }
            if(nearPath<6)
            {
                foreach(var hit in Physics.RaycastAll(pathPoint+Vector3.up*3,Vector3.down,7,~0,QueryTriggerInteraction.Ignore))
                {
                    if(hit.transform.IsChildOf(transform)||hit.normal.y<.7f||(!hit.collider.CompareTag("Ground")&&!hit.collider.CompareTag("Dirt")))continue;
                    safePosition=previousPosition;safeRotation=previousRotation;
                    previousPosition=hit.point+Vector3.up*.6f;
                    previousRotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(pathDirection,Vector3.up).normalized,Vector3.up);
                    break;
                }
            }
        }
    }
    public void ReturnToTrack()
    {
        if(recovery!=null&&recovery.PlayerBeingMoved)return;
        body.isKinematic=false;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        kart.currentspeed=0;kart.Boost_time=0;kart.stopDrift();
        Vector3 destination=safePosition+safeRotation*Vector3.up*.5f;
        transform.SetPositionAndRotation(destination,safeRotation);
        body.position=destination;body.rotation=safeRotation;
        if(recovery!=null){recovery.FellInWater=false;recovery.outOfBounds=false;}
        stuckTime=0;cooldown=3;Physics.SyncTransforms();
    }
    void OnGUI()
    {
        if(!showHelp){GUI.Label(new Rect(12,12,200,24),"F1 : commandes");return;}
        GUI.Box(new Rect(12,12,405,155),"Commandes — F1 pour masquer");
        GUI.Label(new Rect(24,38,385,130),"Flèches / ZQSD : conduire (bas/S : frein puis recul)\nEspace + direction : drift en roulant\nRelâcher Espace : mini-turbo si le drift est chargé\nMaj : objet devant • Ctrl : objet derrière\nTraverser une boîte ? pour récupérer un objet\nChampignon : Maj pour le boost • R : retour sur piste");
    }
}
