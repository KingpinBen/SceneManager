using UnityEngine;
using System.Collections;

public class ObjectFollowTarget : MonoBehaviour
{
    [SerializeField]
    private Transform _target;
    private Rigidbody2D _rigidBody;

    private readonly float _stoppingDistance = 1.5f;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _target = GameObject.FindGameObjectWithTag("Player").transform;
    }

	void FixedUpdate ()
    {
        var dir = (_target.position - transform.position);

        if (dir.magnitude > _stoppingDistance)
            _rigidBody.AddForce(dir.normalized * 30 * Time.deltaTime, ForceMode2D.Force);
	}
}
