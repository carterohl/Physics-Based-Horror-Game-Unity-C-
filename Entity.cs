using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Entity Behavior Script
 * By Jase Beaubien @jaseb@iastate.edu
*/

public class Entity : MonoBehaviour{
    [Header("Config")]
    public float WalkSpeed;
    public float InvestigateSpeed;
    public float RunSpeed;
    public float ChaseCoolDown;
    public float VisibilityDistance;
    //public float SearchRegenCycle;
    public float WanderTurnChance;
    [Header("Debugging")]
    public bool CanHearPlayer; //set private later

    public GameObject Player;
    private Pathfinder PathFind;

    private const float DiagonalCheck = 0.75f; //must be more that sqrt(2)/2 and a bit less than 1

    void Start(){
        PathFind = GetComponent<Pathfinder>();
        OnTile = transform.position;
        LastPosition = TilePosition(OnTile);
    }

    public void PathTo(Vector3 pos){
        StartPosition = TilePosition(transform.position);
        Path = PathFind.Scan(transform.position, pos);
        DirectionStep = 0;
        IsChasing = true;
    }

    public bool CheckForSound(){
        return CanHearPlayer; //placeholder
    }

    private Vector3 WanderDirection;
    private List<Vector3> Path;
    private Vector3 StartPosition;
    private int DirectionStep;
    private bool IsChasing = false;
    private bool PlayerInView = false;
    private bool LastPlayerInView = false;
    private float ChaseTimer = 0.0f;
    private Vector3 OnTile = Vector3.zero;
    private List<Vector3> ViableDirections;
    private bool centered = false;
    private bool lastCentered = false;
    private float MoveSpeed;
    private Vector3 FavoredDirection = Vector3.zero;
    //public float PathTimer = 0.0f;

    private bool CanTurn = false;
    private bool LastCanTurn = false;
    //private bool DoneWithLastStep = false;
    void Update(){
        //Debug.Log(Mathf.RoundToInt(1.0f / Time.deltaTime));
        PlayerInView = CanSeePlayer();
        CanHearPlayer = CheckForSound();
        if(PlayerInView != LastPlayerInView){
            if(PlayerInView && (!IsChasing)){
                //CenterWithTile();
                PathTo(Player.transform.position);
            }
        }
        LastPlayerInView = PlayerInView;

        if(PlayerInView){
            ChaseTimer = ChaseCoolDown;
        }else{
            if(ChaseTimer > 0.0f){
                MoveSpeed = RunSpeed;
                ChaseTimer -= Time.deltaTime;
            }else{
                if(CanHearPlayer){
                    MoveSpeed = InvestigateSpeed;
                }else{
                    MoveSpeed = WalkSpeed;
                }
            }
        }

        if(IsChasing){
            //chasing
            FollowPath(RunSpeed);
        }else{
            //wandering
            centered = Center();
            if(centered && !lastCentered){ //check if in center of tile
                //DoneWithLastStep = true;
                CenterWithTile();
                ViableDirections = ValidDirections();
                FavoredDirection = GetFavoredDirection(ViableDirections);
                if(ChaseTimer > 0.0f || CanHearPlayer){
                    //search bias towards a specific direction
                    WanderDirection = FavoredDirection;
                }else{
                    //wander aimlessly
                    if(!VectorListContains(ViableDirections, WanderDirection)){
                        if(ViableDirections.Count > 1){
                            ViableDirections.Remove(-WanderDirection); //makes it so it won't make a 180 unless it needs to
                        }
                        WanderDirection = ViableDirections[Random.Range(0, (ViableDirections.Count/* - 1*/))];
                    }else{
                        CanTurn = ViableDirections.Count > 2;
                        if(CanTurn && (!LastCanTurn) && (Random.Range(0.0f, 1.0f) <= WanderTurnChance)){
                            ViableDirections.Remove(-WanderDirection);
                            WanderDirection = ViableDirections[Random.Range(0, (ViableDirections.Count/* - 1*/))];
                        }
                        LastCanTurn = CanTurn;
                    }
                }
            }/*else{
                DoneWithLastStep = false;
            }*/
            lastCentered = centered;
            transform.Translate(WanderDirection.normalized * MoveSpeed * Time.deltaTime);
        }
    }

    public bool VectorListContains(List<Vector3> lis, Vector3 vec){
        for(int i = 0; i < lis.Count; i++){
            if(CompareVectors(lis[i], vec, 0.01f)){
                return true;
            }
        }
        return false;
    }

    public Vector3 GetFavoredDirection(List<Vector3> lis){
        Vector3 dif = (Player.transform.position - transform.position).normalized;
        Vector3 dir = Vector3.zero;
        if(Mathf.Abs(dif.x) >= 0.01f){
            if(dif.x > 0.0f){
                dir.x = 1.0f;
            }else{
                dir.x = -1.0f;
            }
        }
        if(Mathf.Abs(dif.z) >= 0.01f){
            if(dif.z > 0.0f){
                dir.z = 1.0f;
            }else{
                dir.z = -1.0f;
            }
        }

        dir *= PathFind.TileScalar;

        Vector3 tryvec;
        
        if(lis.Count > 1){
            lis.Remove(-WanderDirection);
        }

        if(VectorListContains(lis, dir)){
            return dir;
        }

        tryvec = new Vector3(dir.x, 0.0f, 0.0f);
        if(VectorListContains(lis, tryvec)){
            return tryvec;
        }
        tryvec = new Vector3(0.0f, 0.0f, dir.z);
        if(VectorListContains(lis, tryvec)){
            return tryvec;
        }
        
        //remove diagonals before random selection to prevent from straying
        for(int i = 0; i < lis.Count; i++){
            if((lis[i].x + lis[i].z) > (PathFind.TileScalar * DiagonalCheck * 2.0f)){
                lis.Remove(lis[i]);
            }
        }
        return lis[Random.Range(0, lis.Count)];
    }

    public Vector3 Flatten(Vector3 pos){
        return new Vector3(pos.x, 0.0f, pos.z);
    }

    public Vector3 TilePosition(Vector3 pos){
        return PathFind.GetTileTransformUnderneath(pos).position;
    }

    Vector3 LastPosition;
    public bool Center(){
        if((Flatten(transform.position) - LastPosition).magnitude >= WanderDirection.magnitude){
            LastPosition = TilePosition(transform.position);
            return true;
        }
        return false;
    }

    public void CenterWithTile(){
        Vector3 tile = TilePosition(transform.position);
        transform.position = new Vector3(tile.x, transform.position.y, tile.z);
    }

    public void FollowPath(float speed){
        if(Path != null){
            if(DirectionStep < Path.Count){
                transform.Translate(Path[DirectionStep].normalized * speed * Time.deltaTime);
                if((Flatten(transform.position) - StartPosition).magnitude >= Path[DirectionStep].magnitude){
                    //StartPosition += Path[DirectionStep];
                    StartPosition = TilePosition(transform.position);
                    DirectionStep += 1;
                    if(DirectionStep >= Path.Count){
                        IsChasing = false;
                    }
                    if(PlayerInView){
                        PathTo(Player.transform.position);
                        /*PathTimer += Time.deltaTime;
                        if(PathTimer >= SearchRegenCycle){
                            PathTo(Player.transform.position);
                            PathTimer = 0.0f;
                            Debug.Log("G");
                        }*/
                    }
                }
            }else{
                IsChasing = false;
            }
        }else{
            IsChasing = false;
        }
    }

    public bool CanSeePlayer(){
        RaycastHit hit;
        Vector3 SearchDirection = (Player.transform.position - transform.position).normalized;
        if(!Physics.Raycast(transform.position, SearchDirection, out hit, VisibilityDistance)){
            return false;
        }
        if(hit.transform.tag == "Player"){
            return true;
        }
        return false;
    }

    public List<Vector3> ValidDirections(){
        List<Vector3> result = new List<Vector3>();
        //cardinal
        if(IsValidDirection(Vector3.forward)){
            result.Add(Vector3.forward * PathFind.TileScalar);
        }
        if(IsValidDirection(-Vector3.forward)){
            result.Add(-Vector3.forward * PathFind.TileScalar);
        }
        if(IsValidDirection(Vector3.right)){
            result.Add(Vector3.right * PathFind.TileScalar);
        }
        if(IsValidDirection(-Vector3.right)){
            result.Add(-Vector3.right * PathFind.TileScalar);
        }
        //diagonal
        if(IsValidDirection(Vector3.forward + Vector3.right)){
            result.Add((Vector3.forward + Vector3.right) * PathFind.TileScalar);
        }
        if(IsValidDirection(Vector3.forward - Vector3.right)){
            result.Add((Vector3.forward - Vector3.right) * PathFind.TileScalar);
        }
        if(IsValidDirection(-Vector3.forward + Vector3.right)){
            result.Add((-Vector3.forward + Vector3.right) * PathFind.TileScalar);
        }
        if(IsValidDirection(-Vector3.forward - Vector3.right)){
            result.Add((-Vector3.forward - Vector3.right) * PathFind.TileScalar);
        }
        return result;
    }

    public bool IsValidDirection(Vector3 dir){
        RaycastHit tileHit;
        Vector3 ScanPosition = new Vector3(transform.position.x, -1.0f, transform.position.z) + (dir * PathFind.TileScalar);
        if(!Physics.Raycast(ScanPosition, Vector3.up, out tileHit, 5.0f, PathFind.TileLayer)){
            return false;
        }

        RaycastHit wallHit;
        float scanDistance = PathFind.TileScalar * DiagonalCheck;
        if(Physics.Raycast(transform.position, dir, out wallHit, scanDistance, PathFind.WallLayer)){
            return false;
        }
        return true;
    }

    public bool CompareVectors(Vector3 a, Vector3 b, float c){
        if(Mathf.Abs(a.x - b.x) > c){
            return false;
        }
        if(Mathf.Abs(a.y - b.y) > c){
            return false;
        }
        if(Mathf.Abs(a.z - b.z) > c){
            return false;
        }
        return true;
    }
}
