using UnityEngine;
using System.Collections;

public class BallPush : MonoBehaviour
{
    private Rigidbody2D _rigidBody;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
    }

	void Start () {
	
	}

	void Update ()
    {
        const float force = 10;
        var h = Input.GetAxis("Horizontal");
        _rigidBody.AddForce(new Vector2(force * h, 0), ForceMode2D.Force);
	}
}
