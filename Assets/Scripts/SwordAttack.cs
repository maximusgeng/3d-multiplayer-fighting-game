using UnityEngine;
using Mirror;

public class SwordAttack : Attack
{
    public ActionControl actionControl;
    public int damage = 7;
    public Animator animator;
    public NetworkAnimator networkAnimator;

    public override void DealDamage(string id, bool isBlocked)
    {
        if (!isBlocked)
        {
            actionControl.Damage(id, damage, "Sword");
        }
        else
        {
            animator.SetTrigger("Deflected");
            networkAnimator.SetTrigger("Deflected");
            actionControl.Block(id, "Sword");
        }
    }
}