using System;
using System.Collections;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMove : MonoBehaviour
{
    private NavMeshAgent _agent;
    private bool attackReady = true, playerDetected = false;
    private RaycastHit hit;
    public int dmg = 5;
    private Animator animator;
    [SerializeField] private GameObject _look;
    [SerializeField] private AnimationCurve curve;
    private bool is_jumping = false;
    private float initial_speed;

    
    void Start()
    {
        _agent = transform.GetComponent<NavMeshAgent>();
        initial_speed = _agent.speed;
        animator = transform.GetComponentInChildren<Animator>();
    }


    private void PrepareAttack()
    {
        attackReady = true;
    }

    IEnumerator CurveLong(NavMeshAgent agent, float duration)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        float normalizedTime = 0.0f;
        while (normalizedTime < 1.0f)
        {
            float yOffset = curve.Evaluate(normalizedTime);
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * 5f * Vector3.up;
            normalizedTime += Time.deltaTime / duration;

            yield return null;
        }
        animator.enabled = true;
        is_jumping = false;
        agent.speed = initial_speed;
    }

    private void Update()
    {

        if (_agent.isOnOffMeshLink)
        {
            animator.StopPlayback();
            if ((_agent.nextOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeManual || 
                _agent.nextOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeJumpAcross)
                && !is_jumping)
            {
                animator.enabled = false;
                is_jumping = true;
                

                OffMeshLinkData data = _agent.currentOffMeshLinkData;
                Vector3 startPos = _agent.transform.position;
                Vector3 endPos = data.endPos + Vector3.up * _agent.baseOffset;
                float distanse = Vector3.Distance(startPos, endPos);
                if (distanse > 5f)
                    _agent.speed = 25.0f;
                float duration = distanse / _agent.speed;

                StartCoroutine(CurveLong(_agent, duration));
            }

        }
         
        if (Physics.Raycast(new Ray(_look.transform.position, GameManager.player.transform.position - _look.transform.position), out hit) &&
            hit.transform.CompareTag("Player"))
        {
            playerDetected = true;
        }

        if (playerDetected)
        {
            if (Vector3.Distance(
                transform.position,
                GameManager.player.transform.position) <= 2)
            {
                transform.LookAt(
                    new Vector3(GameManager.player.transform.position.x,
                    transform.position.y,
                    GameManager.player.transform.position.z));
                animator.SetInteger("State", 2);
            }
            else
            {
                _agent.SetDestination(
                    GameManager.player.transform.position - 
                    (GameManager.player.transform.position - transform.position).normalized * 1.5f);
                animator.SetInteger("State", 1);
            }
        }
        
        if (Vector3.Distance(
            transform.position,
            GameManager.player.transform.position) <= 2)
        {
            if (attackReady)
            {
                GameManager.player.Damage(dmg);
                attackReady = false;
                Invoke("PrepareAttack", 2);
            }
        }
    }
}

    //private void Rotate()
    //{
    //    if (Vector3.Distance(transform.position, GameManager.player.transform.position) <= 2)
    //    {
    //        transform.LookAt(new Vector3(GameManager.player.transform.position.x, transform.position.y, GameManager.player.transform.position.z));
    //    }
    //}