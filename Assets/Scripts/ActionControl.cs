using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class ActionControl : NetworkBehaviour
{
    Player player;
    private Animator animator;
    private NetworkAnimator networkAnimator;

    void Start()
    {
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
    }

    public void Damage(string id, int damage, string type)
    {
        if (isServer)
            RpcDamage(id, damage, type);
        else
            CmdDamage(id, damage, type);
    }

    public void Block(string id, string type)
    {
        if (isServer)
            RpcBlock(id, type);
        else
            CmdBlock(id, type);
    }

    public void ResetALlPlayers()
    {
        List<string> allPlayerId = Manager.GetAllPlayer();

        if (isServer)
        {
            foreach (string id in allPlayerId)
                RpcResetPlayer(id);
        }
        else
        {
            foreach (string id in allPlayerId)
                CmdResetPlayer(id);
        }
    }

    public void SetOwnRatio(string id, string ratio)
    {
        if (isServer)
            RpcSetOwnRatio(id, ratio);
        else
            CmdSetOwnRatio(id, ratio);
    }

    [Command]
    public void CmdPlayBlockAnimation()
    {
        RpcPlayBlockAnimation();
    }

    [ClientRpc]
    public void RpcPlayBlockAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Blocked");
    }

    [Command]
    public void CmdUnsheath()
    {
        RpcUnsheath();
    }

    [ClientRpc]
    public void RpcUnsheath()
    {
        if (animator != null)
            animator.SetBool("Armed", true);
    }

    [Command]
    public void CmdSheath()
    {
        RpcSheath();
    }

    [ClientRpc]
    public void RpcSheath()
    {
        if (animator != null)
            animator.SetBool("Armed", false);
    }

    [Command]
    public void CmdDamage(string id, int damage, string type)
    {
        RpcDamage(id, damage, type);
    }

    [ClientRpc]
    public void RpcDamage(string id, int damage, string type)
    {
        player = Manager.GetPlayer(id);
        if (player != null)
        {
            player.TakeDamage(damage, type);
        }
    }

    [Command]
    public void CmdBlock(string id, string type)
    {
        RpcBlock(id, type);
    }

    [ClientRpc]
    public void RpcBlock(string id, string type)
    {
        player = Manager.GetPlayer(id);
        if (player != null)
        {
            player.BlockAttack(type);
            CmdPlayBlockAnimation();
        }
    }

    [Command]
    public void CmdSetOwnRatio(string id, string ratio)
    {
        RpcSetOwnRatio(id, ratio);
    }

    [ClientRpc]
    public void RpcSetOwnRatio(string id, string ratio)
    {
        player = Manager.GetPlayer(id);
        if (player != null)
            player.SetMyRatio(ratio);
    }

    [Command]
    public void CmdResetPlayer(string id)
    {
        RpcResetPlayer(id);
    }

    [ClientRpc]
    public void RpcResetPlayer(string id)
    {
        player = Manager.GetPlayer(id);
        if (player != null)
            player.ResetAll();
    }
}