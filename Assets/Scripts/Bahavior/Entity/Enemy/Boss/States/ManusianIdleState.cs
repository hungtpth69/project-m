using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManusianIdleState : IBossState
{
    float idleTime = 0.5f;
    //Stay in this state until boss fight is activated -> IntroState
    public IBossState DoState(BossBehavior boss)
    {
        if(boss.FightIsActivated() == true && boss.fightIsInProgress == false)
        {
            boss.OnPlayerDetected.Invoke();
            return boss.introState;
        }
        else if(boss.fightIsInProgress == true)
        {
            idleTime -= Time.deltaTime;
            if(idleTime > 0)
            {
                //Debug.Log("2nd idle state");
                /*boss.sideMove.moveInput.UpdateInput();
                boss.sideMove.DoFlip();*/
                boss.flip.DoFlipByTargetPosition(boss.playerTarget);
                return boss.idleState;
            }
            else
            {
                idleTime = 0.5f;
                return boss.chasePlayerState;
            }
        }
        else
        {
            //Debug.Log("First idle state");
            return boss.idleState;
        }
    }
}
