using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BaseAnimalAgent : Agent {
    protected ZooheimEnvController EnvController;

    protected Rigidbody AnimalRigidBody;

    protected int AnimalId;

    protected float AnimalHP;
    protected float AnimalHPLimit;

    protected float AnimalMoveSpeed = 0.5f;
    protected float AnimalTurnSpeed = 100f;
    protected float AnimalSpeedLimit = 10f;
    protected float AnimalAccelFactor = 1.5f;

    protected float AnimalEnergy;
    protected float AnimalEnergyLimit;

    protected float AnimalDamage;
    
    protected float AnimalEnoughHP;
    protected float AnimalEnoughEnergy;

    protected float AnimalLeastHP;
    protected float AnimalLeastEnergy;

    protected float AnimalStarvationDamage;

    protected float AnimalChildbirthPeriod;

    public bool AniFlagIdle;
    public bool AniFlagWalking;
    public bool AniFlagAttacking;
    public bool AniFlagRunning;
    public bool AniFlagDead;
    

    protected float AnimalTimer;

    public void Awake() {
        EnvController = FindObjectOfType<ZooheimEnvController>();
        AnimalRigidBody = GetComponent<Rigidbody>();
        InitAniFlag();
    }
    
    public void FixedUpdate() {
        AnimalTimer += Time.deltaTime;
    }

    public override void OnEpisodeBegin() {
    }

    public override void OnActionReceived(ActionBuffers actionBuffers) {
        MoveAgent(actionBuffers);
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 1;
        if (Input.GetKey(KeyCode.D)) discreteActionsOut[1] = 1;
        if (Input.GetKey(KeyCode.A)) discreteActionsOut[1] = 2;
        if (Input.GetKey(KeyCode.LeftShift)) discreteActionsOut[2] = 1;
    }

    public virtual void MoveAgent(ActionBuffers actionBuffers) {
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

    public virtual void OnTriggerEnter(Collider other) {
    }

    public virtual void OnCollisionEnter(Collision other) {
    }
    
    public virtual void OnCollisionStay(Collision other) {
        if(other.gameObject.CompareTag("Wall"))
            AddReward(-0.01f);
    }

    public virtual float Eat() {
    }

    public virtual void Attack() {

    }

    public virtual void Resist() {
        
    }

    public virtual void Childbirth() {

    }

    public virtual void RecoverHP(float Recover) {
        AnimalHP += Recover;
        if(AnimalHP > AnimalHPLimit) AnimalHP = AnimalHPLimit;
    }

    public virtual void LoseHP(float Damage) {
        AnimalHP -= Damage;
        if(AnimalHP < 0) Death();
    }

    public virtual void Death() {
        SwitchDead();
        Destroy(gameObject, 3);
    }

    public void CheckStarvation() {
        if(AnimalEnergy < AnimalLeastEnergy) {
            LoseHP(AnimalStarvationDamage * Time.deltaTime);
        }
    }

    public void Halt(float time) {
        float TempTime = AnimalTimer;
        while(AnimalTimer < TempTime + time) { }
    }

    public void InitAniFlag() {
        AniFlagIdle = true;
        AniFlagAttacking = false;
        AniFlagDead = false;
        AniFlagRunning = false;
        AniFlagWalking = false;
    }

    public void SwitchRunning() {
        AniFlagIdle = false;
        AniFlagAttacking = false;
        AniFlagDead = false;
        AniFlagRunning = true;
        AniFlagWalking = false;
    }

    public void SwitchAttacking() {
        AniFlagIdle = false;
        AniFlagAttacking = true;
        AniFlagDead = false;
        AniFlagRunning = false;
        AniFlagWalking = false;
    }

    public void SwitchWalking() {
        AniFlagIdle = false;
        AniFlagAttacking = false;
        AniFlagDead = false;
        AniFlagRunning = false;
        AniFlagWalking = true;
    }

    public void SwitchIdle() {
        AniFlagIdle = true;
        AniFlagAttacking = false;
        AniFlagDead = false;
        AniFlagRunning = false;
        AniFlagWalking = false;
    }

    public void SwitchDead() {
        AniFlagIdle = false;
        AniFlagAttacking = false;
        AniFlagDead = true;
        AniFlagRunning = false;
        AniFlagWalking = false;
    }
}
