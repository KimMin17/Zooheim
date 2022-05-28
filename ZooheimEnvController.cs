using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZooheimEnvController : MonoBehaviour {
    public BaseAnimalAgent[] Animals;
    List<BaseAnimalAgent> AnimalAgentList = new List<BaseAnimalAgent>();

    public int range = 50;
    int[] AnimalNum;

    float WorldTimer;
    float LearningTimer;

    public void Start() {
        int x, z;
        WorldTimer = 0;
        LearningTimer = 0;

        for(int i = 0; i < Animals.Length; i++) {
            for(int j = 0; j < AnimalNum[i]; j++) {
                x = Random.Range(-range, range);
                z = Random.Range(-range, range); 
                AnimalAgentList.Add(Instantiate(Animals[i], new Vector3(x, 0.5f, z), Quaternion.identity));
            }
        }
    }

    public void FixedUpdate() {
        WorldTimer += Time.deltaTime;
        LearningTimer += Time.deltaTime;

        if(LearningTimer > 360f) FinishEpisode();
        
    }

    void FinishEpisode() {
        foreach(var item in AnimalAgentList) {
            item.EndEpisode();
        }
        ResetScene();
        LearningTimer = 0f;
    }

    void ResetScene() {
        int x, z;
        foreach(var item in AnimalAgentList) {
            x = Random.Range(-range, range);
            z = Random.Range(-range, range); 
            item.transform.localPosition = new Vector3(x, 0.5f, z);
        }
    }
}
