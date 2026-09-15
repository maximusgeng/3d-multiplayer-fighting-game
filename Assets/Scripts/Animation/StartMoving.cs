using System.Collections;
using UnityEngine;

public class StartMoving : StateMachineBehaviour
{
    private GameObject player;
    private PlayerMovement playerMovement;
    private Player playerScript;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null)
        {
            player = animator.gameObject;
            playerScript = player.GetComponent<Player>();
        }

        if (playerScript != null)
            playerScript.DisableShield();

        if (player.name == "local player")
        {
            if (playerMovement == null)
                playerMovement = player.GetComponent<PlayerMovement>();

            if (playerMovement != null)
            {
                playerMovement.canSelfRotate = true;
                playerMovement.ResetSelfRotation();
            }

            Attack.CantDamage();
        }
        else
        {
            if (playerMovement == null)
            {
                GameObject localPlayer = GameObject.Find("local player");
                if (localPlayer != null)
                    playerMovement = localPlayer.GetComponent<PlayerMovement>();
            }

            if (playerMovement != null)
            {
                playerMovement.canOponentRotate = true;
                playerMovement.ResetOponentRotation();
            }
        }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}