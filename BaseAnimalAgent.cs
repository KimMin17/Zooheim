using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BaseAnimalAgent : Agent {
    protected ZooheimEnvController EnvController;   //학습환경을 전체적으로 관리하는 스크립트

    protected Rigidbody AnimalRigidBody;            //물리엔진

    protected int AnimalId;                         //해당 객체의 ID

    protected float AnimalHP;                       //현재 체력
    protected float AnimalHPLimit;                  //최대 체력

    protected float AnimalMoveSpeed = 0.5f;         //이동 속도(Rigidbody에 지속적으로 가해주는 힘의 크기)
    protected float AnimalTurnSpeed = 100f;         //회전 속도
    protected float AnimalSpeedLimit = 10f;         //최대 이동 속도(velocity.sqrMagnitude로 계산되는 값)
    protected float AnimalAccelFactor = 1.5f;       //달릴 때 이동 속도에 곱해지는 값

    protected float AnimalEnergy;                   //현재 에너지
    protected float AnimalEnergyLimit;              //최대 에너지

    protected float AnimalAttackPower;                   //공격력
    
    protected float AnimalEnoughHP;                 //동물이 안전한 상태를 정하기 위한 지표(번식 등을 할 때 참고할 수 있도록 추가함)
    protected float AnimalEnoughEnergy;             //동물이 배부른 상태를 정하기 위한 지표(번식 등을 할 때 참고할 수 있도록 추가함)

    protected float AnimalLeastHP;                  //동물의 위험한 상태를 정하기 위한 지표
    protected float AnimalLeastEnergy;              //동물의 굶주린 상태를 정하기 위한 지표(예를 들어 Energy < LeastEngery일때 ReduceHP(AnimalStarvationDamage) 같은 식으로 이용)

    protected float AnimalStarvationDamage;         //동물이 굶주린 상태에서 지속적으로 받는 데미지

    protected float AnimalChildbirthPeriod;         //번식을 할 수 있는 주기

    public bool AniFlagIdle;                        //Animation idle을 위한 flag
    public bool AniFlagWalking;                     //Animation walk을 위한 flag
    public bool AniFlagAttacking;                   //Animation attack을 위한 flag
    public bool AniFlagRunning;                     //Animation run을 위한 flag
    public bool AniFlagDead;                        //Animation death을 위한 flag
    

    protected float AnimalTimer;                    //시간을 재기 위한 타이머

    public void Awake() {                           //생성될 때 실행되는 함수, 초기화에 이용
        EnvController = FindObjectOfType<ZooheimEnvController>();
        AnimalRigidBody = GetComponent<Rigidbody>();
        InitAniFlag();
    }
    
    public void FixedUpdate() {                     //일정한 간격에 따라 호출됨
        AnimalTimer += Time.deltaTime;
    }

    public override void OnEpisodeBegin() {         //학습 episode가 시작될 때 호출됨
    }

    public override void OnActionReceived(ActionBuffers actionBuffers) {        //ml agent가 입력값을 주었을 때 실행됨
        MoveAgent(actionBuffers);
    }

    public override void Heuristic(in ActionBuffers actionsOut) {               //입력값을 수동으로 주었을 때 실행됨(Heuristic only 사용시)
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 1;
        if (Input.GetKey(KeyCode.D)) discreteActionsOut[1] = 1;
        if (Input.GetKey(KeyCode.A)) discreteActionsOut[1] = 2;
        if (Input.GetKey(KeyCode.LeftShift)) discreteActionsOut[2] = 1;
    }

    public virtual void MoveAgent(ActionBuffers actionBuffers) {                //입력값을 기반으로 agent를 움직이기 위한 코드
        var discreteActions = actionBuffers.DiscreteActions;
        
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var forwardAxis = discreteActions[0];
        var rotateAxis = discreteActions[1];
        var Accel = discreteActions[2];

        var AnimalSpeed;
        if(Accel == 1) AnimalSpeed = AnimalMoveSpeed * AnimalAccelFactor;
        else AnimalSpeed = AnimalMoveSpeed;

        switch(forwardAxis) {
            case 1:
                dirToGo = transform.forward * AnimalSpeed;
                break;
        }

        switch(rotateAxis) {
            case 1:
                rotateDir = transform.up * 1f;
                break;
            case 2:
                rotateDir = transform.up * -1f;
                break;
        }

        transform.Rotate(rotateDir, Time.fixedDeltaTime * AnimalTurnSpeed);
        AnimalRigidBody.AddForce(dirToGo, ForceMode.VelocityChange);

        if (AnimalRigidBody.velocity.sqrMagnitude > AnimalSpeedLimit)
            AnimalRigidBody.velocity *= 0.95f;
    }

    public virtual void OnTriggerEnter(Collider other) {                        //어떤 지역에 진입했을때 호출됨
    }

    public virtual void OnCollisionEnter(Collision other) {                     //어떤 물체와 충돌했을때 호출됨
    }
    
    public virtual void OnCollisionStay(Collision other) {                      //어떤 물체와 계속 접촉하고 있을때 호출됨
        if(other.gameObject.CompareTag("Wall"))
            AddReward(-0.01f);
    }

    public virtual float Eat() {                                                //먹는 기능을 구현한 함수
    }

    public virtual void Attack() {                                              //공격하는 기능을 구현한 함수

    }

    public virtual void Resist() {                                              //공격받았을때 반격하는 기능을 구현한 함수
        
    }

    public virtual void Childbirth() {                                          //자식을 낳는 기능을 구현한 함수

    }

    public virtual void RecoverHP(float Recover) {                              //체력을 회복하는 함수
        AnimalHP += Recover;
        if(AnimalHP > AnimalHPLimit) AnimalHP = AnimalHPLimit;
    }

    public virtual void LoseHP(float Damage) {                                  //체력을 잃는 함수
        AnimalHP -= Damage;
        if(AnimalHP < 0) Death();
    }

    public virtual void Death() {                                               //객체가 사망했을때 실행되는 함수
        SwitchDead();
        Destroy(gameObject, 3);
    }

    public void CheckStarvation() {                                             //객체가 굶주리고 있는지 확인하는 함수
        if(AnimalEnergy < AnimalLeastEnergy) {
            LoseHP(AnimalStarvationDamage * Time.deltaTime);
        }
    }

    public void Halt(float time) {                                              //객체를 time 동안 멈추게 하는 함수
        float TempTime = AnimalTimer;
        while(AnimalTimer < TempTime + time) { }
    }

    public void InitAniFlag() {                                                 //animation flag를 초기화하는 함수
        AniFlagIdle = true;
        AniFlagAttacking = false;
        AniFlagDead = false;
        AniFlagRunning = false;
        AniFlagWalking = false;
    }

    public void SwitchRunning() {                                               //animation flag state를 run으로 변경
        AniFlagIdle = false;
        AniFlagAttacking = false;
        AniFlagDead = false;
        AniFlagRunning = true;
        AniFlagWalking = false;
    }

    public void SwitchAttacking() {                                             //animation flag state를 attack으로 변경
        AniFlagIdle = false;
        AniFlagAttacking = true;
        AniFlagDead = false;
        AniFlagRunning = false;
        AniFlagWalking = false;
    }

    public void SwitchWalking() {                                               //animation flag state를 walk으로 변경
        AniFlagIdle = false;
        AniFlagAttacking = false;
        AniFlagDead = false;
        AniFlagRunning = false;
        AniFlagWalking = true;
    }

    public void SwitchIdle() {                                                  //animation flag state를 idle로 변경
        AniFlagIdle = true;
        AniFlagAttacking = false;
        AniFlagDead = false;
        AniFlagRunning = false;
        AniFlagWalking = false;
    }

    public void SwitchDead() {                                                  //animation flag state를 death로 변경
        AniFlagIdle = false;
        AniFlagAttacking = false;
        AniFlagDead = true;
        AniFlagRunning = false;
        AniFlagWalking = false;
    }
}
