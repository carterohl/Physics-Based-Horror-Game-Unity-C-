using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/* 
 * Pathfinding Algorithm
 * By Jase Beaubien @jaseb@iastate.edu
 * 2/21/2024
 * Explanation: https://youtu.be/-L-WgKMFuhE?si=0p61v3RXhj9XKtxP
 * All code is original.
*/ 

/*
 * Potential Logic Issues/Bugs:
 * - None predicted or displayed
*/

public class Pathfinder : MonoBehaviour{
    public float TileScalar;
    public LayerMask TileLayer;
    public LayerMask WallLayer;
    private const int MaxAttempts = 200;
    private Vector3 FinalPosition;
    private Vector3 InitialPosition;

    public class Node{
        private Transform NodeTransform; //real transform of correlating gameobject
        private Node ParentNode; //preceeding node
        private float GCost; //distance from start
        private float HCost; //distance from end
        private float FCost; //sum
        private bool ScannedAround;

        public Node(Transform Self, Node Parent, Vector3 StartPosition, Vector3 EndPosition){
            NodeTransform = Self;
            ParentNode = Parent;
            GCost = (NodeTransform.position - StartPosition).magnitude;
            HCost = (NodeTransform.position - EndPosition).magnitude;
            FCost = GCost + HCost;
            ScannedAround = false;
        }

        public Node(){} //for null initialization

        public Node(float gc, float hc){ //dummy constructor for debugging. delete later
            GCost = gc;
            HCost = hc;
            FCost = GCost + HCost;
        }

        public void MarkAsScanned(){
            ScannedAround = true;
        }

        public bool hasBeenScanned(){
            return ScannedAround;
        }

        public float getGCost(){
            return GCost;
        }
        public float getHCost(){
            return HCost;
        }
        public float getFCost(){
            return FCost;
        }
        public Vector3 getNodePosition(){
            return NodeTransform.position;
        }
        public Node getPreceeder(){
            return ParentNode;
        }
    }

    private List<Node> ActiveNodes = new List<Node>(); //all accessed nodes
    private List<Vector3> HeldSpots = new List<Vector3>();

    private List<Vector3> RawInstructions = new List<Vector3>();
    private List<Vector3> FormattedInstructions = new List<Vector3>();
    private List<Node> RawPath = new List<Node>();
    public List<Vector3> Scan(Vector3 begin, Vector3 end){
        Reset();

        FinalPosition = GetTileTransformUnderneath(end).position;
        InitialPosition = GetTileTransformUnderneath(begin).position;
        //Debug.Log(begin);
        if(CompareVectors(InitialPosition, FinalPosition, 0.01f)){
            RawInstructions.Add(Vector3.zero);
            return RawInstructions;
        }

        Node EndNode = null;
        Node CurrentBest = new Node();
        Node FirstNode = GetTileUnderneath(begin);
        ActiveNodes.Add(FirstNode);
        for(int i = 0; (i < MaxAttempts) && (EndNode == null); i++){
            CurrentBest = FindBestCandidate(ActiveNodes);

            if(CurrentBest.getHCost() < 0.0001f){
                EndNode = CurrentBest;
            }else{
                ScanAround(CurrentBest);
                CurrentBest.MarkAsScanned();
            }
        }
        //DebugNodes();

        if(EndNode == null){
            //Debug.Log("Pathfinding Failed: End node not found.");
            return null;
        }
        //Debug.Log("Successfully mapped to " + CurrentBest.getNodePosition());

        //Debug.Log("Backtracking...");
        Node Current = EndNode;
        Node Preceeder = Current.getPreceeder();
        for(int i = 0; (i < MaxAttempts) && (Current.getPreceeder() != null); i++){
            //add code to compare current and preceeder position for directions
            Preceeder = Current.getPreceeder();
            RawPath.Add(Current);
            Current = Preceeder;
        }
        RawPath.Add(FirstNode);
        for(int i = RawPath.Count - 1; i > 0; i--){ //flips and formats list
            RawInstructions.Add(RawPath[i - 1].getNodePosition() - RawPath[i].getNodePosition());
        }
        Vector3 LastInstruction = Vector3.zero;
        int formattedCount = 0;
        for(int i = 0; i < RawInstructions.Count; i++){
            if(CompareVectors(RawInstructions[i].normalized, LastInstruction.normalized, 0.001f)){
                FormattedInstructions[formattedCount - 1] += RawInstructions[i];
            }else{
                FormattedInstructions.Add(RawInstructions[i]);
                formattedCount += 1;
            }
            LastInstruction = RawInstructions[i];
        }

        return FormattedInstructions;
    }

    public void DebugNodes(){
        for(int i = 0; i < ActiveNodes.Count; i++){
            Debug.Log("Node (" + (i + 1) + ") position: " + ActiveNodes[i].getNodePosition() + " G: " + ActiveNodes[i].getGCost() + " H: " + ActiveNodes[i].getHCost() + " F: " + ActiveNodes[i].getFCost());
        }
    }

    private Node GetTileUnderneath(Vector3 pos){
        RaycastHit hit;
        pos = new Vector3(pos.x, -1.0f, pos.z);

        if(!Physics.Raycast(pos, Vector3.up, out hit, 5.0f, TileLayer)){
            return null;
        }

        Node result = new Node(hit.transform, null, InitialPosition, FinalPosition);
        HeldSpots.Add(hit.transform.position);
        return result;
    }

    public Transform GetTileTransformUnderneath(Vector3 pos){
        RaycastHit hit;
        pos = new Vector3(pos.x, -1.0f, pos.z);

        if(!Physics.Raycast(pos, Vector3.up, out hit, 5.0f, TileLayer)){
            return null;
        }

        return hit.transform;
    }

    private void Reset(){
        ActiveNodes.Clear();
        HeldSpots.Clear();
        RawPath.Clear();
        RawInstructions.Clear();
        FormattedInstructions.Clear();
    }

    public void ScanAround(Node n){
        //cardinal
        ScanAdjacent(n, Vector3.forward);
        ScanAdjacent(n, -Vector3.forward);
        ScanAdjacent(n, Vector3.right);
        ScanAdjacent(n, -Vector3.right);
        //diagonal
        ScanAdjacent(n, Vector3.forward + Vector3.right);
        ScanAdjacent(n, Vector3.forward - Vector3.right);
        ScanAdjacent(n, -Vector3.forward + Vector3.right);
        ScanAdjacent(n, -Vector3.forward - Vector3.right);
    }

    public void ScanAdjacent(Node Parent, Vector3 Direction){ //checks for node in given direction
        RaycastHit tileHit;
        Vector3 ScanPosition = Parent.getNodePosition() + (Direction * TileScalar) - Vector3.up;
        if(!Physics.Raycast(ScanPosition, Vector3.up, out tileHit, 5.0f, TileLayer)){
            return;
        }

        RaycastHit wallHit;
        float scanDistance = TileScalar * 0.75f; //previously 1.5
        ScanPosition = Parent.getNodePosition() + Vector3.up;
        if(Physics.Raycast(ScanPosition, Direction, out wallHit, scanDistance, WallLayer)){
            return;
        }

        if(HeldSpots.Contains(tileHit.transform.position)){
            return;
        }

        Node result = new Node(tileHit.transform, Parent, InitialPosition, FinalPosition);
        HeldSpots.Add(tileHit.transform.position);
        ActiveNodes.Add(result);
    }

    public Node FindBestCandidate(List<Node> nodes){ //find lowest FCost. if two match, find lowest HCost
        Node mostPromising = nodes[nodes.Count - 1]; //caused issues in the past, watch carefully
        float lowestFCost = mostPromising.getFCost();
        for(int i = 0; i < nodes.Count; i++){
            if(nodes[i].hasBeenScanned()){
                continue;
            }
            if(nodes[i].getFCost() < lowestFCost){
                mostPromising = nodes[i];
                lowestFCost = nodes[i].getFCost();
            }
            if(Mathf.Abs(nodes[i].getFCost() - lowestFCost) <= 0.0001){
                if(nodes[i].getHCost() < mostPromising.getHCost()){
                    mostPromising = nodes[i];
                    lowestFCost = nodes[i].getFCost();
                }
            }
        }
        return mostPromising;
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
