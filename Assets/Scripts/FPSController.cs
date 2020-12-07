using System;
using UnityEngine;

public class FPSController : MonoBehaviour
{
    [Range(-1f, 1f)] public float horizontal;
    [Range(-1f, 1f)] public float vertical;

    public float adjust = 1f;

    private Vector3 leftStick = default;
    private Rigidbody m_rigidbody = default;
    
    void Start()
    {
        Application.targetFrameRate = 60;
        m_rigidbody = GetComponent<Rigidbody>();
    }


    void Update()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        Vector3 vert = Vector3.forward * vertical;
        Vector3 hori = Vector3.right * horizontal;
        
        Vector3 leftInput = (vert + hori);
        leftStick = leftInput.normalized * leftInput.magnitude / Mathf.Pow(vert.magnitude + hori.magnitude, 0.5f);
    }

    private void FixedUpdate()
    {
        if (leftStick.sqrMagnitude < 100)
        {
            var newVelocity = m_rigidbody.velocity + leftStick * Time.fixedDeltaTime * adjust;
            m_rigidbody.velocity = Vector3.Min(newVelocity, new Vector3(1, 1, 0));
            
        }
    }


    private void OnDrawGizmos()
    {
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.localPosition + leftStick, 0.1f);
        }
    }
}