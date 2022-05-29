using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BaseAnimalAgent : Agent {
    //학습환경을 전체적으로 관리하는 스크립트
    protected ZooheimEnvController EnvController;

    //애니메이션 컴포넌트
    protected Animator AnimalAni;
    
    //물리엔진
    protected Rigidbody AnimalRigidBody;

    //해당 객체의 ID
    protected int AnimalId;

    //현재 체력
    protected float AnimalHP;
    //최대 체력
    protected float AnimalHPLimit;

    //이동 속도(Rigidbody에 지속적으로 가해주는 힘의 크기)
    protected float AnimalMoveSpeed;
    //회전 속도
    protected float AnimalTurnSpeed;
    //최대 이동 속도(velocity.sqrMagnitude로 계산되는 값)
    protected float AnimalSpeedLimit;
    //달릴 때 이동 속도에 곱해지는 값
    protected float AnimalAccelFactor;

    //현재 에너지
    protected float AnimalEnergy;
    //최대 에너지
    protected float AnimalEnergyLimit;

    //공격력
    protected float AnimalAttackPower;
    //공격하는데 필요한 에너지의 양
    protected float AnimlaAttackEnergy;
    
    //동물이 안전한 상태를 정하기 위한 지표(번식 등을 할 때 참고할 수 있도록 추가함)
    protected float AnimalEnoughHP;
    //동물이 배부른 상태를 정하기 위한 지표(번식 등을 할 때 참고할 수 있도록 추가함)
    protected float AnimalEnoughEnergy;

    //동물의 위험한 상태를 정하기 위한 지표
    protected float AnimalLeastHP;
    //동물의 굶주린 상태를 정하기 위한 지표(예를 들어 Energy < LeastEngery일때 ReduceHP(AnimalStarvationDamage) 같은 식으로 이용)
    protected float AnimalLeastEnergy;

    //동물이 굶주린 상태에서 지속적으로 받는 데미지
    protected float AnimalStarvationDamage;

    //번식을 할 수 있는 주기
    protected float AnimalChildbirthPeriod;

    //음식을 먹었을 때 상점
    protected float AnimalEatReward;
    //자식을 낳았을 때 상점
    protected float AnimalChildbirthReward;
    //죽었을 때 벌점
    protected float AnimalDeathPenalty;
    //굶고 있을 때 벌점
    protected float AnimalStarvationPenalty;

    //Animation idle을 위한 flag
    public bool AniFlagIdle;
    //Animation walk을 위한 flag
    public bool AniFlagWalking;
    //Animation attack을 위한 flag
    public bool AniFlagAttacking;
    //Animation run을 위한 flag
    public bool AniFlagRunning;
    //Animation death을 위한 flag
    public bool AniFlagDead;
    
    //시간을 재기 위한 타이머
    protected float AnimalTimer;

    //생성될 때 실행되는 함수, 초기화에 이용
    public void Awake() {
        EnvController = FindObjectOfType<ZooheimEnvController>();
        AnimalRigidBody = GetComponent<Rigidbody>();
        AnimalAni = GetComponent<Animator>();
        InitAniFlag();
    }
    
    //일정한 간격에 따라 호출됨
    public void FixedUpdate() {
        AnimalTimer += Time.deltaTime;
    }

    //학습 episode가 시작될 때 호출됨
    public override void OnEpisodeBegin() {
    }

    //ml agent가 입력값을 주었을 때 실행됨
    public override void OnActionReceived(ActionBuffers actionBuffers) {
        MoveAgent(actionBuffers);
    }

    //입력값을 수동으로 주었을 때 실행됨(Heuristic only 사용시)
    public override void Heuristic(in ActionBuffers actionsOut) {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 1;
        if (Input.GetKey(KeyCode.D)) discreteActionsOut[1] = 1;
        if (Input.GetKey(KeyCode.A)) discreteActionsOut[1] = 2;
        if (Input.GetKey(KeyCode.LeftShift)) discreteActionsOut[2] = 1;
    }

    //입력값을 기반으로 agent를 움직이기 위한 코드
    public virtual void MoveAgent(ActionBuffers actionBuffers) {
        var discreteActions = actionBuffers.DiscreteActions;
        
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var forwardAxis = discreteActions[0];
        var rotateAxis = discreteActions[1];
        var Accel = discreteActions[2];

        var AnimalSpeed;
        if(Accel == 1) {
            AnimalSpeed = AnimalMoveSpeed * AnimalAccelFactor;
            SwitchRunning();
        }
        else {
            AnimalSpeed = AnimalMoveSpeed;
        }

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

    //어떤 지역에 진입했을때 호출됨
    public virtual void OnTriggerEnter(Collider other) {
        //자식 클래스에서 상세 구현
    }

    //어떤 물체와 충돌했을때 호출됨
    public virtual void OnCollisionEnter(Collision other) {
        //자식 클래스에서 상세 구현
    }
    
    //어떤 물체와 계속 접촉하고 있을때 호출됨
    public virtual void OnCollisionStay(Collision other) {
        if(other.gameObject.CompareTag("Wall"))
            AddReward(-0.01f);
    }

    //먹는 기능을 구현한 함수
    public virtual float Eat(float Calorie) {
        RecoverHP(Calorie * 0.1f);
        RecoverEnergy(Calorie);
        AddReward(Calorie * 0.1f);      
    }

    //공격하는 기능을 구현한 함수
    public virtual void Attack() {
        //자식 클래스에서 상세 구현
    }

    //공격받았을때 반격하는 기능을 구현한 함수
    public virtual float Resist() {
        LoseEnergy(AnimlaAttackEnergy);
        return AnimalAttackPower;
    }

    //자식을 낳는 기능을 구현한 함수
    public virtual void Childbirth() {
        //자식 클래스에서 상세 구현
    }

    //체력을 회복하는 함수
    public virtual void RecoverHP(float Recover) {
        AnimalHP += Recover;
        if(AnimalHP > AnimalHPLimit) AnimalHP = AnimalHPLimit;
    }

    //체력을 잃는 함수
    public virtual void LoseHP(float Damage) {
        AnimalHP -= Damage;
        if(AnimalHP < 0) Death();
    }

    public virtual void RecoverEnergy(float Rest) {
        AnimalEnergy += Rest;
        if(AnimalEnergy > AnimalEnergyLimit) AnimalEnergy = AnimalLimit;
    }

    public virtual void LoseEnergy(float Loss) {
        AnimalEnergy -= Loss;
        if(AnimalEnergy < 0) AnimalEnergy = 0;
    }

    //객체가 사망했을때 실행되는 함수
    public virtual void Death() {
        SwitchDead();
        AddReward(AnimalDeathPenalty);
        Destroy(gameObject, 3);
    }

    //객체가 굶주리고 있는지 확인하는 함수
    public void CheckStarvation() {
        if(AnimalEnergy < AnimalLeastEnergy) {
            LoseHP(AnimalStarvationDamage * Time.deltaTime);
            AddReward(AnimalStarvationPenalty * Time.deltaTime);
        }
    }

    //객체를 time 동안 멈추게 하는 함수
    public void Halt(float time) {
        float TempTime = AnimalTimer;
        while(AnimalTimer < TempTime + time) { }
    }

    //animation flag를 초기화하는 함수
    public void InitAniFlag() {
        AnimalAni.SetBool(AniFlagIdle, true);
        AnimalAni.SetBool(AniFlagAttacking, false);
        AnimalAni.SetBool(AniFlagDead, false);
        AnimalAni.SetBool(AniFlagRunning, false);
        AnimalAni.SetBool(AniFlagWalking, false);
    }

    //animation flag state를 run으로 변경
    public void SwitchRunning() {
        AnimalAni.SetBool(AniFlagIdle, false);
        AnimalAni.SetBool(AniFlagAttacking, false);
        AnimalAni.SetBool(AniFlagDead, false);
        AnimalAni.SetBool(AniFlagRunning, true);
        AnimalAni.SetBool(AniFlagWalking, false);
    }

    //animation flag state를 attack으로 변경
    public void SwitchAttacking() {
        AnimalAni.SetBool(AniFlagIdle, false);
        AnimalAni.SetBool(AniFlagAttacking, true);
        AnimalAni.SetBool(AniFlagDead, false);
        AnimalAni.SetBool(AniFlagRunning, false);
        AnimalAni.SetBool(AniFlagWalking, false);
    }

    //animation flag state를 walk으로 변경
    public void SwitchWalking() {
        AnimalAni.SetBool(AniFlagIdle, false);
        AnimalAni.SetBool(AniFlagAttacking, false);
        AnimalAni.SetBool(AniFlagDead, false);
        AnimalAni.SetBool(AniFlagRunning, false);
        AnimalAni.SetBool(AniFlagWalking, true);
    }

    //animation flag state를 idle로 변경
    public void SwitchIdle() {
        AnimalAni.SetBool(AniFlagIdle, true);
        AnimalAni.SetBool(AniFlagAttacking, false);
        AnimalAni.SetBool(AniFlagDead, false);
        AnimalAni.SetBool(AniFlagRunning, false);
        AnimalAni.SetBool(AniFlagWalking, false);
    }

    //animation flag state를 death로 변경
    public void SwitchDead() {
        AnimalAni.SetBool(AniFlagIdle, false);
        AnimalAni.SetBool(AniFlagAttacking, false);
        AnimalAni.SetBool(AniFlagDead, true);
        AnimalAni.SetBool(AniFlagRunning, false);
        AnimalAni.SetBool(AniFlagWalking, false);
    }
}
