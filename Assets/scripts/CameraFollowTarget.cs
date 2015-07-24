using UnityEngine;
using System.Collections;

public class CameraFollowTarget : MonoBehaviour
{
    [SerializeField]
    private Transform _target;

	void LateUpdate ()
    {
        transform.position = Vector3.MoveTowards(transform.position, idealPosition, 1.0f);
	}

    private Vector3 idealPosition
    {
        get { return new Vector3(_target.position.x, _target.position.y, -10); }
    }
}
