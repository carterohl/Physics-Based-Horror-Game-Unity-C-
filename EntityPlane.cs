using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityPlane : MonoBehaviour{
    Transform Player;

    void Awake(){
        Player = GameObject.Find("/Player").transform;
    }

    void Update(){
        transform.LookAt(Player);
    }
}
